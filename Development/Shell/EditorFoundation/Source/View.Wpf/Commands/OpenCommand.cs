// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
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
    public class ImportCommand<TController, TModel> : ViewModelCommand<TController, TModel>
        where TController : ControllerBase<TModel>, new ()
        where TModel : ModelRoot, new ()
    {
        #region Fields
        private readonly Window owner;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="OpenCommand&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        public ImportCommand (ViewModel<TController, TModel> viewModel)
            : this (viewModel, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenCommand&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="owner">The owner window.</param>
        public ImportCommand (ViewModel<TController, TModel> viewModel, Window owner)
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
                return "CanOpen";
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
        /// Determines whether a data model can be opened for the specified <see cref="ViewModel&lt;TController, TModel&gt;"/>.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <returns>
        /// 	<c>true</c> if a data model can be opened; otherwise, <c>false</c>.
        /// </returns>
        public static bool CanOpen (ViewModel<TController, TModel> viewModel)
        {
            return viewModel.CanOpen;
        }

        /// <summary>
        /// Opens a data model for the specified <see cref="ViewModel&lt;TController, TModel&gt;"/>.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="owner">The owner window.</param>
        /// <param name="openPath">The path from which to open.</param>
        public static void Open (ViewModel<TController, TModel> viewModel, Window owner, string openPath)
        {
            bool allowOpen = true;
            ShellViewModel shellVM = viewModel as ShellViewModel;
            if(shellVM != null && shellVM.SystemState == SystemState.MigrationProgress && !shellVM.CanStart)
            {
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show(Properties.ShellResources.StopSessionNewMigration,
                    Properties.ShellResources.StopSessionTitle, MessageBoxButton.YesNo, MessageBoxImage.Stop);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    if(shellVM.CanStop)
                    {
                        shellVM.Stop();
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
                // Get the open path from the user
                if (string.IsNullOrEmpty(openPath))
                {
                    string currentDirectory = Environment.CurrentDirectory; // this is line X.  line X and line Y are necessary for back-compat with windows XP.

                    // NOTE: For now, use the WinForms OpenFileDialog since it supports the Vista style common open file dialog.
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    string assemblyParentFolder = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
                    openFileDialog.InitialDirectory = Path.Combine(assemblyParentFolder, "Configurations");
                    openFileDialog.Filter = "Configuration file (*.xml)|*.xml";
                    openFileDialog.Title = "Choose a template";

                    //if (openFileDialog.ShowDialog (owner) == true)
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        openPath = openFileDialog.FileName;
                    }

                    Environment.CurrentDirectory = currentDirectory; // this is line Y.  line X and line Y are necessary for back-compat with windows XP.
                }

                // Open the file
                if (!string.IsNullOrEmpty(openPath))
                {
                    viewModel.Open(openPath);
                    if (shellVM != null)
                    {
                        shellVM.IsCompleted = false;
                        shellVM.ClearViewModels();
                        shellVM.PushViewModel(new HomeViewModel(shellVM));
                    }
                }
            }
        }
        #endregion

        #region Internal Methods
        internal static bool CanExecute (ViewModel<TController, TModel> viewModel, object parameter)
        {
            return ImportCommand<TController, TModel>.CanOpen (viewModel);
        }

        internal static void Execute (ViewModel<TController, TModel> viewModel, Window owner, object parameter)
        {
            ImportCommand<TController, TModel>.Open (viewModel, owner, parameter as string);
        }
        #endregion
    }

    /// <summary>
    /// Provides a <see cref="CommandBinding"/> that delegates the <see cref="ApplicationCommands.Open"/> routed command to the <see cref="OpenCommand&lt;TController, TModel&gt;"/>.
    /// </summary>
    /// <typeparam name="TController">The type of the controller.</typeparam>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    public class OpenCommandBinding<TController, TModel> : CommandBinding
        where TController : ControllerBase<TModel>, new ()
        where TModel : ModelRoot, new ()
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="OpenCommandBinding&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        public OpenCommandBinding (ViewModel<TController, TModel> viewModel)
            : this (viewModel, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenCommandBinding&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="owner">The owner window.</param>
        public OpenCommandBinding (ViewModel<TController, TModel> viewModel, Window owner)
        {
            this.Command = ApplicationCommands.Open;

            this.CanExecute += delegate (object sender, CanExecuteRoutedEventArgs e)
            {
                e.CanExecute = ImportCommand<TController, TModel>.CanExecute (viewModel, e.Parameter);
            };

            this.Executed += delegate (object sender, ExecutedRoutedEventArgs e)
            {
                ImportCommand<TController, TModel>.Execute (viewModel, owner, e.Parameter);
            };
        }
        #endregion
    }
}
