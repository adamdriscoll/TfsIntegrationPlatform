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
using System.IO;
using System.Text;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;

namespace Microsoft.TeamFoundation.Migration.TfsFileSystemAdapter
{
    public class TfsFileSystemAnalysisProvider : IAnalysisProvider
    {
        Dictionary<Guid, ChangeActionHandler> m_supportedChangeActions;
        ICollection<Guid> m_supportedChangeActionsOther;
        Collection<ContentType> m_supportedContentTypes;
        ConflictManager m_conflictManagementService;
        Collection<ContentType> m_supportedContentTypesOther;
        ChangeActionRegistrationService m_changeActionRegistrationService;
        ChangeGroupService m_changeGroupService;
        TfsFileSystemConfiguration m_branchConfiguration;
        IServiceContainer m_analysisServiceContainer;
        VCTranslationService m_translationService;
        protected ConfigurationService m_configurationService;
        VersionControlServer m_destinationTfs;

        protected HighWaterMark<DateTime> m_hwmDelta;
        // The last changeset migrated from TFS to TfsFileSystemAnalysisProvider. 
        HighWaterMark<int> m_hwmLastSyncedTfsChangeset;
        // The last changeset migrated from TfsFileSystemAnalysisProvider to TFS. 
        HighWaterMark<int> m_lastHighWaterMarkMigratedToPeer;
        // The last changeset analyzed
        HighWaterMark<int> m_lastTfsChangesetAnalyzed;
        Dictionary<string, string> m_renameList;
        bool m_renameListUpdated = false;
        // The delta table is generated for content conflict detection only. The delta table will not be translated to migration instructions. 
        bool m_contentConflictDetectionOnly = false;


        internal HighWaterMark<DateTime> HwmDelta
        {
            get
            {
                return m_hwmDelta;
            }
        }

        internal ConfigurationService ConfigurationService
        {
            get
            {
                return m_configurationService;
            }
        }

        private VCTranslationService TranslationService
        {
            get
            {
                // lazy loading translation service
                if (null == m_translationService)
                {
                    m_translationService = m_analysisServiceContainer.GetService(typeof(ITranslationService)) as VCTranslationService;
                    Debug.Assert(m_translationService != null, "Translation service is not initialized");
                }

                return m_translationService;
            }
        }

        #region constructors
        public TfsFileSystemAnalysisProvider()
        {
            m_hwmLastSyncedTfsChangeset = null;
        }

        /// <summary>
        /// This constructor will create a TfsFilesystemAnalysisProvider that 
        /// uses the other side sync point as the base changeset to calculate differences.
        /// </summary>
        /// <param name="syncToLastTfsChangeset"></param>
        public TfsFileSystemAnalysisProvider(bool syncToLastTfsChangeset)
        {
            if (syncToLastTfsChangeset)
            {
                m_hwmLastSyncedTfsChangeset = new HighWaterMark<int>(TFSFileSystemAdapterConstants.HwmLastSyncedTfsChangeset);
            }
            else
            {
                m_hwmLastSyncedTfsChangeset = null;
            }
        }
        #endregion

        #region interfacemethods
        /// <summary>
        /// Initialize method of the analysis provider.  Please implement all the heavey-weight
        /// initialization logic here, e.g. server connection.
        /// </summary>
        public virtual void InitializeClient()
        {
            initializeDestinationTfsClient();
        }

        private void initializeDestinationTfsClient()
        {
            try
            {
                m_destinationTfs = VersionSpecificUtils.GetVersionControlServer(ConfigurationService.PeerServerUrl);
                if (m_hwmLastSyncedTfsChangeset != null)
                {
                    ConfigurationService.RegisterHighWaterMarkWithSession(m_hwmLastSyncedTfsChangeset);
                    m_hwmLastSyncedTfsChangeset.Reload();
                    if (m_hwmLastSyncedTfsChangeset.Value < 1)
                    {
                        m_hwmLastSyncedTfsChangeset.Update(1);
                    }
                }
            }
            catch (Exception ex)
            {
                // The destination must be a TFS server.
                throw new MigrationException(string.Format(TfsFileSystemResources.ErrorConnectingTargetTfsServer, ex.Message), ex);
            }
        }

        /// <summary>
        /// List of change actions supported by the analysis provider. 
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
        /// Detects adapter-specific conflicts.
        /// </summary>
        /// <param name="changeGroup"></param>
        public void DetectConflicts(ChangeGroup changeGroup)
        {
            // No content conflict in file system provider.
            return;
        }

