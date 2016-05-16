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
    /// Provides a command that opens the current data model.
    /// </summary>
    /// <typeparam name="TController">The type of the controller.</typeparam>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    public class SaveAsToDBCommand : ViewModelCommand<ShellController, ConfigurationModel>
    {
        #region Fields
        private readonly Window m_owner;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SaveAsToDBCommandBinding&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        public SaveAsToDBCommand (ShellViewModel viewModel)
            : this (viewModel, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveAsToDBCommandBinding&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="owner">The owner window.</param>
        public SaveAsToDBCommand(ShellViewModel viewModel, Window owner)
            : base (viewModel)
        {
            this.m_owner = owner;
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
                return "CanSaveAsToDB";
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
            return CanExecute(this.viewModel as ShellViewModel);
        }

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        public override void Execute (object parameter)
        {
            Execute(this.viewModel as ShellViewModel, this.m_owner);
        }

        /// <summary>
        /// Determines whether a data model can be opened for the specified <see cref="ViewModel&lt;TController, TModel&gt;"/>.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <returns>
        /// 	<c>true</c> if a data model can be opened; otherwise, <c>false</c>.
        /// </returns>
        public static bool CanSave (ShellViewModel viewModel)
        {
            return viewModel.CanSaveToDB;
        }

        /// <summary>
        /// Store configuration associated with specified <see cref="ViewModel&lt;TController, TModel&gt;"/> in migration tools DB.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="owner">The owner window.</param>
        /// <param name="openPath">The path from which to open.</param>
        public static bool Save (ShellViewModel viewModel, Window owner)
        {
            if (viewModel.DataModel != null)
            {
                // Present the SessionGroup name and other bits of metadata for editing
                // before the configuration is persisted to DB.
                SaveToDBDialog dlg = new SaveToDBDialog(viewModel);
                dlg.Owner = owner;
                dlg.ShowDialog();

                if (dlg.DialogResult == true)
                {
                    return viewModel.SaveToDB(viewModel.DataModel.Configuration.SessionGroupUniqueId, true);
                }
            }
            return false;
        }
        #endregion

        #region Internal Methods
        internal static bool CanExecute (ShellViewModel viewModel)
        {
            return SaveAsToDBCommand.CanSave(viewModel);
        }

        internal static void Execute (ShellViewModel viewModel, Window owner)
        {
            SaveAsToDBCommand.Save(viewModel, owner);
        }
        #endregion
    }

    /// <summary>
    /// Provides a <see cref="CommandBinding"/> that delegates the <see cref="ApplicationCommands.Open"/> routed command to the <see cref="SaveAsToDBCommandBinding&lt;TController, TModel&gt;"/>.
    /// </summary>
    /// <typeparam name="TController">The type of the controller.</typeparam>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    public class SaveAsToDBCommandBinding : CommandBinding
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SaveAsToDBCommandBinding&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        public SaveAsToDBCommandBinding (ShellViewModel viewModel)
            : this (viewModel, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveToDBCommandBinding&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="owner">The owner window.</param>
        public SaveAsToDBCommandBinding(ShellViewModel viewModel, Window owner)
        {
            this.Command = ShellCommands.SaveAsToDB;

            this.CanExecute += delegate (object sender, CanExecuteRoutedEventArgs e)
            {
                e.CanExecute = SaveAsToDBCommand.CanExecute (viewModel);
            };

            this.Executed += delegate (object sender, ExecutedRoutedEventArgs e)
            {
                SaveAsToDBCommand.Execute(viewModel, owner);
            };
        }
        #endregion
    }
}