// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

using ClearCase;

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{
    public class ClearCaseAnalysisProvider : IAnalysisProvider
    {
        Dictionary<Guid, ChangeActionHandler> m_supportedChangeActions;
        ICollection<Guid> m_supportedChangeActionsOther;
        Collection<ContentType> m_supportedContentTypes;
        ConflictManager m_conflictManagementService;
        Collection<ContentType> m_supportedContentTypesOther;
        ChangeActionRegistrationService m_changeActionRegistrationService;
        ChangeGroupService m_changeGroupService;
        IServiceContainer m_analysisServiceContainer;
        ClearCaseServer m_clearCaseServer;
        ITranslationService m_translationService;
        ConfigurationService m_configurationService;
        ChangeGroup m_currentChangeGroup; 
        bool m_sessionlevelSnapshotCompleted;
        DateTime m_sessionLevelSnapshotTime;

        List<string> m_vobList = new List<string>();

        CCConfiguration m_ccConfiguration;

        HighWaterMark<DateTime> m_hwmDelta;
        HighWaterMark<long> m_hwmEventId;

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

        #region interfacemethods
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
            // No CC specific content conflict.
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
            // 1. Process session level snapshot start point.
            if (!m_sessionlevelSnapshotCompleted)
            {
                generateSnapshotForVCSession();
            }
            if (m_ccConfiguration.DetectChangesInCC)
            {
                queryHistory(m_changeGroupService);
            }
        }

        private void generateSnapshotForVCSession()
        {
            foreach (var setting in m_configurationService.VcCustomSetting.Settings.Setting)
            {
                if (setting.SettingKey == "SnapshotStartPoint")
                {
                    m_sessionLevelSnapshotTime = parseSnapShotStartPoint(setting.SettingValue);
                }
            }

            m_hwmDelta.Reload();
            if (m_hwmDelta.Value >= m_sessionLevelSnapshotTime)
            {
                // We've already passed snapshot changeset Id, just return. 
                m_sessionlevelSnapshotCompleted = true;
                return;
            }

            ChangeGroup snapshotGroup = createChangeGroupForSnapshot(m_sessionLevelSnapshotTime, 0);
            m_clearCaseServer.SetConfigSpecForSnapshotStartPoint(m_sessionLevelSnapshotTime);

            try
            {
                foreach (MappingEntry mappingEntry in m_configurationService.Filters)
                {
                    if (mappingEntry.Cloak)
                    {
                        continue;
                    }
                    string localMappingPath = m_clearCaseServer.GetViewLocalPathFromServerPath(mappingEntry.Path);
                    foreach (string subDirectory in Directory.GetDirectories(localMappingPath, "*", SearchOption.AllDirectories))
                    {
                        createAddActionForSnapshot(snapshotGroup, subDirectory, true);
                    }
                    foreach (string subFile in Directory.GetFiles(localMappingPath, "*", SearchOption.AllDirectories))
                    {
                        createAddActionForSnapshot(snapshotGroup, subFile, false);
                    }
                }
            }
            finally
            {
                m_clearCaseServer.ResetConfigSpec();
            }

            snapshotGroup.Save();
            m_hwmDelta.Update(m_sessionLevelSnapshotTime);
            m_changeGroupService.PromoteDeltaToPending();
            m_sessionlevelSnapshotCompleted = true;
        }

        private ChangeGroup createChangeGroupForSnapshot(DateTime snapShotTime, long executionOrder)
        {
            ChangeGroup group = m_changeGroupService.CreateChangeGroupForDeltaTable(snapShotTime.ToString());
            group.Owner = null;
            group.Comment = string.Format("Initial Check-in as snapshot at changeset {0}", snapShotTime);
            // ChangeTimeUtc should be set to DateTime.MaxValue since we don't know the actual original time the change was made on the source
            group.ChangeTimeUtc = DateTime.MaxValue;
            group.Status = ChangeStatus.Delta;
            group.ExecutionOrder = executionOrder;
            return group;
        }

        private void createAddActionForSnapshot(ChangeGroup snapshotGroup, string localPath, bool isDirectory)
        {
            string serverPath = m_clearCaseServer.GetServerPathFromViewLocalPath(localPath);
            CCElement element = null;
            try
            {
                element = m_clearCaseServer.ApplicationClass.get_Element(serverPath);
            }
            catch (ArgumentException)
            {
                TraceManager.TraceWarning(String.Format("Unable to get ClearCase Element corresponding to local path: '{0}'", localPath));
                return;
            }
            snapshotGroup.CreateAction(
                WellKnownChangeActionId.Add,
                new ClearCaseMigrationItem(
                    m_clearCaseServer.ViewName,
                    element.Version.ExtendedPath,
                    isDirectory),
                null,
                serverPath,
                null,
                null,
                isDirectory ? WellKnownContentType.VersionControlledFolder.ReferenceName : WellKnownContentType.VersionControlledFile.ReferenceName,
                null);
        }

        private DateTime parseSnapShotStartPoint(string snapShotValue)
        {
            int seperatorIndex = snapShotValue.IndexOf(';');
            if (seperatorIndex < 0)
            {
                throw new MigrationException(
                    string.Format(CCResources.Culture, CCResources.InvalidSnapshotString, snapShotValue));
            }
            else
            {
                try
                {
                    Guid sourceId = new Guid(snapShotValue.Substring(0, seperatorIndex));
                    if (sourceId == m_conflictManagementService.SourceId)
                    {
                        return DateTime.Parse(snapShotValue.Substring(seperatorIndex + 1));
                    }
                    else
                    {
                        return DateTime.MinValue;
                    }
                }
                catch (FormatException)
                {
                    throw new MigrationException(
                        string.Format(CCResources.Culture, CCResources.InvalidSnapshotString, snapShotValue));
                }
            }
        }
   
        /// <summary>
        /// Initialize method of the analysis provider.  Please implement all the heavey-weight
        /// initialization logic here, e.g. server connection.
        /// </summary>
        public void InitializeClient()
        {
            initializeConfiguration();
            initializeClearCaseServer();
        }

        private void initializeConfiguration()
        {
            m_ccConfiguration = CCConfiguration.GetInstance(m_configurationService.MigrationSource);
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
        /// Initialize SupportedChangeActions list.
        /// </summary>
        private void initiazlieSupportedChangeActions()
        {
            ClearCaseChangeActionHandlers handlers = new ClearCaseChangeActionHandlers(this);
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
            m_hwmEventId = new HighWaterMark<long>("HWMCCEventId");
            m_configurationService.RegisterHighWaterMarkWithSession(m_hwmEventId);
            m_changeGroupService = (ChangeGroupService)m_analysisServiceContainer.GetService(typeof(ChangeGroupService));
            m_changeGroupService.RegisterDefaultSourceSerializer(new ClearCaseV6MigrationItemSerialzier());
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
           
        private void initializeClearCaseServer()
        {
            m_clearCaseServer = ClearCaseServer.GetInstance(m_ccConfiguration, m_ccConfiguration.GetViewName("Analysis1"));
            m_clearCaseServer.Initialize();
        }

        private void queryHistory(ChangeGroupService changeGroupService)
        {
            m_hwmDelta.Reload();
            DateTime since = m_hwmDelta.Value;
            List<CCHistoryRecord> historyRecordList = m_clearCaseServer.GetHistoryRecords(m_configurationService.Filters, since, true);
            historyRecordList.Sort();

            CCVersion version;
            CCItem currentItem = null;
            CCItem previousLnItem = null;
            string previousLnItemLeafName = null;
            string previousMkElemItemPath = null;
            List<CCHistoryRecord> processedRecordList = new List<CCHistoryRecord>();
            foreach (CCHistoryRecord historyRecord in historyRecordList)
            {
                switch (historyRecord.OperationType)
                {
                    case OperationType.Checkin:
                        version = m_clearCaseServer.ApplicationClass.get_Version(historyRecord.VersionExtendedPath);
                        currentItem = new CCItem(version, ClearCasePath.GetVobName(historyRecord.AbsoluteVobPath));
                        if (string.Equals(historyRecord.OperationDescription, OperationDescription.Version))
                        {
                            if (ClearCasePath.Equals(previousMkElemItemPath, currentItem.AbsoluteVobPath))
                            {
                                // File version checkin following a mkelem, create an Add
                                historyRecord.AbsoluteVobPath = currentItem.AbsoluteVobPath;
                                historyRecord.ChangeAction = WellKnownChangeActionId.Add;
                                historyRecord.IsDirectory = version.IsDirectory;
                                processedRecordList.Add(historyRecord);
                                previousMkElemItemPath = null;
                            }
                            else
                            {
                                // File version checkin following a mkelem, create an Edit
                                historyRecord.AbsoluteVobPath = currentItem.AbsoluteVobPath;
                                historyRecord.ChangeAction = WellKnownChangeActionId.Edit;
                                historyRecord.IsDirectory = version.IsDirectory;
                                processedRecordList.Add(historyRecord);
                            }
                        }
                        else if (string.Equals(historyRecord.OperationDescription, OperationDescription.DirectoryVersion) &&
                            (ClearCasePath.Equals(previousMkElemItemPath, currentItem.AbsoluteVobPath)))
                        {
                            // Directory version checkin following a mkelem, create an Add
                            historyRecord.AbsoluteVobPath = currentItem.AbsoluteVobPath;
                            historyRecord.ChangeAction = WellKnownChangeActionId.Add;
                            historyRecord.IsDirectory = version.IsDirectory;
                            processedRecordList.Add(historyRecord);
                            previousMkElemItemPath = null;
                        }
                        break;
                    case OperationType.Mkattr:
                    case OperationType.Mkpool:
                    case OperationType.Mkreplica:
                    case OperationType.Mktype:
                        // ToDo
                        // writeHistoryRecord(historyFields);
                        break;
                    case OperationType.Lnname:
                        version = m_clearCaseServer.ApplicationClass.get_Version(historyRecord.VersionExtendedPath);
                        currentItem = new CCItem(version, ClearCasePath.GetVobName(historyRecord.AbsoluteVobPath));
                        previousLnItem = currentItem;
                        previousLnItemLeafName = ClearCaseEventSpec.ParseLnNameComment(historyRecord.Comment);
                        break;
                    case OperationType.Mkbranch:
                        // ToDo
                        if (string.Equals(historyRecord.OperationDescription, OperationDescription.Version)
                            || string.Equals(historyRecord.OperationDescription, OperationDescription.DirectoryVersion))
                        {
                            /*version = m_clearCaseServer.ApplicationClass.get_Version(historyRecord.VersionExtendedPath);
                            currentItem = new CCItem(version, vob);
                            historyRecord.AbsoluteVobPath = currentItem.AbsoluteVobPath;
                            historyRecord.ChangeAction = WellKnownChangeActionId.Add;
                            historyRecord.IsDirectory = version.IsDirectory;
                            historyRecordList.Add(historyRecord);
                             * */
                        }
                        break;
                    case OperationType.Mkelem:
                        if (string.Equals(historyRecord.OperationDescription, OperationDescription.DirectoryElement))
                        {
                            // Todo
                            /*if (currentState == OperationState.Initialized)
                            {
                                currentState = OperationState.CreateDirectoryElement;
                                currentItem = new Item(historyFields[1], ItemType.Element, m_vobName);
                            }
                            else
                            {
                                logStateTransitionError(currentState, operationType, operationDescription);
                            }*/
                        }
                        else if (string.Equals(historyRecord.OperationDescription, OperationDescription.Branch))
                        {
                           // Todo
                           /*
                           if ((currentState == OperationState.AddDirectoryToParent) 
                               || (currentState == OperationState.CreateDirectoryElement))
                           {
                               currentState = OperationState.CreateDirectoryBranch;
                               currentItem = new Item(historyFields[1], ItemType.Branch, m_vobName);
                           }
                           else
                           {
                               logStateTransitionError(currentState, operationType, operationDescription);
                           }
                            * */
                        }
                        else if (string.Equals(historyRecord.OperationDescription, OperationDescription.DirectoryVersion)
                            || string.Equals(historyRecord.OperationDescription, OperationDescription.Version))
                        {
                            version = m_clearCaseServer.ApplicationClass.get_Version(historyRecord.VersionExtendedPath);
                            currentItem = new CCItem(version, ClearCasePath.GetVobName(historyRecord.AbsoluteVobPath));
                            previousMkElemItemPath = currentItem.AbsoluteVobPath;
                            //if (currentState == OperationState.CreateDirectoryBranch)
                            //{
                            /*version = m_clearCaseServer.ApplicationClass.get_Version(historyFields[1]);
                            currentItem = new Item(version, vob);
                            if (IsPathMapped(currentItem.AbsoluteVobPath) &&
                                IsOurChange(ClearCasePath.removeViewLocationFromVersion(version.ExtendedPath, m_clearCaseServer.ViewRootPath)))
                           {
                                createMigrationAction(
                                version,
                                null,
                                currentItem.AbsoluteVobPath,
                                versionTime,
                                WellKnownChangeActionId.Add,
                                version.IsDirectory);
                            }*/

                            // Verify the version to be 0
                            //currentState = OperationState.Initialized;
                            //}
                            //else
                            //{
                            //    logStateTransitionError(currentState, operationType, operationDescription);
                            //}
                        }
                        else
                        {
                            //logStateTransitionError(currentState, operationType, operationDescription);
                        }
                        break;
                    case OperationType.Mkhlink:
                        // ToDo
                        // writeHistoryRecord(historyFields);
                        break;
                    case OperationType.Mklabel:
                        break;
                    case OperationType.Rmname:
                        bool isDirectory;
                        string rmItemName = ClearCaseEventSpec.ParseRmNameComment(historyRecord.Comment, out isDirectory);
                        if (rmItemName == null)
                        {
                            TraceManager.TraceWarning(String.Format("Skipping rmname operation: Unable to determine element type from history record comment: '{0}'", historyRecord.Comment));
                            continue;
                        }
                        version = m_clearCaseServer.ApplicationClass.get_Version(historyRecord.VersionExtendedPath);
                        currentItem = new CCItem(version, ClearCasePath.GetVobName(historyRecord.AbsoluteVobPath));
                        if (currentItem.Equals(previousLnItem))
                        {
                            historyRecord.AbsoluteVobPath = ClearCasePath.Combine(currentItem.AbsoluteVobPath, previousLnItemLeafName);
                            historyRecord.AbsoluteVobPathFrom = ClearCasePath.Combine(currentItem.AbsoluteVobPath, rmItemName);
                            historyRecord.ChangeAction = WellKnownChangeActionId.Rename;
                            historyRecord.IsDirectory = isDirectory;
                            processedRecordList.Add(historyRecord);
                            previousLnItem = null;
                            previousLnItemLeafName = null;
                            // todo, path not mapped exception
                        }
                        else
                        {
                            // delete operation
                            historyRecord.AbsoluteVobPath = ClearCasePath.Combine(currentItem.AbsoluteVobPath, rmItemName);
                            historyRecord.ChangeAction = WellKnownChangeActionId.Delete;
                            historyRecord.IsDirectory = isDirectory;
                            processedRecordList.Add(historyRecord);
                        }
                        break;
                    case OperationType.Undefined:
                        break;
                    case OperationType.Mkvob:
                    // Add of Vob itself.
                    // ToDo
                    // writeHistoryRecord(historyFields);
                    default:
                        break;
                }
            }

            reviseHistoryRecordsForRename(processedRecordList);

            m_hwmEventId.Reload();
            long lastProcessedEventId = m_hwmEventId.Value;

            foreach (CCHistoryRecord historyRecord in processedRecordList)
            {
                if (historyRecord.EventId <= lastProcessedEventId)
                {
                    // The event has been processed in previous sync.
                    TraceManager.TraceInformation("Skipping history record because the event was processed in a previous sync pass of this session");
                    continue;
                }
                if (Utils.IsOurChange(historyRecord))
                {
                    TraceManager.TraceInformation("Skipping history record because it represents a change migrated by the Integration Platform");
                    continue;
                }
                if (Utils.IsPathMapped(historyRecord.AbsoluteVobPath, m_configurationService))
                {
                    if (historyRecord.ChangeAction == WellKnownChangeActionId.Rename)
                    {
                        if (Utils.IsPathMapped(historyRecord.AbsoluteVobPathFrom, m_configurationService))
                        {
                            m_currentChangeGroup = createMigrationAction(historyRecord, historyRecord.ChangeAction);
                        }
                        else
                        {
                            // ToDo Path not mapped conflict
                            m_currentChangeGroup = createMigrationAction(historyRecord, WellKnownChangeActionId.Add);
                        }
                    }
                    else
                    {
                        m_currentChangeGroup = createMigrationAction(historyRecord, historyRecord.ChangeAction);
                    }
                }
                else
                {
                    // ToDo path not mapped conflict
                    if ((historyRecord.ChangeAction == WellKnownChangeActionId.Rename) && (Utils.IsPathMapped(historyRecord.AbsoluteVobPathFrom, m_configurationService)))
                    {
                        m_currentChangeGroup = createMigrationAction(historyRecord, WellKnownChangeActionId.Delete);
                    }
                    else
                    {
                        TraceManager.TraceWarning("Skipping history record because the path '{0}' is not mapped in a filter string", historyRecord.AbsoluteVobPath);
                    }
                }
            }
            if (m_currentChangeGroup != null)
            {
                m_currentChangeGroup.Save();
            }
            if (processedRecordList.Count > 0)
            {
                m_hwmDelta.Update(processedRecordList.Last().VersionTime);
            }
            m_changeGroupService.PromoteDeltaToPending();
        }

        private void reviseHistoryRecordsForRename(List<CCHistoryRecord> historyRecordList)
        {
            Dictionary<string, string> renameList = new Dictionary<string, string>();

            for (int i = historyRecordList.Count - 1; i >= 0; i--)
            {
                reviseNameForRename(historyRecordList[i], renameList);
                if (historyRecordList[i].ChangeAction == WellKnownChangeActionId.Rename)
                {
                    reviseRenameList(historyRecordList[i], renameList);
                }
            }
        }

        private void reviseRenameList(CCHistoryRecord renameHistoryRecord, Dictionary<string, string> renameList)
        {
            bool needToAddToRenameList = true;
            List<string> subItemRenameToBeModified = new List<string>();
            foreach (string renameToPath in renameList.Keys)
            {
                if (ClearCasePath.Equals(renameList[renameToPath], renameHistoryRecord.AbsoluteVobPath))
                {
                    subItemRenameToBeModified.Add(renameToPath);
                    needToAddToRenameList = false;
                    continue;
                }
                if (ClearCasePath.IsSubItem(renameList[renameToPath], renameHistoryRecord.AbsoluteVobPath))
                {
                    subItemRenameToBeModified.Add(renameToPath);
                }
            }

            foreach (string subItem in subItemRenameToBeModified)
            {
                renameList[subItem] =
                    ClearCasePath.Combine(renameHistoryRecord.AbsoluteVobPathFrom, renameList[subItem].Substring(renameHistoryRecord.AbsoluteVobPath.Length));
            }

            if (needToAddToRenameList)
            {
                renameList.Add(renameHistoryRecord.AbsoluteVobPath, renameHistoryRecord.AbsoluteVobPathFrom);
            }
        }

        private void reviseNameForRename(CCHistoryRecord historyRecord, Dictionary<string, string> renameList)
        {
            if (renameList.Count == 0)
            {
                return;
            }

            string path = historyRecord.AbsoluteVobPath;
            while (!ClearCasePath.IsVobRoot(path))
            {
                if (renameList.ContainsKey(path))
                {
                    historyRecord.AbsoluteVobPath =
                        ClearCasePath.Combine(renameList[path], historyRecord.AbsoluteVobPath.Substring(path.Length));
                    break;
                }
                path = ClearCasePath.GetFolderName(path);
            }

            if (historyRecord.ChangeAction == WellKnownChangeActionId.Rename)
            {
                string fromPath = historyRecord.AbsoluteVobPathFrom;
                while (!ClearCasePath.IsVobRoot(fromPath))
                {
                    if (renameList.ContainsKey(fromPath))
                    {
                        historyRecord.AbsoluteVobPathFrom = 
                            ClearCasePath.Combine(renameList[fromPath], historyRecord.AbsoluteVobPathFrom.Substring(fromPath.Length));
                        break;
                    }
                    fromPath = ClearCasePath.GetFolderName(fromPath);
                }
            }
        }

        private ChangeGroup createMigrationAction(CCHistoryRecord historyRecord, Guid actionId)
        {
            string versionExtendedPath;
            if (m_clearCaseServer.UsePrecreatedView)
            {
                versionExtendedPath = historyRecord.VersionExtendedPath;
            }
            else
            {
                versionExtendedPath = 
                    ClearCasePath.removeViewLocationFromVersion(historyRecord.VersionExtendedPath, m_clearCaseServer.ViewRootPath);
            }
            ChangeGroup oldChangeGroup;
            ChangeGroup currentChangeGroup = m_changeGroupService.AddMigrationActionToDeltaTable(historyRecord.EventId.ToString(CultureInfo.InvariantCulture),
                historyRecord.Comment,
                null,
                historyRecord.EventId,
                actionId,
                new ClearCaseMigrationItem(
                    m_ccConfiguration.GetViewName("Analysis"),
                    versionExtendedPath,
                    historyRecord.IsDirectory),
                historyRecord.AbsoluteVobPathFrom,
                historyRecord.AbsoluteVobPath,
                null,
                null,
                historyRecord.IsDirectory ? WellKnownContentType.VersionControlledFolder.ReferenceName
                : WellKnownContentType.VersionControlledFile.ReferenceName,
                null,
                historyRecord.VersionTime,
                out oldChangeGroup);

            m_hwmEventId.Update(historyRecord.EventId);

            if ((oldChangeGroup!= null) && (oldChangeGroup != currentChangeGroup))
            {
                oldChangeGroup.Save();
                m_changeGroupService.PromoteDeltaToPending();
                m_hwmDelta.Update(historyRecord.VersionTime);
            }
            return currentChangeGroup;
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