        /// <summary>
        /// Generate the context info table
        /// </summary>
        public void GenerateContextInfoTable()
        {
            return;
        }

        /// <summary>
        /// Generate the delta table
        /// </summary>
        public void GenerateDeltaTable()
        {

            // Load the current high water mark from any previous run of this session
            m_hwmDelta.Reload();

            m_renameList = null;
            m_renameListUpdated = false;

            // Removing the in-progress change groups created by this side (fils system)
            m_changeGroupService.RemoveInProgressChangeGroups();

            if (m_changeGroupService.GetInProgressMigrationInstructionCount() > 0)
            {
                // If there are in-progress migration instructions translated from the other side, mark this delta table as contentcon flict detection only.
                m_contentConflictDetectionOnly = true;
                TraceManager.TraceInformation("Migration instruction in progress, the delta table will be generated for content conflict detection only");
            }
            else
            {
                m_contentConflictDetectionOnly = false;
            }

            DateTime newHighWaterMarkTime = DateTime.Now;

            List<String> pathsToBeVerified = new List<String>();
            int versionToBeSynced = 0;

            // In a two-way sync, versionToBeSynced is set to the last sync point - either the latest changeset migrated from TFS or the latest changeset migrated to TFS
            // In one way sync, versionToBeSynced is always set to the GetLatestChangesetId of TFS
            if (m_hwmLastSyncedTfsChangeset != null)
            {
                // m_hwmLastSyncedTfsChangeset is the latest TFS changeset migrated to the file system side. 
                // This highwater mark will be set when TfsFileSystemAnalysisProvider is combined with another provider to form a two-way sync. 
                m_hwmLastSyncedTfsChangeset.Reload();
                // m_lastHighWaterMarkMigratedToPeer is the latest TFS changeset migrated from this TfsFileSystemAnalysisProvider.
                m_lastHighWaterMarkMigratedToPeer.Reload();
                versionToBeSynced = m_lastHighWaterMarkMigratedToPeer.Value > m_hwmLastSyncedTfsChangeset.Value ?
                    m_lastHighWaterMarkMigratedToPeer.Value : m_hwmLastSyncedTfsChangeset.Value;
            }
            else
            {
                versionToBeSynced = m_destinationTfs.GetLatestChangesetId();
            }

            FileSystemVerifier fileSystemVerifier = new FileSystemVerifier();

            ChangeGroup changeGroup = m_changeGroupService.CreateChangeGroupForDeltaTable(DateTime.Now.ToString());
            populateChangeGroupMetaData(changeGroup);

            foreach (MappingEntry m in ConfigurationService.Filters)
            {
                if (m.Cloak)
                {
                    continue;
                }
                string canonicalPath = removeTrailingSlash(m.Path);
                fileSystemVerifier.AddPathForVerification(canonicalPath);
                analyzeFolder(canonicalPath, versionToBeSynced, changeGroup);
            }

            if (!fileSystemVerifier.Verify())
            {
                TraceManager.TraceError(
                    "Analysis failed as the local file system state was changed during the analysis phase. No changes were created.");
                markChangeGroupAsObsolete(changeGroup);
                return;
            }

            if (changeGroup.Actions.Count > 0)
            {
                // We only want to promote deltas to pending if we actually created and saved a change group.
                changeGroup.Save();
                m_changeGroupService.PromoteDeltaToPending();
            }
            else
            {
                markChangeGroupAsObsolete(changeGroup);
            }

            m_lastTfsChangesetAnalyzed.Update(versionToBeSynced);
            m_hwmDelta.Update(newHighWaterMarkTime);
            TraceManager.TraceInformation(String.Format(CultureInfo.InvariantCulture,
                TfsFileSystemResources.UpdatedHighWaterMark, newHighWaterMarkTime));
        }

        private static string removeTrailingSlash(String path)
        {
            while ((path[path.Length-1] == Path.DirectorySeparatorChar) && (path.Length > 0))
            {
                path = path.Remove(path.Length - 1);
            }
            return path;
        }

        private void markChangeGroupAsObsolete(ChangeGroup changeGroup)
        {
            if (changeGroup == null)
            {
                return;
            }
            changeGroup.Status = ChangeStatus.Obsolete;
            changeGroup.Save();
        }

