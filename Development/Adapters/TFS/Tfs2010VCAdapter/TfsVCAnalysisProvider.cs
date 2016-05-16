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
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon;

namespace Microsoft.TeamFoundation.Migration.Tfs2010VCAdapter
{
    public class TfsVCAnalysisProvider : IAnalysisProvider
    {
        private readonly ChangeType[] m_validChangeTypes = new ChangeType[]
        {
            ChangeType.Add,
            ChangeType.Add | ChangeType.Edit,
            ChangeType.Branch,
            ChangeType.Branch | ChangeType.Merge,
            ChangeType.Branch | ChangeType.Merge | ChangeType.Delete,
            ChangeType.Branch | ChangeType.Merge | ChangeType.Edit,
            ChangeType.Rename,
            ChangeType.Rename | ChangeType.Edit,
            ChangeType.Rename | ChangeType.Undelete,
            ChangeType.Rename | ChangeType.Merge,
            ChangeType.Rename | ChangeType.Merge | ChangeType.Delete,
            ChangeType.Rename | ChangeType.Edit | ChangeType.Merge,
            ChangeType.Rename | ChangeType.Undelete | ChangeType.Merge,
            ChangeType.Rename | ChangeType.Edit | ChangeType.Undelete,
            ChangeType.Rename | ChangeType.Edit | ChangeType.Undelete | ChangeType.Merge,
            ChangeType.Rename | ChangeType.Delete | ChangeType.Undelete,
            ChangeType.Rename | ChangeType.Delete,
            ChangeType.Merge,
            ChangeType.Merge | ChangeType.Delete,
            ChangeType.Merge | ChangeType.Delete | ChangeType.Undelete,
            ChangeType.Merge | ChangeType.Rename | ChangeType.Delete | ChangeType.Undelete,
            ChangeType.Merge | ChangeType.Undelete,
            ChangeType.Merge | ChangeType.Edit,
            ChangeType.Merge | ChangeType.Edit | ChangeType.Undelete,
            ChangeType.Merge | ChangeType.Rename | ChangeType.Add,
            ChangeType.Merge | ChangeType.Rename | ChangeType.Add | ChangeType.Undelete,
            ChangeType.Branch | ChangeType.Edit,
            ChangeType.Branch | ChangeType.Delete,
            ChangeType.Undelete,
            ChangeType.Delete,
            ChangeType.Edit,
            ChangeType.Edit | ChangeType.Undelete,
            ChangeType.Delete | ChangeType.Undelete,
            ChangeType.None,
            ChangeType.Rollback,
            ChangeType.SourceRename
        };

        Dictionary<Guid, ChangeActionHandler> m_supportedChangeActions;
        ICollection<Guid> m_supportedChangeActionsOther;
        Collection<ContentType> m_supportedContentTypes;
        Collection<ContentType> m_supportedContentTypesOther;

        ChangeActionRegistrationService m_changeActionRegistrationService;
        ChangeGroupService m_changeGroupService; // Cache change group service as it is accessed frequently.
        ConflictManager m_conflictManagementService;
        ITranslationService m_translationService;

        IServiceContainer m_analysisServiceContainer;
        VersionControlServer m_tfsClient;
        TfsAnalysisAlgorithms m_algorithms;
        TfsChangeActionHandlers m_tfsChangeActionHandlers;
        HighWaterMark<int> m_hwmDelta;

        Dictionary<string, int> m_processedSnapShotChangeset = new Dictionary<string, int>();
        Dictionary<string, int> m_unprocessedSnapshotChangeset = new Dictionary<string,int>();

        bool m_sessionlevelSnapshotCompleted = false;
        int m_sessionLevelSnapshotChangeset = -1;

        int m_snapshotCheckinBatchSize = 100 * 1000;

        internal HighWaterMark<int> HwmDelta
        {
            get
            {
                return m_hwmDelta;
            }
        }

        internal ConfigurationService ConfigurationService
        {
            get;
            private set;
        }

        #region implementation of interface members
        /// <summary>
        /// List of change actions supported by TfsVCAdapter.
        /// </summary>
        public Dictionary<Guid, ChangeActionHandler> SupportedChangeActions
        {
            get
            {
                return m_supportedChangeActions;
            }
        }

