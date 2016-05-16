// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;

namespace Microsoft.TeamFoundation.Migration.Shell.Tfs11ShellAdapter
{
    /// <summary>
    /// Interaction logic for VCServerPathDialog.xaml
    /// </summary>
    public partial class VCServerPathDialog : Window
    {
        private VCServerPathViewModel m_vcServerPathViewModel;

        internal VCServerPathDialog(FilterItem filterItem, MigrationSource migrationSource)
        {
            InitializeComponent();

            m_vcServerPathViewModel = new VCServerPathViewModel(filterItem, migrationSource);
            m_vcServerPathViewModel.Initialize();

            this.Closed += new EventHandler(VCServerPathDialog_Closed);

            DataContext = m_vcServerPathViewModel;

            rootTreeView.Focus();
        }

        void VCServerPathDialog_Closed(object sender, EventArgs e)
        {
            m_vcServerPathViewModel.Dispose();
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            m_vcServerPathViewModel.Save();
            DialogResult = true;
            Close();
        }

        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem)
            {
                TreeViewItem selectedItem = sender as TreeViewItem;
                if (selectedItem.DataContext is VCServerPathNodeViewModel)
                {
                    m_vcServerPathViewModel.SelectedNode = selectedItem.DataContext as VCServerPathNodeViewModel;
                    e.Handled = true;
                }
                selectedItem.BringIntoView();
            }
        }
    }

    internal class VCServerPathViewModel : INotifyPropertyChanged, IDisposable
    {
        private MigrationSource m_migrationSource;
        private FilterItem m_filterItem;
        private BackgroundWorker m_worker;
        private VersionControlServer m_server;
        
        public VCServerPathViewModel(FilterItem filterItem, MigrationSource migrationSource)
        {
            m_filterItem = filterItem;
            m_migrationSource = migrationSource;

            m_worker = new BackgroundWorker();
            m_worker.DoWork += new DoWorkEventHandler(m_worker_DoWork);
            m_worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_worker_RunWorkerCompleted);
        }

        private Queue<VCServerPathNodeViewModel> m_queue = new Queue<VCServerPathNodeViewModel>();
        public void QueueLoadChildren(VCServerPathNodeViewModel node)
        {
            if (m_worker.IsBusy)
            {
                m_queue.Enqueue(node);
            }
            else
            {
                m_worker.RunWorkerAsync(node);
            }
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
            if (e.Argument is VCServerPathNodeViewModel)
            {
                VCServerPathNodeViewModel node = e.Argument as VCServerPathNodeViewModel;
                node.GetItems(m_server);
                e.Result = node;
            }
            else
            {
                TfsTeamProjectCollection collection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(m_migrationSource.ServerUrl));
                m_server = collection.GetService<VersionControlServer>();

                Item item = m_server.GetItem(VersionControlPath.RootFolder + m_migrationSource.SourceIdentifier);
                VCServerPathRootViewModel rootNode = new VCServerPathRootViewModel(item, this);
                rootNode.Load(m_server);
                rootNode.IsExpanded = true;
                SelectedNode = rootNode;
                string[] tokens = m_filterItem.FilterString.Split(VersionControlPath.Separator);
                for (int i = 2; i < tokens.Length; i++)
                {
                    SelectedNode.Load(m_server);
                    SelectedNode.IsExpanded = true;

                    VCServerPathNodeViewModel newSelectedNode = SelectedNode.Children.FirstOrDefault(x => string.Equals(x.DisplayName, tokens[i]));

                    if (newSelectedNode != null)
                    {
                        SelectedNode = newSelectedNode;
                    }
                    else
                    {
                        break;
                    }
                }
                e.Result = rootNode;
            }
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
                if (e.Result is VCServerPathRootViewModel)
                {
                    VCServerPathRootViewModel rootNode = e.Result as VCServerPathRootViewModel;
                    RootList.Add(rootNode);
                }
                else if (e.Result is VCServerPathNodeViewModel)
                {
                    VCServerPathNodeViewModel node = e.Result as VCServerPathNodeViewModel;
                    node.PopulateChildren();

                    if (m_queue.Count > 0)
                    {
                        QueueLoadChildren(m_queue.Dequeue());
                    }
                }
            }
            IsLoading = false;
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
                m_isLoading = value;
                OnPropertyChanged("IsLoading");
            }
        }

        private ObservableCollection<VCServerPathNodeViewModel> m_rootList;
        public ObservableCollection<VCServerPathNodeViewModel> RootList
        {
            get
            {
                if (m_rootList == null)
                {
                    m_rootList = new ObservableCollection<VCServerPathNodeViewModel>();
                }
                return m_rootList;
            }
        }

        private VCServerPathNodeViewModel m_selectedNode;
        public VCServerPathNodeViewModel SelectedNode
        {
            get
            {
                return m_selectedNode;
            }
            set
            {
                if (m_selectedNode != null)
                {
                    m_selectedNode.IsSelected = false;
                }
                m_selectedNode = value;
                if (m_selectedNode != null)
                {
                    m_selectedNode.IsSelected = true;
                }
                OnPropertyChanged("SelectedNode");
            }
        }

        public void Save()
        {
            m_filterItem.FilterString = SelectedNode.ServerPath;
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

    internal class VCServerPathRootViewModel : VCServerPathNodeViewModel
    {
        public VCServerPathRootViewModel(Item item, VCServerPathViewModel container)
            : base(item,  container)
        {
            Type = NodeType.Root;
        }
    }

    internal class VCServerPathStubViewModel : VCServerPathNodeViewModel
    {
        public VCServerPathStubViewModel() : base(null,  null)
        {
            Type = NodeType.None;
        }

        public override string ServerPath
        {
            get
            {
                return null;
            }
        }

        public override string DisplayName
        {
            get
            {
                return "Loading...";
            }
        }
    }

    internal class VCServerPathNodeViewModel : INotifyPropertyChanged
    {
        private Item m_item;
        private VCServerPathViewModel m_container;

        public VCServerPathNodeViewModel(Item item, VCServerPathViewModel container)
        {
            m_container = container;
            m_item = item;
            if (m_item != null)
            {
                if (m_item.IsBranch)
                {
                    Type = NodeType.Branch;
                }
                else
                {
                    Type = NodeType.Folder;
                }
            }
        }

        private ItemSet m_itemSet;
        public void GetItems(VersionControlServer server)
        {
            ItemSpec spec = new ItemSpec(ServerPath, RecursionType.OneLevel);
            GetItemsOptions options = GetItemsOptions.IncludeBranchInfo;
            m_itemSet = server.GetItems(spec, VersionSpec.Latest, DeletedState.NonDeleted, ItemType.Folder, options);
        }

        public void PopulateChildren()
        {
            if (m_itemSet != null)
            {
                Children.Clear();
                for (int i = 1; i < m_itemSet.Items.Length; i++)
                {
                    Children.Add(new VCServerPathNodeViewModel(m_itemSet.Items[i], m_container));
                }
            }
            IsLoading = false;
            IsLoaded = true;
        }

        public void Load(VersionControlServer server)
        {
            if (!IsLoaded)
            {
                GetItems(server);
                PopulateChildren();
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

        public NodeType Type { get; set; }
        public bool IsSelected { get; set; }

        private bool m_isExpanded = false;
        public bool IsExpanded
        {
            get
            {
                return m_isExpanded && Children.Count > 0;
            }
            set
            {
                m_isExpanded = value;
                if (m_isExpanded && !IsLoaded)
                {
                    Children.Clear();
                    IsLoading = true;
                    m_container.QueueLoadChildren(this);
                }
                OnPropertyChanged("IsExpanded");
            }
        }

        private bool m_isLoaded = false;
        public bool IsLoaded
        {
            get
            {
                return m_isLoaded;
            }
            private set
            {
                m_isLoaded = value;
                OnPropertyChanged("IsLoaded");
                OnPropertyChanged("IsExpanded");
            }
        }

        public virtual string ServerPath
        {
            get
            {
                return m_item.ServerItem;
            }
        }

        public virtual string DisplayName
        {
            get
            {
                return VersionControlPath.GetFileName(ServerPath);
            }
        }

        private ObservableCollection<VCServerPathNodeViewModel> m_children;
        public ObservableCollection<VCServerPathNodeViewModel> Children
        {
            get
            {
                if (m_children == null)
                {
                    m_children = new ObservableCollection<VCServerPathNodeViewModel>();
                    m_children.Add(new VCServerPathStubViewModel());
                }
                return m_children;
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

        public enum NodeType
        {
            Root,
            Folder,
            Branch,
            None
        }
    }
}