        /// <summary>
        /// Generate the change group for migration.
        /// </summary>
        /// <returns></returns>
        private void populateChangeGroupMetaData(ChangeGroup changeGroup)
        {
            // Populate the meta data with information.
            changeGroup.Owner = null;
            changeGroup.Comment = null;
            // ChangeTimeUtc should be set to DateTime.MaxValue since we don't know the actual original time the change was made on the source
            changeGroup.ChangeTimeUtc = DateTime.MaxValue;
            changeGroup.Status = ChangeStatus.Delta;
            changeGroup.ExecutionOrder = 0;
        }

        private void analyzeFolder(string rootPath, int versionToBeSynced, ChangeGroup changeGroup)
        {            
            Dictionary<string, bool> localFileItems = new Dictionary<string,bool>();
            Dictionary<string, bool> localDirectoryItems = new Dictionary<string, bool>();
            Dictionary<string, bool> addItems = new Dictionary<string, bool>();
            Dictionary<string, bool> deleteItems = new Dictionary<string, bool>();

            string destinationTfsRootPath = TranslationService.GetMappedPath(rootPath, m_configurationService.SourceId);

            if (Directory.Exists(rootPath))
            {
                foreach (string path in Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories))
                {
                    localDirectoryItems.Add(path, false);
                }
                localDirectoryItems.Add(rootPath, false);

                foreach (string path in Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories))
                {
                    localFileItems.Add(path, false);
                }
            }
            else if (File.Exists(rootPath))
            {
                localFileItems.Add(rootPath, false);
            }
            else
            {
                MigrationConflict deleteAllConfirmationConflict = VCUserPromptConflictType.CreateConflict(
                    string.Format("After comparing the local system with TFS, the folder {0} and all its sub items will be deleted on TFS server. Please choose 'Delete' to continue with the delete; or retry the migration once the local file system is ready.",
                    destinationTfsRootPath),
                    versionToBeSynced.ToString() );
                List<MigrationAction> retActions;
                ConflictResolutionResult resolutionResult =
                    m_conflictManagementService.TryResolveNewConflict(m_configurationService.SourceId, deleteAllConfirmationConflict, out retActions);
                if ((!resolutionResult.Resolved) || (resolutionResult.ResolutionType != ConflictResolutionType.SkipConflictedChangeAction))
                {
                    markChangeGroupAsObsolete(changeGroup);
                    throw new MigrationUnresolvedConflictException(deleteAllConfirmationConflict);
                }
            }

            // We need a related branch that should be treated as the source for branches and merges.
            // TODO: Right now we assume this is the first parent that we find.  There should be a way to configure this.
            List<String> parents = m_branchConfiguration.GetRIList(rootPath);
            String parentRootPath = (parents == null) ? null : parents[0];

            TfsFileSystemRelatedBranchView relatedBranchView = new TfsFileSystemRelatedBranchView(rootPath, parentRootPath);

            ItemSet tfsItemSet;
            tfsItemSet = m_destinationTfs.GetItems(destinationTfsRootPath,
                new ChangesetVersionSpec(versionToBeSynced),
                RecursionType.Full);

            foreach (Item item in tfsItemSet.Items)
            {
                string localPath = TranslationService.GetMappedPath(item.ServerItem, m_configurationService.MigrationPeer);
                if (item.ItemType == ItemType.File)
                {
                    if (localFileItems.ContainsKey(localPath))
                    {
                        localFileItems[localPath] = true;
                        // Consolidate the computation of MD5 hash in toolkit
                        if (!Utils.ContentsMatch(localPath, item.HashValue))
                        {
                            createMigrationAction(changeGroup, WellKnownChangeActionId.Edit, localPath, false);

                            // If the contents in the edited file match the contents of the related file in the source branch
                            // then we should pend a merge
                            String relatedBranchItem;
                            if (relatedBranchView.TryGetRelatedBranchItem(localPath, false, out relatedBranchItem) &&
                                Utils.ContentsMatch(localPath, relatedBranchItem))
                            {
                                createRelativeMigrationAction(changeGroup, WellKnownChangeActionId.Merge, relatedBranchItem, localPath, false);
                            }
                        }
                    }
                    else
                    {
                        deleteItems.Add(localPath, false);
                    }
                }
                else
                {
                    if (localDirectoryItems.ContainsKey(localPath))
                    {
                        localDirectoryItems[localPath] = true;
                    }
                    else
                    {
                        deleteItems.Add(localPath, true);
                    }
                }
            }

