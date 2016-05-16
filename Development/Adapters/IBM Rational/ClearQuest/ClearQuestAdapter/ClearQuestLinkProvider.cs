// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ClearQuestOleServer;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.ConflictManagement;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.Exceptions;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.Linking;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    internal class ClearQuestLinkProvider : ILinkProvider, IServiceProvider
    {
        private readonly List<Guid> m_supportedChangeActions;               // Registered supported link change actions
        private readonly Dictionary<string, LinkType> m_supportedLinkTypes; // Registered supported link types
        private ICollection<string> m_supportedLinkTypeReferenceNamesOther; // Registered link types from the other side
        protected ICollection<IArtifactHandler> m_artifactHandlers;         // Registered artifact handlers

        protected ServiceContainer m_serviceContainer;
        protected ILinkTranslationService m_linkTranslationService;         // LinkService for creating LinkChangeGroup, etc.
        protected ConflictManager m_conflictManager;                        // Conflict Manager to register and resolve link conflicts
        protected ConfigurationService m_configurationService;              // Configuration service
        protected LinkConfigurationLookupService m_linkConfigLookupService; // LinkConfig, such as mapped link type, lookup

        private HighWaterMark<DateTime> m_hwmLink;
        private CQRecordFilters m_filters;
        private ClearQuestOleServer.Session m_userSession;                  // user session; instantiated after InitializeClient()
        #region not need until context sync requires us to access schema info
        //private ClearQuestOleServer.AdminSession m_adminSession;            // admin session; may be NULL if login info is not provided in config file 
        #endregion
        private ClearQuestMigrationContext m_migrationContext;

        private ErrorManager ErrorManager
        {
            get
            {
                ErrorManager errMgr = null;
                if (m_serviceContainer != null)
                {
                    errMgr = m_serviceContainer.GetService(typeof(ErrorManager)) as ErrorManager;
                }
                return errMgr;
            }
        }

        /// <summary>
        /// C'tor
        /// </summary>
        public ClearQuestLinkProvider()
        {
            m_supportedChangeActions = new List<Guid>();
            m_supportedLinkTypes = new Dictionary<string, LinkType>();
            m_artifactHandlers = new Collection<IArtifactHandler>();
        }

        /// <summary>
        /// Gets callback method for extracting links.
        /// </summary>
        protected ExtractLinkChangeActions ExtractLinkChangeActionsCallback { get; set; }

        /// <summary>
        /// Delegate function for extracting links from the given object.
        /// </summary>
        /// <param name="source">Source of links</param>
        /// <param name="linkChangeGroups"></param>
        public delegate void ExtractLinkChangeActions(ClearQuestOleServer.Session session, OAdEntity hostRecord, List<LinkChangeGroup> linkChangeGroups);

        #region IServiceProvider Members

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(ClearQuestMigrationContext))
            {
                return m_migrationContext;
            }

            return null;
        }

        #endregion

        #region ILinkProvider Members
        /// <summary>
        /// Gets link change actions supported by this provider
        /// </summary>
        public ICollection<Guid> SupportedChangeActions
        {
            get { return m_supportedChangeActions; }
        }

        /// <summary>
        /// Gets/Sets link change actions supported by the counterparty
        /// </summary>
        public ICollection<Guid> SupportedChangeActionsOther
        {
            get;
            set;
        }

        /// <summary>
        /// Gets link types supported by this provider
        /// </summary>
        public Dictionary<string, LinkType> SupportedLinkTypes
        {
            get { return m_supportedLinkTypes; }
        }


        public ICollection<string> SupportedLinkTypeReferenceNamesOther
        {
            get
            {
                return m_supportedLinkTypeReferenceNamesOther;
            }
            set
            {
                m_supportedLinkTypeReferenceNamesOther = value;
                Debug.Assert(null != m_supportedLinkTypeReferenceNamesOther,
                             "null == m_supportedLinkTypeReferenceNamesOther");

                foreach (var supportedLinkType in m_supportedLinkTypes)
                {
                    if (m_supportedLinkTypeReferenceNamesOther.Contains(supportedLinkType.Key))
                    {
                        continue;
                    }

                    // link config lookup service returns the mapped link type on otherside if there is a mapping
                    // or the unmapped link type on this side
                    string mappedLinkType = m_linkConfigLookupService.FindMappedLinkType(m_configurationService.SourceId, supportedLinkType.Key);
                    if (!m_linkTranslationService.LinkTypeSupportedByOtherSide(mappedLinkType))
                    {
                        ExtractLinkChangeActionsCallback -= ((ILinkHandler)supportedLinkType.Value).ExtractLinkChangeActions;
                    }
                }
            }
        }

        public void Initialize(ServiceContainer serviceContainer)
        {
            Debug.Assert(null != serviceContainer, "ServiceContainer is NULL");

            m_serviceContainer = serviceContainer;

            m_linkTranslationService = serviceContainer.GetService(typeof(ILinkTranslationService)) as ILinkTranslationService;
            Debug.Assert(null != m_linkTranslationService, "ILinkTranslationService has not been properly initialized");

            m_configurationService = serviceContainer.GetService(typeof(ConfigurationService)) as ConfigurationService;
            Debug.Assert(null != m_configurationService, "ConfigurationService has not been properly initialized");

            m_hwmLink = new HighWaterMark<DateTime>(ClearQuestConstants.CqLinkHwm);
            m_configurationService.RegisterHighWaterMarkWithSession(m_hwmLink);

            m_linkConfigLookupService = m_linkTranslationService.LinkConfigurationLookupService;

            m_conflictManager = serviceContainer.GetService(typeof(ConflictManager)) as ConflictManager;

            InitializeClient();

            RegisterArtifactHandlers();

            MigrationSource migrSrcConfig = m_configurationService.MigrationSource;
            Debug.Assert(null != migrSrcConfig, "cannot get MigrationSource config from Session");
            foreach (CustomSetting setting in migrSrcConfig.CustomSettings.CustomSetting)
            {
                if (setting.SettingKey.Equals(ClearQuestConstants.CqWebRecordUrlBaseSettingKey, StringComparison.OrdinalIgnoreCase))
                {
                    string urlFormat = setting.SettingValue;
                    var recordHyperLinkType = new ClearQuestWebRecordLinkType(urlFormat);
                    m_supportedLinkTypes.Add(recordHyperLinkType.ReferenceName, recordHyperLinkType);
                    ExtractLinkChangeActionsCallback += ((ILinkHandler)recordHyperLinkType).ExtractLinkChangeActions;
                    break;
                }
            }
        }

        public ReadOnlyCollection<LinkChangeGroup> GenerateNextLinkDeltaSlice(
            LinkService linkService, 
            int maxDeltaSliceSize)
        {
            try
            {
                var linkChangeGroups = new List<LinkChangeGroup>();

                if (null == ExtractLinkChangeActionsCallback)
                {
                    return linkChangeGroups.AsReadOnly();
                }

                // load high watermark; as for CQ, we store local time for queries
                m_hwmLink.Reload();
                DateTime hwmDeltaValue = m_hwmLink.Value;
                if (hwmDeltaValue.Equals(default(DateTime)))
                {
                    hwmDeltaValue = new DateTime(1900, 1, 1);
                }
                hwmDeltaValue = hwmDeltaValue.AddSeconds(-1);           // go back 1 second as we'll drop the millisec below
                
                // HACK HACK HACK
                //string hwmDeltaValueStr = hwmDeltaValue.ToString("o"); // using "ISO 8601" DateTime string format
                string hwmDeltaValueStr = hwmDeltaValue.ToString("u").Replace("Z", ""); // using "ISO 8601" DateTime string format

                if (hwmDeltaValueStr.LastIndexOf('.') >= 0)
                {
                    hwmDeltaValueStr = hwmDeltaValueStr.Substring(0, hwmDeltaValueStr.LastIndexOf('.'));    // drop the millisec
                }
                // HACK HACK HACK

                // record current time to update HWM after processing
                DateTime newHwmValue = DateTime.Now; // ???

                // store to be used for analysis
                WorkItemLinkStore store = new WorkItemLinkStore(m_configurationService.SourceId);

                // extract links
                var inMaxDeltaSliceSize = maxDeltaSliceSize;
                foreach (CQRecordFilter filter in m_filters)
                {
                    CQRecordQueryBase recordQuery = CQRecordQueryFactory.CreatQuery(m_userSession, filter, hwmDeltaValueStr, this);
                    foreach (ClearQuestOleServer.OAdEntity record in recordQuery)
                    {
                        // HACK HACK
                        if (record == null)
                        {
                            continue;
                        }
                        // HACK HACK

                        string recDispName = CQWrapper.GetEntityDisplayName(record);

                        TraceManager.TraceInformation("Generating linking delta for CQ Record: {0}", recDispName);

                        var perWorkItemlinkChangeGroups = new List<LinkChangeGroup>();
                        ExtractLinkChangeActionsCallback(m_userSession, record, perWorkItemlinkChangeGroups);

                        if (perWorkItemlinkChangeGroups.Count == 0)
                        {
                            TraceManager.TraceInformation("Number of links: {0}", 0);
                            continue;
                        }

                        LinkChangeGroup consolidatedLinkChangeGroup = perWorkItemlinkChangeGroups[0];
                        for (int i = 1; i < perWorkItemlinkChangeGroups.Count; ++i)
                        {
                            foreach (LinkChangeAction action in perWorkItemlinkChangeGroups[i].Actions)
                            {
                                consolidatedLinkChangeGroup.AddChangeAction(action);
                            }
                        }
                        TraceManager.TraceInformation("Number of links: {0}", consolidatedLinkChangeGroup.Actions.Count.ToString());

                        // VERY IMPORTANT STEP: update the link delta to store
                        string hostRecDispName = CQWrapper.GetEntityDisplayName(record);
                        string hostRecEntityDefName = CQWrapper.GetEntityDefName(record);
                        string hostRecMigrItemId = UtilityMethods.CreateCQRecordMigrationItemId(hostRecEntityDefName, hostRecDispName);
                        store.UpdatePerItemLinkChangeGroupsByCheckingRelatedItemRecords(
                            hostRecMigrItemId, consolidatedLinkChangeGroup, this);

                        if (consolidatedLinkChangeGroup.Actions.Count > 0)
                        {
                            linkChangeGroups.Add(consolidatedLinkChangeGroup);
                        }
                        maxDeltaSliceSize -= consolidatedLinkChangeGroup.Actions.Count;

                        if (maxDeltaSliceSize <= 0)
                        {
                            // size limit reached - persist groups to DB, then empty the slice and process next slice
                            linkService.AddChangeGroups(linkChangeGroups);
                            linkChangeGroups.Clear();
                            maxDeltaSliceSize = inMaxDeltaSliceSize;
                        }
                    }
                }

                // persist remaining groups to DB
                linkService.AddChangeGroups(linkChangeGroups);

                // clean up the returned link change group collection
                // when the caller (toolkit) receives an empty collection, it understands there is no more
                // delta to generate for the moment, and proceeds to next phase
                linkChangeGroups.Clear();

                // update primary Highwater Mark
                m_hwmLink.Update(newHwmValue);
                TraceManager.TraceInformation("Persisted CQ linking HWM: {0}", ClearQuestConstants.CqLinkHwm);
                TraceManager.TraceInformation("Updated CQ linking HWM: {0}", newHwmValue.ToString());

                return linkChangeGroups.AsReadOnly();
            }
            catch (Exception exception)
            {
                // [teyang] TODO CONFLICT HANDLING

                //MigrationConflict genericeConflict = WitGeneralConflictType.CreateConflict(exception);
                //var conflictManager = m_conflictManager.GetService(typeof(ConflictManager)) as ConflictManager;
                //Debug.Assert(null != conflictManager);
                //List<MigrationAction> resolutionActions;
                //ConflictResolutionResult resolveRslt =
                //    conflictManager.TryResolveNewConflict(conflictManager.SourceId, genericeConflict, out resolutionActions);
                //Debug.Assert(!resolveRslt.Resolved);
                TraceManager.TraceException(exception);
                return new List<LinkChangeGroup>().AsReadOnly();
            }
        }

        public void SubmitLinkChange(LinkService linkService)
        {
            long firstChangeGroupId = 0;
            const LinkChangeGroup.LinkChangeGroupStatus status = LinkChangeGroup.LinkChangeGroupStatus.ReadyForMigration;

            while (true)
            {
                long lastChangeGroupId;
                ReadOnlyCollection<LinkChangeGroup> pagedChangeGroups = linkService.GetLinkChangeGroups(
                    firstChangeGroupId, 10000, status, false, out lastChangeGroupId);

                if (pagedChangeGroups.Count <= 0 || lastChangeGroupId < firstChangeGroupId) break;
                firstChangeGroupId = lastChangeGroupId + 1;

                foreach (LinkChangeGroup changeGroup in pagedChangeGroups)
                {
                    if (linkService.ChangeGroupIsCompleted(changeGroup))
                    {
                        changeGroup.Status = LinkChangeGroup.LinkChangeGroupStatus.Completed;
                    }
                    else if (linkService.AllLinkMigrationInstructionsAreConflicted(changeGroup))
                    {
                        changeGroup.IsConflicted = true;
                    }
                    else
                    {
                        LinkChangeGroup.LinkChangeGroupStatus statusCache = changeGroup.Status;

                        TraceManager.TraceInformation("Start migration link change group {0}", changeGroup.GroupName);
                        BatchSubmitLinkChange(changeGroup);
                        TraceManager.TraceInformation("Finish migration link change group {0}", changeGroup.GroupName);

                        // if the change group contains the two special skip change actions
                        // we leave the group status untouched but update the action status changes, if any
                        if (linkService.ContainsSpecialSkipActions(changeGroup))
                        {
                            changeGroup.Status = statusCache;
                        }
                        else if (linkService.AllLinkMigrationInstructionsAreConflicted(changeGroup))
                        {
                            changeGroup.IsConflicted = true;
                        }
                        else if (linkService.ChangeGroupIsCompleted(changeGroup))
                        {
                            changeGroup.Status = LinkChangeGroup.LinkChangeGroupStatus.Completed;
                        }
                    }
                    linkService.SaveChangeGroupActionStatus(changeGroup);
                }
            }
        }
        
        public void BatchSubmitLinkChange(LinkChangeGroup linkChanges)
        {
            if (linkChanges.Actions.Count == 0)
            {
                linkChanges.Status = LinkChangeGroup.LinkChangeGroupStatus.Completed;
                return;
            }

            // group changes by work item Id
            Dictionary<string, List<LinkChangeAction>> perRecordLinkChanges = RegroupLinkChangeActions(linkChanges);

            WorkItemLinkStore relatedArtifactsStore = new WorkItemLinkStore(m_configurationService.SourceId);

            // batch-submit links of each cq record
            bool successForAllActions = true;
            foreach (var perWorkItemLinkChange in perRecordLinkChanges)
            {
                string[] identity = UtilityMethods.ParseCQRecordMigrationItemId(perWorkItemLinkChange.Key);
                OAdEntity hostEntity = CQWrapper.GetEntity(m_userSession, identity[0], identity[1]);
                
                foreach (LinkChangeAction linkChangeAction in perWorkItemLinkChange.Value)
                {
                    if (linkChangeAction.Status != LinkChangeAction.LinkChangeActionStatus.ReadyForMigration
                        || linkChangeAction.IsConflicted)
                    {
                        continue;
                    }

                    var handler = linkChangeAction.Link.LinkType as ILinkHandler;
                    Debug.Assert(null != handler, "linktype is not an ILinkHandler");
                    if (!handler.Update(m_migrationContext, m_userSession, hostEntity, linkChangeAction))
                    {
                        successForAllActions = false;
                        // [teyang] todo conflict handling
                        linkChangeAction.Status = LinkChangeAction.LinkChangeActionStatus.Completed;
                        linkChangeAction.IsConflicted = true;
                        TraceManager.TraceError("Failed processing link change action: {0} linked to {1}",
                                                linkChangeAction.Link.SourceArtifact.Uri,
                                                linkChangeAction.Link.TargetArtifact.Uri);
                    }
                    else
                    {
                        MarkLinkChangeActionCompleted(linkChangeAction, relatedArtifactsStore);
                    }
                }
            }

            linkChanges.Status = successForAllActions
                                 ? LinkChangeGroup.LinkChangeGroupStatus.Completed
                                 : LinkChangeGroup.LinkChangeGroupStatus.ReadyForMigration;
        }

        public bool TryGetArtifactById(
            string artifactTypeReferenceName, 
            string id, 
            out IArtifact artifact)
        {
            artifact = null;

            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            if (string.IsNullOrEmpty(artifactTypeReferenceName))
            {
                return false;
            }

            ArtifactType artifactType;
            bool succeeded = TryGetArtifactType(artifactTypeReferenceName, out artifactType);
            if (!succeeded)
            {
                return false;
            }
            Debug.Assert(null != artifactType, "null == artifactType");

            foreach (IArtifactHandler artifactHandler in m_artifactHandlers)
            {
                succeeded = artifactHandler.TryCreateArtifactFromId(artifactType, id, out artifact);
                if (succeeded)
                {
                    break;
                }
            }
            return succeeded;
        }

        public void RegisterSupportedLinkOperations()
        {
            m_supportedChangeActions.Add(WellKnownChangeActionId.Add);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Delete);
        }

        public void RegisterSupportedLinkTypes()
        {
            // register duplicate record link type
            LinkType linkType = new ClearQuestDuplicateRecordLinkType();
            m_supportedLinkTypes.Add(linkType.ReferenceName, linkType);
            ExtractLinkChangeActionsCallback += ((ILinkHandler)linkType).ExtractLinkChangeActions;

            // register per record-type reference field & reference-list field links
            foreach (CQRecordFilter filter in m_filters)
            {
                List<LinkType> linkTypes = ClearQuestReferenceFieldLinkTypeBase.ExtractSupportedLinkTypes(
                                                m_userSession,
                                                filter.RecordType);
                foreach (LinkType lType in linkTypes)
                {
                    if (!m_supportedLinkTypes.ContainsKey(lType.ReferenceName))
                    {
                        m_supportedLinkTypes.Add(lType.ReferenceName, lType);
                        ExtractLinkChangeActionsCallback += ((ILinkHandler)lType).ExtractLinkChangeActions;
                    }
                }

                linkTypes = ClearQuestReferenceListFieldLinkTypeBase.ExtractSupportedLinkTypes(
                                m_userSession,
                                filter.RecordType);
                foreach (LinkType lType in linkTypes)
                {
                    if (!m_supportedLinkTypes.ContainsKey(lType.ReferenceName))
                    {
                        m_supportedLinkTypes.Add(lType.ReferenceName, lType);
                        ExtractLinkChangeActionsCallback += ((ILinkHandler)lType).ExtractLinkChangeActions;
                    }
                }
            }

            // register CQ-CC integration link types
        }

        public void RegisterConflictTypes(ConflictManager conflictManager, Guid sourceId)
        {
            if (conflictManager == null)
            {
                throw new ArgumentNullException("conflictManager");
            }
            m_conflictManager = conflictManager;

            m_conflictManager.RegisterConflictType(new GenericConflictType());

            m_conflictManager.RegisterConflictType(sourceId, new ClearQuestGenericConflictType(),
                                                          SyncOrchestrator.ConflictsSyncOrchOptions.StopConflictedSessionCurrentTrip);

            m_conflictManager.RegisterConflictType(sourceId, new ClearQuestMissingCQDllConflictType(),
                                                          SyncOrchestrator.ConflictsSyncOrchOptions.StopConflictedSession);
        }

        public void Analyze(
            LinkService linkService, 
            Guid sessionId, 
            Guid sourceId)
        {
            return;
        }

        public bool TryExtractArtifactId(
            IArtifact artifact, 
            out string id)
        {
            id = string.Empty;
            bool succeeded = false;

            if (null != artifact)
            {
                foreach (IArtifactHandler artifactHandler in m_artifactHandlers)
                {
                    succeeded = artifactHandler.TryExtractArtifactId(artifact, out id);
                    if (succeeded)
                    {
                        break;
                    }
                }
            }

            return succeeded;
        }

        public bool TryGetVersionControlledArtifactDetails(
            IArtifact artifact, 
            out string path, 
            out string changeId)
        {
            throw new NotImplementedException();
        }

        public string GetVersionControlledArtifactUri(string path, string changeId)
        {
            throw new NotImplementedException();
        }

        public List<ILink> GetLinks(
            IArtifact sourceArtifact, 
            LinkType linkType)
        {
            string id;
            bool idExtractionRslt = TryExtractArtifactId(sourceArtifact, out id);
            Debug.Assert(idExtractionRslt);

            string[] identity = UtilityMethods.ParseCQRecordMigrationItemId(id);
            OAdEntity hostEntity = CQWrapper.GetEntity(m_userSession, identity[0], identity[1]);

            var links = new List<ILink>();

            if (null == ExtractLinkChangeActionsCallback)
            {
                return links;
            }

            var perWorkItemlinkChangeGroups = new List<LinkChangeGroup>();
            ExtractLinkChangeActionsCallback(m_userSession, hostEntity, perWorkItemlinkChangeGroups);

            foreach (LinkChangeGroup group in perWorkItemlinkChangeGroups)
            {
                foreach (LinkChangeAction action in group.Actions)
                {
                    if (!CQStringComparer.LinkType.Equals(action.Link.LinkType.ReferenceName, linkType.ReferenceName))
                    {
                        continue;
                    }

                    string mappedLinkType = m_linkConfigLookupService.FindMappedLinkType(m_configurationService.SourceId, action.Link.LinkType.ReferenceName);
                    if (!m_linkTranslationService.LinkTypeSupportedByOtherSide(mappedLinkType))
                    {
                        continue;
                    }

                    if (!links.Contains(action.Link))
                    {
                        links.Add(action.Link);
                    }
                }
            }

            return links;
        }

        public NonCyclicReferenceClosure CreateNonCyclicLinkReferenceClosure(
            LinkType linkType, 
            IArtifact artifact)
        {
            throw new NotImplementedException();
        }

        public bool TryGetSingleParentLinkSourceArtifacts(
            LinkType linkType, 
            IArtifact artifact, 
            out IArtifact[] parentArtifacts)
        {
            parentArtifacts = new IArtifact[0];
            return true;
        }

        #endregion
        private ClearQuestConnectionConfig UserSessionConnConfig
        {
            get;
            set;
        }

        private ClearQuestConnectionConfig AdminSessionConnConfig
        {
            get;
            set;
        }

        private void RegisterArtifactHandlers()
        {
            m_artifactHandlers.Add(new ClearQuestRecordArtifactHandler());
            m_artifactHandlers.Add(new ClearQuestWebRecordHyperLinkArtifactHandler());
        }

        private void InitializeClient()
        {
            InitializeCQClient();
        }

        private void InitializeCQClient()
        {
            MigrationSource migrationSourceConfig = m_configurationService.MigrationSource;
            string dbSet = migrationSourceConfig.ServerUrl;
            string userDb = migrationSourceConfig.SourceIdentifier;

            ICredentialManagementService credManagementService =
                m_serviceContainer.GetService(typeof(ICredentialManagementService)) as ICredentialManagementService;

            ICQLoginCredentialManager loginCredManager = 
                CQLoginCredentialManagerFactory.CreateCredentialManager(credManagementService, migrationSourceConfig);

            // connect to user session
            UserSessionConnConfig = new ClearQuestConnectionConfig(loginCredManager.UserName,
                                                           loginCredManager.Password,
                                                           userDb,
                                                           dbSet);
            m_userSession = CQConnectionFactory.GetUserSession(UserSessionConnConfig);

            #region admin session is not needed until we sync context
            //// connect to admin session
            //if (!string.IsNullOrEmpty(loginCredManager.AdminUserName))
            //{
            //    AdminSessionConnConfig = new ClearQuestConnectionConfig(loginCredManager.AdminUserName,
            //                                                                loginCredManager.AdminPassword ?? string.Empty,
            //                                                                userDb,
            //                                                                dbSet);
            //    m_adminSession = CQConnectionFactory.GetAdminSession(AdminSessionConnConfig);
            //} 
            #endregion

            // parse the filter strings in the configuration file
            m_filters = new CQRecordFilters(m_configurationService.Filters, m_userSession);

            m_migrationContext = new ClearQuestMigrationContext(m_userSession, migrationSourceConfig);
        }

        private void MarkLinkChangeActionCompleted(LinkChangeAction action, WorkItemLinkStore relatedArtifactsStore)
        {
            if (action.Status == LinkChangeAction.LinkChangeActionStatus.ReadyForMigration)
            {
                action.Status = LinkChangeAction.LinkChangeActionStatus.Completed;
            }

            var actions = new List<LinkChangeAction>();
            actions.Add(action);
            relatedArtifactsStore.UpdateSyncedLinks(actions);
        }

        private Dictionary<string, List<LinkChangeAction>> RegroupLinkChangeActions(LinkChangeGroup linkChanges)
        {
            Dictionary<string, List<LinkChangeAction>> retVal = new Dictionary<string, List<LinkChangeAction>>();

            foreach (LinkChangeAction action in linkChanges.Actions)
            {
                if (!retVal.ContainsKey(action.Link.SourceArtifactId))
                {
                    retVal.Add(action.Link.SourceArtifactId, new List<LinkChangeAction>());
                }

                if (!retVal[action.Link.SourceArtifactId].Contains(action))
                {
                    retVal[action.Link.SourceArtifactId].Add(action);
                }
            }

            return retVal;
        }

        private bool TryGetArtifactType(string artifactTypeReferenceName, out ArtifactType artifactType)
        {
            artifactType = null;

            if (string.IsNullOrEmpty(artifactTypeReferenceName))
            {
                return false;
            }

            bool succeeded = false;
            foreach (var supportedLinkType in m_supportedLinkTypes)
            {
                if (CQStringComparer.LinkArtifactType.Equals(supportedLinkType.Value.SourceArtifactType.ReferenceName, 
                                                             artifactTypeReferenceName))
                {
                    succeeded = true;
                    artifactType = supportedLinkType.Value.SourceArtifactType;
                    break;
                }

                if (CQStringComparer.LinkArtifactType.Equals(supportedLinkType.Value.TargetArtifactType.ReferenceName, 
                                                             artifactTypeReferenceName))
                {
                    succeeded = true;
                    artifactType = supportedLinkType.Value.TargetArtifactType;
                    break;
                }
            }

            return succeeded;
        }
    }
}
