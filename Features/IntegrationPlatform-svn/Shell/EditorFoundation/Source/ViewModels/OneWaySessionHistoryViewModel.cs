// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Threading;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Shell.Model;

namespace Microsoft.TeamFoundation.Migration.Shell.ViewModel
{
    public class OneWaySessionHistoryViewModel : ModelObject
    {
        private BackgroundWorker m_worker;
        private OneWaySessionViewModel m_oneWaySession;
        private bool m_isIndeterminate = false;
        public bool IsIndeterminate
        {
            get
            {
                return m_isIndeterminate;
            }
            set
            {
                m_isIndeterminate = value;
                RaisePropertyChangedEvent("IsIndeterminate", null, null);
            }
        }

        private RuntimeEntityModel m_context;
        private Dispatcher m_dispatcher;
        public OneWaySessionHistoryViewModel(OneWaySessionViewModel oneWaySession)
        {
            m_context = RuntimeEntityModel.CreateInstance();

            m_dispatcher = Dispatcher.CurrentDispatcher;
            m_oneWaySession = oneWaySession;

            m_worker = new BackgroundWorker();
            m_worker.WorkerReportsProgress = true;

            m_worker.DoWork += new DoWorkEventHandler(m_worker_DoWork);
            m_worker.ProgressChanged += new ProgressChangedEventHandler(m_worker_ProgressChanged);
            m_worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_worker_RunWorkerCompleted);

            RuntimeManager runtimeManager = RuntimeManager.GetInstance();
            IRefreshService refresh = (IRefreshService)runtimeManager.GetService(typeof(IRefreshService));
            refresh.AutoRefresh += this.AutoRefresh;
        }

