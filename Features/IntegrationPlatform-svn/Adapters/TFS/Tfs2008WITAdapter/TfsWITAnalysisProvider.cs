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
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter.Conflicts;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement.WITBasicConflicts;
using Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter
{
    /// <summary>
    /// TFS Whidbey and Orcas WIT analysis provider
    /// </summary>
    public partial class TfsWITAnalysisProvider : IAnalysisProvider
    {
        public TfsWITAnalysisProvider()
        { }

        #region IServiceProvider Members

        /// <summary>
        /// Gets the analysis provider instance.
        /// </summary>
        /// <param name="serviceType">IAnalysisProvider type</param>
        /// <returns></returns>
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IAnalysisProvider))
            {
                return this;
            }

            if (serviceType == typeof(ConflictManager))
            {
                return m_conflictManagerService;
            }

            if (serviceType == typeof(ITranslationService))
            {
                return TranslationService;
            }

            return null;
        }

        #endregion

        #region IDisposable members
        public void Dispose()
        {
        }
        #endregion

        #region IAnalysisProvider Members

        /// <summary>
        /// Generate the context info table
        /// </summary>
        public virtual void GenerateContextInfoTable()
        {
            try
            {
                ChangeGroup group = m_changeGroupService.CreateChangeGroupForDeltaTable(ContextInformGroupName);

                if (m_supportedContentTypeOther.ContainsKey(WellKnownContentType.UserGroupList.ReferenceName))
                {
                    AddUserGroup(group);
                }
                
                if (m_supportedContentTypeOther.ContainsKey(WellKnownContentType.ValueListCollection.ReferenceName))
                {
                    AddGlobalList(group);
                }            
                
                if (m_supportedContentTypeOther.ContainsKey(WellKnownContentType.Tfs2008WorkItemFieldMetadata.ReferenceName)
                    || m_supportedContentTypeOther.ContainsKey(WellKnownContentType.Tfs2005WorkItemFieldMetadata.ReferenceName))
                {                
                    AddOrcasCompatibleWITD(group);
                }

                if (m_supportedContentTypeOther.ContainsKey(s_CssNodeChangesContentType.ReferenceName))
                {
                    AddCSSNodeChanges(group);
                }

                if (group.Actions.Count > 0)
                {
                    group.Save();
                    m_changeGroupService.PromoteDeltaToPending();
                }
                else
                {
                    TraceManager.TraceInformation(
                        "There is no change to WIT metadata since last sync point. Metadata delta generation has been skipped.");
                }
            }
            catch (Exception exception)
            {
                if (exception is MigrationUnresolvedConflictException)
                {
                    return;
                }

                ErrorManager errMgr = m_analysisServiceContainer.GetService(typeof(ErrorManager)) as ErrorManager;
                errMgr.TryHandleException(exception);
                return;
            }
        }

        private void AddCSSNodeChanges(ChangeGroup group)
        {
            m_hwmCSSMaxSeqId.Reload();
            int startSeqId = m_hwmCSSMaxSeqId.Value > 0 ? m_hwmCSSMaxSeqId.Value : 0;

            CSSAdapter cssAdapter = new CSSAdapter(m_migrationSource.WorkItemStore.Core.Css, m_configurationService.SourceId);

            Project p = m_migrationSource.WorkItemStore.WorkItemStore.Projects[m_migrationSource.WorkItemStore.Core.Config.Project];
            int maxSeqId;
            XmlDocument changes = cssAdapter.GetTeamProjectSpecificCSSNodeChanges(p, startSeqId, out maxSeqId);

            if (null != changes)
            {
                group.CreateAction(
                    WellKnownChangeActionId.SyncContext,
                    new WorkItemContextSyncMigrationItem(s_CssNodeChangesContentType),
                    s_CssNodeChangesContentType.FriendlyName,
                    "",
                    "0",
                    "",
                    s_CssNodeChangesContentType.ReferenceName,
                    changes);
            }

            if (maxSeqId > startSeqId)
            {
                m_hwmCSSMaxSeqId.Update(maxSeqId);
            }
        }

        /// <summary>
        /// List of change actions supported by TfsWITAdapter.
        /// </summary>
        public virtual Dictionary<Guid, ChangeActionHandler> SupportedChangeActions
        {
            get
            {
                return m_supportedChangeActions;
            }
        }

        /// <summary>
        ///  List of change actions supported by the other side.
        /// </summary>
        public virtual ICollection<Guid> SupportedChangeActionsOther
        {
            set
            {
                m_supportedChangeActionsOther = value;
            }
        }

        /// <summary>
        /// List of content types supported by this provider
        /// </summary>
        public virtual Collection<ContentType> SupportedContentTypes
        {
            get
            {
                return m_supportedContentTypes;
            }
        }

        /// <summary>
        /// List of content types supported by the other side
        /// </summary>
        public virtual Collection<ContentType> SupportedContentTypesOther
        {
            set
            {
                if (m_supportedContentTypeOther == null)
                {
                    m_supportedContentTypeOther = new Dictionary<string, ContentType>();
                }

                m_supportedContentTypeOther.Clear();
                foreach (ContentType type in value)
                {
                    m_supportedContentTypeOther.Add(type.ReferenceName, type);
                }
            }
        }

        /// <summary>
        /// Initialize the adapter services
        /// </summary>
        /// <param name="analysisServiceContainer"></param>
        public virtual void InitializeServices(IServiceContainer analysisServiceContainer)
        {
            m_analysisServiceContainer = analysisServiceContainer;

            m_changeGroupService = (ChangeGroupService)m_analysisServiceContainer.GetService(typeof(ChangeGroupService));
            Debug.Assert(m_changeGroupService != null, "Change group service is not initialized");

            m_changeGroupService.RegisterDefaultSourceSerializer(new MigrationItemSerializer<TfsWITMigrationItem>());
            m_changeGroupService.RegisterSourceSerializer(
                WellKnownChangeActionId.SyncContext, new MigrationItemSerializer<WorkItemContextSyncMigrationItem>());
            m_changeGroupService.RegisterSourceSerializer(
                WellKnownChangeActionId.AddAttachment, new MigrationItemSerializer<TfsMigrationFileAttachment>());
            m_changeGroupService.RegisterSourceSerializer(
                WellKnownChangeActionId.DelAttachment, new MigrationItemSerializer<TfsMigrationFileAttachment>());

            m_configurationService = (ConfigurationService)m_analysisServiceContainer.GetService(typeof(ConfigurationService));
            Debug.Assert(m_configurationService != null, "Configuration service is not initialized");

            m_hwmDelta = new HighWaterMark<DateTime>(Toolkit.Constants.HwmDeltaWit);
            m_configurationService.RegisterHighWaterMarkWithSession(m_hwmDelta);

            m_hwmCSSMaxSeqId = new HighWaterMark<int>("HwmTfsCommonStructureNodeChanges");
            m_configurationService.RegisterHighWaterMarkWithSession(m_hwmCSSMaxSeqId);
        }

        /// <summary>
        /// Perform the adapter-specific initialization
        /// </summary>
        public virtual void InitializeClient()
        {
            try
            {
                InitializeTfsClient();
            }
            catch (PermissionException ex)
            {
                MigrationConflict conflict = InsufficientPermissionConflictType.CreateConflict(
                    m_configurationService.SourceId, ex);

                List<MigrationAction> actions;
                m_conflictManagerService.TryResolveNewConflict(m_configurationService.SourceId, conflict, out actions);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Register all change actions supported by this adapter.
        /// </summary>
        /// <param name="changeActionRegistrationService"></param>
        public void RegisterSupportedChangeActions(ChangeActionRegistrationService changeActionRegistrationService)
        {
            if (changeActionRegistrationService == null)
            {
                throw new ArgumentNullException("changeActionRegistrationService");
            }

            RegisterSupportedChangeActions();

            m_changeActionRegistrationService = changeActionRegistrationService;
            foreach (KeyValuePair<Guid, ChangeActionHandler> supportedChangeAction in m_supportedChangeActions)
            {
                // note: for now, WIT adapter uses a single change action handler for all content types
                foreach (ContentType contentType in SupportedContentTypes)
                {
                    m_changeActionRegistrationService.RegisterChangeAction(
                        supportedChangeAction.Key, 
                        contentType.ReferenceName,
                        supportedChangeAction.Value);   
                }
            }
        }

        /// <summary>
        /// Register content types supported by this adapter.
        /// </summary>
        /// <param name="contentTypeRegistrationService"></param>
        public virtual void RegisterSupportedContentTypes(ContentTypeRegistrationService contentTypeRegistrationService)
        {
            if (contentTypeRegistrationService == null)
            {
                throw new ArgumentNullException("contentTypeRegistrationService");
            }

            RegisterSupportedContentTypes();

            foreach (ContentType contentType in m_supportedContentTypes)
            {
                contentTypeRegistrationService.RegisterContentType(contentType);
            }
        }

        /// <summary>
        /// Register all conflict handlers with ConflictManager
        /// </summary>
        /// <param name="conflictManager"></param>
        public virtual void RegisterConflictTypes(ConflictManager conflictManager)
        {
            if (conflictManager == null)
            {
                throw new ArgumentNullException("conflictManager");
            }

            m_conflictManagerService = conflictManager;

            m_conflictManagerService.RegisterConflictType(new InsufficientPermissionConflictType(),
                                                          SyncOrchestrator.ConflictsSyncOrchOptions.StopConflictedSessionCurrentTrip);
            m_conflictManagerService.RegisterConflictType(new WitGeneralConflictType(),
                                                          SyncOrchestrator.ConflictsSyncOrchOptions.StopConflictedSessionCurrentTrip);
            m_conflictManagerService.RegisterConflictType(new InvalidFieldValueConflictType(), SyncOrchestrator.ConflictsSyncOrchOptions.Continue);
            m_conflictManagerService.RegisterConflictType(new WorkItemTypeNotExistConflictType(),
                                                          SyncOrchestrator.ConflictsSyncOrchOptions.StopConflictedSessionCurrentTrip);
            m_conflictManagerService.RegisterConflictType(new WITUnmappedWITConflictType(),
                                                          SyncOrchestrator.ConflictsSyncOrchOptions.StopConflictedSessionCurrentTrip);
            m_conflictManagerService.RegisterConflictType(new FileAttachmentOversizedConflictType(), SyncOrchestrator.ConflictsSyncOrchOptions.Continue);
            m_conflictManagerService.RegisterConflictType(new GenericConflictType());
            m_conflictManagerService.RegisterConflictType(new InvalidFieldConflictType(), SyncOrchestrator.ConflictsSyncOrchOptions.Continue);
            m_conflictManagerService.RegisterConflictType(new ExcessivePathConflictType(), SyncOrchestrator.ConflictsSyncOrchOptions.Continue);
            m_conflictManagerService.RegisterConflictType(new WorkItemHistoryNotFoundConflictType(), SyncOrchestrator.ConflictsSyncOrchOptions.Continue);
            m_conflictManagerService.RegisterConflictType(new InvalidSubmissionConflictType(), SyncOrchestrator.ConflictsSyncOrchOptions.Continue);
        }

        /// <summary>
        /// Generate a slice of delta table, persist to DB, and then return
        /// </summary>
        /// <param name="maxDeltaSliceSize"></param>
        /// <returns>True if all delta in the time sliced window are processed; False otherwise</returns>
        public void GenerateNextDeltaSlice(int maxDeltaSliceSize)
        {
            // load main Highwater Mark
            m_hwmDelta.Reload();
            DateTime hwmDeltaValue = m_hwmDelta.Value;
            // search back 60 seconds to deal with potential WIT race condition
            if (!hwmDeltaValue.Equals(default(DateTime))) 
            {
                hwmDeltaValue = hwmDeltaValue.AddSeconds(-60);
            }
            string hwmDeltaValueStr = hwmDeltaValue.ToString(CultureInfo.InvariantCulture);

            // load Work Items for extracting delta information
            string sourceId = m_migrationSource.UniqueId;
            string storeName = m_migrationSource.WorkItemStore.StoreName;
            
            // Get items based on primary Highwater Mark
            TraceManager.TraceInformation(TfsWITAdapterResources.GettingModifiedItems, sourceId, storeName);
            IEnumerable<TfsMigrationWorkItem> items = m_migrationSource.WorkItemStore.GetItems(ref hwmDeltaValueStr);
            TraceManager.TraceInformation(TfsWITAdapterResources.ReceivedModifiedItems, sourceId, storeName);
            
            // Record the updated HWM value
            DateTime wiqlExecutionTime = Convert.ToDateTime(hwmDeltaValueStr, CultureInfo.InvariantCulture);            

            // extract delta information
            DateTime lastWorkITemUpdateTime = DateTime.MinValue;
            var inMaxDeltaSliceSize = maxDeltaSliceSize;
            var changeGroups = new List<ChangeGroup>();
            foreach (TfsMigrationWorkItem tfsMigrationWorkItem in items)
            {
                if (tfsMigrationWorkItem.WorkItem == null)
                {
                    continue;
                }

                // Compute per Work Item delta
                TraceManager.TraceInformation(TfsWITAdapterResources.StartingComputeDelta);
                List<ChangeGroup> itemDeltaChangeGroups = ComputeDelta(tfsMigrationWorkItem, hwmDeltaValue);
                TraceManager.TraceInformation(TfsWITAdapterResources.FinishedComputeDelta);

                foreach (ChangeGroup group in itemDeltaChangeGroups)
                {
                    if (group.Actions.Count == 0)
                    {
                        continue;
                    }

                    changeGroups.Add(group);
                    maxDeltaSliceSize -= group.Actions.Count;
                }

                if (maxDeltaSliceSize <= 0)
                {
                    // size limit reached - persist groups to DB
                    SaveDeltaEntries(changeGroups);
                    changeGroups.Clear();
                    maxDeltaSliceSize = inMaxDeltaSliceSize;
                }

                DateTime lastRevChangedDate = tfsMigrationWorkItem.WorkItem.ChangedDate;

                if (lastWorkITemUpdateTime.CompareTo(lastRevChangedDate) <= 0)
                {
                    lastWorkITemUpdateTime = lastRevChangedDate;
                }
            }

            // persist remaining groups to DB
            SaveDeltaEntries(changeGroups);

            // update primary Highwater Mark
            string newHwmValueStr = hwmDeltaValueStr;
            if (lastWorkITemUpdateTime.Equals(DateTime.MinValue))
            {
                // no changes in this sync cycle, record the wiql query execution time
                m_hwmDelta.Update(wiqlExecutionTime);
            }
            else
            {
                // hwm is recorded in UTC, so does the WIQL query asof time
                lastWorkITemUpdateTime = lastWorkITemUpdateTime.ToUniversalTime();

                if (lastWorkITemUpdateTime.CompareTo(wiqlExecutionTime) <= 0)
                {
                    // last work item rev time is earlier than wiql query execution time, use it as hwm
                    m_hwmDelta.Update(lastWorkITemUpdateTime);
                    newHwmValueStr = lastWorkITemUpdateTime.ToString();
                }
                else
                {
                    m_hwmDelta.Update(wiqlExecutionTime);
                }
            }
            TraceManager.TraceInformation("Persisted WIT HWM: {0}", Toolkit.Constants.HwmDeltaWit);
            TraceManager.TraceInformation(TfsWITAdapterResources.UpdatedHighWatermark, newHwmValueStr);
        }

        private void SaveDeltaEntries(List<ChangeGroup> changeGroups)
        {
            // persist groups to DB
            long lastChangeGroupId = 0;
            foreach (ChangeGroup group in changeGroups)
            {
                try
                {
                    // cache change group info, because after ChangeGroup.Save
                    // all change action information will be disposed
                    int wiId = int.Parse(group.Actions.First().FromPath);
                    int rev;
                    bool saveRev = int.TryParse(group.Actions.First().Version, out rev);                    

                    group.Save();

                    if (saveRev)
                    {
                        UpdateProcessedRev(wiId, rev);
                    }
                    lastChangeGroupId = group.ChangeGroupId;
                }
                catch (Exception)
                {
                    // throw away the cached last processed rev in this session run
                    m_lastProcessedWorkItemRevCache.Clear();
                    throw;
                }
            }
            m_changeGroupService.PromoteDeltaToPending();
            m_lastDeltaChangeGroupId = lastChangeGroupId;
        }

        private void UpdateProcessedRev(int wiId, int rev)
        {
            // update in-memory cache (both current cache and CacheCpy)
            // this update m_lastProcessedWorkItemRevCahce, hance must be called first
            UpdateCaches(wiId, rev);
            
            Dictionary<string, string> itemRevisionPairs = new Dictionary<string, string>();
            itemRevisionPairs.Add(wiId.ToString(), m_lastProcessedWorkItemRevCache[wiId].ToString());
            TranslationService.UpdateLastProcessedItemVersion(itemRevisionPairs, m_lastDeltaChangeGroupId, new Guid(m_migrationSource.UniqueId));
        }

        /// <summary>
        /// Generate the delta table.
        /// </summary>
        public virtual void GenerateDeltaTable()
        {
            try
            {
                GenerateNextDeltaSlice(MaxChangeActionsInSlice);
            }
            catch (Exception exception)
            {
                if (exception is MigrationUnresolvedConflictException)
                {
                    return;
                }

                ErrorManager errMgr = m_analysisServiceContainer.GetService(typeof(ErrorManager)) as ErrorManager;
                errMgr.TryHandleException(exception);
                return;
            }
        }

        public void DetectConflicts(ChangeGroup group)
        {
            TfsMigrationWorkItemStore store = m_migrationSource.WorkItemStore;
            Debug.Assert(null != store, "store is NULL");

            if (!store.ByPassrules)
            {
                foreach (IMigrationAction action in group.Actions)
                {
                    if (action.State == ActionState.Skipped)
                    {
                        continue;
                    }

                    // Try apply changes
                    if (action.MigrationActionDescription == null
                        || action.MigrationActionDescription.DocumentElement == null)
                    {
                        throw new MigrationException(TfsWITAdapterResources.ErrorInvalidActionDescription, action.ActionId);
                    }

                    if (action.Action == WellKnownChangeActionId.Add)
                    {
                        var workItem = store.CreateNewWorkItem(action, m_conflictManagerService);
                        store.TryApplyWitDataChanges(action, workItem, true, m_configurationService.MigrationPeer, m_conflictManagerService);
                    }
                    else if (action.Action == WellKnownChangeActionId.Edit)
                    {
                        // check if work item is in backlog only; if it is, this revision will be backlogged automatically
                        store.IsSourceWorkItemInBacklog(m_conflictManagerService, action);
                    }
                    else
                    {
                        // skip detecting attachment-related conflicts
                        continue;
                    }
                }
            }
        }

        #endregion

        public virtual string GetNativeId(MigrationSource migrationSourceConfig)
        {
            Microsoft.TeamFoundation.Client.TeamFoundationServer tfsServer =
                Microsoft.TeamFoundation.Client.TeamFoundationServerFactory.GetServer(migrationSourceConfig.ServerUrl);
            return tfsServer.InstanceId.ToString();
        }

        /// <summary>
        /// Returns field value comparer used on the TFS side.
        /// </summary>
        public virtual FieldValueComparer TfsValueComparer
        {
            get
            {
                return new FieldValueComparer(StringComparer.InvariantCultureIgnoreCase);
            }
        }

        #region private helpers

        private void RegisterSupportedContentTypes()
        {
            m_supportedContentTypes = new Collection<ContentType>();

            // basic work item content type
            m_supportedContentTypes.Add(WellKnownContentType.WorkItem);

            // work item metadata content type
            m_supportedContentTypes.Add(WellKnownContentType.GenericWorkItemFieldMetadata);
            m_supportedContentTypes.Add(WellKnownContentType.Tfs2005WorkItemFieldMetadata);
            m_supportedContentTypes.Add(WellKnownContentType.Tfs2008WorkItemFieldMetadata);
            m_supportedContentTypes.Add(WellKnownContentType.UserGroupList);
            m_supportedContentTypes.Add(WellKnownContentType.ValueListCollection);
            m_supportedContentTypes.Add(s_CssNodeChangesContentType);
        }

        private void AddOrcasCompatibleWITD(ChangeGroup group)
        {
            Project p = m_migrationSource.WorkItemStore.WorkItemStore.Projects[m_migrationSource.WorkItemStore.Core.Config.Project];
            XmlDocument workItemTypesDoc = m_migrationSource.WorkItemStore.GetWorkItemTypes(p);

            byte[] newDocHash = new byte[0];
            bool hashMatched = m_md5Utility.CompareDocHash(workItemTypesDoc, m_witdDocMD5, ref newDocHash);
            if (!hashMatched)
            {
                group.CreateAction(
                    WellKnownChangeActionId.SyncContext,
                    new WorkItemContextSyncMigrationItem(WellKnownContentType.Tfs2008WorkItemFieldMetadata),
                    WellKnownContentType.Tfs2008WorkItemFieldMetadata.FriendlyName,
                    "",
                    "0",
                    "",
                    WellKnownContentType.Tfs2008WorkItemFieldMetadata.ReferenceName,
                    workItemTypesDoc);
                m_md5Utility.UpdateDocHash(ref m_witdDocMD5, newDocHash);
            }
        }

        private void AddUserGroup(ChangeGroup group)
        {
            Project p = m_migrationSource.WorkItemStore.WorkItemStore.Projects[m_migrationSource.WorkItemStore.Core.Config.Project];

            XmlDocument groupsDoc = new XmlDocument();
            XmlElement root = groupsDoc.CreateElement("UserGroups");
            groupsDoc.AppendChild(root);
            XmlElement globGroupNode = groupsDoc.CreateElement("GlobalGroups");
            root.AppendChild(globGroupNode);
            XmlElement projGroupNode = groupsDoc.CreateElement("ProjectGroups");
            root.AppendChild(projGroupNode);

            Identity[] globalGroups = m_migrationSource.WorkItemStore.GetGlobalGroups(p);
            AddToGroupsDoc(groupsDoc, globGroupNode, globalGroups);

            Identity[] projectGroups = m_migrationSource.WorkItemStore.GetProjectGroups(p);
            AddToGroupsDoc(groupsDoc, projGroupNode, projectGroups);

            byte[] newDocHash = new byte[0];
            bool hashMatched = m_md5Utility.CompareDocHash(groupsDoc, m_userAccountsDocMD5, ref newDocHash);
            if (!hashMatched)
            {
                group.CreateAction(
                    WellKnownChangeActionId.SyncContext,
                    new WorkItemContextSyncMigrationItem(WellKnownContentType.UserGroupList),
                    WellKnownContentType.UserGroupList.FriendlyName,
                    "",
                    "0",
                    "",
                    WellKnownContentType.UserGroupList.ReferenceName,
                    groupsDoc);
                m_md5Utility.UpdateDocHash(ref m_userAccountsDocMD5, newDocHash);
            }
        }

        private void AddToGroupsDoc(XmlDocument groupsDoc, XmlElement groupRootNode, Identity[] groups)
        {
            foreach (Identity i in groups)
            {
                StringBuilder sb = new StringBuilder();
                XmlWriter wr = XmlWriter.Create(sb);
                XmlDocument identityDoc = new XmlDocument();

                if (null == wr)
                {
                    throw new InvalidOperationException("XmlWriter wr is null");
                }

                XmlSerializer serializer = new XmlSerializer(typeof(Identity));
                serializer.Serialize(wr, i);

                identityDoc.LoadXml(sb.ToString());
                XmlNode identityNode = groupsDoc.ImportNode(identityDoc.DocumentElement, true);
                groupRootNode.AppendChild(identityNode);
            }
        }

        private void AddGlobalList(ChangeGroup group)
        {
            Project p = m_migrationSource.WorkItemStore.WorkItemStore.Projects[m_migrationSource.WorkItemStore.Core.Config.Project];
            //TODO: support "Ignored Lists"
            XmlDocument globalListDoc = m_migrationSource.WorkItemStore.GetGlobalList(p, new List<string>().AsReadOnly());

            if (null == globalListDoc)
            {
                return;
            }

            byte[] newDocHash = new byte[0];
            bool hashMatched = m_md5Utility.CompareDocHash(globalListDoc, m_globalListDocMD5, ref newDocHash);
            if (!hashMatched)
            {
                group.CreateAction(
                    WellKnownChangeActionId.SyncContext,
                    new WorkItemContextSyncMigrationItem(WellKnownContentType.ValueListCollection),
                    WellKnownContentType.ValueListCollection.FriendlyName,
                    "",
                    "0",
                    "",
                    WellKnownContentType.ValueListCollection.ReferenceName,
                    globalListDoc);
                m_md5Utility.UpdateDocHash(ref m_globalListDocMD5, newDocHash);
            }
        }
        
        /// <summary>
        /// extract un-synced revisions with details of the item and persist them to db
        /// </summary>
        /// <param name="item"></param>
        /// <param name="tfsWitWaterMark"></param>
        /// <returns></returns>
        private List<ChangeGroup> ComputeDelta(TfsMigrationWorkItem item, DateTime waterMarkChangeStartTime)
        {
            List<ChangeGroup> groups = new List<ChangeGroup>();
            ComputeFieldDelta(item, waterMarkChangeStartTime, groups);
            ComputeAttachmentDelta(item, waterMarkChangeStartTime, groups);

            return groups;
        }

        private void ComputeAttachmentDelta(TfsMigrationWorkItem item, DateTime waterMarkChangeStartTime, List<ChangeGroup> groups)
        {
            if (!m_supportedChangeActionsOther.Contains(WellKnownChangeActionId.AddAttachment) &&
                !m_supportedChangeActionsOther.Contains(WellKnownChangeActionId.DelAttachment))
            {
                return;
            }

            item.ComputeAttachmentDelta(m_changeGroupService, 
                                        waterMarkChangeStartTime, 
                                        TranslationService,
                                        m_configurationService.SourceId,
                                        groups);
        }

        private void ComputeFieldDelta(TfsMigrationWorkItem item, DateTime waterMarkChangeStartTime, List<ChangeGroup> groups)
        {
            TraceManager.TraceInformation(string.Format(
                "Start generating revision delta information for Work Item #{0} at {1}",
                item.WorkItem.Id,
                DateTime.Now.ToString("o")));

            item.ComputeFieldDelta(m_changeGroupService, 
                                   waterMarkChangeStartTime, 
                                   TfsValueComparer, 
                                   TranslationService, 
                                   m_configurationService,
                                   groups, 
                                   IsWorkItemRevisionProcessed);
        }

        private bool IsWorkItemRevisionProcessed(int wiId, int rev)
        {
            if (IsLastRevInCache(wiId))
            {
                return rev <= m_lastProcessedWorkItemRevCache[wiId];
            }

            if (IsRevInStorage(wiId, rev))
            {
                return true;
            }

            return false;
        }

        private void UpdateCaches(int wiId, int rev)
        {
            const int CacheSize = 100000;
            if (m_lastProcessedWorkItemRevCache.Count() > CacheSize)
            {
                TraceManager.TraceInformation("WIT last delta revision cache max size is reached - start cleaning up job.");
                var keys = m_lastProcessedWorkItemRevCache.Keys;
                for (int i = 0; i < keys.Count() && i <= CacheSize / 2; ++i)
                {
                    m_lastProcessedWorkItemRevCache.Remove(keys.ElementAt(i));
                }
                TraceManager.TraceInformation("WIT last delta revision cache cleaning up job finishes.");
            }

            if (!m_lastProcessedWorkItemRevCache.ContainsKey(wiId))
            {
                m_lastProcessedWorkItemRevCache.Add(wiId, 0);
            }
            if (rev > m_lastProcessedWorkItemRevCache[wiId])
            {
                m_lastProcessedWorkItemRevCache[wiId] = rev;
            }
        }

        private bool IsRevInStorage(int wiId, int rev)
        {
            string lastRevInStorage = m_translationService.GetLastProcessedItemVersion(wiId.ToString(), new Guid(this.m_migrationSource.UniqueId));
            
            // if the last rev is not in storage, we use 0 as last rev for TFS
            if (string.IsNullOrEmpty(lastRevInStorage))
            {
                lastRevInStorage = "0";
            }

            int lastRev = int.Parse(lastRevInStorage);

            UpdateCaches(wiId, lastRev);

            return rev <= lastRev;
        }

        private bool IsLastRevInCache(int wiId)
        {
            return m_lastProcessedWorkItemRevCache.ContainsKey(wiId);
        }

        protected virtual TfsMigrationDataSource InitializeMigrationDataSource()
        {
            return new TfsMigrationDataSource();
        }

        private void InitializeTfsClient()
        {
            Microsoft.TeamFoundation.Migration.BusinessModel.MigrationSource migrationSourceConfiguration = m_configurationService.MigrationSource;
            Debug.Assert(null != migrationSourceConfiguration, "cannot get MigrationSource config from Session");


            TfsMigrationDataSource dataSourceConfig = InitializeMigrationDataSource();
            ReadOnlyCollection<MappingEntry> filters = m_configurationService.Filters;
            // Allow multiple filter strings from other adapters
            // Debug.Assert(filters.Count == 1, "filters.Count != 1 for WIT migration source");
            dataSourceConfig.Filter = filters[0].Path;
            dataSourceConfig.ServerId = migrationSourceConfiguration.ServerIdentifier;
            dataSourceConfig.ServerName = migrationSourceConfiguration.ServerUrl;
            dataSourceConfig.Project = migrationSourceConfiguration.SourceIdentifier;

            this.m_migrationSource = new TfsWITMigrationSource(
                migrationSourceConfiguration.InternalUniqueId, 
                dataSourceConfig.CreateWorkItemStore());
            this.m_migrationSource.WorkItemStore.ServiceContainer = this.m_analysisServiceContainer;

            bool? enableReflectedIdInsertion = null;
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
                else if (setting.SettingKey.Equals(TfsConstants.ReflectedWorkItemIdFieldReferenceName, StringComparison.OrdinalIgnoreCase))
                {
                    m_migrationSource.WorkItemStore.ReflectedWorkItemIdFieldReferenceName = setting.SettingValue;
                }
                else if (setting.SettingKey.Equals(TfsConstants.EnableInsertReflectedWorkItemId))
                {
                    bool val;
                    if (bool.TryParse(setting.SettingValue, out val))
                    {
                        enableReflectedIdInsertion = val;
                    }
                }
            }

            if (!enableReflectedIdInsertion.HasValue)
            {
                // default to enable
                enableReflectedIdInsertion = true;
                m_migrationSource.WorkItemStore.EnableInsertReflectedWorkItemId = enableReflectedIdInsertion.Value;
            }

            if (string.IsNullOrEmpty(m_migrationSource.WorkItemStore.ReflectedWorkItemIdFieldReferenceName))
            {
                m_migrationSource.WorkItemStore.ReflectedWorkItemIdFieldReferenceName = TfsConstants.MigrationTracingFieldRefName;
            }
        }

        private void RegisterSupportedChangeActions()
        {
            m_tfsWitChangeActionHandlers = new BasicChangeActionHandlers(this);
            m_supportedChangeActions = new Dictionary<Guid, ChangeActionHandler>();
            m_supportedChangeActions.Add(WellKnownChangeActionId.Add, this.m_tfsWitChangeActionHandlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Delete, this.m_tfsWitChangeActionHandlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Edit, this.m_tfsWitChangeActionHandlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.AddAttachment, this.m_tfsWitChangeActionHandlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.DelAttachment, this.m_tfsWitChangeActionHandlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.SyncContext, this.m_tfsWitChangeActionHandlers.BasicActionHandler);
        }

        private ITranslationService TranslationService
        {
            get
            {
                // lazy loading translation service
                if (null == m_translationService)
                {
                    m_translationService = m_analysisServiceContainer.GetService(typeof(ITranslationService)) as ITranslationService;
                    Debug.Assert(m_translationService != null, "Translation service is not initialized");
                }

                return m_translationService;
            }
        }
        #endregion

        private Dictionary<Guid, ChangeActionHandler> m_supportedChangeActions;
        private ICollection<Guid> m_supportedChangeActionsOther;
        private Collection<ContentType> m_supportedContentTypes;
        private Dictionary<string, ContentType> m_supportedContentTypeOther;

        private ChangeActionRegistrationService m_changeActionRegistrationService;
        private ConflictManager m_conflictManagerService;
        private ChangeGroupService m_changeGroupService;
        private ConfigurationService m_configurationService;
        private ITranslationService m_translationService;

        private IServiceContainer m_analysisServiceContainer;
        private BasicChangeActionHandlers m_tfsWitChangeActionHandlers;
        private TfsWITMigrationSource m_migrationSource;
        private HighWaterMark<DateTime> m_hwmDelta;
        private HighWaterMark<int> m_hwmCSSMaxSeqId;
        private const int MaxChangeActionsInSlice = 500;

        private long m_lastDeltaChangeGroupId = 0;
        private Dictionary<int, int> m_lastProcessedWorkItemRevCache = new Dictionary<int, int>();

        private const string ContextInformGroupName = "Context Information";
        private static readonly ContentType s_CssNodeChangesContentType =
            new ContentType(TfsConstants.TfsCSSNodeChangesContentTypeRefName, TfsConstants.TfsCSSNodeChangesContentTypeDispName);

        private readonly Md5HashUtility m_md5Utility = new Md5HashUtility();
        private byte[] m_globalListDocMD5 = new byte[0];
        private byte[] m_userAccountsDocMD5 = new byte[0];
        private byte[] m_witdDocMD5 = new byte[0];
    }
}
