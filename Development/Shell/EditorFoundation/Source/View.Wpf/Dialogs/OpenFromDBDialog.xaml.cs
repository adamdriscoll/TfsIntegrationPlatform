// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Interaction logic for OpenFromDBDialog.xaml
    /// </summary>
    public partial class OpenFromDBDialog : Window
    {
        private ActiveConfigurationsViewModel m_viewModel;

        public OpenFromDBDialog()
        {
            InitializeComponent();
            m_viewModel = new ActiveConfigurationsViewModel();
            DataContext = m_viewModel;
        }

        public SessionGroupConfigViewModel SelectedConfiguration
        {
            get
            {
                return m_viewModel.SelectedConfiguration;
            }
        }

        private void OnOpenButton_Click(object sender, RoutedEventArgs e)
        {
            // Dialog box accepted
            this.DialogResult = true;
        }

        private void listView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.DialogResult = true;
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (m_viewModel.SelectedConfiguration != null)
            {
                string message = string.Format("Are you sure you want to delete '{0}'?", m_viewModel.SelectedConfiguration.FriendlyName);
                MessageBoxResult result = MessageBox.Show(message, "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    SessionGroupDeletionTask task = new SessionGroupDeletionTask(m_viewModel.SelectedConfiguration.SessionGroupUniqueId);
                    try
                    {
                        task.DeleteSessionGroup();
                        m_viewModel.Refresh();
                    }
                    catch (Exception exception)
                    {
                        Utilities.HandleException(exception);
                    }
                }
            }
        }
    }

    public class ActiveConfigurationsViewModel
    {
        public ActiveConfigurationsViewModel()
        {
            Initialize();
        }

        private void Initialize()
        {
            using (TfsMigrationConsolidatedDBEntities context = TfsMigrationConsolidatedDBEntities.CreateInstance())
            {
                int initializedStateVal = (int)Microsoft.TeamFoundation.Migration.BusinessModel.BusinessModelManager.SessionStateEnum.Initialized;
                int runningStateVal = (int)Microsoft.TeamFoundation.Migration.BusinessModel.BusinessModelManager.SessionStateEnum.Running;
                int pausedStateVal = (int)Microsoft.TeamFoundation.Migration.BusinessModel.BusinessModelManager.SessionStateEnum.Paused;
                int completedStateVal = (int)Microsoft.TeamFoundation.Migration.BusinessModel.BusinessModelManager.SessionStateEnum.Completed;

                var activeGroupIdQuery =
                    from g in context.SessionGroupSet
                    where g.State == initializedStateVal
                       || g.State == runningStateVal
                       || g.State == pausedStateVal
                       || g.State == completedStateVal
                    select g.Configs;

                foreach (var v in activeGroupIdQuery)
                {
                    ActiveConfigurations.Add(new SessionGroupConfigViewModel(v.Last()));
                }
            }
        }

        public void Refresh()
        {
            ActiveConfigurations.Clear();
            Initialize();
        }

        public SessionGroupConfigViewModel SelectedConfiguration { get; set; }
        
        /// <summary>
        /// Returns a collection of all active Configurations stored in TFS migration DB.
        /// </summary>
        private ObservableCollection<SessionGroupConfigViewModel> m_activeConfigurations;
        public ObservableCollection<SessionGroupConfigViewModel> ActiveConfigurations
        {
            get
            {
                if (m_activeConfigurations == null)
                {
                    m_activeConfigurations = new ObservableCollection<SessionGroupConfigViewModel>();
                }
                return m_activeConfigurations;
            }
        }
    }

    public class SessionGroupConfigViewModel
    {
        private SessionGroupConfig m_config;

        public SessionGroupConfigViewModel(SessionGroupConfig config)
        {
            m_config = config;
        }

        public string FriendlyName
        {
            get
            {
                return m_config.FriendlyName;
            }
        }

        public DateTime CreationTime
        {
            get
            {
                return m_config.CreationTime.ToLocalTime();
            }
        }

        public string Creator
        {
            get
            {
                return m_config.Creator;
            }
        }

        public WorkFlowType WorkFlowType
        {
            get
            {
                return new WorkFlowType(m_config.WorkFlowType);
            }
        }

        public Guid SessionGroupUniqueId
        {
            get
            {
                return m_config.SessionGroup.GroupUniqueId;
            }
        }
    }
}
