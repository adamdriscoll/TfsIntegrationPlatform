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
    /// Provides a command that displays information about the application.
    /// </summary>
    /// <typeparam name="TController">The type of the controller.</typeparam>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    public class AboutCommand<TController, TModel> : ViewModelCommand<TController, TModel>
        where TController : ControllerBase<TModel>, new ()
        where TModel : ModelRoot, new ()
    {
        #region Fields
        private readonly Window owner;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AboutCommand&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        public AboutCommand (ViewModel<TController, TModel> viewModel)
            : this (viewModel, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AboutCommand&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="owner">The owner window.</param>
        public AboutCommand (ViewModel<TController, TModel> viewModel, Window owner)
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
                return null;
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
        /// Determines whether a data model can be aboutd for the specified <see cref="ViewModel&lt;TController, TModel&gt;"/>.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <returns>
        /// 	<c>true</c> if a data model can be aboutd; otherwise, <c>false</c>.
        /// </returns>
        public static bool CanDisplayAbout (ViewModel<TController, TModel> viewModel)
        {
            return true;
        }

        /// <summary>
        /// Abouts a data model for the specified <see cref="ViewModel&lt;TController, TModel&gt;"/>.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="owner">The owner window.</param>
        public static void DisplayAbout (ViewModel<TController, TModel> viewModel, Window owner)
        {
            About<TController, TModel> about = new About<TController, TModel> (viewModel);
            about.Owner = owner;
            about.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            about.ShowDialog ();
        }
        #endregion

        #region Internal Methods
        internal static bool CanExecute (ViewModel<TController, TModel> viewModel, object parameter)
        {
            return AboutCommand<TController, TModel>.CanDisplayAbout (viewModel);
        }

        internal static void Execute (ViewModel<TController, TModel> viewModel, Window owner, object parameter)
        {
            AboutCommand<TController, TModel>.DisplayAbout (viewModel, owner);
        }
        #endregion
    }

    /// <summary>
    /// Provides a <see cref="CommandBinding"/> that delegates the <see cref="EditorCommands.About"/> routed command to the <see cref="AboutCommand&lt;TController, TModel&gt;"/>.
    /// </summary>
    /// <typeparam name="TController">The type of the controller.</typeparam>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    public class AboutCommandBinding<TController, TModel> : CommandBinding
        where TController : ControllerBase<TModel>, new ()
        where TModel : ModelRoot, new ()
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AboutCommandBinding&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        public AboutCommandBinding (ViewModel<TController, TModel> viewModel)
            : this (viewModel, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AboutCommandBinding&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="owner">The owner window.</param>
        public AboutCommandBinding (ViewModel<TController, TModel> viewModel, Window owner)
        {
            this.Command = EditorCommands.About;

            this.CanExecute += delegate (object sender, CanExecuteRoutedEventArgs e)
            {
                e.CanExecute = AboutCommand<TController, TModel>.CanExecute (viewModel, e.Parameter);
            };

            this.Executed += delegate (object sender, ExecutedRoutedEventArgs e)
            {
                AboutCommand<TController, TModel>.Execute (viewModel, owner, e.Parameter);
            };
        }
        #endregion
    }
}
