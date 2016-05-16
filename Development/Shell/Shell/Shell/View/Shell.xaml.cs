// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.TeamFoundation.Migration.Shell.Controller;
using Microsoft.TeamFoundation.Migration.Shell.Model;
using Microsoft.TeamFoundation.Migration.Shell.Properties;
using Microsoft.TeamFoundation.Migration.Shell.View;

namespace Microsoft.TeamFoundation.Migration.Shell.Tfs
{
    /// <summary>
    /// Interaction logic for Shell.xaml
    /// </summary>
    public partial class Shell : Microsoft.TeamFoundation.Migration.Shell.View.Shell<ShellViewModel, ShellController, ConfigurationModel>
    {
        // TODO: Look closely at EditorFoundation for cleaner alignment with intended model for extension
        private CommandBinding importCommandBinding;
        private CommandBinding openRecentCommandBinding;
        private CommandBinding exportCommandBinding;
        private CommandBinding exportAsCommandBinding;
        private CommandBinding startCommandBinding;
        private CommandBinding pauseCommandBinding;
        private CommandBinding stopCommandBinding;
        private CommandBinding viewConfigurationCommandBinding;
        private CommandBinding viewSettingsCommandBinding;
        private CommandBinding refreshCommandBinding;
        private CommandBinding helpCommandBinding;
        private CommandBinding viewConflictsCommandBinding;
        private CommandBinding viewProgressCommandBinding;
        private CommandBinding viewCurrentConfigurationCommandBinding;
        private CommandBinding editCurrentConfigurationCommandBinding;
        private CommandBinding viewEventLogsCommandBinding;
        private CommandBinding viewLogsCommandBinding;
        private CommandBinding viewImportExportPageCommandBinding;
        private CommandBinding viewHomeCommandBinding;

        public Shell()
        {
            InitializeComponent();

            // Turn off views on runtime localization and skin collections in the UI
            this.IsRuntimeLocalizationEnabled = false;
            this.IsRuntimeSkinningEnabled = false;

            // Load the default skin
            Skin defaultSkin = new Skin("Default Skin", new DefaultSkin());
            SkinManager.AvailableSkins.Add(defaultSkin);
            // LoadSkins here if runtime skinning is enabled
            SkinManager.SetActiveSkinToLast(defaultSkin);

            this.OpenFromDBCommandBinding = new OpenFromDBCommandBinding(this.ViewModel, this);
            this.OpenRecentCommandBinding = new OpenRecentCommandBinding(this.ViewModel, this);
            this.SaveToDBCommandBinding = new SaveToDBCommandBinding(this.ViewModel, this);
            this.SaveAsToDBCommandBinding = new SaveAsToDBCommandBinding(this.ViewModel, this);
            this.StartCommandBinding = new StartCommandBinding(this.ViewModel);
            this.PauseCommandBinding = new PauseCommandBinding(this.ViewModel);
            this.StopCommandBinding = new StopCommandBinding(this.ViewModel);
            OpenCommandBinding = new ImportCommandBinding(this.ViewModel, this);
            SaveCommandBinding = new ExportCommandBinding(this.ViewModel, this);
            SaveAsCommandBinding = new ExportCommandBinding(this.ViewModel, this);
            this.ViewConfigurationCommandBinding = new ViewConfigurationCommandBinding(this.ViewModel, this);
            this.ViewSettingsCommandBinding = new ViewSettingsCommandBinding(this.ViewModel, this);
            this.RefreshCommandBinding = new RefreshCommandBinding(this.ViewModel);
            this.HelpCommandBinding = new HelpCommandBinding(this.ViewModel);
            this.ViewConflictsCommandBinding = new ViewConflictsCommandBinding(this.ViewModel);
            this.ViewProgressCommandBinding = new ViewProgressCommandBinding(this.ViewModel);
            this.ViewCurrentConfigurationCommandBinding = new ViewCurrentConfigurationCommandBinding(this.ViewModel);
            this.EditCurrentConfigurationCommandBinding = new EditCurrentConfigurationCommandBinding(this.ViewModel);
            this.ViewEventLogsCommandBinding = new ViewEventLogsCommandBinding();
            this.ViewLogsCommandBinding = new ViewLogsCommandBinding();
            this.ViewImportExportPageCommandBinding = new ViewImportExportPageCommandBinding(this.ViewModel);
            this.ViewHomeCommandBinding = new ViewHomeCommandBinding(this.ViewModel);

            NewCommandBinding = null;
        }

