// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Interaction logic for ImportExportPage.xaml
    /// </summary>
    public partial class ImportExportPage : System.Windows.Controls.UserControl
    {
        private ImportExportViewModel m_viewModel;

        public ImportExportPage()
        {
            InitializeComponent();
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is ImportExportViewModel)
            {
                m_viewModel = DataContext as ImportExportViewModel;
            }
        }

        private void chooseImportPathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                m_viewModel.ChooseImportPath();
            }
            catch (Exception exception)
            {
                Utilities.HandleException(exception);
            }
        }

        private void importButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                m_viewModel.Import();
            }
            catch (Exception exception)
            {
                Utilities.HandleException(exception);
            }
        }

        private void exportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                m_viewModel.Export();
            }
            catch (Exception exception)
            {
                Utilities.HandleException(exception);
            }
        }

        private void configListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                m_viewModel.Export();
            }
            catch (Exception exception)
            {
                Utilities.HandleException(exception);
            }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                m_viewModel.Close();
            }
            catch (Exception exception)
            {
                Utilities.HandleException(exception);
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                m_viewModel.ClearStatus();
            }
            catch (Exception exception)
            {
                Utilities.HandleException(exception);
            }
        }

        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UIHelper.AdjustLastColumnWidth(sender as System.Windows.Controls.ListView);
        }
    }

    public class ImportExportViewModel : INotifyPropertyChanged
    {
        private ShellViewModel m_viewModel;

        public ImportExportViewModel(ShellViewModel viewModel)
        {
            m_viewModel = viewModel;
            ActiveConfigurationsList = new ActiveConfigurationsViewModel();
        }

        public SessionGroupConfigViewModel SelectedConfiguration
        {
            get
            {
                return ActiveConfigurationsList.SelectedConfiguration;
            }
        }

        public ActiveConfigurationsViewModel ActiveConfigurationsList { get; private set; }

        public bool CanImportIncludeRules
        {
            get
            {
                if (string.IsNullOrEmpty(ImportPath))
                {
                    return false;
                }
                else
                {
                    return ImportPath.EndsWith(".zip");
                }
            }
        }

        private string m_importPath;
        public string ImportPath
        {
            get
            {
                return m_importPath;
            }
            private set
            {
                m_importPath = value;
                OnPropertyChanged("ImportPath");
                OnPropertyChanged("CanImportIncludeRules");
                OnPropertyChanged("ImportIncludeRules");
            }
        }

        private bool m_importIncludeRules = true;
        public bool ImportIncludeRules
        {
            get
            {
                return m_importIncludeRules && CanImportIncludeRules;
            }
            set
            {
                if (m_importIncludeRules != value)
                {
                    m_importIncludeRules = value;
                    OnPropertyChanged("ImportIncludeRules");
                }
            }
        }

        private string m_status;
        public string Status
        {
            get
            {
                return m_status;
            }
            private set
            {
                m_status = value;
                OnPropertyChanged("Status");
            }
        }

        public void Close()
        {
            m_viewModel.ClearModalViewModel();
        }

        public void ChooseImportPath()
        {
            string currentDirectory = Environment.CurrentDirectory; // this is line X.  line X and line Y are necessary for back-compat with windows XP.

            // NOTE: For now, use the WinForms OpenFileDialog since it supports the Vista style common open file dialog.
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Configuration file (*.zip)|*.zip";

            //if (openFileDialog.ShowDialog (owner) == true)
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                ImportPath = openFileDialog.FileName;
            }

            Environment.CurrentDirectory = currentDirectory; // this is line Y.  line X and line Y are necessary for back-compat with windows XP.

            ClearStatus();
        }

        private string ChooseExportPath()
        {
            string currentDirectory = Environment.CurrentDirectory; // this is line X.  line X and line Y are necessary for back-compat with windows XP.

            // Get the save path from the user
            // NOTE: For now, use the WinForms SaveFileDialog since it supports the Vista style common save file dialog.
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Configuration file (*.zip)|*.zip";
            if (SelectedConfiguration != null)
            {
                saveFileDialog.FileName = SelectedConfiguration.FriendlyName;
            }

            string exportPath;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                exportPath = saveFileDialog.FileName;
            }
            else
            {
                exportPath = string.Empty;
            }

            Environment.CurrentDirectory = currentDirectory; // this is line Y.  line X and line Y are necessary for back-compat with windows XP.
            return exportPath;
        }

        public void Import()
        {
            if (string.IsNullOrEmpty(ImportPath))
            {
                Status = "Please specify an import path.";
            }
            else
            {
                ConfigImporter importer = new ConfigImporter(ImportPath, !ImportIncludeRules);
                try
                {
                    Configuration config = importer.Import();
                    if (m_viewModel.OpenFromDB(config.SessionGroupUniqueId))
                    {
                        m_viewModel.ClearViewModels();
                        m_viewModel.SystemState = SystemState.ConfigurationSaved;
                        m_viewModel.ShowMigrationView = MigrationStatusViews.Progress;
                    }
                    Status = string.Format("The configuration has been successfully imported. The imported configuration name is '{0}' (session group unique id: {1}).", config.FriendlyName, config.SessionGroupUniqueId);
                }
                catch (ConfigNotExistInPackageException)
                {
                    Status = "There is no configuration file in the configuration package to import from.";
                }
                
                ActiveConfigurationsList.Refresh();
            }
        }

        public void Export()
        {
            if (SelectedConfiguration == null)
            {
                Status = "Please select a configuration to export.";
            }
            else
            {
                string exportPath = ChooseExportPath();
                if (!string.IsNullOrEmpty(exportPath))
                {
                    ConfigExporter exporter = new ConfigExporter(false, exportPath);
                    string output = exporter.Export(SelectedConfiguration.SessionGroupUniqueId);
                    Status = string.Format("The configuration has been exported to file '{0}'.", output);
                }
                else
                {
                    ClearStatus();
                }
            }
        }

        public void ClearStatus()
        {
            Status = string.Empty;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion
    }
}
