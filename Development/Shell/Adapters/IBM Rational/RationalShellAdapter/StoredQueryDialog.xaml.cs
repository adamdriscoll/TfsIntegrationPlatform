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
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Shell.RationalShellAdapter
{
    /// <summary>
    /// Interaction logic for StoredQueryDialog.xaml
    /// </summary>
    public partial class StoredQueryDialog : Window
    {
        private StoredQueriesViewModel m_storedQueries;

        public StoredQueryDialog(FilterItem filterItem, MigrationSource migrationSourceConfig)
        {
            InitializeComponent();

            m_storedQueries = new StoredQueriesViewModel(filterItem, migrationSourceConfig);
            DataContext = m_storedQueries;
        }
        
        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            TrySaveAndClose();
        }

        private void TextBlock_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                TrySaveAndClose();
            }
        }

        private void TrySaveAndClose()
        {
            if (m_storedQueries.Save())
            {
                DialogResult = true;
                Close();
            }
        }

        private void rootTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is StoredQueryNodeViewModel)
            {
                m_storedQueries.SelectedNode = e.NewValue as StoredQueryNodeViewModel;
            }
            else
            {
                m_storedQueries.SelectedNode = null;
            }
        }
    }

    public class StoredQueriesViewModel : INotifyPropertyChanged, IDisposable
    {
        private const string StoredQueryPrefix = "@StoredQuery@::";
        
        private FilterItem m_filterItem;
        private MigrationSource m_migrationSource;
        private StoredQueryNodeViewModel m_rootNode;
        private BackgroundWorker m_worker;

        public StoredQueriesViewModel(FilterItem filterItem, MigrationSource migrationSource)
        {
            m_filterItem = filterItem;
            m_migrationSource = migrationSource;

            m_worker = new BackgroundWorker();
            m_worker.DoWork += new DoWorkEventHandler(m_worker_DoWork);
            m_worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_worker_RunWorkerCompleted);
        }

        void m_worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Debug.Assert(e.Argument is MigrationSource, "Wrong argument type");
            MigrationSource migrationSourceConfig = e.Argument as MigrationSource;

            string dbSet = migrationSourceConfig.ServerUrl;
            string userDb = migrationSourceConfig.SourceIdentifier;

            CredentialManagementService credManagementService =
                new CredentialManagementService(migrationSourceConfig);

            ICQLoginCredentialManager loginCredManager = CQLoginCredentialManagerFactory.CreateCredentialManager(credManagementService, migrationSourceConfig);
            ClearQuestConnectionConfig userSessionConnConfig = new ClearQuestConnectionConfig(loginCredManager.UserName,
                                                           loginCredManager.Password,
                                                           userDb,
                                                           dbSet);
            ClearQuestOleServer.Session userSession = CQConnectionFactory.GetUserSession(userSessionConnConfig);

            Dictionary<string, bool> queryList = CQWorkSpace.GetQueryListWithValidity(userSession);
            e.Result = queryList;
        }

        void m_worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (m_disposeRequested)
            {
                Dispose();
                return;
            }
            if (e.Error == null)
            {
                Debug.Assert(e.Result is Dictionary<string, bool>, "Wrong result type");
                Dictionary<string, bool> queryList = e.Result as Dictionary<string, bool>;

                List<string> queries = queryList.Keys.ToList();
                queries.Sort();

                foreach (string query in queries)
                {
                    string[] path = query.Split('/');
                    StoredQueryNodeViewModel current = Root;
                    for (int i = 0; i < path.Length; i++)
                    {
                        StoredQueryNodeViewModel childNode = current.Children.FirstOrDefault(x => string.Equals(x.Name, path[i]));
                        if (childNode == null)
                        {
                            childNode = new StoredQueryNodeViewModel(path[i]);
                            current.Children.Add(childNode);
                        }
                        current = childNode;
                        if (i == path.Length - 1)
                        {
                            current.Query = query;
                            current.IsValid = queryList[query];
                        }
                    }
                }
            }
            IsLoading = false;
        }

        public StoredQueryNodeViewModel Root
        {
            get
            {
                if (m_rootNode == null)
                {
                    m_rootNode = new StoredQueryNodeViewModel("ROOT");

                    if (!m_worker.IsBusy)
                    {
                        IsLoading = true;
                        m_worker.RunWorkerAsync(m_migrationSource);
                    }
                }
                return m_rootNode;
            }
        }

        public StoredQueryNodeViewModel SelectedNode { get; set; }

        private bool m_isLoading = false;
        public bool IsLoading
        {
            get
            {
                return m_isLoading;
            }
            set
            {
                if (m_isLoading != value)
                {
                    m_isLoading = value;
                    OnPropertyChanged("IsLoading");
                }
            }
        }

        public bool Save()
        {
            if (SelectedNode != null && !string.IsNullOrEmpty(SelectedNode.Query) && SelectedNode.IsValid)
            {
                m_filterItem.FilterString = StoredQueryPrefix + SelectedNode.Query;
                return true;
            }
            else
            {
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

    public class StoredQueryNodeViewModel
    {
        public StoredQueryNodeViewModel(string name)
        {
            Name = name;
            Children = new ObservableCollection<StoredQueryNodeViewModel>();
            IsValid = true;
        }

        public string Name { get; private set; }

        public string Query { get; set; }

        public bool IsValid { get; set; }

        public ObservableCollection<StoredQueryNodeViewModel> Children { get; private set; }

        public string ToolTip
        {
            get
            {
                if (!string.IsNullOrEmpty(Query))
                {
                    if (IsValid)
                    {
                        return Query;
                    }
                    else
                    {
                        return "This is an invalid query because it contains record types of All_UCM_Activities.";
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is StoredQueryNodeViewModel)
            {
                StoredQueryNodeViewModel other = obj as StoredQueryNodeViewModel;
                return string.Equals(other.Name, Name);
            }
            else if (obj is string)
            {
                string other = obj as string;
                return string.Equals(other, Name);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
