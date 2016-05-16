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
    /// Provides a command that exits the application.
    /// </summary>
    /// <typeparam name="TController">The type of the controller.</typeparam>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    public class ExitCommand<TController, TModel> : ViewModelCommand<TController, TModel>
        where TController : ControllerBase<TModel>, new ()
        where TModel : ModelRoot, new ()
    {
        #region Fields
        private readonly Window owner;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ExitCommand&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        public ExitCommand (ViewModel<TController, TModel> viewModel)
            : this (viewModel, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExitCommand&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="owner">The owner window.</param>
        public ExitCommand (ViewModel<TController, TModel> viewModel, Window owner)
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
        /// Determines whether the application can be exited.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <returns>
        /// 	<c>true</c> if the application can be exited; otherwise, <c>false</c>.
        /// </returns>
        public static bool CanExit (ViewModel<TController, TModel> viewModel)
        {
            return true;
        }

        /// <summary>
        /// Exits the application.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <returns>
        ///     <c>true</c> if the application is exited; otherwise, <c>false</c>.
        /// </returns>
        public static bool Exit (ViewModel<TController, TModel> viewModel)
        {
            if (!CloseCommand<TController, TModel>.CanClose (viewModel) || CloseCommand<TController, TModel>.Close (viewModel))
            {
                Application.Current.Shutdown ();
                return true;
            }

            return false;
        }
        #endregion

        #region Internal Methods
        internal static bool CanExecute (ViewModel<TController, TModel> viewModel, object parameter)
        {
            return ExitCommand<TController, TModel>.CanExit (viewModel);
        }

        internal static void Execute (ViewModel<TController, TModel> viewModel, Window owner, object parameter)
        {
            ExitCommand<TController, TModel>.Exit (viewModel);
        }
        #endregion
    }

    /// <summary>
    /// Provides a <see cref="CommandBinding"/> that delegates the <see cref="EditorCommands.Exit"/> routed command to the <see cref="ExitCommand&lt;TController, TModel&gt;"/>.
    /// </summary>
    /// <typeparam name="TController">The type of the controller.</typeparam>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    public class ExitCommandBinding<TController, TModel> : CommandBinding
        where TController : ControllerBase<TModel>, new ()
        where TModel : ModelRoot, new ()
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ExitCommandBinding&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        public ExitCommandBinding (ViewModel<TController, TModel> viewModel)
            : this (viewModel, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExitCommandBinding&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="owner">The owner window.</param>
        public ExitCommandBinding (ViewModel<TController, TModel> viewModel, Window owner)
        {
            this.Command = EditorCommands.Exit;

            this.CanExecute += delegate (object sender, CanExecuteRoutedEventArgs e)
            {
                e.CanExecute = ExitCommand<TController, TModel>.CanExecute (viewModel, e.Parameter);
            };

            this.Executed += delegate (object sender, ExecutedRoutedEventArgs e)
            {
                ExitCommand<TController, TModel>.Execute (viewModel, owner, e.Parameter);
            };
        }
        #endregion
    }
}
