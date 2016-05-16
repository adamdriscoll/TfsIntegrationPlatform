// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement.WITBasicConflicts;
using Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using BM = Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal sealed class AnalysisEngine
    {
        private Dictionary<Guid, ServiceContainer> m_serviceContainers;
        private Dictionary<Guid, IAnalysisProvider> m_analysisProviders;
        private readonly RuntimeSession m_session;
        private BM.Configuration m_configuration;
        private IAddinManagementService m_addinManagementService;
        private Dictionary<Guid, AnalysisContext> m_analysisContextsByMigrationSource = new Dictionary<Guid, AnalysisContext>();
        private ITranslationService m_translationService;
        private Dictionary<Guid, ChangeGroupService> m_changeGroupServices = new Dictionary<Guid,ChangeGroupService>();
        private IConflictAnalysisService m_basicConflictAnalysisService;
        private IDeltaTableMaintenanceService m_deltaTableMaintenanceService;
        private int m_pageSize;
        
        private bool m_stopRequested;
        private readonly object m_stopRequestedLock = new object();
        private bool m_stopMigrationEngineOnBasicConflict = false;
        private bool m_disableTargetAnalysis = false;

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

        internal bool StopMigrationEngineOnBasicConflict
        {
            get { return m_stopMigrationEngineOnBasicConflict; }
            set { m_stopMigrationEngineOnBasicConflict = value; }
        }

        public bool DisableTargetAnalysis
        {
            get { return m_disableTargetAnalysis; }
            set { m_disableTargetAnalysis = value; }
        }

        public IConflictAnalysisService BasicConflictAnalysisService
        {
            get { return m_basicConflictAnalysisService; }
            set { m_basicConflictAnalysisService = value; }
        }

        public IDeltaTableMaintenanceService DeltaTableMaintenanceService
        {
            get { return m_deltaTableMaintenanceService; }
            set { m_deltaTableMaintenanceService = value; }
        }

        internal ErrorManager ErrorManager
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor of AnalysisEngine
        /// </summary>
        /// <param name="session"></param>
        internal AnalysisEngine(RuntimeSession session, BM.Configuration configuration, IAddinManagementService addinManagementService)
        {
            m_session = session;
            m_configuration = configuration;
            m_addinManagementService = addinManagementService;

            m_stopRequested = false;

            m_pageSize = 50;

            ConstructServiceHierarchy(session);
        }

        /// <summary>
        /// Heavier-weight Phase2 initialization 
        /// </summary>
        /// <param name="sessionRunId"></param>
        internal void Initialize(int sessionRunId, ISessionOrchestrator sessionOrchestrator)
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

            foreach (var provider in m_analysisProviders.Values)
            {
                provider.InitializeClient();
            }
        }

        public Guid SourceMigrationSourceId { get; set; }

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
            foreach (ChangeGroupService changeGroupService in m_changeGroupServices.Values)
            {
                changeGroupService.PreChangeGroupSaved -= new EventHandler<ChangeGroupEventArgs>(changeGroupService_PreChangeGroupSaved);
            }
        }

        internal void SessionPauseEventHandler(object sender, SessionControlEventArgs e)
        {
            // not handling pause event at engine level
        }

        internal void SessionPauseForConflictEventHandler(object sender, SessionControlEventArgs e)
        {
            StopRequested = true;
        }

        internal void SessionResumeEventHandler(object sender, SessionControlEventArgs e)
        {
            StopRequested = false;
        }

        /// <summary>
        /// Create analysis enginer service containers for left and right source systems 
        /// and register them with parent session containers.
        /// </summary>
        private void ConstructServiceHierarchy(RuntimeSession session)
        {
            m_serviceContainers = new Dictionary<Guid, ServiceContainer>(m_session.ServiceContainers.Count);
            m_analysisProviders = new Dictionary<Guid, IAnalysisProvider>(m_session.ServiceContainers.Count);
            foreach (KeyValuePair<Guid, ServiceContainer> serviceContainerEntry in m_session.ServiceContainers)
            {
                m_serviceContainers.Add(serviceContainerEntry.Key, new ServiceContainer(serviceContainerEntry.Value));

                // note that service container hierarchy is built and services are registered 
                // but the conflict manager still need to be bound to a particular SessionRun
                // That will be taken care of in Stage2 (SyncOrchestrator.InitializePipeline)
                RegisterServices(serviceContainerEntry.Key, session);
            }
        }

        /// <summary>
        /// Register an analysis provider with the analysis engine,
        /// 1. Intialize analysis service container and services.
        /// 2. Call analysis provider to register supported change actions.
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="analysisProvider"></param>
        internal void RegisterAnalysisProvider(
            Guid sourceId, 
            Session sessionConfig, 
            IAnalysisProvider analysisProvider,
            IProvider adapter)
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

            m_serviceContainers[sourceId].AddService(typeof(IAnalysisProvider), analysisProvider);
            m_analysisProviders.Add(sourceId, analysisProvider);

            // Call analysis provider to initialize basic services
            analysisProvider.InitializeServices(m_serviceContainers[sourceId]);

            // Call analysis provider to register supported content types
            ContentTypeRegistrationService contentTypeRegService = m_serviceContainers[sourceId].GetService(typeof(ContentTypeRegistrationService)) as ContentTypeRegistrationService;
            Debug.Assert(null != contentTypeRegService, "contentTypeRegService == NULL");
            analysisProvider.RegisterSupportedContentTypes(contentTypeRegService);

            // Call analysis provider to register supported change actions
            ChangeActionRegistrationService changeActionRegistrationService = m_serviceContainers[sourceId].GetService(typeof(ChangeActionRegistrationService)) as ChangeActionRegistrationService;
            Debug.Assert(null != changeActionRegistrationService, "changeActionRegistrationService == NULL");
            analysisProvider.RegisterSupportedChangeActions(changeActionRegistrationService);

            // Call analysis provider to register supported conflict types
            ConflictManager conflictManagementService = m_serviceContainers[sourceId].GetService(typeof(ConflictManager)) as ConflictManager;
            Debug.Assert(null != conflictManagementService, "conflictManager == NULL");
            analysisProvider.RegisterConflictTypes(conflictManagementService);

            IServerPathTranslationService serverPathTranslationService = adapter.GetService(typeof(IServerPathTranslationService)) as IServerPathTranslationService;
            if (serverPathTranslationService != null)
            {
                m_serviceContainers[sourceId].AddService(typeof(IServerPathTranslationService), serverPathTranslationService);
            }

            foreach (AnalysisAddin analysisAddin in m_addinManagementService.GetMigrationSourceAnalysisAddins(sourceId))
            {
                Debug.Assert(m_serviceContainers.ContainsKey(sourceId), "No ServiceContainer found for MigrationSource with Id: " + sourceId);
                AnalysisContext analysisContext = new AnalysisContext(m_serviceContainers[sourceId]);
                m_analysisContextsByMigrationSource.Add(sourceId, analysisContext);
                // Just need to create one AnalysisContext that can be shared by all of the Addins for a migration source since the contents are read-only
                // to the Addin
                break;
            }
        }

        /// <summary>
        /// Initialize analysis services
        /// </summary>
        private void RegisterServices(Guid sourceId, RuntimeSession session)
        {
            Debug.Assert(m_serviceContainers.ContainsKey(sourceId),
                string.Format(MigrationToolkitResources.UnknownSourceId, sourceId));

            ChangeActionRegistrationService changeActionRegistrationService = new ChangeActionRegistrationService();
            m_serviceContainers[sourceId].AddService(typeof(ChangeActionRegistrationService), changeActionRegistrationService);

            ContentTypeRegistrationService contentTypeRegistrationService = new ContentTypeRegistrationService();
            m_serviceContainers[sourceId].AddService(typeof(ContentTypeRegistrationService), contentTypeRegistrationService);

            ConflictManager conflictManagementService = new ConflictManager(sourceId);
            conflictManagementService.ScopeId = new Guid(m_session.Configuration.SessionUniqueId);
            m_serviceContainers[sourceId].AddService(typeof(ConflictManager), conflictManagementService);
            conflictManagementService.InitializePhase1(m_serviceContainers[sourceId]);

            RegisterGenericeConflicts(conflictManagementService);
            RegisterSessionSpecificConflicts(conflictManagementService, session.Configuration.SessionType);

            ICommentDecorationService commentDecorationService = new CommentDecorationService(m_session, m_serviceContainers[sourceId]);
            m_serviceContainers[sourceId].AddService(typeof(ICommentDecorationService), commentDecorationService);

            ChangeGroupService changeGroupService = m_serviceContainers[sourceId].GetService(typeof(ChangeGroupService)) as ChangeGroupService;
            if (changeGroupService != null)  
            {
                changeGroupService.PreChangeGroupSaved += new EventHandler<ChangeGroupEventArgs>(changeGroupService_PreChangeGroupSaved);
                if (!m_changeGroupServices.ContainsKey(sourceId))
                {
                    m_changeGroupServices.Add(sourceId, changeGroupService);
                }
            }
        }

        void changeGroupService_PreChangeGroupSaved(object sender, ChangeGroupEventArgs e)
        {
            // Only invoke the AnalysisAddins configured for the source (left) side of this sync pass
            // This is consistent with the invocation pattern of AnalysisAddins in the SessionWorker class: we only
            // want to invoke AnalysisAddins on the source side
            if (e.SourceId == SourceMigrationSourceId)
            {
                IAnalysisProvider analysisProvider;
                if (!m_analysisProviders.TryGetValue(e.SourceId, out analysisProvider))
                {
                    Debug.Fail("AnalysisProvider not found with migrationSourceId: " + e.SourceId);
                    return;
                }

                foreach (AnalysisAddin analysisAddin in m_addinManagementService.GetMigrationSourceAnalysisAddins(e.SourceId))
                {
                    Debug.Assert(m_analysisContextsByMigrationSource.ContainsKey(e.SourceId));
                    Debug.Assert(m_analysisProviders.ContainsKey(e.SourceId));
                    AnalysisContext analysisContext;
                    if (m_analysisContextsByMigrationSource.TryGetValue(e.SourceId, out analysisContext))
                    {
                        try
                        {
                            analysisAddin.PostChangeGroupDeltaComputation(analysisContext, e.ChangeGroup);
                        }
                        catch (Exception ex)
                        {
                            ProcessAddinException(analysisAddin, e.SourceId, ex);
                        }
                    }
                }
            }
        }

        private void RegisterSessionSpecificConflicts(ConflictManager conflictManagementService, SessionTypeEnum sessionType)
        {
            switch (sessionType)
            {
                case SessionTypeEnum.VersionControl:
                    RegisterVCContentConflicts(conflictManagementService);
                    break;
                case SessionTypeEnum.WorkItemTracking:
                    RegisterWITBasicConflicts(conflictManagementService);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        private void RegisterGenericeConflicts(ConflictManager conflictManagementService)
        {
            conflictManagementService.RegisterToolkitConflictType(
                new GenericConflictType(),
                SyncOrchestrator.ConflictsSyncOrchOptions.StopConflictedSessionCurrentTrip);

        }

        private void RegisterWITBasicConflicts(ConflictManager conflictManagementService)
        {
            conflictManagementService.RegisterToolkitConflictType(new WITEditEditConflictType(), 
                SyncOrchestrator.ConflictsSyncOrchOptions.Continue);
            conflictManagementService.RegisterToolkitConflictType(new ChainOnBackloggedItemConflictType(),
                SyncOrchestrator.ConflictsSyncOrchOptions.Continue);
            conflictManagementService.RegisterToolkitConflictType(new WITUnmappedWITConflictType(),
                SyncOrchestrator.ConflictsSyncOrchOptions.Continue);
        }

        private void RegisterVCContentConflicts(ConflictManager conflictManagementService)
        {
            conflictManagementService.RegisterToolkitConflictType(new VCContentConflictType());
            conflictManagementService.RegisterToolkitConflictType(new VCNameSpaceContentConflictType());
        }

        /// <summary>
        /// Initialize the following handlers
        /// 1. ConflictHandler
        /// 2. ChangeActionHandler
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="supportedChangeActionsOther"></param>
        /// <param name="supportedContentTypeOther"></param>
        internal void InitializeHandlers(
            Guid sourceId,
            ICollection<Guid> supportedChangeActionsOther,
            Collection<ContentType> supportedContentTypeOther)
        {
            Debug.Assert(m_serviceContainers.ContainsKey(sourceId),
                string.Format(MigrationToolkitResources.UnknownSourceId, sourceId));

            IAnalysisProvider analysisProvider;

            if (!m_analysisProviders.TryGetValue(sourceId, out analysisProvider))
            {
                throw new MigrationException(string.Format(
                    MigrationToolkitResources.Culture,
                    MigrationToolkitResources.UnknownSourceId,
                    sourceId));
            }

            analysisProvider.SupportedChangeActionsOther = supportedChangeActionsOther;
            analysisProvider.SupportedContentTypesOther = supportedContentTypeOther;
        }

        /// <summary>
        /// Generate the context information of the "sourceId" side
        /// </summary>
        /// <param name="sourceId"></param>
        internal void GenerateContextInfoTables(Guid sourceId)
        {
            Debug.Assert(m_serviceContainers.ContainsKey(sourceId),
                string.Format(MigrationToolkitResources.UnknownSourceId, sourceId));

            IAnalysisProvider analysisProvider;

            if (!m_analysisProviders.TryGetValue(sourceId, out analysisProvider))
            {
                throw new MigrationException(string.Format(
                    MigrationToolkitResources.Culture,
                    MigrationToolkitResources.UnknownSourceId,
                    sourceId));
            }

            try
            {
                analysisProvider.GenerateContextInfoTable();
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

        /// <summary>
        /// Generate delta table
        /// </summary>
        internal void GenerateDeltaTables(Guid sourceId)
        {
            Debug.Assert(m_serviceContainers.ContainsKey(sourceId),
                string.Format(MigrationToolkitResources.UnknownSourceId, sourceId));

            IAnalysisProvider analysisProvider;

            if (!m_analysisProviders.TryGetValue(sourceId, out analysisProvider))
            {
                throw new MigrationException(string.Format(
                    MigrationToolkitResources.Culture,
                    MigrationToolkitResources.UnknownSourceId,
                    sourceId));
            }

            try
            {
                ChangeGroupService cgService =
                    m_serviceContainers[sourceId].GetService(typeof(ChangeGroupService)) as ChangeGroupService;
                Debug.Assert(null != cgService, "Change group service is not properly initialized");
                cgService.RemoveIncompleteChangeGroups();

                analysisProvider.GenerateDeltaTable();
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

            IForceSyncAnalysisProvider forceSyncAnalysisProvider = analysisProvider as IForceSyncAnalysisProvider;
            if (forceSyncAnalysisProvider != null)
            {
                IForceSyncItemService forceSyncItemService = m_serviceContainers[sourceId].GetService(typeof(IForceSyncItemService)) as IForceSyncItemService;
                if (forceSyncItemService == null)
                {
                    Debug.Fail("ForceSyncItemService not found");
                }
                else
                {
                    GenerateDeltaForForceSync(forceSyncItemService, forceSyncAnalysisProvider);
                }
            }
        }

        /// <summary>
        /// Generate migration instructions
        /// </summary>
        internal void GenerateMigrationInstructions(Guid targetSystemId)
        {
            try
            {
                // Given target system, find change group service for source and for ourselves...
                ConfigurationService configurationService = m_serviceContainers[targetSystemId].GetService(typeof(ConfigurationService)) as ConfigurationService;

                // ToDo, not sure, we can probably just pass in ource system id to let target change group service to load it. But source/target may be different, not sqlchangegroupmanager
                ChangeGroupService sourceChangeGroupService = m_serviceContainers[configurationService.MigrationPeer].GetService(typeof(ChangeGroupService)) as ChangeGroupService;
                ChangeGroupService targetChangeGroupService = m_serviceContainers[targetSystemId].GetService(typeof(ChangeGroupService)) as ChangeGroupService;

                // CopySourceDeltaTableToTarget
                //ChangeGroup deltaTableEntry;

                if (StopMigrationEngineOnBasicConflict)
                {
                    // if one of the delta table entry on source side is conflicted, we stop
                    long? firstConflictedChangeGroupId = sourceChangeGroupService.GetFirstConflictedChangeGroup(ChangeStatus.DeltaPending);
                    if (firstConflictedChangeGroupId.HasValue)
                    {
                        return;
                    }

                    // if one of the migration instruction for target side is conflict, we also stop
                    firstConflictedChangeGroupId = targetChangeGroupService.GetFirstConflictedChangeGroup(ChangeStatus.Pending);
                    if (firstConflictedChangeGroupId.HasValue)
                    {
                        return;
                    }
                }

                ChangeActionRegistrationService changeActionRegistrationService =
                    m_serviceContainers[targetSystemId].GetService(typeof(ChangeActionRegistrationService)) as ChangeActionRegistrationService;

                int pageNumber = 0;
                IEnumerable<ChangeGroup> changeGroups;
                do
                {
                    // NOTE: we do not increment pageNumber here, because the processed ChangeGroups are marked "DeltaComplete" and no longer
                    //       appear in the delta table
                    changeGroups = sourceChangeGroupService.NextDeltaTablePage(pageNumber, m_pageSize, false);
                    foreach (ChangeGroup deltaTableEntry in changeGroups)
                    {
                        TraceManager.TraceInformation(string.Format(
                                    "Generating migration instruction for ChangeGroup {0}",
                                    deltaTableEntry.ChangeGroupId));

                        ChangeGroup migrationInstructionChangeGroup = targetChangeGroupService.CreateChangeGroupForMigrationInstructionTable(deltaTableEntry);

                        // NOTE:
                        // migration instruction change group is created using the target change group manager/service
                        // however, the MigrationItems in it are created by the source-side adapter
                        // by setting the UseOtherSideMigrationItemSerializers flag, we tell this change group to use the source-side change group manager
                        // to find the registered IMigrationItem serializer to persist these MigrationItems
                        migrationInstructionChangeGroup.UseOtherSideMigrationItemSerializers = true;

                        migrationInstructionChangeGroup.ReflectedChangeGroupId = deltaTableEntry.ChangeGroupId;

                        foreach (MigrationAction action in deltaTableEntry.Actions)
                        {
                            try
                            {
                                BeforeCopyDeltaTableEntryToMigrationInstructionTable(action, configurationService.MigrationPeer);
                            }
                            catch (UnmappedWorkItemTypeException unmappedWITEx)
                            {
                                ConflictManager conflictManager = 
                                    m_serviceContainers[configurationService.MigrationPeer].GetService(typeof(ConflictManager)) as ConflictManager;

                                var conflict = WITUnmappedWITConflictType.CreateConflict(unmappedWITEx.SourceWorkItemType, action);

                                List<MigrationAction> actions;
                                var result = conflictManager.TryResolveNewConflict(conflictManager.SourceId, conflict, out actions);

                                if (!result.Resolved)
                                {
                                    continue;
                                }
                                else
                                {
                                    if (result.ResolutionType == ConflictResolutionType.SkipConflictedChangeAction)
                                    {
                                        action.State = ActionState.Skipped;
                                        continue;
                                    }
                                    else
                                    {
                                        // NOTE:
                                        // So far this conflict can only be:
                                        // 1. manually resolved (skipped) AFTER
                                        //    the configuration is updated with the requirement WIT mapping;
                                        // 2. skipping the conflicted migration action (i.e. not migrating the source
                                        //    Work Item type.
                                        Debug.Assert(
                                            false,
                                            string.Format("WITUnmappedWITConflict is auto-resolved. Skipping this assertion will SKIP the original conflicted action '{0}'.",
                                                          action.ActionId.ToString()));
                                        action.State = ActionState.Skipped;
                                        continue;
                                    }
                                }
                            }

                            if (action.State == ActionState.Skipped || action.ChangeGroup.ContainsBackloggedAction)
                            {
                                continue;
                            }
                            ChangeActionHandler actionHandler;
                            if (changeActionRegistrationService.TryGetChangeActionHandler(action.Action, action.ItemTypeReferenceName, out actionHandler))
                            {
                                try
                                {
                                    actionHandler(action, migrationInstructionChangeGroup);
                                }
                                catch (MigrationUnresolvedConflictException)
                                {
                                    // We have already created an unresolved conflict, just return.
                                    return;
                                }
                                catch (Exception e)
                                {
                                    ConflictManager manager = m_serviceContainers[targetSystemId].GetService(typeof(ConflictManager)) as ConflictManager;
                                    ErrorManager.TryHandleException(e, manager);
                                }
                            }
                            else
                            {
                                string analysisProviderName;
                                IAnalysisProvider analysisProvider;
                                if (m_analysisProviders.TryGetValue(targetSystemId, out analysisProvider))
                                {
                                    analysisProviderName = analysisProvider.GetType().ToString();
                                }
                                else
                                {
                                    Debug.Fail("Unable to find IAnalysisProvider with Id: " + targetSystemId);
                                    analysisProviderName = "Unknown";
                                }
                                throw new MigrationException(
                                    string.Format(MigrationToolkitResources.Culture, MigrationToolkitResources.UnknownChangeAction,
                                        action.Action.ToString(), analysisProviderName));
                            }
                        }

                        if (!migrationInstructionChangeGroup.ContainsBackloggedAction
                            && migrationInstructionChangeGroup.Actions.Count > 0)
                        {
                            ChangeStatus status = migrationInstructionChangeGroup.Status;
                            migrationInstructionChangeGroup.Status = ChangeStatus.ChangeCreationInProgress;
                            migrationInstructionChangeGroup.Owner = deltaTableEntry.Owner; // owner may be translated too
                            // Save the partial Change group into DB.
                            migrationInstructionChangeGroup.Save();

                            // Commit the status change together.
                            migrationInstructionChangeGroup.Status = status;
                            deltaTableEntry.Status = ChangeStatus.DeltaComplete;
                            migrationInstructionChangeGroup.Manager.BatchUpdateStatus(
                                new ChangeGroup[] { migrationInstructionChangeGroup, deltaTableEntry });
                        }
                        else
                        {
                            // If all change actions in the delta table entry are skipped. 
                            // Just mark the delta table entry as completed. 
                            deltaTableEntry.UpdateStatus(ChangeStatus.DeltaComplete);
                        }

                        if (this.StopRequested)
                        {
                            return;
                        }
                    }
                }
                while (changeGroups.Count() == m_pageSize);

                DetectBasicConflicts(targetChangeGroupService, targetSystemId, configurationService.MigrationPeer);

                if (this.StopRequested)
                {
                    return;
                }

                ProviderDetectConflicts(targetSystemId, targetChangeGroupService);

                if (this.StopRequested)
                {
                    return;
                }

                // dispose the target side delta table entries after we've done all conflict analysis
                targetChangeGroupService.BatchMarkMigrationInstructionsAsPending();
            }
            catch (Exception e)
            {
                ConflictManager manager = m_serviceContainers[targetSystemId].GetService(typeof(ConflictManager)) as ConflictManager;
                ErrorManager.TryHandleException(e, manager);
            }
        }

        /// <summary>
        /// PostProcessing Delta Table entries after GeneratingMigrationInstruction
        /// </summary>
        /// <param name="targetSystemId">the target system SourceId of the current run through the pipeline</param>
        /// <param name="isBidirectional">true if the current session is bi-directional</param>
        internal void PostProcessDeltaTableEntries(Guid targetSystemId, bool isBidirectional)
        {
            Debug.Assert(
                DeltaTableMaintenanceService != null,
                "DeltaTableMaintenanceService is not properly initialized.");

            if (!isBidirectional)
            {
                TraceManager.TraceInformation("Marking as 'DeltaComplete' the target-side delta table for uni-directional session");
                DeltaTableMaintenanceService.BatchMarkDeltaTableEntriesAsDeltaCompleted(targetSystemId);
            }
        }

        private void DetectBasicConflicts(ChangeGroupService targetChangeGroupService, Guid targetSystemId, Guid sourceSystemId)
        {
            TraceManager.TraceInformation("Starting basic conflict detection");
            if (null == m_basicConflictAnalysisService)
            {
                return;
            }

            m_basicConflictAnalysisService.Configuration = m_session.Configuration;
            m_basicConflictAnalysisService.TargetChangeGroupService = targetChangeGroupService;
            m_basicConflictAnalysisService.ConflictManager = m_serviceContainers[targetSystemId].GetService(typeof(ConflictManager)) as ConflictManager;
            m_basicConflictAnalysisService.TranslationService = m_translationService;
            m_basicConflictAnalysisService.TargetSystemId = targetSystemId;
            m_basicConflictAnalysisService.SourceSystemId = sourceSystemId;
            m_basicConflictAnalysisService.Analyze();
            TraceManager.TraceInformation("Finishing basic conflict detection");
        }

        private void BeforeCopyDeltaTableEntryToMigrationInstructionTable(
            MigrationAction migrationAction, 
            Guid sourceIdOfDeltaTableOwner)
        {
            if (null == m_translationService)
            {
                return;
            }

            m_translationService.Translate(migrationAction, sourceIdOfDeltaTableOwner);
        }

        private void ProviderDetectConflicts(Guid targetSystemId, ChangeGroupService targetChangeGroupService)
        {
            Debug.Assert(m_serviceContainers.ContainsKey(targetSystemId),
               string.Format(MigrationToolkitResources.UnknownSourceId, targetSystemId));

            IAnalysisProvider targetAnalysisProvider;
            if (!m_analysisProviders.TryGetValue(targetSystemId, out targetAnalysisProvider))
            {
                throw new MigrationException(string.Format(
                    MigrationToolkitResources.Culture,
                    MigrationToolkitResources.UnknownSourceId,
                    targetSystemId));
            }

            try
            {
                int pageNumber = 0;
                IEnumerable<ChangeGroup> changeGroups;
                do
                {
                    TraceManager.TraceInformation("Loading {0} ChangeGroup(s)", m_pageSize);
                    changeGroups = targetChangeGroupService.NextMigrationInstructionTablePage(pageNumber++, m_pageSize, true, false);

                    foreach (ChangeGroup nextChangeGroup in changeGroups)
                    {
                        TraceManager.TraceInformation("Target AnalysisProvider detecting conflicts in ChangeGroup #{0}", nextChangeGroup.ChangeGroupId);
                        targetAnalysisProvider.DetectConflicts(nextChangeGroup);
                    }
                }
                while (changeGroups.Count() == m_pageSize);
            }
            catch (MigrationUnresolvedConflictException)
            {
                // We have already created an unresolved conflict, just return.
                return;
            }
            catch (Exception e)
            {
                ConflictManager manager = m_serviceContainers[targetSystemId].GetService(typeof(ConflictManager)) as ConflictManager;
                ErrorManager.TryHandleException(e, manager);
            }
        }

        internal void InvokePreAnalysisAddins(Guid migrationSourceId)
        {
            IAnalysisProvider analysisProvider;
            if (!m_analysisProviders.TryGetValue(migrationSourceId, out analysisProvider))
            {
                Debug.Fail("AnalysisProvider not found with migrationSourceId: " + migrationSourceId);
                return;
            }

            foreach (AnalysisAddin analysisAddin in m_addinManagementService.GetMigrationSourceAnalysisAddins(migrationSourceId))
            {
                Debug.Assert(m_analysisContextsByMigrationSource.ContainsKey(migrationSourceId));
                Debug.Assert(m_analysisProviders.ContainsKey(migrationSourceId));
                AnalysisContext analysisContext;
                if (m_analysisContextsByMigrationSource.TryGetValue(migrationSourceId, out analysisContext))
                {
                    try
                    {
                        analysisAddin.Initialize(m_configuration);
                        analysisAddin.PreAnalysis(analysisContext);
                    }
                    catch (Exception ex)
                    {
                        ProcessAddinException(analysisAddin, migrationSourceId, ex);
                    }
                }
            }
        }

        internal bool InvokeProceedToAnalysisOnAnalysisAddins(Guid migrationSourceId)
        {
            foreach (AnalysisAddin analysisAddin in m_addinManagementService.GetMigrationSourceAnalysisAddins(migrationSourceId))
            {
                Debug.Assert(m_analysisContextsByMigrationSource.ContainsKey(migrationSourceId));
                Debug.Assert(m_analysisProviders.ContainsKey(migrationSourceId));
                AnalysisContext analysisContext;
                if (m_analysisContextsByMigrationSource.TryGetValue(migrationSourceId, out analysisContext))
                {
                    try
                    {
                        if (!analysisAddin.ProceedToAnalysis(analysisContext))
                        {
                            TraceManager.TraceWarning(String.Format(CultureInfo.InvariantCulture, MigrationToolkitResources.AnalysisAddinSkippingPass,
                                analysisAddin.GetType().ToString()));
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        ProcessAddinException(analysisAddin, migrationSourceId, ex);
                    }
                }
            }
            return true;
        }

        internal void InvokePostAnalysisAddins(Guid migrationSourceId)
        {
            foreach (AnalysisAddin analysisAddin in m_addinManagementService.GetMigrationSourceAnalysisAddins(migrationSourceId))
            {
                Debug.Assert(m_analysisContextsByMigrationSource.ContainsKey(migrationSourceId));
                Debug.Assert(m_analysisProviders.ContainsKey(migrationSourceId));
                AnalysisContext analysisContext;
                if (m_analysisContextsByMigrationSource.TryGetValue(migrationSourceId, out analysisContext))
                {
                    try
                    {
                        analysisAddin.PostAnalysis(analysisContext);
                    }
                    catch (Exception ex)
                    {
                        ProcessAddinException(analysisAddin, migrationSourceId, ex);
                    }
                }
            }
        }

        internal void InvokePostDeltaComputationAddins(Guid migrationSourceId)
        {
            foreach (AnalysisAddin analysisAddin in m_addinManagementService.GetMigrationSourceAnalysisAddins(migrationSourceId))
            {
                Debug.Assert(m_analysisContextsByMigrationSource.ContainsKey(migrationSourceId));
                Debug.Assert(m_analysisProviders.ContainsKey(migrationSourceId));
                AnalysisContext analysisContext;
                if (m_analysisContextsByMigrationSource.TryGetValue(migrationSourceId, out analysisContext))
                {
                    try
                    {
                        analysisAddin.PostDeltaComputation(analysisContext);
                    }
                    catch (Exception ex)
                    {
                        ProcessAddinException(analysisAddin, migrationSourceId, ex);
                    }
                }
            }
        }

        internal void ObsoleteDeltaTableEntries(Guid migrationSourceid)
        {
            ChangeGroupService changeGroupService;
            if (this.m_changeGroupServices.TryGetValue(migrationSourceid, out changeGroupService))
            {
                changeGroupService.BatchMarkDeltaTableEntriesAsDeltaCompleted();
            }
        }

        private void ProcessAddinException(AnalysisAddin analysisAddin, Guid sourceId, Exception ex)
        {
            AddinException addinException = new AddinException(String.Format(MigrationToolkitResources.ErrorCallingAddin,
                analysisAddin.FriendlyName, ex.Message), ex);
            ConflictManager manager = m_serviceContainers[sourceId].GetService(typeof(ConflictManager)) as ConflictManager;
            ErrorManager.TryHandleException(addinException, manager);
        }

        private void GenerateDeltaForForceSync(
            IForceSyncItemService forceSyncItemService,
            IForceSyncAnalysisProvider forceSyncAnalysisProvider)
        {
            int batchSize = 100;
            List<string> forceSyncItemIds = new List<string>();
            foreach(string forceSyncItemId in forceSyncItemService.GetItemsForForceSync())
            {
                TraceManager.TraceInformation(String.Format(CultureInfo.InvariantCulture,
                    "Will attempt to force sync work item {0} from migration source {1}",
                    forceSyncItemId, forceSyncItemService.MigrationSourceid));

                forceSyncItemIds.Add(forceSyncItemId);
                if (forceSyncItemIds.Count == batchSize)
                {
                    forceSyncAnalysisProvider.GenerateDeltaForForceSync(forceSyncItemIds);
                    forceSyncItemIds.Clear();
                }
            }
            if (forceSyncItemIds.Count > 0)
            {
                forceSyncAnalysisProvider.GenerateDeltaForForceSync(forceSyncItemIds);
            }
        }
    }
}