        /// <summary>
        /// List of change actions supported by the other side.
        /// </summary>
        public ICollection<Guid> SupportedChangeActionsOther
        {
            set
            {
                m_supportedChangeActionsOther = value;
            }
        }

        /// <summary>
        /// List of content types supported by this provider
        /// </summary>
        public Collection<ContentType> SupportedContentTypes
        {
            get
            {
                return m_supportedContentTypes;
            }
        }

        internal ConflictManager ConflictManager
        {
            get
            {
                return m_conflictManagementService;
            }
        }

        /// <summary>
        /// List of content types supported by the other side
        /// </summary>
        public Collection<ContentType> SupportedContentTypesOther
        {
            set
            {
                m_supportedContentTypesOther = value;
            }
        }

        /// <summary>
        /// Register all conflict types with the conflict manager.
        /// </summary>
        /// <param name="conflictManager"></param>
        public virtual void RegisterConflictTypes(ConflictManager conflictManager)
        {
            if (conflictManager == null)
            {
                throw new ArgumentNullException("conflictManager");
            }

            m_conflictManagementService = conflictManager;
            m_conflictManagementService.RegisterConflictType(new VCInvalidPathConflictType());
            m_conflictManagementService.RegisterConflictType(new VCPathNotMappedConflictType());
            m_conflictManagementService.RegisterConflictType(new VCBranchParentNotFoundConflictType());
            m_conflictManagementService.RegisterConflictType(new GenericConflictType());
            m_conflictManagementService.RegisterConflictType(new TFSHistoryNotFoundConflictType());
            m_conflictManagementService.RegisterConflictType(new UnhandledChangeTypeConflictType(m_validChangeTypes));
        }

        /// <summary>
        /// Initialize TfsVCAdapter 
        /// </summary>
        public void InitializeServices(IServiceContainer analysisServiceContainer)
        {
            m_analysisServiceContainer = analysisServiceContainer;

            ConfigurationService = (ConfigurationService)m_analysisServiceContainer.GetService(typeof(ConfigurationService));

            m_hwmDelta = new HighWaterMark<int>(Constants.HwmDelta);
            ConfigurationService.RegisterHighWaterMarkWithSession(m_hwmDelta);
            m_changeGroupService = (ChangeGroupService)m_analysisServiceContainer.GetService(typeof(ChangeGroupService));
            m_changeGroupService.RegisterDefaultSourceSerializer(new TfsMigrationItemSerialzier());
        }

        /// <summary>
        /// Perform the adapter-specific initialization
        /// </summary>
        public virtual void InitializeClient()
        {
            initializeTfsClient();
            initializeSnapshotTable();
        }

        /// <summary>
        /// 1. Read snapshot information from configuration.
        /// 2. Populate the 2 in-memory snapshot table
        /// </summary>
        private void initializeSnapshotTable()
        {
            m_hwmDelta.Reload();
            // Read snapshot information from configuration.
            foreach (MappingEntry mappingEntry in ConfigurationService.Filters)
            {
                if ((!string.IsNullOrEmpty(mappingEntry.SnapshotStartPoint)) && !mappingEntry.Cloak)
                {
                    int snapshotChangeset;
                    try
                    {
                        snapshotChangeset = int.Parse(mappingEntry.SnapshotStartPoint);

                        int peerSnapshotChangeset;
                        if (int.TryParse(mappingEntry.PeerSnapshotStartPoint, out peerSnapshotChangeset))
                        {
                            // create conversion history
                            ConversionResult convRslt = new ConversionResult(ConfigurationService.SourceId, ConfigurationService.MigrationPeer);
                            convRslt.ChangeId = mappingEntry.PeerSnapshotStartPoint;
                            convRslt.ItemConversionHistory.Add(new ItemConversionHistory(mappingEntry.SnapshotStartPoint, string.Empty, convRslt.ChangeId, string.Empty));
                            try
                            {
                                convRslt.Save(ConfigurationService.SourceId);
                            }
                            catch (System.Data.DataException)
                            {
                                // conversion history already updated
                            }
                            continue;
                        }
                    }
                    catch (Exception e)
                    {
                        throw new MigrationException(
                            string.Format(TfsVCAdapterResource.Culture, TfsVCAdapterResource.PathSnapShotFormatError, mappingEntry.SnapshotStartPoint),
                            e);
                    }
                    if (snapshotChangeset < m_hwmDelta.Value)
                    {
                        m_processedSnapShotChangeset.Add(mappingEntry.Path, snapshotChangeset);
                    }
                    else
                    {
                        m_unprocessedSnapshotChangeset.Add(mappingEntry.Path, snapshotChangeset);
                    }
                }
            }
        }