            // Now add all directory items exist only on local system. 
            foreach (KeyValuePair<string, bool> localItem in localDirectoryItems)
            {
                if (!localItem.Value)
                {
                    String relatedBranchItem;
                    if (relatedBranchView.TryGetRelatedBranchItem(localItem.Key, true, out relatedBranchItem))
                    {
                        // Create the branch. Since we are dealing with folders we don't need to worry about
                        // pending edits for different content.
                        createRelativeMigrationAction(changeGroup, WellKnownChangeActionId.Branch, relatedBranchItem, localItem.Key, true);
                    }
                    else
                    {
                        addItems.Add(localItem.Key, true);
                    }
                }
            }

            // Now add all file items exist only on local system.
            foreach (KeyValuePair<string, bool> localItem in localFileItems)
            {
                if (!localItem.Value)
                {
                    String relatedBranchItem; 
                    if (relatedBranchView.TryGetRelatedBranchItem(localItem.Key, false, out relatedBranchItem))
                    {
                        // Create the branch. 
                        createRelativeMigrationAction(changeGroup, WellKnownChangeActionId.Branch, relatedBranchItem, localItem.Key, false);

                        // If the contents of the file we are branching from and our local item are different
                        // then we will need to pend an edit as well.
                        if (!Utils.ContentsMatch(localItem.Key, relatedBranchItem))
                        {
                            createMigrationAction(changeGroup, WellKnownChangeActionId.Edit, localItem.Key, false);
                        }
                    }
                    else
                    {
                        addItems.Add(localItem.Key, false);
                    }
                }
            }

            if ((deleteItems.Count > 0) && (!m_renameListUpdated))
            {
                m_renameList = GetRenameList();
            }
            List<string> deleteItemToBeRemoved = new List<string>();
            if (m_renameList != null && m_renameList.Count > 0)
            {
                foreach (string deleteItem in deleteItems.Keys)
                {
                    string ancestryPath = deleteItem;
                    while (!Path.Equals(rootPath, ancestryPath))
                    {
                        if (m_renameList.ContainsKey(ancestryPath))
                        {
                            string renameToAncestryPath = m_renameList[ancestryPath];
                            string renameToPath = string.Concat(renameToAncestryPath, deleteItem.Substring(ancestryPath.Length));
                            if (addItems.ContainsKey(renameToPath))
                            {
                                createRenameMigrationAction(changeGroup, deleteItem, renameToPath, deleteItems[deleteItem]);
                                addItems.Remove(renameToPath);
                                deleteItemToBeRemoved.Add(deleteItem);
                                break;
                            }
                        }
                        ancestryPath = Path.GetDirectoryName(ancestryPath);
                    }
                }
            }

