// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Interaction logic for SaveToDBDialog.xaml
    /// </summary>
    public partial class SaveToDBDialog : Window
    {
        private ShellViewModel m_viewModel;

        public SaveToDBDialog(ShellViewModel viewModel)
        {
            m_viewModel = viewModel;

            if (ActiveSessionGroup.SessionGroup.Creator == null)
            {
                ActiveSessionGroup.SessionGroup.Creator = Environment.UserDomainName + "\\" + Environment.UserName;
            }

            InitializeComponent();
            LoadActiveConfigurations();
        }

        public Configuration ActiveSessionGroup
        {
            get
            {
                return m_viewModel.DataModel.Configuration;
            }
        }

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

        private void LoadActiveConfigurations()
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

        private void OnSaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Dialog box accepted
            this.DialogResult = true;
        }
    }
}