        /// <summary>
        /// For a given mapping entry, test to see wether the snapshot start point is less than current hwm or not.
        /// </summary>
        /// <param name="mappingEntry"></param>
        /// <returns></returns>
        public bool ValidateSnapshotSetting()
        {
            m_hwmDelta.Reload();
            int pathLevelSnapshotChangeset;

            foreach (var setting in ConfigurationService.VcCustomSetting.Settings.Setting)
            {
                if (setting.SettingKey == "SnapshotStartPoint")
                {
                    m_sessionLevelSnapshotChangeset = parseSnapShotStartPoint(setting.SettingValue);
                    if (m_sessionLevelSnapshotChangeset <= m_hwmDelta.Value)
                    {
                        TraceManager.TraceError(
                            string.Format("Session level snapshot changeset {0} is not larger thant the current high water mark {1}", 
                            m_sessionLevelSnapshotChangeset, 
                            m_hwmDelta.Value));
                        return false;
                    }
                    break;
                }
            }

            foreach (MappingEntry mappingEntry in ConfigurationService.Filters)
            {
                try
                {
                    pathLevelSnapshotChangeset = int.Parse(mappingEntry.SnapshotStartPoint);
                    if (pathLevelSnapshotChangeset < m_sessionLevelSnapshotChangeset)
                    {
                        TraceManager.TraceError(
                            string.Format("Path level snapshot changeset {0} is less than the session level snapshot changeset {1}.",
                            pathLevelSnapshotChangeset, 
                            m_sessionLevelSnapshotChangeset));
                        return false;
                    }
                    if (pathLevelSnapshotChangeset <= m_hwmDelta.Value)
                    {
                        TraceManager.TraceError(
                            string.Format("path level snapshot changeset {0} is not larger than the current high water mark {1}",
                            pathLevelSnapshotChangeset,
                            m_hwmDelta.Value));
                        return false;
                    }
                }
                catch (Exception e)
                {
                    throw new MigrationException(
                        string.Format(TfsVCAdapterResource.Culture, TfsVCAdapterResource.PathSnapShotFormatError, mappingEntry.SnapshotStartPoint),
                        e);
                }
            }
            return true;
        }

        private void initializeSupportedContentTypes()
        {
            m_supportedContentTypes = new Collection<ContentType>();
            m_supportedContentTypes.Add(WellKnownContentType.VersionControlChangeGroup);
            m_supportedContentTypes.Add(WellKnownContentType.VersionControlledFile);
            m_supportedContentTypes.Add(WellKnownContentType.VersionControlledFolder);
            m_supportedContentTypes.Add(WellKnownContentType.VersionControlledArtifact);
            m_supportedContentTypes.Add(WellKnownContentType.VersionControlLabel);
            m_supportedContentTypes.Add(WellKnownContentType.VersionControlLabelItem);
            m_supportedContentTypes.Add(WellKnownContentType.VersionControlRecursiveLabelItem);
            m_supportedContentTypes.Add(WellKnownContentType.VersionControlLabel);
            m_supportedContentTypes.Add(WellKnownContentType.VersionControlLabelItem);
            m_supportedContentTypes.Add(WellKnownContentType.VersionControlRecursiveLabelItem);
        }

