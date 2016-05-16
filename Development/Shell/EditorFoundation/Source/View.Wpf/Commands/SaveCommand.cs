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
    /// Provides a command that saves the current data model.
    /// </summary>
    /// <typeparam name="TController">The type of the controller.</typeparam>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    public class SaveCommand<TController, TModel> : ViewModelCommand<TController, TModel>
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
        public SaveCommand (ViewModel<TController, TModel> viewModel)
            : this (viewModel, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveCommand&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="owner">The owner window.</param>
        public SaveCommand (ViewModel<TController, TModel> viewModel, Window owner)
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
                return "CanSave";
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
        /// Determines whether the data model can be saved for the specified <see cref="ViewModel&lt;TController, TModel&gt;"/>.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <returns>
        /// 	<c>true</c> if the data model can be saved; otherwise, <c>false</c>.
        /// </returns>
        public static bool CanSave (ViewModel<TController, TModel> viewModel)
        {
            return viewModel.CanSave;
        }

        /// <summary>
        /// Saves the data model for the specified <see cref="ViewModel&lt;TController, TModel&gt;"/>.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="owner">The owner window.</param>
        /// <returns>
        /// 	<c>true</c> if the data model is saved; otherwise, <c>false</c>.
        /// </returns>
        public static bool Save (ViewModel<TController, TModel> viewModel, Window owner)
        {
            return SaveCommand<TController, TModel>.Save (viewModel, owner, null);
        }

        /// <summary>
        /// Saves the data model for the specified <see cref="ViewModel&lt;TController, TModel&gt;"/>.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="owner">The owner window.</param>
        /// <param name="savePath">The path to the file to which to save.</param>
        /// <returns>
        /// 	<c>true</c> if the data model is saved; otherwise, <c>false</c>.
        /// </returns>
        public static bool Save (ViewModel<TController, TModel> viewModel, Window owner, string savePath)
        {
            // Get the save path from the controller
            if (string.IsNullOrEmpty (savePath))
            {
                savePath = viewModel.Controller.SavePath;
            }

            // Save using the save path
            if (!string.IsNullOrEmpty (savePath))
            {
                return viewModel.Save (savePath);
            }
            
            // No save path specified - fall back to SaveAs
            return SaveAsCommand<TController, TModel>.SaveAs (viewModel, owner);
        }
        #endregion

        #region Internal Methods
        internal static bool CanExecute (ViewModel<TController, TModel> viewModel, object parameter)
        {
            return SaveCommand<TController, TModel>.CanSave (viewModel);
        }

        internal static void Execute (ViewModel<TController, TModel> viewModel, Window owner, object parameter)
        {
            SaveCommand<TController, TModel>.Save (viewModel, owner, parameter as string);
        }
        #endregion
    }

    /// <summary>
    /// Provides a <see cref="CommandBinding"/> that delegates the <see cref="ApplicationCommands.Save"/> routed command to the <see cref="SaveCommand&lt;TController, TModel&gt;"/>.
    /// </summary>
    /// <typeparam name="TController">The type of the controller.</typeparam>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    public class SaveCommandBinding<TController, TModel> : CommandBinding
        where TController : ControllerBase<TModel>, new ()
        where TModel : ModelRoot, new ()
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SaveCommandBinding&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        public SaveCommandBinding (ViewModel<TController, TModel> viewModel)
            : this (viewModel, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveCommandBinding&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="owner">The owner window.</param>
        public SaveCommandBinding (ViewModel<TController, TModel> viewModel, Window owner)
        {
            this.Command = ApplicationCommands.Save;

            this.CanExecute += delegate (object sender, CanExecuteRoutedEventArgs e)
            {
                e.CanExecute = SaveCommand<TController, TModel>.CanExecute (viewModel, e.Parameter);
            };

            this.Executed += delegate (object sender, ExecutedRoutedEventArgs e)
            {
                SaveCommand<TController, TModel>.Execute (viewModel, owner, e.Parameter);
            };
        }
        #endregion
    }
}
