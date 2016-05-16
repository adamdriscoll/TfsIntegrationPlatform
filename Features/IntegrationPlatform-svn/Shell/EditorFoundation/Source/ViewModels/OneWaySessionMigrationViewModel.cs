// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Shell.Model;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.ViewModel
{
    public class OneWaySessionMigrationViewModel : ModelObject
    {
        private int m_numConflicts;
        private BackgroundWorker m_backgroundWorker;
        private OneWaySessionViewModel m_oneWaySession;
        private IRefreshService m_refreshService;

        public OneWaySessionMigrationViewModel(OneWaySessionViewModel oneWaySession)
        {
            m_oneWaySession = oneWaySession;

            m_backgroundWorker = new BackgroundWorker();
            m_backgroundWorker.DoWork += new DoWorkEventHandler(m_backgroundWorker_DoWork);

            Attach();

            Refresh();
        }

        private void Attach()
        {
            if (m_refreshService == null)
            {
                RuntimeManager runtimeManager = RuntimeManager.GetInstance();
                m_refreshService = (IRefreshService)runtimeManager.GetService(typeof(IRefreshService));
            }
            m_refreshService.AutoRefresh += this.AutoRefresh;
        }

        public void Detach()
        {
            if (m_refreshService != null)
            {
                m_refreshService.AutoRefresh -= this.AutoRefresh;
            }
        }

        private void m_backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                // migration instruction status
                int analysisMIStatus = (int)ChangeStatus.AnalysisMigrationInstruction;
                int pendingStatus = (int)ChangeStatus.Pending;
                int inProgressStatus = (int)ChangeStatus.InProgress;
                int pendingConflictDetectStatus = (int)ChangeStatus.PendingConflictDetection;
                int completedStatus = (int)ChangeStatus.Complete;

                // delta status
                int deltaStatus = (int)ChangeStatus.Delta;
                int deltaPendingStatus = (int)ChangeStatus.DeltaPending;
                int deltaCompletedStatus = (int)ChangeStatus.DeltaComplete;
                int deltaSyncedStatus = (int)ChangeStatus.DeltaSynced;

                var allMigrationInstructions = from mi in context.RTChangeGroupSet
                                               where mi.SourceUniqueId.Equals(Target.UniqueId) &&
                                               mi.SessionUniqueId.Equals(SessionConfig.SessionUniqueId) &&
                                               (mi.Status == analysisMIStatus || mi.Status == pendingStatus
                                               || mi.Status == inProgressStatus || mi.Status == pendingConflictDetectStatus
                                               || mi.Status == completedStatus)
                                               select mi;

                bool isInInitialDeltaComputationPhase = allMigrationInstructions.FirstOrDefault() == null; // db hit #1
                if (isInInitialDeltaComputationPhase)
                {
                    // discovery phase
                    TotalProgress = 0;

                    var deltaTable = from d in context.RTChangeGroupSet
                                     where d.SourceUniqueId.Equals(Source.UniqueId) &&
                                     d.SessionUniqueId.Equals(SessionConfig.SessionUniqueId) &&
                                     (d.Status == deltaStatus || d.Status == deltaPendingStatus
                                     || d.Status == deltaCompletedStatus || d.Status == deltaSyncedStatus)
                                     select d;
                    int deltaTableCount = deltaTable.Count();  // db hit #2

                    AnalysisProgress = -1;
                    MigrationProgress = -1;
                    if (deltaTableCount == 0) // no changegroups discovered
                    {
                        DiscoveryProgress = -1;
                        CurrentProgressHint = "Not yet migrated.";
                    }
                    else
                    {
                        DiscoveryProgress = RuntimeManager.GetInstance().IsAutoRefreshing ? 0 : -1; // do not show progress if not refreshing
                        CurrentProgressHint = string.Format("{0} change groups discovered", deltaTableCount);
                    }
                }
                else
                {
                    TotalProgress = (from mi in context.RTChangeGroupSet
                                    where mi.SourceUniqueId.Equals(Target.UniqueId) &&
                                    mi.SessionUniqueId.Equals(SessionConfig.SessionUniqueId) &&
                                    mi.Status == completedStatus
                                    select mi).Count(); // db hit #2

                    long lastChangeGroupId = (from sp in context.RTSyncPointSet
                                              where sp.SourceUniqueId.Equals(Source.UniqueId)
                                              select sp.LastChangeGroupId).FirstOrDefault() ?? 0; // db hit #3

                    var deltasWithoutMigrationInstructions = from d in context.RTChangeGroupSet
                                                             where d.SourceUniqueId.Equals(Source.UniqueId) &&
                                                             d.SessionUniqueId.Equals(SessionConfig.SessionUniqueId) &&
                                                             (d.Status == deltaStatus || d.Status == deltaPendingStatus)
                                                             select d;

                    int unanalyzedChangeGroupsCount = deltasWithoutMigrationInstructions.Count(); // db hit #4

                    if (unanalyzedChangeGroupsCount > 0)
                    {
                        // analysis phase
                        int analyzedChangeGroupsCount = (from d in context.RTChangeGroupSet
                                                        where d.SourceUniqueId.Equals(Source.UniqueId) &&
                                                        d.SessionUniqueId.Equals(SessionConfig.SessionUniqueId) &&
                                                        (d.Status == deltaCompletedStatus || d.Status == deltaSyncedStatus) &&
                                                        d.Id > lastChangeGroupId
                                                        select d).Count(); // db hit #5
                        int totalChangeGroupsToAnalyzeCount = (from d in context.RTChangeGroupSet
                                                               where d.SourceUniqueId.Equals(Source.UniqueId) &&
                                                               d.SessionUniqueId.Equals(SessionConfig.SessionUniqueId) &&
                                                               (d.Status == deltaStatus || d.Status == deltaPendingStatus
                                                                || d.Status == deltaCompletedStatus || d.Status == deltaSyncedStatus) &&
                                                               d.Id > lastChangeGroupId
                                                               select d).Count(); // db hit #6

                        DiscoveryProgress = 1;
                        AnalysisProgress = (double)analyzedChangeGroupsCount / totalChangeGroupsToAnalyzeCount;
                        MigrationProgress = -1;
                        CurrentProgressHint = string.Format("{0} of {1} change groups analyzed", analyzedChangeGroupsCount, totalChangeGroupsToAnalyzeCount);
                    }
                    else
                    {
                        // migration phase
                        int migratedChangeGroupsCount = (from mi in context.RTChangeGroupSet
                                                        where mi.SourceUniqueId.Equals(Target.UniqueId) &&
                                                        mi.SessionUniqueId.Equals(SessionConfig.SessionUniqueId) &&
                                                        mi.Status == completedStatus
                                                        select mi).Count(); // db hit #5
                        int totalChangeGroupsToMigrateCount = (from mi in allMigrationInstructions
                                                              where mi.Id > lastChangeGroupId || mi.ContainsBackloggedAction
                                                              select mi).Count(); // db hit #6

                        DiscoveryProgress = 1;
                        AnalysisProgress = 1;
                        if (migratedChangeGroupsCount < totalChangeGroupsToMigrateCount)
                        {
                            MigrationProgress = (double)migratedChangeGroupsCount / totalChangeGroupsToMigrateCount;
                            CurrentProgressHint = string.Format("{0} of {1} change groups migrated", migratedChangeGroupsCount, totalChangeGroupsToMigrateCount);
                        }
                        else
                        {
                            CurrentChangeGroup = null;
                            MigrationProgress = 1;
                            CurrentProgressHint = RuntimeManager.GetInstance().IsAutoRefreshing ? "Migrating..." : "Migration Complete!";
                        }
                    }
                }
            }
        }
        
        public void AutoRefresh(object sender, EventArgs e)
        {
            Refresh();
        }

        public void Refresh()
        {
            if (!m_backgroundWorker.IsBusy)
            {
                m_backgroundWorker.RunWorkerAsync();
            }
        }

        public int NumConflicts
        {
            get
            {
                return m_numConflicts;
            }
            set
            {
                if (m_numConflicts != value)
                {
                    m_numConflicts = value;
                    RaisePropertyChangedEvent("NumConflicts", null, null);
                }
            }
        }

        public string FriendlyName
        {
            get
            {
                return m_oneWaySession.FriendlyName;
            }
        }

        public RTMigrationSource Source
        {
            get
            {
                return m_oneWaySession.Source;
            }
        }

        public RTMigrationSource Target
        {
            get
            {
                return m_oneWaySession.Target;
            }
        }

        RTSessionConfig SessionConfig
        {
            get
            {
                return m_oneWaySession.Session;
            }
        }

        private int m_totalProgress;
        private double m_discoveryProgress;
        private double m_analysisProgress;
        private double m_migrationProgress;
        private string m_currentProgressHint;
        private RTChangeGroup m_currentChangeGroup;

        public int TotalProgress
        {
            get
            {
                return m_totalProgress;
            }
            private set
            {
                if (m_totalProgress != value)
                {
                    m_totalProgress = value;
                    RaisePropertyChangedEvent("TotalProgress", null, null);
                }
            }
        }

        public double DiscoveryProgress
        {
            get
            {
                return m_discoveryProgress;
            }
            set
            {
                if (m_discoveryProgress != value)
                {
                    m_discoveryProgress = value;
                    RaisePropertyChangedEvent("DiscoveryProgress", null, null);
                }
            }
        }

        public double AnalysisProgress
        {
            get
            {
                return m_analysisProgress;
            }
            set
            {
                if (m_analysisProgress != value)
                {
                    m_analysisProgress = value;
                    RaisePropertyChangedEvent("AnalysisProgress", null, null);
                }
            }
        }

        public double MigrationProgress
        {
            get
            {
                return m_migrationProgress;
            }
            set
            {
                if (m_migrationProgress != value)
                {
                    m_migrationProgress = value;
                    RaisePropertyChangedEvent("MigrationProgress", null, null);
                }
            }
        }

        public string CurrentProgressHint
        {
            get
            {
                return m_currentProgressHint;
            }
            set
            {
                if (m_currentProgressHint == null || !m_currentProgressHint.Equals(value))
                {
                    m_currentProgressHint = value;
                    RaisePropertyChangedEvent("CurrentProgressHint", null, null);
                }
            }
        }

        public RTChangeGroup CurrentChangeGroup
        {
            get
            {
                return m_currentChangeGroup;
            }
            set
            {
                m_currentChangeGroup = value;
                RaisePropertyChangedEvent("CurrentChangeGroup", null, null);
            }
        }
    }
}