        /// <summary>
        /// Register all supported change actions with ChangeActionRegistrationService
        /// </summary>
        /// <param name="changeActionRegistrationService"></param>
        public void RegisterSupportedChangeActions(ChangeActionRegistrationService changeActionRegistrationService)
        {
            if (changeActionRegistrationService == null)
            {
                throw new ArgumentNullException("changeActionRegistrationService");
            }

            initiazlieSupportedChangeActions();

            m_changeActionRegistrationService = changeActionRegistrationService;

            foreach (KeyValuePair<Guid, ChangeActionHandler> supportedChangeAction in m_supportedChangeActions)
            {
                // note: for now, VC adapter uses a single change action handler for all content types
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
        public void RegisterSupportedContentTypes(ContentTypeRegistrationService contentTypeRegistrationService)
        {
            initializeSupportedContentTypes();
        }

        /// <summary>
        /// Generate the context info table
        /// </summary>
        public void GenerateContextInfoTable()
        {
        }

        private int parseSnapShotStartPoint(string snapShotValue)
        {
            int seperatorIndex = snapShotValue.IndexOf(';');
            if (seperatorIndex < 0)
            {
                throw new MigrationException(string.Format(TfsVCAdapterResource.Culture, TfsVCAdapterResource.InvalidSnapShotString, snapShotValue));
            }
            else
            {
                try
                {
                    Guid sourceId = new Guid(snapShotValue.Substring(0, seperatorIndex));
                    if (sourceId == m_conflictManagementService.SourceId)
                    {
                        return int.Parse(snapShotValue.Substring(seperatorIndex + 1));
                    }
                    else
                    {
                        return 0;
                    }
                }
                catch (FormatException)
                {
                    throw new MigrationException(string.Format(TfsVCAdapterResource.Culture, TfsVCAdapterResource.InvalidSnapShotString, snapShotValue));
                }
            }
        }

        /// <summary>
        /// Create a changegroup that contains all change actions needed to bring a migration target to the specificed snapshot
        /// </summary>
        /// <param name="changeGroupName">The change group name of the snapshot</param>
        private void generateSnapshotForVCSession()
        {
            foreach (var setting in ConfigurationService.VcCustomSetting.Settings.Setting)
            {
                if (setting.SettingKey == "SnapshotStartPoint")
                {
                    m_sessionLevelSnapshotChangeset = parseSnapShotStartPoint(setting.SettingValue);
                }
                else if (setting.SettingKey == "SnapshotBatchSize")
                {
                    try
                    {
                        m_snapshotCheckinBatchSize = int.Parse(setting.SettingValue);
                    }
                    catch (Exception)
                    {
                        // wrong format, use the default batch size
                    }
                }
            }

            m_hwmDelta.Reload();
            if (m_hwmDelta.Value >= m_sessionLevelSnapshotChangeset)
            {
                // We've already passed snapshot changeset Id, just return. 
                m_sessionlevelSnapshotCompleted = true;
                return;
            }

            VersionSpec snapshotVersionSpec = new ChangesetVersionSpec(m_sessionLevelSnapshotChangeset);
            List<string> pathsToGet = new List<string>();

            foreach (MappingEntry mappingEntry in ConfigurationService.Filters)
            {
                if (mappingEntry.Cloak)
                {
                    continue;
                }

                // Always query at one level down the mapping
                ItemSet itemSet = m_tfsClient.GetItems(mappingEntry.Path, snapshotVersionSpec, RecursionType.OneLevel);
                Item[] items = itemSet.Items;
                foreach (Item childItem in items)
                {
                    // Avoid the item itself. 
                    if (!VersionControlPath.Equals(childItem.ServerItem, mappingEntry.Path))
                    {
                        pathsToGet.Add(childItem.ServerItem);
                    }
                }
            }

            int countDownToCreateNewChangeGroup = m_snapshotCheckinBatchSize;
            int batchExecutionOrder = m_sessionLevelSnapshotChangeset;
            ChangeGroup batchGroup = createChangeGroupForSnapshot(m_sessionLevelSnapshotChangeset, batchExecutionOrder);

            foreach (string path in pathsToGet)
            {
                TraceManager.TraceInformation("Getting snapshot at changeset: {0}, path: {1}", m_sessionLevelSnapshotChangeset, path);
                ItemSet itemSet = m_tfsClient.GetItems(path, snapshotVersionSpec, RecursionType.Full);
                Item[] items = itemSet.Items;
                itemSet = null;
                TraceManager.TraceInformation("Snapshot contains {0} items", items.Length);
                for (int i = 0; i < items.Length; i++)
                {
                    // We want to include the situation where a snapshot on a path is the same as the snapshot of the VC session.
                    // In this situation, we want to include the item in the session snapshot changeset. 
                    // So we use changesetId + 1 as the reference changeset id.
                    if (IsPathMapped(items[i].ServerItem, m_sessionLevelSnapshotChangeset + 1) == MappingResult.Mapped)
                    {
                        countDownToCreateNewChangeGroup--;
                        if (countDownToCreateNewChangeGroup == 0)
                        {
                            TraceManager.TraceInformation("Saving {0} change actions", m_snapshotCheckinBatchSize);
                            batchGroup.Save();

                            batchGroup.Actions.Clear();
                            batchGroup = null;

                            batchExecutionOrder--;
                            batchGroup = createChangeGroupForSnapshot(m_sessionLevelSnapshotChangeset, batchExecutionOrder);
                            countDownToCreateNewChangeGroup = m_snapshotCheckinBatchSize;
                            TraceManager.TraceInformation("Saved {0} change actions", m_snapshotCheckinBatchSize);
                        }
                        batchGroup.CreateAction(
                            WellKnownChangeActionId.Add,
                            new TfsMigrationItem(items[i]),
                            null,
                            items[i].ServerItem,
                            null,
                            null,
                            TfsAnalysisAlgorithms.convertContentType(items[i].ItemType),
                            null);
                    }
                    items[i] = null; // Dispose this object to reduce memory consumption.
                }
            }

            if (batchGroup.Actions.Count > 0)
            {
                int numRemainingItems = batchGroup.Actions.Count;
                TraceManager.TraceInformation("Saving {0} change actions", numRemainingItems);
                batchGroup.Save();
                TraceManager.TraceInformation("Saved {0} change actions", numRemainingItems);
            }

            m_hwmDelta.Update(m_sessionLevelSnapshotChangeset);
            m_changeGroupService.PromoteDeltaToPending();
            m_sessionlevelSnapshotCompleted = true;
        }

        private void generateDeltaTableForSnapshot(List<string> paths, int snapshotChangeset)
        {
            List<string> pathsToGet = new List<string>();
            VersionSpec snapshotVersionSpec = new ChangesetVersionSpec(snapshotChangeset);
            foreach (string mappingPath in paths)
            {
                // Always query at one level down the mapping
                ItemSet itemSet = m_tfsClient.GetItems(mappingPath, snapshotVersionSpec, RecursionType.OneLevel);
                Item[] items = itemSet.Items;
                foreach (Item childItem in items)
                {
                    // Avoid the item itself. 
                    if (!VersionControlPath.Equals(childItem.ServerItem, mappingPath))
                    {
                        pathsToGet.Add(childItem.ServerItem);
                    }
                }
            }

            int countDownToCreateNewChangeGroup = m_snapshotCheckinBatchSize;
            ChangeGroup batchGroup = createChangeGroupForSnapshot(snapshotChangeset, snapshotChangeset);

            foreach (string path in pathsToGet)
            {
                TraceManager.TraceInformation("Getting snapshot at changeset: {0}, path: {1}", snapshotChangeset, path);
                ItemSet itemSet = m_tfsClient.GetItems(path, snapshotVersionSpec, RecursionType.Full);
                Item[] items = itemSet.Items;
                itemSet = null;
                TraceManager.TraceInformation("Snapshot contains {0} items", items.Length);
                for (int i = 0; i < items.Length; i++)
                {
                    if (FindMappedPath(items[i].ServerItem) != null)
                    {
                        countDownToCreateNewChangeGroup--;
                        if (countDownToCreateNewChangeGroup == 0)
                        {
                            TraceManager.TraceInformation("Saving {0} change actions", m_snapshotCheckinBatchSize);
                            batchGroup.Save();

                            batchGroup.Actions.Clear();
                            batchGroup = null;

                            batchGroup = createChangeGroupForSnapshot(snapshotChangeset, snapshotChangeset);
                            countDownToCreateNewChangeGroup = m_snapshotCheckinBatchSize;
                            TraceManager.TraceInformation("Saved {0} change actions", snapshotChangeset);
                        }
                        batchGroup.CreateAction(
                            WellKnownChangeActionId.Add,
                            new TfsMigrationItem(items[i]),
                            null,
                            items[i].ServerItem,
                            null,
                            null,
                            TfsAnalysisAlgorithms.convertContentType(items[i].ItemType),
                            null);
                    }
                    items[i] = null; // Dispose this object to reduce memory consumption.
                }
            }

            if (batchGroup.Actions.Count > 0)
            {
                int numRemainingItems = batchGroup.Actions.Count;
                TraceManager.TraceInformation("Saving {0} change actions", numRemainingItems);
                batchGroup.Save();
                TraceManager.TraceInformation("Saved {0} change actions", numRemainingItems);
            }
        }

        private ChangeGroup createChangeGroupForSnapshot(int snapShotChangeset, long executionOrder)
        {
            ChangeGroup group = m_changeGroupService.CreateChangeGroupForDeltaTable(snapShotChangeset.ToString());
            group.Owner = null;
            group.Comment = string.Format("Initial Check-in as snapshot at changeset {0}", snapShotChangeset);
            group.ChangeTimeUtc = DateTime.UtcNow;
            group.Status = ChangeStatus.Delta;
            group.ExecutionOrder = executionOrder;
            return group;
        }

        /// <summary>
        /// Generate the delta table.
        /// </summary>
        public void GenerateDeltaTable()
        {
            // 1. Process session level snapshot start point.
            if (!m_sessionlevelSnapshotCompleted)
            {
                generateSnapshotForVCSession();
            }

            int index = 0;
            int[] mappedChangesets = getMappedTfsChange();
            foreach (int changesetId in mappedChangesets)
            {
                index++;
                TraceManager.TraceInformation("Analyzing TFS change {0} : {1}/{2}", changesetId, index, mappedChangesets.Length);

                Changeset changeset = m_tfsClient.GetChangeset(changesetId, true, true);
                int actions = analyzeChangeset(changeset);

                TraceManager.TraceInformation("Created {0} actions for TFS change {1}",
                        actions, changesetId);

                // Processing snapshot changesets in mapped paths 
                if (m_unprocessedSnapshotChangeset.Count > 0)
                {
                    List<string> snapshotPaths = new List<string>();
                    foreach (KeyValuePair<string, int> unprocessedSnapshotChangeset in m_unprocessedSnapshotChangeset)
                    {
                        if (unprocessedSnapshotChangeset.Value <= changesetId)
                        {
                            snapshotPaths.Add(unprocessedSnapshotChangeset.Key);
                        }
                    }

                    if (snapshotPaths.Count > 0)
                    {
                        generateDeltaTableForSnapshot(snapshotPaths, changesetId);
                        foreach (string snapshotPathProcessed in snapshotPaths)
                        {
                            m_processedSnapShotChangeset.Add(snapshotPathProcessed, m_unprocessedSnapshotChangeset[snapshotPathProcessed]);
                            m_unprocessedSnapshotChangeset.Remove(snapshotPathProcessed);
                        }
                    }
                }

                m_hwmDelta.Update(changesetId);

                m_changeGroupService.PromoteDeltaToPending();
            }
        }

        public void DetectConflicts(ChangeGroup group)
        {
            return;
        }

        #endregion

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

        #region initialize methods
        private void initializeTfsClient()
        {
            TfsTeamProjectCollection tfsServer = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(ConfigurationService.ServerUrl));
            if (!TfsUtil.IsTfs2010Server(tfsServer))
            {
                throw new MigrationException(string.Format("The target server {0} is not a TFS2010 server", ConfigurationService.ServerUrl));
            }
            m_tfsClient = (VersionControlServer)tfsServer.GetService(typeof(VersionControlServer));
            m_analysisServiceContainer.AddService(typeof(VersionControlServer), m_tfsClient);
        }

        /// <summary>
        /// Initialize SupportedChangeActions list.
        /// </summary>
        private void initiazlieSupportedChangeActions()
        {
            m_tfsChangeActionHandlers = new TfsChangeActionHandlers(this);
            m_supportedChangeActions = new Dictionary<Guid, ChangeActionHandler>(11);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Add, m_tfsChangeActionHandlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Branch, m_tfsChangeActionHandlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Delete, m_tfsChangeActionHandlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Edit, m_tfsChangeActionHandlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Encoding, m_tfsChangeActionHandlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Label, m_tfsChangeActionHandlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Merge, m_tfsChangeActionHandlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.BranchMerge, m_tfsChangeActionHandlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Rename, m_tfsChangeActionHandlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Undelete, m_tfsChangeActionHandlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.AddFileProperties, m_tfsChangeActionHandlers.BasicActionHandler);
        }

