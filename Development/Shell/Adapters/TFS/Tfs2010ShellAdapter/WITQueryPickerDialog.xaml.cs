// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Microsoft.TeamFoundation.Migration.Shell.Tfs2010ShellAdapter
{
    /// <summary>
    /// Interaction logic for WITQueryPickerDialog.xaml
    /// </summary>
    public partial class WITQueryPickerDialog : Window
    {
        private WITQueryPickerViewModel m_witQueryPickerViewModel;

        public WITQueryPickerDialog(FilterItem filterItem, MigrationSource migrationSource)
        {
            InitializeComponent();

            m_witQueryPickerViewModel = new WITQueryPickerViewModel(filterItem, migrationSource);
            m_witQueryPickerViewModel.Initialize();
            DataContext = m_witQueryPickerViewModel;
        }

        private void rootTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is WITQueryNodeViewModel)
            {
                m_witQueryPickerViewModel.SelectedNode = e.NewValue as WITQueryNodeViewModel;
            }
            else
            {
                m_witQueryPickerViewModel.SelectedNode = null;
            }
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            TrySaveAndClose();
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                TrySaveAndClose();
            }
        }

        private void TrySaveAndClose()
        {
            if (m_witQueryPickerViewModel.TrySave())
            {
                DialogResult = true;
                Close();
            }
        }
    }

    internal class WITQueryPickerViewModel : INotifyPropertyChanged, IDisposable
    {
        private FilterItem m_filterItem;
        private Project m_project;
        private BackgroundWorker m_worker;
        private MigrationSource m_migrationSource;

        public WITQueryPickerViewModel(FilterItem filterItem, MigrationSource migrationSource)
        {
            m_filterItem = filterItem;
            m_migrationSource = migrationSource;

            m_worker = new BackgroundWorker();
            m_worker.DoWork += new DoWorkEventHandler(m_worker_DoWork);
            m_worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_worker_RunWorkerCompleted);
        }

        public void Initialize()
        {
            if (!m_worker.IsBusy)
            {
                IsLoading = true;
                m_worker.RunWorkerAsync();
            }
        }

        void m_worker_DoWork(object sender, DoWorkEventArgs e)
        {
            TfsTeamProjectCollection collection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(m_migrationSource.ServerUrl));
            WorkItemStore store = collection.GetService<WorkItemStore>();

            foreach (Project project in store.Projects)
            {
                if (string.Equals(project.Name, m_migrationSource.SourceIdentifier))
                {
                    m_project = project;
                }
            }

            e.Result = new WITQueryNodeViewModel(m_project.QueryHierarchy);
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
                WITQueryNodeViewModel root = e.Result as WITQueryNodeViewModel;
                RootList.Add(root);
                IsLoading = false;
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

        private ObservableCollection<WITQueryNodeViewModel> m_rootList;
        public ObservableCollection<WITQueryNodeViewModel> RootList
        {
            get
            {
                if (m_rootList == null)
                {
                    m_rootList = new ObservableCollection<WITQueryNodeViewModel>();
                }
                return m_rootList;
            }
        }

        private WITQueryNodeViewModel m_selectedNode;
        public WITQueryNodeViewModel SelectedNode
        {
            get
            {
                return m_selectedNode;
            }
            set
            {
                if (m_selectedNode != value)
                {
                    m_selectedNode = value;
                    OnPropertyChanged("SelectedNode");
                }
            }
        }

        public bool TrySave()
        {
            if (!string.IsNullOrEmpty(SelectedNode.QueryText))
            {
                m_filterItem.FilterString = SelectedNode.QueryText;
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
            if (m_worker != null)
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
        }

        #endregion
    }

    internal class WITQueryNodeViewModel
    {
        private QueryItem m_queryItem;

        public WITQueryNodeViewModel(QueryItem queryItem)
        {
            m_queryItem = queryItem;
        }

        public string DisplayName
        {
            get
            {
                return m_queryItem.Name;
            }
        }

        private List<WITQueryNodeViewModel> m_children;
        public List<WITQueryNodeViewModel> Children
        {
            get
            {
                if (m_children == null)
                {
                    m_children = new List<WITQueryNodeViewModel>();
                    if (m_queryItem is QueryFolder)
                    {
                        foreach (QueryItem queryItem in (QueryFolder)m_queryItem)
                        {
                            m_children.Add(new WITQueryNodeViewModel(queryItem));
                        }
                    }
                }
                return m_children;
            }
        }

        private bool m_isExpanded = false;
        public bool IsExpanded
        {
            get
            {
                return m_isExpanded && Children.Count > 0;
            }
            set
            {
                if (m_isExpanded != value)
                {
                    m_isExpanded = value;
                }
            }
        }

        public NodeType Type
        {
            get
            {
                if (m_queryItem is QueryHierarchy)
                {
                    return NodeType.Root;
                }
                else if (m_queryItem is QueryDefinition)
                {
                    QueryDefinition queryDefinition = m_queryItem as QueryDefinition;
                    switch (queryDefinition.QueryType)
                    {
                        case QueryType.List:
                        case QueryType.Invalid:
                            return NodeType.QueryList;
                        case QueryType.Tree:
                            return NodeType.QueryTree;
                        case QueryType.OneHop:
                            return NodeType.QueryOneHop;
                        default:
                            throw new ArgumentException("Invalid enum");
                    }
                }
                else
                {
                    return NodeType.Folder;
                }
            }
        }

        public string QueryText
        {
            get
            {
                if (m_queryItem is QueryDefinition)
                {
                    QueryDefinition queryDefinition = m_queryItem as QueryDefinition;
                    if (queryDefinition.QueryType == QueryType.List)
                    {
                        return ParseQueryText(queryDefinition.QueryText);
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        private const string whereClause = "where";
        private const string orderByClause = "order by";
        private string ParseQueryText(string str)
        {
            int start = str.IndexOf(whereClause, StringComparison.OrdinalIgnoreCase) + whereClause.Length;
            int end = str.IndexOf(orderByClause, StringComparison.OrdinalIgnoreCase);
            if (end == -1)
            {
                end = str.Length;
            }
            string s = str.Substring(start, end - start);
            return s.Trim();
        }

        internal enum NodeType
        {
            Root,
            Folder,
            QueryList,
            QueryOneHop,
            QueryTree
        }
    }
}
