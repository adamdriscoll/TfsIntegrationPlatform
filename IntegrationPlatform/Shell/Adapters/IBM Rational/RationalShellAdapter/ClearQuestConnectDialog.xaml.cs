// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ClearQuestOleServer;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop;
using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.RationalShellAdapter
{
    /// <summary>
    /// Interaction logic for GenericMigrationSourceDialog.xaml
    /// </summary>
    public partial class ClearQuestConnectDialog : Window
    {
        private ClearQuestMigrationSourceViewModel m_source;

        public ClearQuestConnectDialog(MigrationSource source)
        {
            InitializeComponent();
            m_source = new ClearQuestMigrationSourceViewModel(source);
            m_source.PropertyChanged += new PropertyChangedEventHandler(m_source_PropertyChanged);
            DataContext = m_source;
            Closed += new EventHandler(ClearQuestConnectDialog_Closed);

            passwordBox.Password = source.CustomSettings.CustomSetting.Where(x => x.SettingKey.Equals("Password")).Select(x=>x.SettingValue).FirstOrDefault();
        }

        void ClearQuestConnectDialog_Closed(object sender, EventArgs e)
        {
            m_source.Dispose();
        }

        void m_source_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, "IsValid") && m_source.IsValid == true)
            {
                m_source.Save();

                DialogResult = true;
                Close();
            }
        }

        private void validateButton_Click(object sender, RoutedEventArgs e)
        {
            m_source.Validate(passwordBox.Password);
        }

        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UIHelper.AdjustLastColumnWidth(sender as ListView);
        }

        private void userNameTextBox_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox)
            {
                TextBox textBox = sender as TextBox;
                textBox.SelectAll();
            }
        }

        private void passwordBox_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            if (sender is PasswordBox)
            {
                PasswordBox textBox = sender as PasswordBox;
                textBox.SelectAll();
            }
        }
    }

    public class ClearQuestDatabaseViewModel
    {
        public ClearQuestDatabaseViewModel(IOAdDatabaseDesc databaseDesc)
        {
            DatabaseSetName = databaseDesc.GetDatabaseSetName();
            DatabaseName = databaseDesc.GetDatabaseName();
            Description = databaseDesc.GetDescription();
        }
        public string DatabaseSetName { get; private set; }
        public string DatabaseName { get; private set; }
        public string Description { get; private set; }
    }

    public class ClearQuestMigrationSourceViewModel : INotifyPropertyChanged, IDisposable
    {
        private enum WorkerTask
        {
            GetDatabases,
            Validate
        }

        private BackgroundWorker m_worker;
        private WorkerTask m_workerTask;
        private MigrationSource m_migrationSource;
        private string m_password;
        
        public ClearQuestMigrationSourceViewModel(MigrationSource migrationSource)
        {
            m_migrationSource = migrationSource;

            UserName = m_migrationSource.CustomSettings.CustomSetting.Where(x => string.Equals(x.SettingKey, ClearQuestConstants.UserNameKey)).Select(x => x.SettingValue).FirstOrDefault();

            m_worker = new BackgroundWorker();
            m_worker.DoWork += new DoWorkEventHandler(m_worker_DoWork);
            m_worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_worker_RunWorkerCompleted);
        }

        private ClearQuestDatabaseViewModel m_selectedDatabase;
        public ClearQuestDatabaseViewModel SelectedDatabase
        {
            get
            {
                return m_selectedDatabase;
            }
            set
            {
                if (m_selectedDatabase != value)
                {
                    m_selectedDatabase = value;
                    OnPropertyChanged("SelectedDatabase");
                }
            }
        }

        private bool m_isLoading = false;
        public bool IsLoading
        {
            get
            {
                return m_isLoading;
            }
            private set
            {
                if (m_isLoading != value)
                {
                    m_isLoading = value;
                    OnPropertyChanged("IsLoading");
                }
            }
        }

        private string m_userName;
        public string UserName
        {
            get
            {
                return m_userName;
            }
            set
            {
                m_userName = value;
                OnPropertyChanged("UserName");
            }
        }

        private bool? m_isValid;
        public bool? IsValid
        {
            get
            {
                return m_isValid;
            }
            set
            {
                if (m_isValid != value)
                {
                    m_isValid = value;
                    OnPropertyChanged("IsValid");
                }
            }
        }

        private bool m_isValidating = false;
        public bool IsValidating
        {
            get
            {
                return m_isValidating;
            }
            private set
            {
                if (m_isValidating != value)
                {
                    m_isValidating = value;
                    OnPropertyChanged("IsValidating");
                }
            }
        }

        private ObservableCollection<ClearQuestDatabaseViewModel> m_databases;
        public ObservableCollection<ClearQuestDatabaseViewModel> Databases
        {
            get
            {
                if (m_databases == null)
                {
                    m_databases = new ObservableCollection<ClearQuestDatabaseViewModel>();

                    if (!m_worker.IsBusy)
                    {
                        IsLoading = true;
                        m_worker.RunWorkerAsync(WorkerTask.GetDatabases);
                    }
                }

                return m_databases;
            }
        }

        private void m_worker_DoWork(object sender, DoWorkEventArgs e)
        {
            m_workerTask = (WorkerTask)e.Argument;

            if (m_workerTask == WorkerTask.GetDatabases)
            {
                IEnumerable<IOAdDatabaseDesc> databases = CQWorkSpace.GetAccessibleDatabases();
                List<ClearQuestDatabaseViewModel> databaseList = new List<ClearQuestDatabaseViewModel>();
                foreach (IOAdDatabaseDesc databaseDesc in databases)
                {
                    databaseList.Add(new ClearQuestDatabaseViewModel(databaseDesc));
                }

                e.Result = databaseList;
            }
            else if (m_workerTask == WorkerTask.Validate)
            {
                ClearQuestConnectionConfig userSessionConnConfig = new ClearQuestConnectionConfig(UserName, m_password, SelectedDatabase.DatabaseName, SelectedDatabase.DatabaseSetName);
                ClearQuestOleServer.Session userSession = CQConnectionFactory.GetUserSession(userSessionConnConfig);
            }
        }

        private void m_worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (m_disposeRequested)
            {
                Dispose();
                return;
            }
            if (m_workerTask == WorkerTask.GetDatabases)
            {
                if (e.Error == null)
                {
                    List<ClearQuestDatabaseViewModel> databases = e.Result as List<ClearQuestDatabaseViewModel>;
                    foreach (ClearQuestDatabaseViewModel database in databases)
                    {
                        Databases.Add(database);
                    }
                    SelectedDatabase = Databases.FirstOrDefault(x => string.Equals(x.DatabaseSetName, m_migrationSource.ServerUrl) && string.Equals(x.DatabaseName, m_migrationSource.SourceIdentifier));
                    if (SelectedDatabase == null)
                    {
                        SelectedDatabase = databases.FirstOrDefault();
                    }
                }
                else
                {
                    Utilities.HandleException(e.Error);
                }
                IsLoading = false;
            }
            else if (m_workerTask == WorkerTask.Validate)
            {
                if (e.Error == null)
                {
                    IsValid = true;
                }
                else
                {
                    IsValid = false;
                    // TODO: provide additional feedback
                }

                IsValidating = false;
            }
        }

        public void Validate(string password)
        {
            m_password = password;
            Debug.Assert(SelectedDatabase != null);
            if (SelectedDatabase != null)
            {
                if (!m_worker.IsBusy)
                {
                    IsValidating = true;
                    IsValid = null;
                    m_worker.RunWorkerAsync(WorkerTask.Validate);
                }
            }
        }

        public void Save()
        {
            m_migrationSource.ServerUrl = SelectedDatabase.DatabaseSetName;
            m_migrationSource.SourceIdentifier = SelectedDatabase.DatabaseName;
            m_migrationSource.FriendlyName = string.Format("{0}@{1}", m_migrationSource.ServerUrl, m_migrationSource.SourceIdentifier);
            m_migrationSource.ServerIdentifier = m_migrationSource.FriendlyName;

            m_migrationSource.CustomSettings.CustomSetting.Clear();
            SaveCustomSetting(ClearQuestConstants.UserNameKey, UserName);

            if (CQLoginCredentialManagerFactory.OSIsNotSupported()) // windows XP does not support stored credentials
            {
                SaveCustomSetting(ClearQuestConstants.PasswordKey, m_password);
                SaveCustomSetting(ClearQuestConstants.LoginCredentialSettingKey, ClearQuestConstants.LoginCredentialSettingUseTextUsernamePasswordPairInConfig);
                m_migrationSource.StoredCredential.CredentialString = null;
                TraceManager.TraceInformation("ClearQuestConnectDialog: Skipped adding credentials to the credential store because the operating system does not support it");
            }
            else
            {
                // todo: move this to the adapter as a helper method
                string credentialString = ClearQuestConstants.CqConnectionStringUrlPrefix + m_migrationSource.FriendlyName;
                if (WinCredentialsProxy.CredentialExists(credentialString, WinCredentials.CredentialType.Generic))
                {
                    WinCredentialsProxy.DeleteCredential(credentialString);
                }
                WinCredentialsProxy.AddGeneralCredential(credentialString, UserName, m_password, WinCredentials.CredentialPersistence.LocalComputer, string.Empty);
                m_migrationSource.StoredCredential.CredentialString = credentialString;
                TraceManager.TraceInformation("ClearQuestConnectDialog: Successfully added credentials to the credential store with the key '{0}'", credentialString);
            }
        }

        /// <summary>
        /// Save key-value pair to CustomSettings.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>Item already exists.</returns>
        private bool SaveCustomSetting(string key, string value)
        {
            CustomSetting userNameSetting = m_migrationSource.CustomSettings.CustomSetting.FirstOrDefault(x => string.Equals(x.SettingKey, key));
            if (userNameSetting != null)
            {
                userNameSetting.SettingValue = value;
                return true;
            }
            else
            {
                CustomSetting newUserNameSetting = new CustomSetting();
                newUserNameSetting.SettingKey = key;
                newUserNameSetting.SettingValue = value;
                m_migrationSource.CustomSettings.CustomSetting.Add(newUserNameSetting);
                return false;
            }
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

        #region IDisposable Members

        private bool m_disposeRequested = false;
        public void Dispose()
        {
            if (m_worker.IsBusy)
            {
                m_disposeRequested = true;
                return;
            }

            m_worker.DoWork -= m_worker_DoWork;
            m_worker.RunWorkerCompleted -= m_worker_RunWorkerCompleted;
            m_worker.Dispose();
            m_worker = null;
        }

        #endregion
    }
}
