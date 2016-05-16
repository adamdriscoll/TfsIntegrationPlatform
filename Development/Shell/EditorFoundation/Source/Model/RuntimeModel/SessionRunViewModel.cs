// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Objects;
using System.Linq;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Shell.ViewModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    public class SessionRunViewModel : ModelObject
    {
        private RTSessionRun m_sessionRun;
        private RuntimeManager m_host;

        public SessionRunViewModel(RTSessionRun sessionRun)
        {
            m_sessionRun = sessionRun;

            m_host = RuntimeManager.GetInstance();
            IRefreshService refresh = (IRefreshService)m_host.GetService(typeof(IRefreshService));
            refresh.AutoRefresh += this.AutoRefresh;

            // Avoid race by driving through top level refresh instead of relying upon background thread.
            Refresh();
        }

        #region Properties
        // Selected RTSessionRun properties
        public int Id { get { return m_sessionRun.Id; } }
        public DateTime? StartTime { get { return m_sessionRun.StartTime; } }
        public DateTime? FinishTime { get { return m_sessionRun.FinishTime; } }
        public bool IsPreview { get { return m_sessionRun.IsPreview; } }
        public string LeftHighWaterMark { get { return m_sessionRun.LeftHighWaterMark; } }
        public string RightHighWaterMark { get { return m_sessionRun.RightHighWaterMark; } }
        public int? State { get { return m_sessionRun.State; } }

        // Collections and relationships
        private SessionConfigViewModel m_sessionConfig;

        public SessionConfigViewModel SessionConfig
        {
            get
            {
                if (!m_sessionRun.ConfigReference.IsLoaded)
                {
                    m_sessionRun.ConfigReference.Load();
                }

                if (m_sessionConfig == null)
                {
                    m_sessionConfig = new SessionConfigViewModel(m_sessionRun.Config);
                }

                return m_sessionConfig;
            }
        }

        private ObservableCollection<ChangeGroupViewModel> m_changeGroups;

        public ObservableCollection<ChangeGroupViewModel> ChangeGroupsInScope
        {
            get
            {
                return m_changeGroups;
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
        #endregion

        // View related

        // ChangeStatus is a case where the ViewModel could extend the underlying storage
        // primitives to include notions like All and None.

        private ObservableCollection<string> m_changeStatusScope;
        private ChangeStatus m_selectedChangeStatusScope = ChangeStatus.DeltaPending;

        // Surface a list of choices that might appear in a combo box
        public ObservableCollection<string> ChangeStatusScope
        {
            get
            {
                if (m_changeStatusScope == null)
                {
                    // Explicit value list because Enum.GetNames fetches values in value order.
                    m_changeStatusScope = new ObservableCollection<string>();
                    m_changeStatusScope.Add(Enum.GetName(typeof(ChangeStatus), ChangeStatus.Delta));
                    m_changeStatusScope.Add(Enum.GetName(typeof(ChangeStatus), ChangeStatus.DeltaPending));
                    m_changeStatusScope.Add(Enum.GetName(typeof(ChangeStatus), ChangeStatus.PendingConflictDetection));
                    m_changeStatusScope.Add(Enum.GetName(typeof(ChangeStatus), ChangeStatus.Pending));
                    m_changeStatusScope.Add(Enum.GetName(typeof(ChangeStatus), ChangeStatus.InProgress));
                    m_changeStatusScope.Add(Enum.GetName(typeof(ChangeStatus), ChangeStatus.Complete));
                    m_changeStatusScope.Add(Enum.GetName(typeof(ChangeStatus), ChangeStatus.Skipped));
                    m_changeStatusScope.Add(Enum.GetName(typeof(ChangeStatus), ChangeStatus.DeltaComplete));
                    m_changeStatusScope.Add(Enum.GetName(typeof(ChangeStatus), ChangeStatus.DeltaSynced));
                    m_changeStatusScope.Add(Enum.GetName(typeof(ChangeStatus), ChangeStatus.Unintialized));
                }
                return m_changeStatusScope;
            }
        }

        // Latch the active choice
        public string SelectedChangeStatusScope
        {
            get
            {
                return Enum.GetName(typeof(ChangeStatus), m_selectedChangeStatusScope);
            }
            set
            {
                m_selectedChangeStatusScope = (ChangeStatus)Enum.Parse(typeof(ChangeStatus), (string)value);

                // TODO: Finish, though we are the only setter in UI at the moment
                //
                //    RaisePropertyChangedEvent("SelectedChangeStatusScope", <Event string form>...)

                // Force a refresh
                Refresh();
            }
        }

        /// <summary>
        /// A collection of pipeline stage name and count pairs.
        /// </summary>
        private ObservableCollection<KeyValuePair<string, int>> m_pipelineStageTotals;

        public ObservableCollection<KeyValuePair<string, int>> PipelineStageTotals
        {
            get
            {
                return m_pipelineStageTotals;
            }
        }

        /// <summary>
        /// A collection of conflict type ids and conflict type name pairs.
        /// </summary>
        private ObservableCollection<ConflictTypeViewModel> m_conflictTypes;
        private ConflictTypeViewModel m_selectedConflictType;

        public ObservableCollection<ConflictTypeViewModel> ConflictTypes
        {
            get
            {
                return m_conflictTypes;
            }
        }

        // Latch the active choice
        public ConflictTypeViewModel SelectedConflictType
        {
            get
            {
                return m_selectedConflictType;
            }
            set
            {
                m_selectedConflictType = value;
                // TODO: Raise property changed event

                //Refresh();
            }
        }

        /// <summary>
        /// Conflict type totals keyed by conflict type friendly name.
        /// </summary>
        private ObservableCollection<KeyValuePair<string, int>> m_conflictTypeTotals;

        public ObservableCollection<KeyValuePair<string, int>> ConflictTypeTotals
        {
            get
            {
                return m_conflictTypeTotals;
            }
        }

        // Dynamic view model update support
        public void AutoRefresh(object sender, EventArgs e)
        {
            Refresh();
        }

        private void Refresh()
        {
            RefreshChangeGroups();
            RefreshPipelineStageTotals();
            // TODO: Verify query health under SQL profiler
            // TODO: Consider refresh only when conflict collection has been loaded 
            // rather than on every sync interval
            RefreshConflictCollection();
            RefreshConflictTypesCollection();
        }

        private void RefreshChangeGroups()
        {
            if (m_changeGroups == null)
            {
                m_changeGroups = new ObservableCollection<ChangeGroupViewModel>();
            }

            // The default of MergeOption.AppendOnly does show new rows added to the DB
            // and cuts down on the creation and collection of EF objects in memory.  
            // OverwriteChanges might be a better choice if we end up with views that
            // show stale and changing property values, but the overhead of setting up
            // listeners for property change events and managing the whole structure does 
            // not seem worth the effort for a collection that changes so rapidly.
            ObjectQuery<RTChangeGroup> changeGroupsObjectQuery = m_sessionRun.ChangeGroups.CreateSourceQuery();
            changeGroupsObjectQuery.MergeOption = MergeOption.OverwriteChanges;

            var query = (from cg in changeGroupsObjectQuery
                         //where cg.Status == selectedStatus
                         orderby cg.Id descending
                         select cg).Take(m_host.MaxQueryResults);
            
            // Compute ChangeAction counts for the new ChangeGroups loaded into the view model
            // and prime the ChangeActionCount for the view on the ChangeGroup.  This approach
            // allows a single count based SQL statement for a collection of ChangeGroups to 
            // be issued in place of N queries to load all ChangeAction collections just to 
            // count them.
            //
            // Note that this query over the previous result set is not guaranteed to be
            // working on exactly the same top N items.  They are close enough in time
            // that the optimization works, but it is possible that a ChangeGroup will 
            // have to be asked to explicitly refresh the count if we don't get it here.

            var actions = (from cg in query
                           join ca in m_host.Context.RTChangeActionSet on cg.Id equals ca.ChangeGroupId into j
                           select new
                           {
                               Key = cg.Id,
                               Count = j.Count()
                           });

            Dictionary<long, int> actionTotals = actions.ToDictionary(n => n.Key, n => n.Count);

            // TODO: Finish view element update handling
            // 
            // The code below is enough to grow the list sensibly, but not to shrink it when counts are decreasing to zero.  
            //
            //List<RTChangeGroup> results = query.ToList();
            //int trimVisibleResults = m_changeGroupsInScope.Count - results.Count;
            //
            //if (m_changeGroupsInScope.Count > 0)
            //{
            //    RTChangeGroupDescendingComparer changeGroupComparer = new RTChangeGroupDescendingComparer();
            //    int firstVisibleIndex = results.BinarySearch(m_changeGroupsInScope[0].ChangeGroup, changeGroupComparer);
            //
            //    if (firstVisibleIndex >= 0)
            //    {
            //        results.RemoveRange(firstVisibleIndex, results.Count);
            //    }
            //}
            //
            //foreach (RTChangeGroup changeGroup in results)
            //... delete Clear ...
            m_changeGroups.Clear();

            foreach (RTChangeGroup changeGroup in query)
            {
                ChangeGroupViewModel changeGroupViewModel = new ChangeGroupViewModel(changeGroup);

                int actionCount = 0;

                if (actionTotals.TryGetValue(changeGroup.Id, out actionCount))
                {
                    changeGroupViewModel.ChangeActionCount = actionCount;
                }
                else
                {
                    // This should be relatively rare.  This will happen if the group join used to acquire
                    // change action counts works on a different set of Top N than the base query.
                    changeGroupViewModel.RefreshChangeActionCount();
                }

                m_changeGroups.Add(changeGroupViewModel);
            }

            // TODO: Find a more elegant way to trim
            //for (int i = 0; i < trimVisibleResults; ++i)
            //{
            //    m_changeGroupsInScope.RemoveAt(m_changeGroupsInScope.Count - 1);
            //}
        }

        /// <summary>
        /// Count of ChangeGroups in various stages of the sync pipeline.
        /// </summary>
        private void RefreshPipelineStageTotals()
        {
            if (m_pipelineStageTotals == null)
            {
                m_pipelineStageTotals = new ObservableCollection<KeyValuePair<string, int>>();
            }
            else
            {
                // TODO: Be gentler
                m_pipelineStageTotals.Clear();
            }

            ObjectQuery<RTChangeGroup> changeGroupsObjectQuery = m_sessionRun.ChangeGroups.CreateSourceQuery();

            // Focused request for counts into anonymous type to pull limited data out of the back end

            var query = (from changeGroups in changeGroupsObjectQuery
                         group changeGroups by changeGroups.Status into g
                         select new
                         {
                             Key = g.Key,
                             Count = g.Count()
                         });

            Dictionary<int, int> pipelineTotals = query.ToDictionary(n => n.Key, n => n.Count);

            // Force value order to match ChangeStatusScope order by iterating over that collection
            foreach (string changeStatusName in ChangeStatusScope)
            {
                int changeStatusValue;

                pipelineTotals.TryGetValue((int)Enum.Parse(typeof(ChangeStatus), changeStatusName), out changeStatusValue);
                m_pipelineStageTotals.Add(new KeyValuePair<string, int>(changeStatusName, changeStatusValue));
            }
        }

        /// <summary>
        /// A MaxQueryResult sized collection of conflicts of the SelectedConflictType.
        /// </summary>
        private void RefreshConflictCollection()
        {
            if (m_conflicts == null)
            {
                m_conflicts = new ObservableCollection<ConflictViewModel>();
            }

            if (!m_sessionRun.ConflictCollectionReference.IsLoaded)
            {
                m_sessionRun.ConflictCollectionReference.Load();
            }

            ObjectQuery<RTConflict> conflicts = m_sessionRun.ConflictCollection.Conflicts.CreateSourceQuery();

            int selectedConflictType = 1;

            if (m_selectedConflictType != null)
            {
                selectedConflictType = m_selectedConflictType.Id;
            }

            var query = (from c in conflicts
                         where c.ConflictType.Id == selectedConflictType
                         orderby c.Id descending
                         select c).Take(m_host.MaxQueryResults);

            m_conflicts.Clear();

            foreach (RTConflict conflict in query)
            {
                m_conflicts.Add(new ConflictViewModel(conflict));
            }
        }

        /// <summary>
        /// The set of active ConflcitTypes and a count of conflicts of each type are
        /// refreshed in this method.
        /// </summary>
        private void RefreshConflictTypesCollection()
        {
            if (m_conflictTypes == null)
            {
                m_conflictTypes = new ObservableCollection<ConflictTypeViewModel>();
            }

            if (m_conflictTypeTotals == null)
            {
                m_conflictTypeTotals = new ObservableCollection<KeyValuePair<string, int>>();
            }
            else
            {
                // TODO: Be gentler
                m_conflictTypeTotals.Clear();
            }

            if (!m_sessionRun.ConflictCollectionReference.IsLoaded)
            {
                m_sessionRun.ConflictCollectionReference.Load();
            }

            ObjectQuery<RTConflict> conflicts = m_sessionRun.ConflictCollection.Conflicts.CreateSourceQuery();

            // Take the unique types referenced in the conflict collection associated with this element
            var typeids = (from c in conflicts
                           select c.ConflictType.Id).Distinct();

            var totals = (from c in conflicts
                          group c by c.ConflictType.Id into g
                          select new
                          {
                              Key = g.Key,
                              Count = g.Count()
                          });

            Dictionary<int, int> conflictTotals = totals.ToDictionary(n => n.Key, n => n.Count);
            // TODO: It seems like we should be able to just pick off unique ids here for the join 
            // rather than having to pull a list of distinct ids from the DB.
            //List<int> typeids = conflictTotals.Keys.ToList();

            var types = (from ct in m_host.Context.RTConflictTypeSet
                         join cti in typeids on ct.Id equals cti
                         select ct).Take(m_host.MaxQueryResults);

            Dictionary<int, ConflictTypeViewModel> updateDictionary = m_conflictTypes.ToDictionary(n => n.Id, n => n);

            foreach (RTConflictType conflictType in types)
            {
                ConflictTypeViewModel conflictTypeViewModel;

                if (!updateDictionary.TryGetValue(conflictType.Id, out conflictTypeViewModel))
                {
                    conflictTypeViewModel = new ConflictTypeViewModel(conflictType);
                    m_conflictTypes.Add(conflictTypeViewModel);
                }

                int conflictCount = 0;

                if (conflictTotals.TryGetValue(conflictType.Id, out conflictCount))
                {
                    m_conflictTypeTotals.Add(new KeyValuePair<string, int>(conflictType.FriendlyName, conflictCount));
                }
                else
                {
                    // If we are here, the totals and types queries don't quite line up in this pass.  That can
                    // happen if a new type is added after the totals are latched... but it should be rare.  Just
                    // eat it and catch it on the next sync cycle rather than forcing another round trip to the DB
                    // to fix the problem.
                }
            }
        }
    }
}
