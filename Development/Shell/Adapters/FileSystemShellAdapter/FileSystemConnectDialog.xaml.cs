// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Shell.FileSystemShellAdapter
{
    /// <summary>
    /// Interaction logic for FileSystemConnectDialog.xaml
    /// </summary>
    public partial class FileSystemConnectDialog : Window
    {
        public FileSystemConnectDialog(MigrationSource source)
        {
            InitializeComponent();
            m_connectVM = new FileSystemConnectDialogViewModel(source);
            this.DataContext = m_connectVM;
        }

        private FileSystemConnectDialogViewModel m_connectVM;

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            m_connectVM.Save();
            DialogResult = true;
            Close();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.ShowNewFolderButton = false;
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                m_connectVM.RootFolder = dlg.SelectedPath;
            }
        }
    }

    public class FileSystemConnectDialogViewModel : INotifyPropertyChanged
    {
        public FileSystemConnectDialogViewModel(MigrationSource source)
        {
            m_source = source;
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

        private MigrationSource m_source;
        private string m_rootFolder;
        public string RootFolder
        {
            get
            {
                return m_rootFolder;
            }
            set
            {
                m_rootFolder = value;
                CanSave = !string.IsNullOrEmpty(m_rootFolder);
                OnPropertyChanged("RootFolder");
            }
        }

        private bool m_canSave;
        public bool CanSave
        {
            get
            {
                return m_canSave;
            }
            set
            {
                m_canSave = value;
                OnPropertyChanged("CanSave");
            }
        }
        internal void Save()
        {
            m_source.ServerUrl = "FileSystem";
            m_source.SourceIdentifier = RootFolder;
            m_source.FriendlyName = "File System";
            m_source.ServerIdentifier = "File System";
        }
    }
}