        protected override void OnClosing(object sender, ClosingEventArgs eventArgs)
        {
            try
            {
                if (!eventArgs.Cancel && this.ViewModel.IsDirty)
                {
                    MessageBoxResult messageBoxResult = MessageBox.Show(this, Properties.WpfViewResources.QuerySaveMessageString, Properties.WpfViewResources.QuerySaveCaptionString, MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);
                    if (messageBoxResult == MessageBoxResult.Cancel)
                    {
                        eventArgs.Cancel = true;
                    }
                    else if (messageBoxResult == MessageBoxResult.Yes)
                    {
                        SaveToDBCommand.Save(this.ViewModel, this);
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.HandleException(ex);
            }
        }
        
        protected override void OnOpened(object sender, OpenedEventArgs eventArgs)
        {
            try
            {
                this.IsEnabled = true;

                // State has changed that may affect routed commands
                CommandManager.InvalidateRequerySuggested();

                // Check for open errors
                if (eventArgs.Error != null)
                {
                    Utilities.HandleException(eventArgs.Error, false, Properties.WpfViewResources.OpenErrorCaptionString, Properties.WpfViewResources.OpenErrorMessageString + ": ", eventArgs.FilePath);
                }
            }
            catch (Exception ex)
            {
                Utilities.HandleException(ex);
            }
        }        

        #region Properties
        /// <summary>
        /// Gets or sets the binding for the ViewEventLogsCommand command.
        /// </summary>
        protected CommandBinding ViewEventLogsCommandBinding
        {
            get
            {
                return this.viewEventLogsCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding(ref this.viewEventLogsCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the ViewHomeCommand command.
        /// </summary>
        protected CommandBinding ViewHomeCommandBinding
        {
            get
            {
                return this.viewHomeCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding(ref this.viewHomeCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the ViewLogsCommand command.
        /// </summary>
        protected CommandBinding ViewLogsCommandBinding
        {
            get
            {
                return this.viewLogsCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding(ref this.viewLogsCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the HelpCommand command.
        /// </summary>
        protected CommandBinding HelpCommandBinding
        {
            get
            {
                return this.helpCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding(ref this.helpCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the ViewConflicts command.
        /// </summary>
        protected CommandBinding ViewConflictsCommandBinding
        {
            get
            {
                return this.viewConflictsCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding(ref this.viewConflictsCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the ViewProgress command.
        /// </summary>
        protected CommandBinding ViewProgressCommandBinding
        {
            get
            {
                return this.viewProgressCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding(ref this.viewProgressCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the ViewProgress command.
        /// </summary>
        protected CommandBinding ViewCurrentConfigurationCommandBinding
        {
            get
            {
                return this.viewCurrentConfigurationCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding(ref this.viewCurrentConfigurationCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the ViewConfiguration command.
        /// </summary>
        protected CommandBinding EditCurrentConfigurationCommandBinding
        {
            get
            {
                return this.editCurrentConfigurationCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding(ref this.editCurrentConfigurationCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the ViewConfiguration command.
        /// </summary>
        protected CommandBinding RefreshCommandBinding
        {
            get
            {
                return this.refreshCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding(ref this.refreshCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the ViewConfiguration command.
        /// </summary>
        protected CommandBinding ViewSettingsCommandBinding
        {
            get
            {
                return this.viewSettingsCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding(ref this.viewSettingsCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the ViewConfiguration command.
        /// </summary>
        protected CommandBinding ViewConfigurationCommandBinding
        {
            get
            {
                return this.viewConfigurationCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding(ref this.viewConfigurationCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the ViewConfiguration command.
        /// </summary>
        protected CommandBinding ViewImportExportPageCommandBinding
        {
            get
            {
                return this.viewImportExportPageCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding(ref this.viewImportExportPageCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the Import command.
        /// </summary>
        protected CommandBinding OpenFromDBCommandBinding
        {
            get
            {
                return this.importCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding(ref this.importCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the Import command.
        /// </summary>
        protected CommandBinding OpenRecentCommandBinding
        {
            get
            {
                return this.openRecentCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding(ref this.openRecentCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the Export command.
        /// </summary>
        protected CommandBinding SaveToDBCommandBinding
        {
            get
            {
                return this.exportCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding(ref this.exportCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the Export command.
        /// </summary>
        protected CommandBinding SaveAsToDBCommandBinding
        {
            get
            {
                return this.exportAsCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding(ref this.exportAsCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the Start command.
        /// </summary>
        protected CommandBinding StartCommandBinding
        {
            get
            {
                return this.startCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding(ref this.startCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the Pause command.
        /// </summary>
        protected CommandBinding PauseCommandBinding
        {
            get
            {
                return this.pauseCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding(ref this.pauseCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the Start command.
        /// </summary>
        protected CommandBinding StopCommandBinding
        {
            get
            {
                return this.stopCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding(ref this.stopCommandBinding, value);
            }
        }
        #endregion
    }
}
