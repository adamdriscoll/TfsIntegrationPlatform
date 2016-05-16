// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Shell.View;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.ConflictManagement
{
    public class ApplicationViewModel : INotifyPropertyChanged
    {
        public bool CanSetSessionGroupUniqueId
        {
            get
            {
                return !IsActiveConflictsBWBusy;
            }
        }

        public bool CanGetMoreRuntimeConflicts
        {
            get
            {
                return !IsRuntimeConflictsBWBusy;
            }
        }

        private bool m_isRuntimeConflictsBWBusy = false;
        public bool IsRuntimeConflictsBWBusy
        {
            get
            {
                return m_isRuntimeConflictsBWBusy;
            }
            private set
            {
                m_isRuntimeConflictsBWBusy = value;
                OnPropertyChanged("IsRuntimeConflictsBWBusy");
                OnPropertyChanged("CanGetMoreRuntimeConflicts");
            }
        }

        private bool m_isActiveConflictsBWBusy = false;
        public bool IsActiveConflictsBWBusy
        {
            get
            {
                return m_isActiveConflictsBWBusy;
            }
            private set
            {
                m_isActiveConflictsBWBusy = value;
                OnPropertyChanged("IsActiveConflictsBWBusy");
                OnPropertyChanged("CanSetSessionGroupUniqueId");
            }
        }

        private bool m_isRulesBWBusy = false;
        public bool IsRulesBWBusy
        {
            get
            {
                return m_isRulesBWBusy;
            }
            private set
            {
                m_isRulesBWBusy = value;
                OnPropertyChanged("IsRulesBWBusy");
            }
        }

        private int m_runtimeConflictCount;
        public int RuntimeConflictCount
        {
            get
            {
                return m_runtimeConflictCount;
            }
            private set
            {
                if (m_runtimeConflictCount != value)
                {
                    m_runtimeConflictCount = value;
                    OnPropertyChanged("RuntimeConflictCount");
                    OnPropertyChanged("TotalConflicts");
                    if (ShellViewModel != null)
                    {
                        ShellViewModel.NotificationBarViewModel.RefreshDefaultNotification();
                    }
                }
            }
        }

        public ApplicationViewModel()
            : this(null)
        {
        }

        public ExtensibilityViewModel ExtensibilityViewModel
        {
            get
            {
                if (ShellViewModel == null)
                {
                    return null;
                }
                else
                {
                    return ShellViewModel.ExtensibilityViewModel;
                }
            }
        }

        public ShellViewModel ShellViewModel {get; set;}

        public ExistingRuleListView ExistingRuleListView { get; set; }

        public ApplicationViewModel(ShellViewModel shellViewModel)
        {
            ShellViewModel = shellViewModel;
            
            activeConflictsBW = new BackgroundWorker();
            activeConflictsBW.DoWork += new DoWorkEventHandler(activeConflictsBW_DoWork);
            activeConflictsBW.RunWorkerCompleted += new RunWorkerCompletedEventHandler(activeConflictsBW_RunWorkerCompleted);

            activeConflictsWithRuntimeBW = new BackgroundWorker();
            activeConflictsWithRuntimeBW.DoWork += new DoWorkEventHandler(activeConflictsWithRuntimeBW_DoWork);
            activeConflictsWithRuntimeBW.RunWorkerCompleted += new RunWorkerCompletedEventHandler(activeConflictsWithRuntimeBW_RunWorkerCompleted);

            m_rulesBW = new BackgroundWorker();
            m_rulesBW.DoWork += new DoWorkEventHandler(m_rulesBW_DoWork);
            m_rulesBW.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_rulesBW_RunWorkerCompleted);
        }

        void m_rulesBW_DoWork(object sender, DoWorkEventArgs e)
        {
            IsRulesBWBusy = true;

            IEnumerable<ExistingRuleViewModel> rules = GetRules(SessionGroupUniqueId);
            e.Result = rules;
        }

        void m_rulesBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IEnumerable<ExistingRuleViewModel> rules = (IEnumerable<ExistingRuleViewModel>)e.Result;
            m_rules.Clear();
            foreach (var rule in rules)
            {
                m_rules.Add(rule);
            }

            IsRulesBWBusy = false;
        }

        void activeConflictsBW_DoWork(object sender, DoWorkEventArgs e)
        {
            IsActiveConflictsBWBusy = true;
            ConflictChanges conflictChanges = GetConflictChanges();
            e.Result = conflictChanges;
        }

        void activeConflictsBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                ConflictChanges conflictChanges = (ConflictChanges)e.Result;

                foreach (RTConflict conflict in conflictChanges.NewConflicts)
                {
                    m_allActiveConflicts.Add(new ConflictRuleViewModel(conflict, this));
                }

                foreach (int resolvedConflictId in conflictChanges.ResolvedConflictIds)
                {
                    IEnumerable<ConflictRuleViewModel> conflictModelsToRemove = m_allActiveConflicts.Where(x => x.ConflictInternalId == resolvedConflictId);
                    // Create a separate list of items to remove because you can't remove while enumerating over m_allActiveConflicts 
                    List<ConflictRuleViewModel> conflictModelsToRemoveList = new List<ConflictRuleViewModel>();
                    foreach (ConflictRuleViewModel conflictModelToRemove in conflictModelsToRemove)
                    {
                        conflictModelsToRemoveList.Add(conflictModelToRemove);
                    }
                    foreach (ConflictRuleViewModel conflictModelToRemove in conflictModelsToRemoveList)
                    {
                        m_allActiveConflicts.Remove(conflictModelToRemove);
                    }
                }
            }

            RefreshFilteredConflicts();
            if (ShellViewModel != null)
            {
                ShellViewModel.NotificationBarViewModel.RefreshDefaultNotification();
            }
            IsActiveConflictsBWBusy = false;
        }

        void activeConflictsWithRuntimeBW_DoWork(object sender, DoWorkEventArgs e)
        {
            IsRuntimeConflictsBWBusy = true;

            List<ConflictRuleViewModel> conflicts = new List<ConflictRuleViewModel>();
            IQueryable<RTConflict> conflictsQuery = GetPreviousRuntimeConflicts();
            foreach (RTConflict conflict in (conflictsQuery).Take(500))
            {
                conflicts.Add(new ConflictRuleViewModel(conflict, this));
            }
            e.Result = conflicts;
        }

        void activeConflictsWithRuntimeBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IEnumerable<ConflictRuleViewModel> conflicts = (IEnumerable<ConflictRuleViewModel>)e.Result;
            foreach (var conflict in conflicts)
            {
                m_allActiveConflictsWithRuntime.Add(conflict);
            }
            if (ShellViewModel != null)
            {
                ShellViewModel.NotificationBarViewModel.RefreshDefaultNotification();
            }
            IsRuntimeConflictsBWBusy = false;
        }

        public int TotalConflicts
        {
            get
            {
                if (m_allActiveConflicts == null)
                {
                    return 0;
                }
                else
                {
                    return m_allActiveConflicts.Where(x => x.IsResolved != ResolvedStatus.Resolved).Count() + RuntimeConflictCount;
                }
            }
        }

        private ObservableCollection<LightWeightSource> m_migrationSources;
        public ObservableCollection<LightWeightSource> MigrationSources
        {
            get
            {
                if (m_migrationSources == null)
                {
                    m_migrationSources = new ObservableCollection<LightWeightSource>();
                }
                if (m_migrationSources.Count == 0)
                {
                    m_migrationSources.Add(new LightWeightSource());
                    SelectedMigrationSource = m_migrationSources.First();
                }
                return m_migrationSources;
            }
        }

        private LightWeightSource m_selectedMigrationSource;
        public LightWeightSource SelectedMigrationSource
        {
            get
            {
                if (m_selectedMigrationSource == null)
                {
                    m_selectedMigrationSource = MigrationSources.First();
                }
                return m_selectedMigrationSource;
            }
            set
            {
                m_selectedMigrationSource = value;
                OnPropertyChanged("SelectedMigrationSource");
                RefreshFilteredConflicts();
            }
        }

        private Guid m_sessionGroupUniqueId;
        public Guid SessionGroupUniqueId
        {
            get
            {
                return m_sessionGroupUniqueId;
            }
            private set
            {
                m_sessionGroupUniqueId = value;
                ConflictRuleViewModel.s_otherMigrationSourceLookup = null;
            }
        }

        private void RefreshConflicts()
        {
            if (m_allActiveConflicts == null)
            {
                m_allActiveConflicts = new List<ConflictRuleViewModel>();
            }
            if (!activeConflictsBW.IsBusy && !IsBusy && Sync != null)
            {
                activeConflictsBW.RunWorkerAsync();
            }

            RefreshFilteredConflicts();
        }

        private void RefreshFilteredConflicts()
        {
            if (m_allActiveConflicts != null)
            {
                OnPropertyChanged("TotalConflicts");
                if (ShellViewModel != null)
                {
                    ShellViewModel.NotificationBarViewModel.RefreshDefaultNotification();
                }
                m_filteredConflicts = (from c in m_allActiveConflicts
                                       where (SelectedMigrationSource.UniqueId.Equals(Guid.Empty) || c.SourceId.Equals(SelectedMigrationSource.UniqueId))
                                       select c).ToList();

                OnPropertyChanged("FilteredConflicts");
            }
        }

        public IEnumerable<ConflictRuleViewModel> AllConflicts
        {
            get
            {
                return m_allActiveConflicts;
            }
        }

        private IList<ConflictRuleViewModel> m_filteredConflicts;
        public IList<ConflictRuleViewModel> FilteredConflicts
        {
            get
            {
                return m_filteredConflicts;
            }
        }

        private ConflictRuleViewModel m_currentRule;
        public ConflictRuleViewModel CurrentRule
        {
            get
            {
                return m_currentRule;
            }
            set
            {
                m_currentRule = value;
                OnPropertyChanged("CurrentRule");
            }
        }

        private ObservableCollection<ExistingRuleViewModel> m_rules;
        private BackgroundWorker m_rulesBW;
        public IList<ExistingRuleViewModel> Rules
        {
            get
            {
                if (m_rules == null)
                {
                    m_rules = new ObservableCollection<ExistingRuleViewModel>();
                    m_rulesBW.RunWorkerAsync();
                }
                return m_rules;
            }
        }

        private List<ConflictRuleViewModel> m_allActiveConflicts;
        private ObservableCollection<ConflictRuleViewModel> m_allActiveConflictsWithRuntime;

        public void Refresh() // TODO: make async
        {
            if (SessionGroupUniqueId != null && !SessionGroupUniqueId.Equals(Guid.Empty))
            {
                if (!activeConflictsBW.IsBusy)
                {
                    RefreshConflicts();
                }
                RuntimeConflictCount = GetRuntimeConflictCount();
            }
        }

        public Configuration Config { get; private set; }
        public SyncOrchestrator Sync { get; private set; }

        private BackgroundWorker m_constructPipelinesBW;

        public void SetSessionGroupUniqueId(Guid sessionGroupUniqueId, bool constructPipelines)
        {
            try
            {
                m_allActiveConflicts = null;
                m_rules = null;
                CurrentRule = null;

                SessionGroupUniqueId = sessionGroupUniqueId;

                if (ShellViewModel != null)
                {
                    Config = ShellViewModel.DataModel.Configuration;
                }
                else // only used for automated testing
                {
                    BusinessModelManager businessModelManager = new BusinessModelManager();
                    Config = businessModelManager.LoadConfiguration(sessionGroupUniqueId);
                }
                if (Config != null)
                {
                    if (m_constructPipelinesBW == null)
                    {
                        m_constructPipelinesBW = new BackgroundWorker();
                        m_constructPipelinesBW.DoWork += new DoWorkEventHandler(m_constructPipelinesBW_DoWork);
                        m_constructPipelinesBW.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_constructPipelinesBW_RunWorkerCompleted);
                    }

                    if (!m_constructPipelinesBW.IsBusy)
                    {
                        IsBusy = true;
                        m_constructPipelinesBW.RunWorkerAsync(constructPipelines);
                    }
                }
            }
            catch (Exception e)
            {
                SessionGroupUniqueId = Guid.Empty;
                Utilities.HandleException(e);
            }
        }

        private bool m_isBusy = false;

        public bool IsBusy
        {
            get
            {
                return m_isBusy;
            }
            private set
            {
                if (m_isBusy != value)
                {
                    m_isBusy = value;
                    OnPropertyChanged("IsBusy");
                    if (!m_isBusy)
                    {
                        if (ShellViewModel != null)
                        {
                            ShellViewModel.NotificationBarViewModel.RefreshDefaultNotification();
                        }
                    }
                }
            }
        }

        private Guid m_constructedPipelineId;
        private void m_constructPipelinesBW_DoWork(object sender, DoWorkEventArgs e)
        {
            bool constructPipelines = (bool)e.Argument;
            // 3 ways to get here:
            // 1. open existing config
            // 2. start existing config
            // 3. automated testing
            if (constructPipelines && !m_constructedPipelineId.Equals(Config.SessionGroupUniqueId))
            {
                // 3 ways to get here:
                // 1. open existing config that has been run before
                // 2. start existing config for the first time
                // 3. automated testing
                try
                {
                    Sync = new SyncOrchestrator(Config);
                    Sync.ConstructPipelines();

                    if (ShellViewModel != null)
                    {
                        ShellViewModel.LoadConflictTypes();
                    }
                    m_constructedPipelineId = Config.SessionGroupUniqueId;
                }
                catch (Exception ex)
                {
                    e.Result = (object)ex;
                }
            }
        }

        private void m_constructPipelinesBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsBusy = false;
            if (e.Result != null)
            {
                Exception ex = e.Result as Exception;
                Utilities.HandleException(ex);
            }

            MigrationSources.Clear();
            foreach (var v in Config.SessionGroup.MigrationSources.MigrationSource)
            {
                MigrationSources.Add(new LightWeightSource(v));
            }

            if (Sync != null)
            {
                RefreshConflicts();
            }
        }

        private BackgroundWorker activeConflictsBW;
        private BackgroundWorker activeConflictsWithRuntimeBW;
        public ObservableCollection<ConflictRuleViewModel> ActiveConflictsWithRuntime
        {
            get
            {
                if (!activeConflictsWithRuntimeBW.IsBusy)
                {
                    m_allActiveConflictsWithRuntime = new ObservableCollection<ConflictRuleViewModel>();

                    activeConflictsWithRuntimeBW.RunWorkerAsync();
                }
                return m_allActiveConflictsWithRuntime;
            }
        }

        public void GetMoreRuntimeConflicts()
        {
            if (!activeConflictsWithRuntimeBW.IsBusy)
            {
                activeConflictsWithRuntimeBW.RunWorkerAsync();
            }
        }

        public Dictionary<Guid, int> GetConflictCounts()
        {
            Dictionary<Guid, int> conflictCounts = new Dictionary<Guid, int>();
            return m_allActiveConflicts.Where(x => x.IsResolved != ResolvedStatus.Resolved).GroupBy(x => x.SourceId).ToDictionary(x => x.Key, x => x.Count());
        }

        internal IQueryable<RTConflict> GetActiveConflicts(Guid sessionGroupUniqueId)
        {
            RuntimeEntityModel context = RuntimeEntityModel.CreateInstance();

            // hidden conflicts
            Guid chainOnBackloggedItemConflictTypeRefName = new ChainOnBackloggedItemConflictType().ReferenceName; // A7EFC8C6-A6CF-45e7-BFA6-471942A54F37
            Guid chainOnConflictConflictTypeRefName = Constants.chainOnConflictConflictTypeRefName; //F6BFB484-EE70-4ffc-AAB3-4F659B0CAF7F
            
            // runtime conflicts
            Guid witGeneralConflictTypeRefName = Constants.witGeneralConflictTypeRefName; //470F9617-FC96-4166-96EB-44CC2CF73A97
            Guid generalConflictTypeRefName = new GenericConflictType().ReferenceName; // F6DAB314-2792-40D9-86CC-B40F5B827D86
            
            var conflictQuery =
                from c in context.RTConflictSet
                where (c.InCollection.SessionGroupRun.Config.SessionGroup.GroupUniqueId.Equals(sessionGroupUniqueId)
                    || c.InCollection.SessionRun.SessionGroupRun.Config.SessionGroup.GroupUniqueId.Equals(sessionGroupUniqueId))
                && c.Status == 0 // only search for active conflicts 
                && !c.ConflictType.ReferenceName.Equals(chainOnBackloggedItemConflictTypeRefName)
                && !c.ConflictType.ReferenceName.Equals(chainOnConflictConflictTypeRefName)
                && !c.ConflictType.ReferenceName.Equals(witGeneralConflictTypeRefName)
                && !c.ConflictType.ReferenceName.Equals(generalConflictTypeRefName)
                select c;
            
            return conflictQuery;
        }

        private int GetRuntimeConflictCount()
        {
            return ConflictManagementServiceProxy.RuntimeErrors.GetAllActiveRuntimeConflictsCount(SessionGroupUniqueId);
        }

        private ConflictChanges GetConflictChanges()
        {
            int lastConflictId;
            if (m_allActiveConflicts == null || m_allActiveConflicts.Count == 0)
            {
                lastConflictId = 0;
            }
            else
            {
                lastConflictId = m_allActiveConflicts.Select(x => x.ConflictInternalId).Max();
            }

            // Build a Hashset with all of the existing conflict Ids
            HashSet<int> currentConflictIdsResolved = new HashSet<int>();
            foreach (ConflictRuleViewModel conflictView in m_allActiveConflicts)
            {
                if (!currentConflictIdsResolved.Contains(conflictView.ConflictInternalId))
                {
                    currentConflictIdsResolved.Add(conflictView.ConflictInternalId);
                }
            }

            ConflictChanges conflictChanges = new ConflictChanges();
            foreach (RTConflict conflict in GetActiveConflicts(SessionGroupUniqueId))
            {
                if (conflict.Id > lastConflictId)
                {
                    conflictChanges.NewConflicts.Add(conflict);
                }
                else
                {
                    if (currentConflictIdsResolved.Contains(conflict.Id))
                    {
                        // The conflict was returned by GetActiveConflicts and was already in memory, so remove it from the Resolved list       
                        currentConflictIdsResolved.Remove(conflict.Id);
                    }
                }
            }

            // Anything remaining in currentConflictIdsResolved must have been resolved because it was not returned by GetActiveConflicts
            foreach (int conflictId in currentConflictIdsResolved)
            {
                conflictChanges.ResolvedConflictIds.Add(conflictId);
            }

            return conflictChanges;
        }

        public void AcknowledgeAllRuntimeConflicts()
        {
            IEnumerable<int> resolvedConflictIds = ConflictManagementServiceProxy.RuntimeErrors.AcknowledgeAllActiveRuntimeConflicts(SessionGroupUniqueId);
            foreach (ConflictRuleViewModel runtimeConflict in m_allActiveConflictsWithRuntime)
            {
                if (resolvedConflictIds.Contains(runtimeConflict.ConflictInternalId))
                {
                    runtimeConflict.IsResolved = ResolvedStatus.Resolved;
                }
                else
                {
                    runtimeConflict.IsResolved = ResolvedStatus.Failed;
                }
            }
        }

        private IQueryable<RTConflict> GetPreviousRuntimeConflicts()
        {
            int earliestConflictId;
            if (m_allActiveConflictsWithRuntime.Count == 0)
            {
                earliestConflictId = int.MaxValue;
            }
            else
            {
                earliestConflictId = m_allActiveConflictsWithRuntime.Select(x => x.ConflictInternalId).Min();
            }

            RuntimeEntityModel context = RuntimeEntityModel.CreateInstance();

            var conflictQuery =
                from c in ConflictManagementServiceProxy.RuntimeErrors.GetAllActiveRuntimeConflicts(context, SessionGroupUniqueId)
                where c.Id < earliestConflictId
                orderby c.Id descending
                select c;

            return conflictQuery;
        }

        private List<ExistingRuleViewModel> GetRules(Guid sessionGroupUniqueId)
        {
            List<ExistingRuleViewModel> rules = new List<ExistingRuleViewModel>();

            RuntimeEntityModel context = RuntimeEntityModel.CreateInstance();
            var rulesQuery = from r in context.RTResolutionRuleSet
                        join s in context.RTSessionSet
                        on r.ScopeInfoUniqueId equals s.SessionUniqueId
                        where (r.ScopeInfoUniqueId.Equals(sessionGroupUniqueId)
                        || s.SessionGroup.GroupUniqueId.Equals(sessionGroupUniqueId))
                        && r.Status == ConflictResolutionRuleState.Valid.StorageValue
                        orderby r.CreationTime descending
                        select r;
            
            foreach (RTResolutionRule rule in rulesQuery)
            {
                rules.Add(new ExistingRuleViewModel(rule, this));
            }

            return rules;
        }

        public void RemoveSelectedRule(ExistingRuleViewModel rule)
        {
            rule.RemoveFromDB();
            Rules.Remove(rule);
        }

        public void SetResolvedConflicts(IEnumerable<ConflictResolutionResult> results, int resolvedByRuleId)
        {
            foreach (ConflictResolutionResult result in results)
            {
                ConflictRuleViewModel conflict = m_allActiveConflicts.SingleOrDefault(x => x.ConflictInternalId == result.ConflictInternalId);
                if (conflict != null)
                {
                    if (result.Resolved)
                    {
                        conflict.IsResolved = ResolvedStatus.Resolved;
                        conflict.ResolvedByRuleId = resolvedByRuleId;
                    }
                    else
                    {
                        conflict.IsResolved = ResolvedStatus.Failed;
                    }
                }
            }
        }

        internal IEnumerable<ConflictResolutionResult> ResolveConflict(ConflictRuleViewModel conflict)
        {
            IEnumerable<ConflictResolutionResult> results = conflict.Save();
            RefreshFilteredConflicts();
            return results;
        }
        
        public void ResetResolvableConflicts()
        {
            if (AllConflicts != null)
            {
                foreach (ConflictRuleViewModel conflictRule in AllConflicts)
                {
                    conflictRule.IsResolvable = false;
                }
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        #endregion

        class MigrationConflictComparer : IEqualityComparer<MigrationConflict>
        {
            #region IEqualityComparer<MigrationConflict> Members

            public bool Equals(MigrationConflict x, MigrationConflict y)
            {
                return x.ConflictType.ReferenceName.Equals(y.ConflictType.ReferenceName)
                    && x.ConflictDetails.Equals(y.ConflictDetails)
                    && x.ScopeHint.Equals(y.ScopeHint);
            }

            public int GetHashCode(MigrationConflict obj)
            {
                return obj.ConflictType.ReferenceName.GetHashCode()
                    | obj.ConflictDetails.GetHashCode()
                    | obj.ScopeHint.GetHashCode();
            }

            #endregion
        }

        internal void ClearResolvedConflicts()
        {
            // TODO: ObservableCollection
            m_allActiveConflicts.RemoveAll(x=>x.IsResolved == ResolvedStatus.Resolved);
            RefreshFilteredConflicts();
        }
    }
}