        #endregion

        private int[] getMappedTfsChange()
        {
            m_hwmDelta.Reload();


            Debug.Assert(m_hwmDelta.Value >= 0, "High water mark of delta table must be non-negtive");

            int latestChangeset = m_tfsClient.GetLatestChangesetId();

            // No new changesets on server, return.
            if (m_hwmDelta.Value >= latestChangeset)
            {
                return new int[0];
            }

            int startingChangeset = m_hwmDelta.Value + 1;

            string skipComment = ConfigurationService.GetValue<string>(Constants.SkipComment, "**NOMIGRATION**");
            string commentModifier = ConfigurationService.GetValue<string>(Constants.CommentModifier, TfsVCAdapterResource.DefaultCommentModifier);

            SortedDictionary<int, bool> mappedChangesets = new SortedDictionary<int, bool>();

            foreach (MappingEntry m in ConfigurationService.Filters)
            {
                if (m.Cloak)
                {
                    continue;
                }
                try
                {
                    foreach (Changeset changeset in m_tfsClient.QueryHistory(m.Path,
                            VersionSpec.Latest,
                            0,
                            RecursionType.Full,
                            null,
                            new ChangesetVersionSpec(startingChangeset),
                            new ChangesetVersionSpec(latestChangeset),
                            int.MaxValue, // All changes
                            false,
                            true))
                    {
                        if (mappedChangesets.ContainsKey(changeset.ChangesetId))
                        {
                            continue;
                        }

                        if (TfsUtil.IsOurTfsChange(changeset, TranslationService, m_conflictManagementService.SourceId))
                        {
                            TraceManager.TraceInformation("Skipping mirrored change {0}", changeset.ChangesetId);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(skipComment))
                        {
                            if (changeset.Comment != null && changeset.Comment.Contains(skipComment))
                            {
                                TraceManager.TraceInformation("Changeset {0} contains the skip comment {1}",
                                    changeset.ChangesetId,
                                    skipComment);
                                continue;
                            }
                        }

                        if (!mappedChangesets.ContainsKey(changeset.ChangesetId))
                        {
                            mappedChangesets.Add(changeset.ChangesetId, true);
                        }
                    }
                }
                catch (ItemNotFoundException)
                {
                    // the path does not contain any changesets
                }
            }
            if (mappedChangesets.Count > 0)
            {
                int[] mappedChangesetsArray = new int[mappedChangesets.Count];
                mappedChangesets.Keys.CopyTo(mappedChangesetsArray, 0);
                return mappedChangesetsArray;
            }
            else
            {
                // No new changesets are found, update the HWM as current latest
                m_hwmDelta.Update(latestChangeset);
                return new int[0];
            }
        }

