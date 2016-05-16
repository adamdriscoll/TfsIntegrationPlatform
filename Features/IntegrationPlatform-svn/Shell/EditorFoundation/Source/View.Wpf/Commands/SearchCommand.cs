// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Windows;
using System.Windows.Input;
using Microsoft.TeamFoundation.Migration.Shell.Controller;
using Microsoft.TeamFoundation.Migration.Shell.Model;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Provides a command that initiates a search.
    /// </summary>
    /// <typeparam name="TController">The type of the controller.</typeparam>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    public class SearchCommand<TController, TModel> : ViewModelCommand<TController, TModel>
        where TController : ControllerBase<TModel>, new ()
        where TModel : ModelRoot, new ()
    {
        #region Fields
        private readonly Window owner;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SaveCommand&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        public SearchCommand (ViewModel<TController, TModel> viewModel)
            : this (viewModel, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveCommand&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="owner">The owner window.</param>
        public SearchCommand (ViewModel<TController, TModel> viewModel, Window owner)
            : base (viewModel)
        {
            this.owner = owner;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the name of the view model property that determines whether this command can be executed.
        /// </summary>
        /// <value></value>
        protected override string ViewModelPropertyName
        {
            get
            {
                return "CanSearch";
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Defines the method that determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        /// <returns>
        /// true if this command can be executed; otherwise, false.
        /// </returns>
        public override bool CanExecute (object parameter)
        {
            return CanExecute (this.viewModel, parameter);
        }

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        public override void Execute (object parameter)
        {
            Execute (this.viewModel, this.owner, parameter);
        }

        /// <summary>
        /// Determines whether a search operation can be initiated for the specified <see cref="ViewModel&lt;TController, TModel&gt;"/>.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="searchString">The search string.</param>
        /// <returns>
        /// 	<c>true</c> if a search operation can be initiated; otherwise, <c>false</c>.
        /// </returns>
        public static bool CanSearch (ViewModel<TController, TModel> viewModel, string searchString)
        {
            return viewModel.CanSearch && !string.IsNullOrEmpty (searchString);
        }

        /// <summary>
        /// Initiates a search operation for the specified <see cref="ViewModel&lt;TController, TModel&gt;"/>.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="searchString">The search string.</param>
        public static void Search (ViewModel<TController, TModel> viewModel, string searchString)
        {
            viewModel.Search (searchString);
        }
        #endregion

        #region Internal Methods
        internal static bool CanExecute (ViewModel<TController, TModel> viewModel, object parameter)
        {
            return SearchCommand<TController, TModel>.CanSearch (viewModel, parameter as string);
        }

        internal static void Execute (ViewModel<TController, TModel> viewModel, Window owner, object parameter)
        {
            SearchCommand<TController, TModel>.Search (viewModel, parameter as string);
        }
        #endregion
    }

    /// <summary>
    /// Provides a <see cref="CommandBinding"/> that delegates the <see cref="NavigationCommands.Search"/> routed command to the <see cref="SearchCommand&lt;TController, TModel&gt;"/>.
    /// </summary>
    /// <typeparam name="TController">The type of the controller.</typeparam>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    public class SearchCommandBinding<TController, TModel> : CommandBinding
        where TController : ControllerBase<TModel>, new ()
        where TModel : ModelRoot, new ()
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchCommandBinding&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        public SearchCommandBinding (ViewModel<TController, TModel> viewModel)
            : this (viewModel, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchCommandBinding&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="owner">The owner window.</param>
        public SearchCommandBinding (ViewModel<TController, TModel> viewModel, Window owner)
        {
            this.Command = NavigationCommands.Search;

            this.CanExecute += delegate (object sender, CanExecuteRoutedEventArgs e)
            {
                e.CanExecute = SearchCommand<TController, TModel>.CanExecute (viewModel, e.Parameter);
            };

            this.Executed += delegate (object sender, ExecutedRoutedEventArgs e)
            {
                SearchCommand<TController, TModel>.Execute (viewModel, owner, e.Parameter);
            };
        }
        #endregion
    }
}
