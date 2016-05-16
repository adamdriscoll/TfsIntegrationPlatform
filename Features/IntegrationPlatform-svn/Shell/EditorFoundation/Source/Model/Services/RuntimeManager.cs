// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.ComponentModel.Design;
using System.Windows.Threading;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Shell.Model;

namespace Microsoft.TeamFoundation.Migration.Shell.ViewModel
{
    public class RuntimeManager : ModelObject, IServiceProvider
    {
        #region Fields
        private static RuntimeManager m_instance;
        private ServiceContainer m_serviceContainer;
        private RefreshService m_refreshService;
        #endregion

        #region Constructors
        public static RuntimeManager GetInstance()
        {
            if (m_instance == null)
            {
                m_instance = new RuntimeManager();
            }
            return m_instance;
        }

        protected RuntimeManager()
        {
            // Default to (*).Take(200) for queries
            this.MaxQueryResults = 200;
            this.Context = RuntimeEntityModel.CreateInstance();

            m_serviceContainer = new ServiceContainer();

            // The RuntimeViewModelHost MUST BE CREATED on the UI thread to get the
            // correct dispatcher.
            int refreshIntervalSeconds = Properties.Settings.Default.RefreshIntervalSeconds;
            m_refreshService = new RefreshService(Dispatcher.CurrentDispatcher, refreshIntervalSeconds);
            m_serviceContainer.AddService(typeof(IRefreshService), m_refreshService);
        }
        #endregion

        #region Properties

        public bool IsOutputEnabled
        {
            get
            {
                return Properties.Settings.Default.IsOutputEnabled;
            }
            set
            {
                if (Properties.Settings.Default.IsOutputEnabled != value)
                {
                    Properties.Settings.Default.IsOutputEnabled = value;
                    RaisePropertyChangedEvent("IsOutputEnabled", null, null);
                }
            }
        }
        
        public int RefreshIntervalSeconds
        {
            get
            {
                return m_refreshService.RefreshIntervalMilliseconds / 1000;
            }
            set
            {
                m_refreshService.RefreshIntervalMilliseconds = 1000 * value;
                Properties.Settings.Default.RefreshIntervalSeconds = value;
                RaisePropertyChangedEvent("RefreshIntervalSeconds", null, null);
            }
        }

        public void EnableAutoRefresh()
        {
            m_refreshService.Resume();
            IsAutoRefreshing = true;
        }

        public void DisableAutoRefresh()
        {
            if (!GlobalConfiguration.UseWindowsService)
            {
                m_refreshService.Pause();
                IsAutoRefreshing = false;
            }
        }

        private bool m_isAutoRefreshing = false;
        public bool IsAutoRefreshing
        {
            get
            {
                return m_isAutoRefreshing;
            }
            private set
            {
                if (m_isAutoRefreshing != value)
                {
                    m_isAutoRefreshing = value;
                    RaisePropertyChangedEvent("IsAutoRefreshing", null, null);
                }
            }
        }

        public void ForceRefresh()
        {
            m_refreshService.ForceRefresh();
        }

        public int MaxQueryResults { get; set; }

        public RuntimeEntityModel Context { get; set; }

        public RuntimeSessionGroupViewModel<SessionMigrationViewModel> MigrationViewModel { get; set; }

        public HistoryViewModel HistoryViewModel { get; set; }

        public ApplicationViewModel ConflictManager { get; private set; }

        public SessionGroupViewModel ActiveSessionGroup { get; private set; }
        
        public void SetSessionGroupUniqueId(Guid sessionGroupUniqueId, ApplicationViewModel conflictManager)
        {
            ConflictManager = conflictManager;

            if (ActiveSessionGroup == null)
            {
                ActiveSessionGroup = new SessionGroupViewModel(sessionGroupUniqueId);
                MigrationViewModel = new RuntimeSessionGroupViewModel<SessionMigrationViewModel>(ActiveSessionGroup);
                HistoryViewModel = new HistoryViewModel(ActiveSessionGroup);
            }
            else
            {
                ActiveSessionGroup.SessionGroupUniqueId = sessionGroupUniqueId;
            }
            RaisePropertyChangedEvent("MigrationViewModel", null, ActiveSessionGroup);
            RaisePropertyChangedEvent("HistoryViewModel", null, ActiveSessionGroup);
        }

        #endregion

        #region IServiceProvider Members

        public object GetService(Type serviceType)
        {
            return m_serviceContainer.GetService(serviceType);
        }

        #endregion
    }
}