        void m_worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            IsIndeterminate = e.ProgressPercentage < 0;
            LoadProgress = e.ProgressPercentage;
        }

        void m_worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                bool tryToRefresh = (bool)e.Result;
                if (tryToRefresh)
                {
                    Refresh();
                }
                RaisePropertyChangedEvent("CanGetNext", null, null);
            }
            else
            {
                Utilities.HandleException(e.Error);
            }
        }

        void m_worker_DoWork(object sender, DoWorkEventArgs e)
        {
            // migration instruction status
            int analysisMIStatus = (int)ChangeStatus.AnalysisMigrationInstruction;
            int pendingStatus = (int)ChangeStatus.Pending;
            int inProgressStatus = (int)ChangeStatus.InProgress;
            int pendingConflictDetectStatus = (int)ChangeStatus.PendingConflictDetection;
            int completedStatus = (int)ChangeStatus.Complete;

            RaisePropertyChangedEvent("CanGetNext", null, null);
            int numToTake = (int)e.Argument;

            m_worker.ReportProgress(-1);

            long lastChangeGroupTaken;
            if (numToTake > 0)
            {
                lastChangeGroupTaken = ChangeGroups.Select(x => x.MigrationInstructions.Id).LastOrDefault();
            }
            else
            {
                lastChangeGroupTaken = ChangeGroups.Select(x => x.MigrationInstructions.Id).FirstOrDefault();
            }

            IQueryable<RTChangeGroup> migrationInstructionsQuery;
            if (lastChangeGroupTaken == 0) // haven't gotten anything yet
            {
                long pendingChangeGroup = (from cg in m_context.RTChangeGroupSet
                                           where cg.SourceUniqueId.Equals(m_oneWaySession.Target.UniqueId)
                                           && cg.SessionUniqueId.Equals(m_oneWaySession.Session.SessionUniqueId)
                                           && (cg.Status == (int)ChangeStatus.Pending || cg.Status == (int)ChangeStatus.InProgress)
                                           && !cg.ContainsBackloggedAction
                                           orderby cg.Id ascending
                                           select cg.Id).FirstOrDefault();
                int lastSessionRunId = 0;
                if (pendingChangeGroup == 0)
                {
                    lastSessionRunId = (from cg in m_context.RTChangeGroupSet
                                        where cg.SourceUniqueId.Equals(m_oneWaySession.Target.UniqueId)
                                        && cg.SessionUniqueId.Equals(m_oneWaySession.Session.SessionUniqueId)
                                        && (cg.Status == (int)ChangeStatus.Pending || cg.Status == (int)ChangeStatus.InProgress)
                                        && !cg.ContainsBackloggedAction
                                        select cg.SessionRun.Id).FirstOrDefault();

                    if (lastSessionRunId == 0)
                    {
                        var lastSessionRunQuery = (from cg in m_context.RTChangeGroupSet
                                                   where cg.SessionUniqueId.Equals(m_oneWaySession.Session.SessionUniqueId)
                                                   && cg.SourceUniqueId.Equals(m_oneWaySession.Source.UniqueId)
                                                   orderby cg.SessionRun.Id descending
                                                   select cg.SessionRun.Id).Take(Math.Abs(numToTake));
                        if (lastSessionRunQuery.Count() != 0)
                        {
                            lastSessionRunId = lastSessionRunQuery.Min();
                        }
                    }
                }

                migrationInstructionsQuery = from cg in m_context.RTChangeGroupSet
                                             where cg.SourceUniqueId.Equals(m_oneWaySession.Target.UniqueId)
                                             && cg.SessionUniqueId.Equals(m_oneWaySession.Session.SessionUniqueId)
                                             && (cg.Status == analysisMIStatus || cg.Status == pendingStatus
                                                 || cg.Status == inProgressStatus || cg.Status == pendingConflictDetectStatus
                                                 || cg.Status == completedStatus)
                                             && cg.Id >= pendingChangeGroup
                                             && cg.SessionRun.Id >= lastSessionRunId
                                             select cg;
            }
            else if (numToTake > 0)
            {
                migrationInstructionsQuery = from cg in m_context.RTChangeGroupSet
                                             where cg.SourceUniqueId.Equals(m_oneWaySession.Target.UniqueId)
                                             && cg.SessionUniqueId.Equals(m_oneWaySession.Session.SessionUniqueId)
                                             && (cg.Status == analysisMIStatus || cg.Status == pendingStatus
                                                 || cg.Status == inProgressStatus || cg.Status == pendingConflictDetectStatus
                                                 || cg.Status == completedStatus)
                                             && cg.Id > lastChangeGroupTaken
                                             orderby cg.Id ascending
                                             select cg;
            }
            else
            {
                migrationInstructionsQuery = from cg in m_context.RTChangeGroupSet
                                             where cg.SourceUniqueId.Equals(m_oneWaySession.Target.UniqueId)
                                             && cg.SessionUniqueId.Equals(m_oneWaySession.Session.SessionUniqueId)
                                             && (cg.Status == analysisMIStatus || cg.Status == pendingStatus
                                                 || cg.Status == inProgressStatus || cg.Status == pendingConflictDetectStatus
                                                 || cg.Status == completedStatus)
                                             && cg.Id < lastChangeGroupTaken
                                             orderby cg.Id descending
                                             select cg;
            }

            int migrationInstructionsCount = migrationInstructionsQuery.Count();

            var migrationInstructionsSubset = migrationInstructionsQuery.Take(Math.Abs(numToTake));

            int total;
            if (migrationInstructionsCount < Math.Abs(numToTake))
            {
                total = migrationInstructionsCount;
                e.Result = false; // do not refresh afterwards
            }
            else
            {
                total = numToTake;
                e.Result = true; // try to refresh afterwards
            }

            if (numToTake > 0)
            {
                // dt means deltaTables
                var pairs = from mi in migrationInstructionsSubset
                            join dt in m_context.RTChangeGroupSet
                            on mi.ReflectedChangeGroupId equals dt.Id
                            join ca in m_context.RTChangeActionSet
                            on dt.Id equals ca.ChangeGroupId into j
                            orderby dt.Id ascending
                            select new
                            {
                                Left = dt,
                                Right = mi,
                                Count = j.Count()
                            };

                int currentProgress = 0;
                int counter = 0;
                foreach (var entry in pairs)
                {
                    DualChangeGroupViewModel changeGroup = new DualChangeGroupViewModel(entry.Left, entry.Right, entry.Count);
                    if (changeGroup.Bucket > maxBucket)
                    {
                        maxBucket = changeGroup.Bucket;
                        if (ChangeGroups.Count == 0)
                        {
                            minBucket = maxBucket;
                        }
                        else
                        {
                            if (counter > 10)
                            {
                                changeGroup.IsMilestone = true;
                                counter = 0;
                            }
                        }
                    }
                    counter++;
                    m_dispatcher.BeginInvoke(new UpdateProgressDelegate(delegate { ChangeGroups.AddLast(changeGroup); }), changeGroup);
                    currentProgress++;
                    m_worker.ReportProgress((int)((double)currentProgress / (double)total * 100));
                }
            }
            else
            {
                // dt means deltaTables
                var pairs = from mi in migrationInstructionsSubset
                            join dt in m_context.RTChangeGroupSet
                            on mi.ReflectedChangeGroupId equals dt.Id
                            join ca in m_context.RTChangeActionSet
                            on dt.Id equals ca.ChangeGroupId into j
                            orderby dt.Id descending
                            select new
                            {
                                Left = dt,
                                Right = mi,
                                Count = j.Count()
                            };

                int currentProgress = 0;
                DualChangeGroupViewModel lastChangeGroup = null;
                int counter = 0;
                foreach (var entry in pairs)
                {
                    DualChangeGroupViewModel changeGroup = new DualChangeGroupViewModel(entry.Left, entry.Right, entry.Count);
                    if (changeGroup.Bucket != 0 && changeGroup.Bucket < minBucket)
                    {
                        minBucket = changeGroup.Bucket;
                        if (lastChangeGroup != null)
                        {
                            if (counter > 10)
                            {
                                lastChangeGroup.IsMilestone = true;
                                counter = 0;
                            }
                        }
                    }
                    counter++;
                    m_dispatcher.BeginInvoke(new UpdateProgressDelegate(delegate { ChangeGroups.AddFirst(changeGroup); }), changeGroup);
                    currentProgress++;
                    m_worker.ReportProgress((int)((double)currentProgress / (double)total * 100));

                    lastChangeGroup = changeGroup;
                }
            }
            m_worker.ReportProgress(100);
        }

        public void AutoRefresh(object sender, EventArgs e)
        {
            Refresh();
        }

        private int maxBucket = 0;
        private int minBucket = int.MaxValue;

        public delegate void UpdateProgressDelegate(DualChangeGroupViewModel changeGroup);

        public int Width { get; set; }

        private int m_loadProgress = 100;
        public int LoadProgress
        {
            get
            {
                return m_loadProgress;
            }
            private set
            {
                m_loadProgress = value;
                RaisePropertyChangedEvent("LoadProgress", null, null);
            }
        }

        public void Refresh()
        {
            if (!m_worker.IsBusy)
            {
                int numToTake = Width / 2 - ChangeGroups.Count;
                if (numToTake > 0)
                {
                    m_worker.RunWorkerAsync(numToTake);
                }
            }
        }

        public bool CanGetNext
        {
            get
            {
                return !m_worker.IsBusy && ChangeGroups.Count != 0;
            }
        }

        public void GetNext()
        {
            if (CanGetNext)
            {
                int numToTake = Width / 4;
                m_worker.RunWorkerAsync(numToTake);
            }
        }

        public void GetPrevious()
        {
            if (CanGetNext)
            {
                int numToTake = Width / 4;
                m_worker.RunWorkerAsync(-numToTake);
            }
        }

        private ObservableLinkedList<DualChangeGroupViewModel> m_changeGroups;
        public ObservableLinkedList<DualChangeGroupViewModel> ChangeGroups
        {
            get
            {
                if (m_changeGroups == null)
                {
                    m_changeGroups = new ObservableLinkedList<DualChangeGroupViewModel>();
                    m_changeGroups.CollectionChanged += new NotifyCollectionChangedEventHandler(m_changeGroups_CollectionChanged);
                }
                return m_changeGroups;
            }
        }

        void m_changeGroups_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (var v in e.NewItems)
                {
                    DualChangeGroupViewModel changeGroup = v as DualChangeGroupViewModel;
                    if (changeGroup.ChangeGroupHeight > MaxHeight)
                    {
                        MaxHeight = changeGroup.ChangeGroupHeight;
                    }
                }
            }
        }

        private double m_maxHeight = 20;
        public double MaxHeight
        {
            get
            {
                return m_maxHeight;
            }
            set
            {
                if (m_maxHeight != value)
                {
                    m_maxHeight = value;
                    RaisePropertyChangedEvent("MaxHeight", null, null);
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
    }

    public class ObservableLinkedList<T> : LinkedList<T>, INotifyCollectionChanged
    {
        public new LinkedListNode<T> AddFirst(T value)
        {
            LinkedListNode<T> addedNode = base.AddFirst(value);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value, 0));
            return addedNode;
        }

        public new LinkedListNode<T> AddLast(T value)
        {
            LinkedListNode<T> addedNode = base.AddLast(value);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value));
            return addedNode;
        }

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, e);
            }
        }

        #endregion
    }
}
