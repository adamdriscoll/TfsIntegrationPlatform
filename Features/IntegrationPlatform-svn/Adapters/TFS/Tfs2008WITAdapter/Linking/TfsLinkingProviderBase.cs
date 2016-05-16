// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter.Conflicts;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter.Linking
{
    public abstract class TfsLinkingProviderBase : ILinkProvider
    {
        private readonly List<Guid> m_supportedChangeActions;               // Registered supported link change actions
        private readonly Dictionary<string, LinkType> m_supportedLinkTypes; // Registered supported link types
        private ICollection<string> m_supportedLinkTypeReferenceNamesOther; // Registered link types from the other side
        protected ICollection<IArtifactHandler> m_artifactHandlers;         // Registered artifact handlers
        private VersionControlServer m_tfsClient;                           // Tfs Version Control Server instance
        protected TfsWITMigrationSource m_migrationSource;                  // Tfs Workitem Tracking Store instance

        protected ILinkTranslationService m_linkTranslationService;         // LinkTranslationService
        protected ConflictManager m_conflictManager;                        // Conflict Manager to register and resolve link conflicts
        protected ConfigurationService m_configurationService;

        private ServiceContainer m_serviceContainer;
        protected HighWaterMark<DateTime> m_hwmLink;

        protected TfsLinkingProviderBase()
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
        public delegate void ExtractLinkChangeActions(TfsMigrationWorkItem source, List<LinkChangeGroup> linkChangeGroups, WorkItemLinkStore store);

        #region ILinkProvider
        public virtual ICollection<Guid> SupportedChangeActions
        {
            get
            {
                return m_supportedChangeActions;
            }
        }

        public virtual ICollection<Guid> SupportedChangeActionsOther { get; set; }

        public virtual Dictionary<string, LinkType> SupportedLinkTypes
        {
            get
            {
                return m_supportedLinkTypes;
            }
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
                Debug.Assert(null != m_supportedLinkTypeReferenceNamesOther);

                foreach (var supportedLinkType in m_supportedLinkTypes)
                {
                    if (m_supportedLinkTypeReferenceNamesOther.Contains(supportedLinkType.Key))
                    {
                        continue;
                    }

                    // link config lookup service returns the mapped link type on otherside if there is a mapping
                    // or the unmapped link type on this side
                    string mappedLinkType = m_linkTranslationService.LinkConfigurationLookupService.FindMappedLinkType(
                        m_configurationService.SourceId, supportedLinkType.Key);
                    if (!m_linkTranslationService.LinkTypeSupportedByOtherSide(mappedLinkType))
                    {
                        ExtractLinkChangeActionsCallback -=
                            ((ILinkHandler) supportedLinkType.Value).ExtractLinkChangeActions;
                    }
                }
            }
        }

        public virtual void Initialize(ServiceContainer serviceContainer)
        {
            Debug.Assert(null != serviceContainer, "ServiceContainer is NULL");
            m_serviceContainer = serviceContainer;

            m_linkTranslationService = serviceContainer.GetService(typeof(ILinkTranslationService)) as ILinkTranslationService;
            Debug.Assert(null != m_linkTranslationService, "ILinkTranslationService has not been properly initialized");

            m_configurationService = serviceContainer.GetService(typeof (ConfigurationService)) as ConfigurationService;
            Debug.Assert(null != m_configurationService, "ConfigurationService has not been properly initialized");

            m_hwmLink = new HighWaterMark<DateTime>(Toolkit.Constants.HwmDeltaLink);
            m_configurationService.RegisterHighWaterMarkWithSession(m_hwmLink);

            InitializeTfsClient();

            RegisterArtifactHandlers();
        }

        public virtual ReadOnlyCollection<LinkChangeGroup> GenerateNextLinkDeltaSlice(
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

                // load main Highwater Mark
                m_hwmLink.Reload();
                DateTime hwmLinkValue = m_hwmLink.Value;
                string hwmLinkValueStr = hwmLinkValue.ToString(CultureInfo.InvariantCulture);

                // load Work Items for extracting links
                string sourceId = m_migrationSource.UniqueId;
                string storeName = m_migrationSource.WorkItemStore.StoreName;
                
                // Get items based on primary Highwater Mark
                TraceManager.TraceInformation(TfsWITAdapterResources.GettingModifiedItems, sourceId, storeName);
                IEnumerable<TfsMigrationWorkItem> items = m_migrationSource.WorkItemStore.GetItems(ref hwmLinkValueStr);
                TraceManager.TraceInformation(TfsWITAdapterResources.ReceivedModifiedItems, sourceId, storeName);

                // Record the updated HWM value
                DateTime newHwmLinkValue = Convert.ToDateTime(hwmLinkValueStr, CultureInfo.InvariantCulture);

                // store to be used to analyze deleted links
                WorkItemLinkStore store = new WorkItemLinkStore(new Guid(sourceId));

                // extract links
                var inMaxDeltaSliceSize = maxDeltaSliceSize;
                foreach (TfsMigrationWorkItem tfsMigrationWorkItem in items)
                {
                    if (tfsMigrationWorkItem.WorkItem == null)
                    {
                        continue;
                    }

                    TraceManager.TraceInformation("Generating linking delta for Work Item: {0}", tfsMigrationWorkItem.WorkItem.Id.ToString());
                    var perWorkItemlinkChangeGroups = new List<LinkChangeGroup>();
                    ExtractLinkChangeActionsCallback(tfsMigrationWorkItem, perWorkItemlinkChangeGroups, store);
                    
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

                    // VERY IMPORTANT: use the RelatedArtifactsStore to detect link deletion
                    store.UpdatePerItemLinkChangeGroupsByCheckingRelatedItemRecords(
                        tfsMigrationWorkItem.Uri, consolidatedLinkChangeGroup, this);

                    if (consolidatedLinkChangeGroup.Actions.Count > 0)
                    {
                        linkChangeGroups.Add(consolidatedLinkChangeGroup);
                    }
                    maxDeltaSliceSize -= consolidatedLinkChangeGroup.Actions.Count;

                    if (maxDeltaSliceSize <= 0)
                    {
                        // size limit reached - persist groups to DB
                        linkService.AddChangeGroups(linkChangeGroups);
                        linkChangeGroups.Clear();
                        maxDeltaSliceSize = inMaxDeltaSliceSize;
                    }
                }

                // persist remaining groups to DB
                linkService.AddChangeGroups(linkChangeGroups);

                // clean up the returned link change group collection
                // when the caller (toolkit) receives an empty collection, it understands there is no more
                // delta to generate for the moment, and proceeds to next phase
                linkChangeGroups.Clear();

                // update primary Highwater Mark
                m_hwmLink.Update(newHwmLinkValue);
                TraceManager.TraceInformation("Persisted WIT linking HWM: {0}", Toolkit.Constants.HwmDeltaLink);
                TraceManager.TraceInformation(TfsWITAdapterResources.UpdatedHighWatermark, hwmLinkValueStr);
                
                return linkChangeGroups.AsReadOnly();
            }
            catch (Exception exception)
            {
                ErrorManager errMgr = m_serviceContainer.GetService(typeof(ErrorManager)) as ErrorManager;
                errMgr.TryHandleException(exception);
                return new List<LinkChangeGroup>().AsReadOnly();
            }
        }

        public virtual void SubmitLinkChange(LinkService linkService)
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

                        #region to be deleted
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
                        #endregion
                    }
                    linkService.SaveChangeGroupActionStatus(changeGroup);
                }
            }
        }
        
        public virtual void BatchSubmitLinkChange(LinkChangeGroup linkChanges)
        {
            try
            {
                m_migrationSource.WorkItemStore.SubmitLinkChanges(linkChanges, m_serviceContainer);
            }
            catch (Exception exception)
            {
                ErrorManager errMgr = m_serviceContainer.GetService(typeof(ErrorManager)) as ErrorManager;
                errMgr.TryHandleException(exception);
                linkChanges.IsConflicted = true;
            }
        }

        public virtual bool TryGetArtifactById(string artifactTypeReferenceName, string id, out IArtifact artifact)
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
            Debug.Assert(null != artifactType);

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

        public virtual void RegisterSupportedLinkOperations()
        {
            m_supportedChangeActions.Add(WellKnownChangeActionId.Add);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Delete);
        }

        public virtual void RegisterSupportedLinkTypes()
        {
            LinkType linkType = new WorkItemChangeListLinkType();
            m_supportedLinkTypes.Add(linkType.ReferenceName, linkType);
            ExtractLinkChangeActionsCallback += ((ILinkHandler) linkType).ExtractLinkChangeActions;

            linkType = new WorkItemHyperlinkLinkType();
            m_supportedLinkTypes.Add(linkType.ReferenceName, linkType);
            ExtractLinkChangeActionsCallback += ((ILinkHandler)linkType).ExtractLinkChangeActions;

            linkType = new WorkItemLatestFileLinkType();
            m_supportedLinkTypes.Add(linkType.ReferenceName, linkType);
            ExtractLinkChangeActionsCallback += ((ILinkHandler)linkType).ExtractLinkChangeActions;

            linkType = new WorkItemRelatedLinkType();
            m_supportedLinkTypes.Add(linkType.ReferenceName, linkType);
            ExtractLinkChangeActionsCallback += ((ILinkHandler)linkType).ExtractLinkChangeActions;

            linkType = new WorkItemRevisionFileLinkType();
            m_supportedLinkTypes.Add(linkType.ReferenceName, linkType);
            ExtractLinkChangeActionsCallback += ((ILinkHandler)linkType).ExtractLinkChangeActions;

            foreach (RegisteredLinkType registeredLinkType in m_migrationSource.WorkItemStore.WorkItemStore.RegisteredLinkTypes)
            {
                Debug.Assert(!string.IsNullOrEmpty(registeredLinkType.Name));
                if (!TfsDefaultLinkType(registeredLinkType))
                {
                    linkType = new WorkItemExternalLinkType(registeredLinkType.Name);
                    m_supportedLinkTypes.Add(linkType.ReferenceName, linkType);
                    ExtractLinkChangeActionsCallback += ((ILinkHandler)linkType).ExtractLinkChangeActions;
                }
            }
        }

        public virtual void RegisterConflictTypes(ConflictManager conflictManager, Guid sourceId)
        {
            if (conflictManager == null)
            {
                throw new ArgumentNullException("conflictManager");
            }
            m_conflictManager = conflictManager;
            m_conflictManager.RegisterConflictType(new GenericConflictType());
            m_conflictManager.RegisterConflictType(sourceId, new WitGeneralConflictType(),
                                       SyncOrchestrator.ConflictsSyncOrchOptions.StopConflictedSessionCurrentTrip);
        }

        public virtual void Analyze(LinkService linkService, Guid sessionId, Guid sourceId)
        {
            return;
        }
        
        public virtual bool TryExtractArtifactId(IArtifact artifact, out string id)
        {
            id = string.Empty;

            if (null == artifact)
            {
                return false;
            }

            bool succeeded = false;
            foreach (IArtifactHandler artifactHandler in m_artifactHandlers)
            {
                succeeded = artifactHandler.TryExtractArtifactId(artifact, out id);
                if (succeeded)
                {
                    break;
                }
            }
            return succeeded;
        }

        public virtual bool TryGetVersionControlledArtifactDetails(IArtifact artifact, out string path, out string changeId)
        {
            path = changeId = string.Empty;

            if (artifact.ArtifactType.ContentTypeReferenceName != WellKnownContentType.VersionControlledArtifact.ReferenceName
                && artifact.ArtifactType.ContentTypeReferenceName != WellKnownContentType.VersionControlledFile.ReferenceName
                && artifact.ArtifactType.ContentTypeReferenceName != WellKnownContentType.VersionControlChangeGroup.ReferenceName
                && artifact.ArtifactType.ContentTypeReferenceName != WellKnownContentType.VersionControlledFolder.ReferenceName)
            {
                return false;
            }

            try
            {
                Item item = m_tfsClient.ArtifactProvider.GetVersionedItem(new Uri(artifact.Uri));
                path = item.ServerItem;
                changeId = item.ChangesetId.ToString(CultureInfo.InvariantCulture);
                return true;
            }
            catch (VersionControlException e)
            {
                // It is possible that this is a deferred link. Then it is not an error.
                TraceManager.TraceInformation(e.Message);
                path = null;
                changeId = null;
                return false;
            }
        }

        public virtual string GetVersionControlledArtifactUri(string path, string changeId)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    Changeset changeSet = m_tfsClient.GetChangeset(Convert.ToInt32(changeId, CultureInfo.InvariantCulture));
                    return changeSet.ArtifactUri.ToString();
                }
                else
                {
                    Item item = m_tfsClient.GetItem(path);
                    item = m_tfsClient.GetItem(item.ItemId, Convert.ToInt32(changeId, CultureInfo.InvariantCulture));
                    return item.ArtifactUri.ToString();
                }
            }
            catch (VersionControlException e)
            {
                // It is possible that this is a deferred link. Then it is not an error.
                TraceManager.TraceInformation(e.Message);
                return string.Empty;
            }
        }

        public virtual List<ILink> GetLinks(
            IArtifact sourceArtifact, 
            LinkType linkType)
        {
            string id;
            bool idExtractionRslt = TryExtractArtifactId(sourceArtifact, out id);
            Debug.Assert(idExtractionRslt);

            int workItemId = int.Parse(id);
            WorkItem workItem = m_migrationSource.WorkItemStore.WorkItemStore.GetWorkItem(workItemId);
            var sourceArtifactWorkItem = new TfsMigrationWorkItem(m_migrationSource.WorkItemStore.Core, workItem);

            var perWorkItemlinkChangeGroups = new List<LinkChangeGroup>();
            ExtractLinkChangeActionsCallback(sourceArtifactWorkItem, perWorkItemlinkChangeGroups, null);
            
            var links = new List<ILink>();
            foreach (LinkChangeGroup group in perWorkItemlinkChangeGroups)
            {
                foreach (LinkChangeAction action in group.Actions)
                {
                    if (!action.Link.LinkType.ReferenceName.Equals(linkType.ReferenceName))
                    {
                        continue;
                    }

                    string mappedLinkType = m_linkTranslationService.LinkConfigurationLookupService.FindMappedLinkType(
                        m_configurationService.SourceId, action.Link.LinkType.ReferenceName);
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

        public virtual NonCyclicReferenceClosure CreateNonCyclicLinkReferenceClosure(LinkType linkType, IArtifact artifact)
        {
            return new NonCyclicReferenceClosure(linkType);
        }

        public virtual bool TryGetSingleParentLinkSourceArtifacts(LinkType linkType, IArtifact artifact, out IArtifact[] parentArtifacts)
        {
            parentArtifacts = new IArtifact[0];
            return true;
        }

        #endregion


        protected bool TfsDefaultLinkType(RegisteredLinkType type)
        {
            return TFStringComparer.LinkName.Equals(type.Name, LinkingConstants.VcChangelistLinkType)
                   || TFStringComparer.LinkName.Equals(type.Name, LinkingConstants.WitRelatedWorkItemLinkType)
                   || TFStringComparer.LinkName.Equals(type.Name, LinkingConstants.VcFileLinkType)
                   || TFStringComparer.LinkName.Equals(type.Name, LinkingConstants.WitTestResultLinkType)
                   || TFStringComparer.LinkName.Equals(type.Name, LinkingConstants.WitHyperLinkType);
        }

        protected virtual void RegisterArtifactHandlers()
        {
            m_artifactHandlers.Add(new TfsChangeListHandler());
            m_artifactHandlers.Add(new TfsExternalArtifactHandler());
            m_artifactHandlers.Add(new TfsHyperlinkHandler());
            m_artifactHandlers.Add(new TfsLatestFileHandler());
            m_artifactHandlers.Add(new TfsRevisionFileHandler());
            m_artifactHandlers.Add(new TfsWorkItemHandler());
        }

        protected virtual TfsMigrationDataSource InitializeMigrationDataSource()
        {
            return new TfsMigrationDataSource();
        }

        private void InitializeTfsClient()
        {
            TeamFoundationServer tfsServer = TeamFoundationServerFactory.GetServer(m_configurationService.ServerUrl);
            m_tfsClient = tfsServer.GetService(typeof (VersionControlServer)) as VersionControlServer;

            MigrationSource migrationSourceConfiguration = m_configurationService.MigrationSource;
            Debug.Assert(null != migrationSourceConfiguration, "cannot get MigrationSource config from Session");

            TfsMigrationDataSource dataSourceConfig = InitializeMigrationDataSource();
            ReadOnlyCollection<MappingEntry> filters = m_configurationService.Filters;
            // Allow multiple filter strings from other adapters
            // Debug.Assert(filters.Count == 1, "filters.Count != 1 for WIT migration source");
            dataSourceConfig.Filter = filters[0].Path;
            dataSourceConfig.ServerId = migrationSourceConfiguration.ServerIdentifier;
            dataSourceConfig.ServerName = migrationSourceConfiguration.ServerUrl;
            dataSourceConfig.Project = migrationSourceConfiguration.SourceIdentifier;

            m_migrationSource = new TfsWITMigrationSource(
                migrationSourceConfiguration.InternalUniqueId,
                dataSourceConfig.CreateWorkItemStore());
            m_migrationSource.WorkItemStore.ServiceContainer = this.m_serviceContainer;

            foreach (CustomSetting setting in migrationSourceConfiguration.CustomSettings.CustomSetting)
            {
                if (setting.SettingKey.Equals(TfsConstants.DisableAreaPathAutoCreation, StringComparison.InvariantCultureIgnoreCase))
                {
                    m_migrationSource.WorkItemStore.Core.DisableAreaPathAutoCreation =
                        TfsWITCustomSetting.GetBooleanSettingValueDefaultToTrue(setting);
                }
                else if (setting.SettingKey.Equals(TfsConstants.DisableIterationPathAutoCreation, StringComparison.InvariantCultureIgnoreCase))
                {
                    m_migrationSource.WorkItemStore.Core.DisableIterationPathAutoCreation =
                        TfsWITCustomSetting.GetBooleanSettingValueDefaultToTrue(setting);
                }
                else if (setting.SettingKey.Equals(TfsConstants.EnableBypassRuleDataSubmission, StringComparison.InvariantCultureIgnoreCase))
                {
                    m_migrationSource.WorkItemStore.ByPassrules =
                        TfsWITCustomSetting.GetBooleanSettingValueDefaultToTrue(setting);
                }
            }
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
                if (TFStringComparer.LinkName.Equals(supportedLinkType.Value.SourceArtifactType.ReferenceName, artifactTypeReferenceName))
                {
                    succeeded = true;
                    artifactType = supportedLinkType.Value.SourceArtifactType;
                    break;
                }

                if (TFStringComparer.LinkName.Equals(supportedLinkType.Value.TargetArtifactType.ReferenceName, artifactTypeReferenceName))
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