        /// <summary>
        /// Analyzes the TFS changeset to generate a change group.
        /// </summary>
        /// <param name="changeset"></param>
        /// <returns></returns>
        private int analyzeChangeset(Changeset changeset)
        {
            if (changeset == null)
            {
                throw new ArgumentNullException("changeset");
            }

            lazyInit();

            TraceManager.TraceInformation("Starting analysis of TFS change {0}", changeset.ChangesetId);
            
            int changeCount = 0;
            ChangeGroup group = m_changeGroupService.CreateChangeGroupForDeltaTable(changeset.ChangesetId.ToString(CultureInfo.InvariantCulture));
            populateChangeGroupMetaData(group, changeset);
            if (changeset != null)
            {
                m_algorithms.Initialize();
                foreach (Change c in changeset.Changes)
                {
                    // Either no snapshot start point is specified or we already passed the snapshot start point.
                    if (IsPathMapped(c.Item.ServerItem, changeset.ChangesetId) == MappingResult.Mapped)
                    {
                        try
                        {
                            m_algorithms.Execute(c, group);
                        }
                        catch (MissingMethodException mme)
                        {
                            throw new VersionControlMigrationException(
                                string.Format(TfsVCAdapterResource.Culture,
                                TfsVCAdapterResource.ClientNotSupported), mme);
                        }
                    }
                }
                m_algorithms.Finish(group);
            }

            changeCount = group.Actions.Count;

            if (group.Actions.Count > 0)
            {
                group.Save();
            }

            if (changeCount == 0)
            {
                TraceManager.TraceInformation("No relevent changes found in TFS change {0}",
                    changeset.ChangesetId);
            }
            return changeCount;
        }

