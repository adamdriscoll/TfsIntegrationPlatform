// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.ObjectModel;
using System.Data.Objects;
using System.Linq;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Shell.ViewModel;

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    /// <summary>
    /// This is a view on active runs within a parent session group.
    /// </summary>
    public class SessionGroupRunViewModel : ModelObject
    {
        RTSessionGroupRun m_sessionGroupRun;
        private RuntimeManager m_host;

        public SessionGroupRunViewModel(RTSessionGroupRun sessionGroupRun)
        {
            m_sessionGroupRun = sessionGroupRun;

            m_host = RuntimeManager.GetInstance();
            IRefreshService refresh = (IRefreshService)m_host.GetService(typeof(IRefreshService));
            refresh.AutoRefresh += this.AutoRefresh;

            // Avoid race by driving through top level refresh instead of relying upon background thread.
            Refresh();
        }

        #region Properties
        // Selected RTSessionGroupRun properties
        public int Id { get { return m_sessionGroupRun.Id; } }
        public DateTime StartTime { get { return m_sessionGroupRun.StartTime; } }
        public DateTime? FinishTime { get { return m_sessionGroupRun.FinishTime; } }

        private SessionGroupConfigViewModel m_sessionGroupConfigViewModel;

        public SessionGroupConfigViewModel SessionGroupConfig
        {
            get
            {
                if (m_sessionGroupConfigViewModel == null)
                {
                    m_sessionGroupRun.ConfigReference.Load();
                    m_sessionGroupConfigViewModel = new SessionGroupConfigViewModel(m_sessionGroupRun.Config);
                }
                return m_sessionGroupConfigViewModel;
            }
        }

        private ObservableCollection<SessionRunViewModel> m_sessionRunsViewModel;

        public ObservableCollection<SessionRunViewModel> SessionRuns
        {
            get
            {
                if (m_sessionRunsViewModel == null)
                {
                    m_sessionRunsViewModel = new ObservableCollection<SessionRunViewModel>();
                    m_sessionGroupRun.SessionRuns.Load();

                    foreach (RTSessionRun sessionRun in m_sessionGroupRun.SessionRuns)
                    {
                        SessionRunViewModel sessionRunViewModel = new SessionRunViewModel(sessionRun);

                        m_sessionRunsViewModel.Add(sessionRunViewModel);
                    }
                }
                return m_sessionRunsViewModel;
            }
        }

        private ObservableCollection<ConflictViewModel> m_conflicts;

        public ObservableCollection<ConflictViewModel> Conflicts
        {
            get
            {
                return m_conflicts;
            }
        }

        // View related
        private bool m_isSelected;

        public bool IsSelected
        {
            get 
            { 
                return m_isSelected; 
            }
            set 
            {
                if (m_isSelected != value)
                {
                    m_isSelected = value;
                    this.RaisePropertyChangedEvent("IsSelected", !m_isSelected, m_isSelected);
                }
                
            }
        }
        #endregion

        public void AutoRefresh(object sender, EventArgs e)
        {
            Refresh();
        }

        private void Refresh()
        {
            RefreshConflictCollection();
        }

        private void RefreshConflictCollection()
        {
            if (m_conflicts == null)
            {
                m_conflicts = new ObservableCollection<ConflictViewModel>();
            }

            if (!m_sessionGroupRun.ConflictCollectionReference.IsLoaded)
            {
                m_sessionGroupRun.ConflictCollectionReference.Load();
            }

            ObjectQuery<RTConflict> conflicts = m_sessionGroupRun.ConflictCollection.Conflicts.CreateSourceQuery();

            var query = (from c in conflicts
                         orderby c.Id descending
                         select c).Take(m_host.MaxQueryResults);

            m_conflicts.Clear();

            foreach (RTConflict conflict in query)
            {
                m_conflicts.Add(new ConflictViewModel(conflict));
            }
        }

    }
}
