// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Windows;
using System.Windows.Input;
using Microsoft.TeamFoundation.Migration.Shell.Controller;
using Microsoft.TeamFoundation.Migration.Shell.Model;
using Microsoft.TeamFoundation.Migration.Shell.ViewModel;
using System;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Provides a command that opens the current data model.
    /// </summary>
    /// <typeparam name="TController">The type of the controller.</typeparam>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    public class EditCurrentConfigurationCommand : ViewModelCommand<ShellController, ConfigurationModel>
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="EditCurrentConfigurationCommand&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        public EditCurrentConfigurationCommand (ShellViewModel viewModel)
            : base (viewModel)
        {
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
                return "CanEditCurrentConfigurationCommand";
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
            return CanExecute(this.viewModel as ShellViewModel, parameter);
        }

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        public override void Execute (object parameter)
        {
            Execute(this.viewModel as ShellViewModel);
        }

        /// <summary>
        /// Determines whether a data model can be opened for the specified <see cref="ViewModel&lt;TController, TModel&gt;"/>.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <returns>
        /// 	<c>true</c> if a data model can be opened; otherwise, <c>false</c>.
        /// </returns>
        public static bool CanOpen (ShellViewModel viewModel)
        {
            return !(viewModel.SystemState == SystemState.EditConfiguration ||
                    viewModel.SystemState == SystemState.NoConfiguration);
        }

        /// <summary>
        /// Opens a data model for the specified <see cref="ViewModel&lt;TController, TModel&gt;"/>.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        public static void Open (ShellViewModel viewModel)
        {
            bool allowOpen = true;
            if (viewModel.SystemState == SystemState.MigrationProgress && !viewModel.CanStart)
            {
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show(Properties.ShellResources.StopSessionEditConfiguration,
                    Properties.ShellResources.StopSessionTitle, MessageBoxButton.YesNo, MessageBoxImage.Stop);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    if (viewModel.CanStop)
                    {
                        viewModel.Stop();
                    }
                    else
                    {
                        allowOpen = false;
                        Utilities.ShowError(Properties.Resources.CannotStopError, Properties.Resources.CannotStopCaption);
                    }
                }
                else
                {
                    allowOpen = false;
                }
            }

            if (allowOpen)
            {

                if (viewModel.ConfigViewModel != null)
                {
                    viewModel.PushViewModel(viewModel.ConfigViewModel);
                }
                else
                {
                    Utilities.HandleException(new Exception("Could not open configuration file."), false, "Open Error", "Invalid XML");
                }
            }
        }
        #endregion

        #region Internal Methods
        internal static bool CanExecute (ShellViewModel viewModel, object parameter)
        {
            return EditCurrentConfigurationCommand.CanOpen (viewModel);
        }

        internal static void Execute (ShellViewModel viewModel)
        {
            EditCurrentConfigurationCommand.Open (viewModel);
        }
        #endregion
    }

    /// <summary>
    /// Provides a <see cref="CommandBinding"/> that delegates the <see cref="ApplicationCommands.Open"/> routed command to the <see cref="EditCurrentConfigurationCommand&lt;TController, TModel&gt;"/>.
    /// </summary>
    /// <typeparam name="TController">The type of the controller.</typeparam>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    public class EditCurrentConfigurationCommandBinding : CommandBinding
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="EditCurrentConfigurationCommandBinding&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        public EditCurrentConfigurationCommandBinding(ShellViewModel viewModel)
        {
            this.Command = ShellCommands.EditCurrentConfiguration;

            this.CanExecute += delegate (object sender, CanExecuteRoutedEventArgs e)
            {
                e.CanExecute = EditCurrentConfigurationCommand.CanExecute (viewModel, e.Parameter);
            };

            this.Executed += delegate (object sender, ExecutedRoutedEventArgs e)
            {
                EditCurrentConfigurationCommand.Execute (viewModel);
            };
        }
        #endregion
    }
}