        private static void populateChangeGroupMetaData(ChangeGroup group, Changeset changeset)
        {
            group.Owner = changeset.Owner;
            group.Comment = changeset.Comment;
            group.ChangeTimeUtc = changeset.CreationDate.ToUniversalTime();
            group.Status = ChangeStatus.Delta;
            group.ExecutionOrder = changeset.ChangesetId;
        }

        private void lazyInit()
        {
            if (m_algorithms == null)
            {
                m_algorithms = new Tfs2008AnalysisAlgorithms(this);
            }
        }

        #region IServiceProvider implementation
        /// <summary>
        /// Gets the service object of the specified type. 
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public Object GetService(Type serviceType)
        {
            return (IServiceProvider)this;
        }
        #endregion

        #region IDisposable members
        public void Dispose()
        {
        }
        #endregion

        #region mapping methods
        /// <summary>
        /// Find the mapping entry for the given path. 
        /// </summary>
        /// <param name="serverPath"></param>
        /// <returns>Null if the path is not mapped or cloaked. Otherwise, return the mapping entry.</returns>
        internal MappingEntry FindMappedPath(string serverPath)
        {
            MappingEntry mostSpecificMapping = null;
            foreach (MappingEntry current in ConfigurationService.Filters)
            {
                if (VersionControlPath.IsSubItem(serverPath, VersionControlPath.GetFullPath(current.Path)))
                {
                    if (mostSpecificMapping == null ||
                        VersionControlPath.IsSubItem(VersionControlPath.GetFullPath(current.Path),
                        VersionControlPath.GetFullPath(mostSpecificMapping.Path)))
                    {
                        mostSpecificMapping = current;
                    }
                }
            }

            if ((mostSpecificMapping != null) && (!mostSpecificMapping.Cloak))
            {
                return mostSpecificMapping;
            }
            else
            {
                return null;
            }
        }
        internal MappingResult IsMergePathMapped(string mergeFromPath, string mergeToPath, int referencedChangeset)
        {
            MappingEntry mappingEntry = FindMappedPath(mergeToPath);
            if (!string.IsNullOrEmpty(mappingEntry.MergeScope) && !VersionControlPath.IsSubItem(mergeFromPath, mappingEntry.MergeScope))
            {
                return MappingResult.OutOfMergeScope;
            }
            else
            {
                return IsPathMapped(mergeFromPath, referencedChangeset);
            }
            
        }