            foreach (string addItem in addItems.Keys)
            {
                createMigrationAction(changeGroup, WellKnownChangeActionId.Add, addItem, addItems[addItem]);
            }
            foreach (string deleteItem in deleteItems.Keys)
            {
                // We have already transferred this delete to a rename, just continue.
                if (deleteItemToBeRemoved.Contains(deleteItem))
                {
                    continue;
                }
                createMigrationAction(changeGroup, WellKnownChangeActionId.Delete, deleteItem, deleteItems[deleteItem]);
            }
        }

        /// <summary>
        /// Detect any renames under the rootpath. 
        /// Inherited class should override this method, otherwise, null will be returned.
        /// </summary>
        /// <returns></returns>
        public virtual Dictionary<string, string> GetRenameList()
        {
            return null;
        }


        private void createMigrationAction(ChangeGroup changeGroup, Guid actionId, string localPath, bool isDirectory)
        {
            IMigrationAction action = changeGroup.CreateAction(
                actionId,
                new TfsFileSystemMigrationItem(localPath, isDirectory),
                null,
                localPath,
                null,
                null,
                isDirectory ? WellKnownContentType.VersionControlledFolder.ReferenceName 
                            : WellKnownContentType.VersionControlledFile.ReferenceName,
                null,
                m_contentConflictDetectionOnly);
        }

        private void createRenameMigrationAction(ChangeGroup changeGroup, string localPathFrom, string localPath, bool isDirectory)
        {
            IMigrationAction action = changeGroup.CreateAction(
                WellKnownChangeActionId.Rename,
                new TfsFileSystemMigrationItem(localPath, isDirectory),
                localPathFrom,
                localPath,
                null,
                null,
                isDirectory ? WellKnownContentType.VersionControlledFolder.ReferenceName
                            : WellKnownContentType.VersionControlledFile.ReferenceName,
                null,
                m_contentConflictDetectionOnly);
            // For rename of file, also add an Edit action.
            if (!isDirectory)
            {
                createMigrationAction(changeGroup, WellKnownChangeActionId.Edit, localPath, false);
            }
        }

        private void createRelativeMigrationAction(ChangeGroup changeGroup, Guid actionId, String relatedBranchLocalPath, String localPath, Boolean isDirectory)
        {
            IMigrationAction action = changeGroup.CreateAction(
                actionId,
                new TfsFileSystemMigrationItem(localPath, isDirectory),
                relatedBranchLocalPath,
                localPath,
                "T",
                "T",
                isDirectory ? WellKnownContentType.VersionControlledFolder.ReferenceName
                            : WellKnownContentType.VersionControlledFile.ReferenceName,
                null,
                m_contentConflictDetectionOnly);
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

            initializeSupportedChangeActions();

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
        /// Initialize SupportedChangeActions list.
        /// </summary>
        private void initializeSupportedChangeActions()
        {
            TfsFileSystemChangeActionHandlers handlers = new TfsFileSystemChangeActionHandlers(this);
            m_supportedChangeActions = new Dictionary<Guid, ChangeActionHandler>(11);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Add, handlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Branch, handlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Delete, handlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Edit, handlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Encoding, handlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Label, handlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Merge, handlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.BranchMerge, handlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Rename, handlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Undelete, handlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.AddFileProperties, handlers.BasicActionHandler);
        }

        /// <summary>
        /// Initialize method of the analysis provider - acquire references to the services provided by the platform.
        /// </summary>
        /// <param name="analysisServiceContainer"></param>
        public void InitializeServices(IServiceContainer analysisServiceContainer)
        {
            m_analysisServiceContainer = analysisServiceContainer;

            m_configurationService = (ConfigurationService)m_analysisServiceContainer.GetService(typeof(ConfigurationService));
            m_hwmDelta = new HighWaterMark<DateTime>(Constants.HwmDelta);
            m_configurationService.RegisterHighWaterMarkWithSession(m_hwmDelta);
            m_lastTfsChangesetAnalyzed = new HighWaterMark<int>(TFSFileSystemAdapterConstants.HwmLastTfsChangesetAnalyzed);
            m_configurationService.RegisterHighWaterMarkWithSession(m_lastTfsChangesetAnalyzed);
            m_lastHighWaterMarkMigratedToPeer = new HighWaterMark<int>(Constants.HwmMigrated);
            m_configurationService.RegisterHighWaterMarkWithSession(m_lastHighWaterMarkMigratedToPeer, m_configurationService.MigrationPeer);
            m_changeGroupService = (ChangeGroupService)m_analysisServiceContainer.GetService(typeof(ChangeGroupService));
            m_changeGroupService.RegisterDefaultSourceSerializer(new TfsFileSystemMigrationItemSerializer());
            m_branchConfiguration = new TfsFileSystemConfiguration(m_configurationService);
        }

        /// <summary>
        /// Register adapter's conflict handlers.
        /// </summary>
        /// <param name="conflictManager"></param>
        public void RegisterConflictTypes(ConflictManager conflictManager)
        {
            if (conflictManager == null)
            {
                throw new ArgumentNullException("conflictManager");
            }
            m_conflictManagementService = conflictManager;
            m_conflictManagementService.RegisterConflictType(new GenericConflictType());
            m_conflictManagementService.RegisterConflictType(new VCUserPromptConflictType());
            m_conflictManagementService.RegisterConflictType(new VCChangeGroupInProgressConflictType());
        }

        /// <summary>
        /// Register adapter's supported content types.
        /// </summary>
        /// <param name="contentTypeRegistrationService"></param>
        public void RegisterSupportedContentTypes(ContentTypeRegistrationService contentTypeRegistrationService)
        {
            initializeSupportedContentTypes();
        }

        public string GetNativeId(MigrationSource migrationSourceConfig)
        {
            return migrationSourceConfig.ServerUrl;
        }
        #endregion

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
    }
}
