// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using BM = Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal class MigrationEngine
    {
        Dictionary<Guid, ServiceContainer> m_serviceContainers;
        Dictionary<Guid, IMigrationProvider> m_migrationProviders;
        RuntimeSession m_session;
        ITranslationService m_translationService;
        BM.Configuration m_configuration;
        IAddinManagementService m_addinManagementService;
        Dictionary<Guid, MigrationContext> m_migrationContextsByMigrationSource = new Dictionary<Guid, MigrationContext>();

        int m_pageSize = 50;
        SyncOrchestrator.ConflictsSyncOrchOptions m_defaultConflictSyncOrchOption = SyncOrchestrator.ConflictsSyncOrchOptions.StopConflictedSessionCurrentTrip;

        IConflictAnalysisService m_basicConflictAnalysisService;
        private bool m_stopRequested = false;
        private object m_stopRequestedLock = new object();
        private bool m_stopMigrationEngineOnBasicConflict = false;

        private bool StopRequested
        {
            get
            {
                lock (m_stopRequestedLock)
                {
                    return m_stopRequested;
                }
            }
            set
            {
                lock (m_stopRequestedLock)
                {
                    m_stopRequested = value;
                }
            }
        }

        internal ErrorManager ErrorManager
        {
            get;
            set;
        }

        internal bool StopMigrationEngineOnBasicConflict
        {
            get { return m_stopMigrationEngineOnBasicConflict; }
            set { m_stopMigrationEngineOnBasicConflict = value; }
        }

        public IConflictAnalysisService BasicConflictAnalysisService
        {
            get { return m_basicConflictAnalysisService; }
            set { m_basicConflictAnalysisService = value; }
        }

        /// <summary>
        /// Constructor of MigrationEngine
        /// </summary>
        /// <param name="session"></param>
        internal MigrationEngine(RuntimeSession session, BM.Configuration configuration, IAddinManagementService addinManagementService)
        {
            m_session = session;
            m_configuration = configuration;
            m_addinManagementService = addinManagementService;

            m_pageSize = 50;

            ConstructServiceHierarchy();
        }

        internal void Initialize(int sessionRunId)
        {
            m_session.Initialize(sessionRunId);

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                RTSessionRun sessionRun = context.RTSessionRunSet.Where
                    (sr => sr.Id == m_session.InternalSessionRunId).First();
                Debug.Assert(sessionRun != null, "Cannot find session run in the Tfs Migration DB");
                sessionRun.ConflictCollectionReference.Load();

                // finishing the conflict manager initialization by assigning the current
                // ConflictCollection Id to them
                foreach (var serviceContainer in this.m_serviceContainers.Values)
                {
                    ConflictManager conflictManager = serviceContainer.GetService(typeof(ConflictManager)) as ConflictManager;
                    Debug.Assert(null != conflictManager, "null == conflictManager");
                    conflictManager.InitializePhase2(sessionRun.ConflictCollection.Id);
                }
            }

            foreach (KeyValuePair<Guid, IMigrationProvider> provider in m_migrationProviders)
            {
                try
                {
                    provider.Value.InitializeClient();
                }
                catch (Exception e)
                {
                    ConflictManager manager = m_serviceContainers[provider.Key].GetService(typeof(ConflictManager)) as ConflictManager;
                    ErrorManager.TryHandleException(e, manager);
                }
            }

        }

        public ITranslationService TranslationService
        {
            get
            {
                return m_translationService;
            }
            set
            {
                m_translationService = value;
                foreach (Guid key in m_serviceContainers.Keys)
                {
                    m_serviceContainers[key].AddService(typeof(ITranslationService), m_translationService);
                }
            }
        }

        internal ServiceContainer this[Guid sourceId]
        {
            get
            {
                if (m_serviceContainers != null && m_serviceContainers.ContainsKey(sourceId))
                {
                    return m_serviceContainers[sourceId];
                }

                return null;
            }
        }

        internal void SessionStopEventHandler(object sender, SessionControlEventArgs e)
        {
            StopRequested = true;
        }

        internal void SessionPauseEventHandler(object sender, SessionControlEventArgs e)
        {
            // not handling pause event at engine level
        }

        internal void SessionResumeEventHandler(object sender, SessionControlEventArgs e)
        {
            StopRequested = false;
        }

        /// <summary>
        /// Create migration enginer service containers for left and right source systems 
        /// and register them with parent session containers.
        /// </summary>
        private void ConstructServiceHierarchy()
        {
            m_serviceContainers = new Dictionary<Guid, ServiceContainer>(m_session.ServiceContainers.Count);
            m_migrationProviders = new Dictionary<Guid, IMigrationProvider>(m_session.ServiceContainers.Count);
            foreach (KeyValuePair<Guid, ServiceContainer> serviceContainerEntry in m_session.ServiceContainers)
            {
                m_serviceContainers.Add(serviceContainerEntry.Key, new ServiceContainer(serviceContainerEntry.Value));
                RegisterServices(serviceContainerEntry.Key);
            }
        }

        private void RegisterServices(Guid sourceId)
        {
            Debug.Assert(m_serviceContainers.ContainsKey(sourceId),
                string.Format(MigrationToolkitResources.UnknownSourceId, sourceId));

            ConflictManager conflictManagementService = new ConflictManager(sourceId);
            conflictManagementService.ScopeId = new Guid(m_session.Configuration.SessionUniqueId);
            m_serviceContainers[sourceId].AddService(typeof(ConflictManager), conflictManagementService);
            conflictManagementService.InitializePhase1(m_serviceContainers[sourceId]);

            RegisterConflictTypes(conflictManagementService);

            ICommentDecorationService commentDecorationService = new CommentDecorationService(m_session, m_serviceContainers[sourceId]);
            m_serviceContainers[sourceId].AddService(typeof(ICommentDecorationService), commentDecorationService);
        }

        private void RegisterConflictTypes(ConflictManager manager)
        {
            manager.RegisterConflictType(
                new GenericConflictType(),
                SyncOrchestrator.ConflictsSyncOrchOptions.StopConflictedSessionCurrentTrip);
        }

        /// <summary>
        /// Register an migration provider with the migration engine,
        /// 1. Intialize migration service container and services.
        /// </summary>
        /// <param name="migrationProvider"></param>
        /// <param name="sourceId"></param>
        internal void RegisterMigrationProvider(Guid sourceId, IMigrationProvider migrationProvider)
        {
            ServiceContainer serviceContainer;

            Debug.Assert(m_serviceContainers.ContainsKey(sourceId),
                string.Format(MigrationToolkitResources.UnknownSourceId, sourceId));

            if (!m_serviceContainers.TryGetValue(sourceId, out serviceContainer))
            {
                throw new MigrationException(string.Format(
                    MigrationToolkitResources.Culture,
                    MigrationToolkitResources.UnknownSourceId,
                    sourceId));
            }

            m_serviceContainers[sourceId].AddService(typeof(IMigrationProvider), migrationProvider);
            m_migrationProviders.Add(sourceId, migrationProvider);

            migrationProvider.InitializeServices(m_serviceContainers[sourceId]);

            ConflictManager conflictManagementService = m_serviceContainers[sourceId].GetService(typeof(ConflictManager)) as ConflictManager;
            Debug.Assert(conflictManagementService != null, "conflictManager == NULL");
            migrationProvider.RegisterConflictTypes(conflictManagementService);

            foreach (MigrationAddin MigrationAddin in m_addinManagementService.GetMigrationSourceMigrationAddins(sourceId))
            {
                Debug.Assert(m_serviceContainers.ContainsKey(sourceId), "No ServiceContainer found for MigrationSource with Id: " + sourceId);
                MigrationContext MigrationContext = new MigrationContext(m_serviceContainers[sourceId], migrationProvider);
                m_migrationContextsByMigrationSource.Add(sourceId, MigrationContext);
                // Just need to create one MigrationContext that can be shared by all of the Addins for a migration source since the contents are read-only
                // to the Addin
                break;
            }
        }

        /// <summary>
        /// Establish migration context (e.g. WIT metadata sync) on the "sourceId" side
        /// </summary>
        /// <param name="sourceId"></param>
        internal virtual void EstablishContext(Guid sourceId)
        {
            Debug.Assert(m_serviceContainers.ContainsKey(sourceId),
                string.Format(MigrationToolkitResources.UnknownSourceId, sourceId));

            IMigrationProvider migrationProvider;

            if (!m_migrationProviders.TryGetValue(sourceId, out migrationProvider))
            {
                throw new MigrationException(string.Format(
                    MigrationToolkitResources.Culture,
                    MigrationToolkitResources.UnknownSourceId,
                    sourceId));
            }

            Guid masterSourceId = GetMasterSourceId(sourceId);
            Debug.Assert(!masterSourceId.Equals(Guid.Empty));

            ChangeGroupService sourceSystemChangeGroupService =
                m_serviceContainers[masterSourceId].GetService(typeof(ChangeGroupService)) as ChangeGroupService;

            try
            {
                migrationProvider.EstablishContext(sourceSystemChangeGroupService);
            }
            catch (MigrationUnresolvedConflictException)
            {
                // We have already created an unresolved conflict, just return.
                return;
            }
            catch (Exception e)
            {
                ConflictManager manager = m_serviceContainers[sourceId].GetService(typeof(ConflictManager)) as ConflictManager;
                ErrorManager.TryHandleException(e, manager);
            }
        }

        private Guid GetMasterSourceId(Guid sourceId)
        {
            foreach (var s in m_serviceContainers)
            {
                if (!s.Key.Equals(sourceId))
                {
                    return s.Key;
                }
            }

            return Guid.Empty;
        }

        internal void Migrate(Guid targetSideSourceId, SessionOrchestrationPolicy orchPolicy)
        {
            try
            {
                Debug.Assert(m_serviceContainers.ContainsKey(targetSideSourceId), string.Format(MigrationToolkitResources.UnknownSourceId, targetSideSourceId));

                ChangeGroupService changegroupService = (ChangeGroupService)m_serviceContainers[targetSideSourceId].GetService(
                    typeof(ChangeGroupService));
                Debug.Assert(changegroupService != null, string.Format("Change group service on {0} is not loaded", targetSideSourceId));

                changegroupService.DemoteInProgressActionsToPending();

                int pageNumber = 0;
                IEnumerable<ChangeGroup> changeGroups = null;

                long? firstConflictedChangeGroupId = null;
                if (StopMigrationEngineOnBasicConflict)
                {
                    firstConflictedChangeGroupId = changegroupService.GetFirstConflictedChangeGroup(ChangeStatus.Pending);
                }

                do
                {
                    // NOTE: we do not increment pageNumber here, because the processed ChangeGroups are marked "Complete" and no longer
                    //       appear in the table
                    TraceManager.TraceInformation("Loading {0} ChangeGroup(s)", m_pageSize);
                    changeGroups = changegroupService.NextMigrationInstructionTablePage(pageNumber, m_pageSize, false, false);

                    foreach (ChangeGroup nextChangeGroup in changeGroups)
                    {
                        if (firstConflictedChangeGroupId.HasValue
                            && firstConflictedChangeGroupId <= nextChangeGroup.ChangeGroupId)
                        {
                            // we should not process any conflicted change group or the following ones
                            // if StopMigrationEngineOnBasicConflict is the policy
                            return;
                        }

                        //ToDo Session.OnMigratingChangeStarting(args);
                        TraceManager.TraceInformation("Processing ChangeGroup #{0}", nextChangeGroup.ChangeGroupId);
                        ProcessMigrInstructionTableEntry(nextChangeGroup, targetSideSourceId);
                        nextChangeGroup.UpdateStatus(ChangeStatus.InProgress);

                        if (NoActiveMigrationInstructionInChangeGroup(nextChangeGroup))
                        {
                            nextChangeGroup.Complete();
                            continue;
                        }

                        ConversionResult result;
                        try
                        {
                            result = m_migrationProviders[targetSideSourceId].ProcessChangeGroup(nextChangeGroup);
                        }
                        catch (MigrationUnresolvedConflictException)
                        {
                            // We have already created an unresolved conflict, just return.
                            return;
                        }
                        catch (Exception e)
                        {
                            ConflictManager manager = m_serviceContainers[targetSideSourceId].GetService(typeof(ConflictManager)) as ConflictManager;
                            ErrorManager.TryHandleException(e, manager);
                            return;
                        }

                        if (!result.ContinueProcessing)
                        {
                            return;
                        }

                        if (!string.IsNullOrEmpty(result.ChangeId))
                        {
                            FinishChangeGroupMigration(nextChangeGroup, result);
                            InvokePostChangeGroupMigrationAddins(targetSideSourceId, nextChangeGroup);
                        }
                        orchPolicy.Check();
                    }
                }
                while (changeGroups.Count() == m_pageSize);
            }
            catch (Microsoft.TeamFoundation.Migration.Toolkit.SessionOrchestrationPolicy.StopSingleTripException)
            {
                throw;
            }
            catch (Microsoft.TeamFoundation.Migration.Toolkit.SessionOrchestrationPolicy.StopSessionException)
            {
                throw;
            }
            catch (Exception e)
            {
                ConflictManager manager = m_serviceContainers[targetSideSourceId].GetService(typeof(ConflictManager)) as ConflictManager;
                ErrorManager.TryHandleException(e, manager);
            }
        }

        private bool StopUponConflict()
        {
            switch (m_defaultConflictSyncOrchOption)
            {
                case SyncOrchestrator.ConflictsSyncOrchOptions.StopAllSessionsCurrentTrip:
                case SyncOrchestrator.ConflictsSyncOrchOptions.StopConflictedSessionCurrentTrip:
                case SyncOrchestrator.ConflictsSyncOrchOptions.StopConflictedSession:
                    return true;
                case SyncOrchestrator.ConflictsSyncOrchOptions.Continue:
                    return false;
                default:
                    Debug.Assert(false, "Unknown conflict sync option.");
                    return false;
            }
        }

        private void FinishChangeGroupMigration(ChangeGroup nextChangeGroup, ConversionResult result)
        {
            // TODO: update group conversion history and mark group complete should be in one transaction
            bool changeGroupIsNotSkipped = !result.ChangeId.Equals(Constants.MigrationResultSkipChangeGroup, StringComparison.InvariantCultureIgnoreCase);
            if (changeGroupIsNotSkipped)
            {
                // HACK: Currently separate ChangeGroups are created to hold label actions
                // These need to be marked Complete, so the value below is store in the ChangeId name
                // but we don't want these in the conversion history table
                if (result.ChangeId == WellKnownContentType.VersionControlLabel.ReferenceName)
                {
                    changeGroupIsNotSkipped = false;
                }
                else
                {
                    nextChangeGroup.UpdateConversionHistory(result);
                }
            }
            nextChangeGroup.Complete();

            if (changeGroupIsNotSkipped)
            {
                // cache the conversion history in TranslactionService
                foreach (ItemConversionHistory hist in result.ItemConversionHistory)
                {
                    if (string.IsNullOrEmpty(hist.SourceItemId)
                        || string.IsNullOrEmpty(hist.TargetItemId))
                    {
                        throw new MigrationException(MigrationToolkitResources.InvalidConversionHistoryInfo);
                    }

                    string targetItemVersionStr = string.IsNullOrEmpty(hist.TargetItemVersion)
                                                   ? Constants.ChangeGroupGenericVersionNumber
                                                   : hist.TargetItemVersion;
                    TranslationService.CacheItemVersion(hist.TargetItemId, targetItemVersionStr, result.TargetSideSourceId);
                }
            }
        }

        private bool NoActiveMigrationInstructionInChangeGroup(ChangeGroup changeGroup)
        {
            foreach (MigrationAction action in changeGroup.Actions)
            {
                if (action.State == ActionState.Pending)
                {
                    return false;
                }
            }

            return true;
        }

        protected virtual void ProcessMigrInstructionTableEntry(ChangeGroup changeGroup, Guid sourceId)
        {
        }

        internal void InvokePreMigrationAddins(Guid migrationSourceId)
        {
            foreach (MigrationAddin migrationAddin in m_addinManagementService.GetMigrationSourceMigrationAddins(migrationSourceId))
            {
                Debug.Assert(m_migrationContextsByMigrationSource.ContainsKey(migrationSourceId));
                Debug.Assert(m_migrationProviders.ContainsKey(migrationSourceId));
                MigrationContext migrationContext;
                if (m_migrationContextsByMigrationSource.TryGetValue(migrationSourceId, out migrationContext))
                {
                    try
                    {
                        migrationAddin.Initialize(m_configuration);
                        migrationAddin.PreMigration(migrationContext);
                    }
                    catch (Exception e)
                    {
                        ProcessAddinException(migrationAddin, migrationSourceId, e);
                    }
                }
            }
        }

        private void InvokePostChangeGroupMigrationAddins(Guid migrationSourceId, ChangeGroup changeGroup)
        {
            foreach (MigrationAddin migrationAddin in m_addinManagementService.GetMigrationSourceMigrationAddins(migrationSourceId))
            {
                Debug.Assert(m_migrationContextsByMigrationSource.ContainsKey(migrationSourceId));
                Debug.Assert(m_migrationProviders.ContainsKey(migrationSourceId));
                MigrationContext migrationContext;
                if (m_migrationContextsByMigrationSource.TryGetValue(migrationSourceId, out migrationContext))
                {
                    try
                    {
                        migrationAddin.PostChangeGroupMigration(migrationContext, changeGroup);
                    }
                    catch (Exception e)
                    {
                        ProcessAddinException(migrationAddin, migrationSourceId, e);
                    }                    
                }
            }
        }

        internal void InvokePostMigrationAddins(Guid migrationSourceId)
        {
            foreach (MigrationAddin migrationAddin in m_addinManagementService.GetMigrationSourceMigrationAddins(migrationSourceId))
            {
                Debug.Assert(m_migrationContextsByMigrationSource.ContainsKey(migrationSourceId));
                Debug.Assert(m_migrationProviders.ContainsKey(migrationSourceId));
                MigrationContext migrationContext;
                if (m_migrationContextsByMigrationSource.TryGetValue(migrationSourceId, out migrationContext))
                {
                    try
                    {
                        migrationAddin.PostMigration(migrationContext);
                    }
                    catch (Exception e)
                    {
                        ProcessAddinException(migrationAddin, migrationSourceId, e);
                    }
                }
            }
        }

        private void ProcessAddinException(MigrationAddin migrationAddin, Guid sourceId, Exception ex)
        {
            AddinException addinException = new AddinException(String.Format(MigrationToolkitResources.ErrorCallingAddin,
                migrationAddin.FriendlyName, ex.Message), ex);
            ConflictManager manager = m_serviceContainers[sourceId].GetService(typeof(ConflictManager)) as ConflictManager;
            ErrorManager.TryHandleException(addinException, manager);
        }
    }
}