        internal MappingResult IsPathMapped(string serverPath, int referencedChangeset)
        {
            MappingEntry mappingEntry = FindMappedPath(serverPath);

            if (mappingEntry == null)
            {
                // Path is not mapped
                return MappingResult.NotMapped;
            }
            else if (string.IsNullOrEmpty(mappingEntry.SnapshotStartPoint))
            {
                // Path is mapped and no snapshot start point is specified.
                if (referencedChangeset >= m_sessionLevelSnapshotChangeset)
                {
                    return MappingResult.Mapped;
                }
                else
                {
                    return MappingResult.MappedBeforeSnapshot;
                }
            }
            else
            {
                int snapshotChangeset;
                try
                {
                   snapshotChangeset  = int.Parse(mappingEntry.SnapshotStartPoint);
                }
                catch (Exception)
                {
                    throw new Exception();
                }
                if (snapshotChangeset < referencedChangeset)
                {
                    // Path is mapped, snapshot start point is less than the referenced changeset.
                    return MappingResult.Mapped;
                }
            }
            // Path is mapped, but the referenced changeset is smaller (before) than the snapshot start point.
            return MappingResult.MappedBeforeSnapshot;
        }
        #endregion


        public string GetNativeId(MigrationSource migrationSourceConfig)
        {
            TfsTeamProjectCollection tfsServer = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(migrationSourceConfig.ServerUrl));
            return tfsServer.InstanceId.ToString();
        }
    }

    public enum MappingResult
    {
        Mapped,
        NotMapped,
        MappedBeforeSnapshot,
        OutOfMergeScope
    }
}
