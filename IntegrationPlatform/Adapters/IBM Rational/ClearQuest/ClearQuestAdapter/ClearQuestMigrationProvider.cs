// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using ClearQuestOleServer;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.ConflictManagement;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.Exceptions;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.MigrationItem;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    internal class ClearQuestMigrationProvider : IMigrationProvider
    {
        private Session m_userSession;
        #region not need until context sync requires us to access schema info
        //private AdminSession m_adminSession; 
        #endregion
        private IServiceContainer m_migrationServiceContainer;
        private ChangeGroupService m_changeGroupService;
        private ConfigurationService m_configurationService;
        private ConflictManager m_conflictManagerService;
        private ITranslationService m_translationService;
        private ICommentDecorationService m_commentDecorationService;
        private ClearQuestMigrationContext m_migrationContext;
        //private ChangeActionRegistrationService m_changeActionRegistrationService;
        //private ClearQuestChangeActionHandlers m_mgrtChangeActionHandlers;
        private ErrorManager ErrorManager
        {
            get
            {
                ErrorManager errMgr = null;
                if (m_migrationServiceContainer != null)
                {
                    errMgr = m_migrationServiceContainer.GetService(typeof(ErrorManager)) as ErrorManager;
                }
                return errMgr;
            }
        }

        #region IMigrationProvider Members

        public void InitializeServices(IServiceContainer migrationServiceContainer)
        {
            m_migrationServiceContainer = migrationServiceContainer;

            m_changeGroupService = (ChangeGroupService)m_migrationServiceContainer.GetService(typeof(ChangeGroupService));
            Debug.Assert(m_changeGroupService != null, "Change group service is not initialized");

            m_changeGroupService.RegisterDefaultSourceSerializer(new ClearQuestRecordItemSerializer());
            //m_changeGroupService.RegisterSourceSerializer(
            //    WellKnownChangeActionId.SyncContext, new WorkItemContextSyncMigrationItemSerializer());
            m_changeGroupService.RegisterSourceSerializer(
                WellKnownChangeActionId.AddAttachment, new ClearQuestAttachmentItemSerializer());
            m_changeGroupService.RegisterSourceSerializer(
                WellKnownChangeActionId.DelAttachment, new ClearQuestAttachmentItemSerializer());

            m_configurationService = (ConfigurationService)m_migrationServiceContainer.GetService(typeof(ConfigurationService));
            Debug.Assert(m_configurationService != null, "Configuration service is not initialized");

            m_commentDecorationService = (ICommentDecorationService)m_migrationServiceContainer.GetService(typeof(ICommentDecorationService));
            Debug.Assert(m_commentDecorationService != null, "Comment decoration service is not initialized");

            m_conflictManagerService = migrationServiceContainer.GetService(typeof(ConflictManager)) as ConflictManager;
        }

        public void InitializeClient()
        {
            InitializeCQClient();
        }

        public void RegisterConflictTypes(ConflictManager conflictManager)
        {
            if (conflictManager == null)
            {
                throw new ArgumentNullException("conflictManager");
            }

            m_conflictManagerService = conflictManager;

            m_conflictManagerService.RegisterConflictType(new GenericConflictType());

            m_conflictManagerService.RegisterConflictType(new ClearQuestGenericConflictType(),
                                                          SyncOrchestrator.ConflictsSyncOrchOptions.StopConflictedSessionCurrentTrip);

            m_conflictManagerService.RegisterConflictType(new ClearQuestSetFieldValueConflictType());

            m_conflictManagerService.RegisterConflictType(new ClearQuestMissingCQDllConflictType(),
                                                          SyncOrchestrator.ConflictsSyncOrchOptions.StopConflictedSession);

            m_conflictManagerService.RegisterConflictType(new ClearQuestInvalidFieldValueConflictType(),
                                                          SyncOrchestrator.ConflictsSyncOrchOptions.Continue);
        }

        public void EstablishContext(ChangeGroupService sourceSystemChangeGroupService)
        {
            return;
        }

        public ConversionResult ProcessChangeGroup(ChangeGroup changeGroup)
        {
            Guid targetSideSourceId = m_configurationService.SourceId;
            Guid sourceSideSourceId = m_configurationService.MigrationPeer;
            ConversionResult convRslt = new ConversionResult(sourceSideSourceId, targetSideSourceId);

            try
            {
                int skippedActionCount = 0;
                foreach (IMigrationAction action in changeGroup.Actions)
                {
                    if (IsSourceWorkItemInBacklog(action))
                    {
                        continue;
                    }

                    if (action.State == ActionState.Skipped)
                    {
                        ++skippedActionCount;
                        continue;
                    }

                    if (action.MigrationActionDescription == null
                        || action.MigrationActionDescription.DocumentElement == null)
                    {
                        throw new MigrationException(ClearQuestResource.ClearQuest_Error_InvalidActionDescription,
                                                     action.ActionId);
                    }

                    if (action.Action == WellKnownChangeActionId.Add)
                    {
                        AddRecord(action, convRslt);
                    }
                    else if (action.Action == WellKnownChangeActionId.Edit)
                    {
                        EditRecord(action, convRslt);
                    }
                    else if (action.Action == WellKnownChangeActionId.AddAttachment)
                    {
                        AddAttachment(action, convRslt);
                    }
                    else if (action.Action == WellKnownChangeActionId.DelAttachment)
                    {
                        DeleteAttachment(action, convRslt);
                    }
                    else
                    {
                        action.State = ActionState.Skipped;
                    }

                    if (action.State == ActionState.Skipped)
                    {
                        ++skippedActionCount;
                    }
                }

                if (skippedActionCount == changeGroup.Actions.Count)
                {
                    convRslt.ChangeId = Microsoft.TeamFoundation.Migration.Toolkit.Constants.MigrationResultSkipChangeGroup;
                }
            }
            catch (ClearQuestCOMDllNotFoundException cqComNotFoundEx)
            {
                if (!UtilityMethods.HandleCOMDllNotFoundException(cqComNotFoundEx, ErrorManager, m_conflictManagerService))
                {
                    convRslt.ContinueProcessing = false;
                }
            }
            catch (ClearQuestCOMCallException cqComCallEx)
            {
                if (!UtilityMethods.HandleCQComCallException(cqComCallEx, ErrorManager, m_conflictManagerService))
                {
                    convRslt.ContinueProcessing = false;
                }
            }
            catch (Exception ex)
            {
                if (!UtilityMethods.HandleGeneralException(ex, ErrorManager, m_conflictManagerService))
                {
                    convRslt.ContinueProcessing = false;
                }
            }

            return convRslt;
        }

        #endregion

        #region IServiceProvider Members

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IMigrationProvider))
            {
                return this;
            }

            return null;
        }

        #endregion

        #region IDisposable members
        public void Dispose()
        {
        }
        #endregion

        protected virtual void InitializeCQClient()
        {
            Microsoft.TeamFoundation.Migration.BusinessModel.MigrationSource migrationSourceConfig = m_configurationService.MigrationSource;
            string dbSet = migrationSourceConfig.ServerUrl;
            string userDb = migrationSourceConfig.SourceIdentifier;

            ICredentialManagementService credManagementService =
                m_migrationServiceContainer.GetService(typeof(ICredentialManagementService)) as ICredentialManagementService;

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

            m_migrationContext = new ClearQuestMigrationContext(m_userSession, migrationSourceConfig);
        }

        internal string NoteEntryFieldName
        {
            get
            {
                return m_migrationContext.NoteEntryFieldName;
            }
        }

        internal string AttachmentSinkField
        {
            get
            {
                // teyang_todo use custom setting
                return "Attachments";
            }
        }

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

        private ITranslationService TranslationService
        {
            get
            {
                // lazy loading translation service
                if (null == m_translationService)
                {
                    m_translationService = m_migrationServiceContainer.GetService(typeof(ITranslationService)) as ITranslationService;
                    Debug.Assert(m_translationService != null, "Translation service is not initialized");
                }

                return m_translationService;
            }
        }

        private void DeleteAttachment(IMigrationAction action, ConversionResult convRslt)
        {
            throw new NotImplementedException();
        }

        private void AddAttachment(IMigrationAction action, ConversionResult convRslt)
        {
            string attName = UtilityMethods.ExtractAttachmentName(action);
            string lengthStr = UtilityMethods.ExtractAttachmentLength(action);
            string attComment = UtilityMethods.ExtractAttachmentComment(action);

            string targetWorkItemId = FindTargetWorkItemId(action);
            string ownerRecordType = ExtractRecordTypeFromItemId(targetWorkItemId);
            string ownerRecordDisplayName = ExtractDisplayNameFromItemId(targetWorkItemId);

            string downloadSubFolder = Path.Combine(m_configurationService.WorkspaceRoot, ownerRecordDisplayName);
            string filePath;
            try
            {
                Directory.CreateDirectory(downloadSubFolder);
                filePath = Path.Combine(downloadSubFolder, attName);
            }
            catch (IOException)
            {
                filePath = Path.Combine(m_configurationService.WorkspaceRoot, ownerRecordDisplayName + attName);
            }

            try
            {
                // find the entity
                OAdEntity entity = CQWrapper.GetEntity(m_userSession, ownerRecordType, ownerRecordDisplayName);

                if (AttachmentExists(entity, attName, attComment, lengthStr))
                {
                    action.State = ActionState.Skipped;
                    return;
                }


                // find the change action def name
                string entityDefName = CQWrapper.GetEntityDefName(entity);
                OAdEntityDef entityDef = CQWrapper.GetEntityDef(m_userSession, entityDefName);

                string modifyActionDefName = FindCQActionDefName(entityDef, CQConstants.ACTION_MODIFY);

                // mark entity to be editable
                CQWrapper.EditEntity(m_userSession, entity, modifyActionDefName);

                // cache the current history count for all "history fields"
                // i.e. pairs of HistoryFieldName, count
                Dictionary<string, int> recordHistoryCountCache = new Dictionary<string, int>();
                BuildRecordHistoryCountCache(entity, recordHistoryCountCache);

                action.SourceItem.Download(filePath);

                string attachmentField = m_migrationContext.GetAttachmentSinkField(entityDefName);
                string retVal = CQWrapper.AddAttachmentFieldValue(entity, attachmentField, filePath, attComment);
                if (!string.IsNullOrEmpty(retVal))
                {
                    // teyang_todo attachment conflict
                }

                retVal = CQWrapper.Validate(entity);
                if (!string.IsNullOrEmpty(retVal))
                {
                    // TODO: Raise a specific conflict 
                    throw new MigrationException(retVal);
                }

                retVal = CQWrapper.Commmit(entity);
                if (!string.IsNullOrEmpty(retVal))
                {
                    // TODO: Raise a specific conflict
                    throw new MigrationException(retVal);
                }

                if (action.State == ActionState.Pending)
                {
                    action.State = ActionState.Complete;
                }

                // *********
                // now comparing to the cache, so that we can clearly identify the item:version pairs
                // e.g. TargetCQRecordDisplayName : HistoryFieldName::LatestHistoryIndex
                Dictionary<string, int[]> updatedHistoryIndices = new Dictionary<string, int[]>();
                FindUpdatedHistoryIndices(entity, recordHistoryCountCache, updatedHistoryIndices);
                recordHistoryCountCache.Clear();

                foreach (string histFieldName in updatedHistoryIndices.Keys)
                {
                    foreach (int histIndex in updatedHistoryIndices[histFieldName])
                    {
                        UpdateConversionHistory(action,
                                                targetWorkItemId,
                                                CQHistoryMigrationItem.CreateHistoryItemVersion(histFieldName, histIndex),
                                                convRslt);
                    }
                }
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                if (Directory.Exists(downloadSubFolder))
                {
                    Directory.Delete(downloadSubFolder);
                }
            }
        }

        /// <summary>
        /// Determine if an action's source item is in backlog, if so, backlog the action
        /// </summary>
        /// <param name="conflictManager"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        private bool IsSourceWorkItemInBacklog(
            IMigrationAction action)
        {
            string sourceSideItemId = action.FromPath;
            Debug.Assert(!string.IsNullOrEmpty(sourceSideItemId), "Work Item Id is not available in conflict details");

            // look up in backlog db table
            bool workItemInBacklog = m_conflictManagerService.IsItemInBacklog(sourceSideItemId);

            if (workItemInBacklog)
            {
                MigrationConflict chainedConflict = new ChainOnBackloggedItemConflictType().CreateConflict(
                    ChainOnBackloggedItemConflictType.CreateConflictDetails(sourceSideItemId, action.Version),
                    ChainOnBackloggedItemConflictType.CreateScopeHint(sourceSideItemId),
                    action);

                // previous revision of the work item has conflict, push this revision to backlog as well
                m_conflictManagerService.BacklogUnresolvedConflict(m_conflictManagerService.SourceId, chainedConflict, true);
            }

            return workItemInBacklog;
        }

        private bool AttachmentExists(
            OAdEntity entity,
            string attName,
            string attComment,
            string lengthStr)
        {
            OAdAttachmentFields aAttachmentFields = CQWrapper.GetAttachmentFields(entity);

            for (int aAttachmentFieldsIndex = 0;
                 aAttachmentFieldsIndex < CQWrapper.AttachmentsFieldsCount(aAttachmentFields);
                 aAttachmentFieldsIndex++)
            {
                object ob = (object)aAttachmentFieldsIndex;
                OAdAttachmentField aAttachmentField = CQWrapper.AttachmentsFieldsItem(aAttachmentFields, ref ob);

                // process all attachments
                OAdAttachments attachments = CQWrapper.GetAttachments(aAttachmentField);
                for (int attachmentIndex = 0;
                     attachmentIndex < CQWrapper.AttachmentsCount(attachments);
                     attachmentIndex++)
                {
                    object obIndex = (object)attachmentIndex;
                    OAdAttachment aAttachment = CQWrapper.AttachmentsItem(attachments, ref obIndex);

                    string name;
                    string comment;
                    string dispName;
                    int fileSize;
                    CQWrapper.GetAttachmentMetadata(aAttachment,
                                                    out name,
                                                    out comment,
                                                    out dispName,
                                                    out fileSize);

                    if (attName.Equals(name) && attComment.Equals(comment) && long.Parse(lengthStr).Equals((long)fileSize))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void FindUpdatedHistoryIndices(
            OAdEntity aRecord,
            Dictionary<string, int> recordHistoryCountCache,
            Dictionary<string, int[]> updatedHistoryIndices)
        {
            OAdHistoryFields aHistFields = CQWrapper.GetHistoryFields(aRecord);
            int historyFldCount = CQWrapper.HistoryFieldsCount(aHistFields);
            for (int histFldIndex = 0; histFldIndex < historyFldCount; histFldIndex++)
            {
                object ob = (object)histFldIndex;
                OAdHistoryField aHistoryField = CQWrapper.HistoryFieldsItem(aHistFields, ref ob);
                string historyFieldName = CQWrapper.GetHistoryFieldName(aHistoryField);
                int latestHistoryCount = CQWrapper.HistoryFieldHistoriesCount(aHistoryField);

                int oldHistoryCount;
                if (recordHistoryCountCache.ContainsKey(historyFieldName))
                {
                    oldHistoryCount = recordHistoryCountCache[historyFieldName];
                }
                else
                {
                    oldHistoryCount = 0;
                }

                int numOfNewHistory = latestHistoryCount - oldHistoryCount;
                if (numOfNewHistory > 0)
                {
                    updatedHistoryIndices.Add(historyFieldName, new int[numOfNewHistory]);
                    for (int i = 0; i < numOfNewHistory; ++i)
                    {
                        updatedHistoryIndices[historyFieldName][i] = oldHistoryCount + i;
                    }
                }
            }
        }

        private void BuildRecordHistoryCountCache(
            OAdEntity aRecord,
            Dictionary<string, int> recordHistoryCountCache)
        {
            OAdHistoryFields aHistFields = CQWrapper.GetHistoryFields(aRecord);
            int historyFldCount = CQWrapper.HistoryFieldsCount(aHistFields);
            for (int histFldIndex = 0; histFldIndex < historyFldCount; histFldIndex++)
            {
                object ob = (object)histFldIndex;
                OAdHistoryField aHistoryField = CQWrapper.HistoryFieldsItem(aHistFields, ref ob);
                string historyFieldName = CQWrapper.GetHistoryFieldName(aHistoryField);

                int historyCount = CQWrapper.HistoryFieldHistoriesCount(aHistoryField);
                recordHistoryCountCache.Add(historyFieldName, historyCount);
            }
        }

        private void EditRecord(IMigrationAction action, ConversionResult convRslt)
        {
            string targetWorkItemId = FindTargetWorkItemId(action);
            string ownerRecordDisplayName = ExtractDisplayNameFromItemId(targetWorkItemId);
            string ownerRecordType = UtilityMethods.ExtractRecordType(action);
            string changeAuthor = UtilityMethods.ExtractAuthor(action);

            // find the entity
            OAdEntity entity = CQWrapper.GetEntity(m_userSession, ownerRecordType, ownerRecordDisplayName);

            XmlNodeList columns = action.MigrationActionDescription.SelectNodes("/WorkItemChanges/Columns/Column");
            if (null == columns)
            {
                throw new MigrationException(ClearQuestResource.ClearQuest_Error_InvalidActionDescription, action.ActionId);
            }

            string stateField = m_migrationContext.GetStateField(ownerRecordType);
            XmlNode stateTransitFieldNode = UtilityMethods.ExtractSingleFieldNodeFromMigrationDescription(
                                                action.MigrationActionDescription, stateField);
            bool containsStateTransit = (stateTransitFieldNode != null);

            if (containsStateTransit)
            {
                // change contains state transition
                List<string> skipFields;
                if (ChangeRecordState(entity, action, convRslt, stateTransitFieldNode, out skipFields))
                {
                    ModifyRecordContent(entity, action, convRslt, skipFields);
                }
            }
            else
            {
                ModifyRecordContent(entity, action, convRslt, null);
            }
        }

        private string FindCQActionDefName(
            OAdEntityDef entityDef,
            int actionType)
        {
            // find the MODIFY action def name to open the record
            object[] actionDefNames = CQWrapper.GetActionDefNames(entityDef) as object[];
            string[] modifyActionDefNames = CQUtilityMethods.FindActionNameByType(entityDef, actionDefNames, actionType);

            if (modifyActionDefNames.Length == 0)
            {
                TraceManager.TraceError("Expected one or more CQ modify action def names for an entity def but did not find any");
                throw new InvalidOperationException();
            }
            else
            {
                // It's possible that there is more than 1 item returned from modifyActionDefNames, but it appears that the first
                // one is always "Modify" which is what we want.
                return modifyActionDefNames[0];
            }
        }

        private void ModifyRecordContent(
            OAdEntity entity,
            IMigrationAction action,
            ConversionResult convRslt,
            List<string> skipFields)
        {
            XmlNodeList columns = action.MigrationActionDescription.SelectNodes("/WorkItemChanges/Columns/Column");
            if (null == columns)
            {
                throw new MigrationException(ClearQuestResource.ClearQuest_Error_InvalidActionDescription, action.ActionId);
            }

            // *********
            // cache the current history count for all "history fields"
            // i.e. pairs of HistoryFieldName, count
            Dictionary<string, int> recordHistoryCountCache = new Dictionary<string, int>();
            BuildRecordHistoryCountCache(entity, recordHistoryCountCache);

            SetRecordEditable(entity);

            StringBuilder updateLog = new StringBuilder();
            PrintUpdateLogHeader(action, updateLog);

            string entityDefName = CQWrapper.GetEntityDefName(entity);
            string stateTransitionFieldDefName = m_migrationContext.GetStateField(entityDefName);

            string retVal;
            bool recordIsUpdated = false;
            foreach (XmlNode columnData in columns)
            {
                string stringVal = columnData.FirstChild.InnerText;
                string fieldName = columnData.Attributes["ReferenceName"].Value;
                Debug.Assert(!string.IsNullOrEmpty(fieldName),
                             "Field ReferenceName is absent in the Migration Description");

                if (CQStringComparer.FieldName.Equals(fieldName, stateTransitionFieldDefName)
                    || (null != skipFields && skipFields.Contains(fieldName, CQStringComparer.FieldName)))
                {
                    // skip or "State" field, as it has already been submitted in a separate history/revision
                    continue;
                }

                bool setFieldValue = false;
                OAdFieldInfo aFieldInfo = CQWrapper.GetEntityFieldValue(entity, fieldName);
                int fieldRequiredness = CQWrapper.GetRequiredness(aFieldInfo);
                switch (fieldRequiredness)
                {
                    case CQConstants.MANDATORY:
                    case CQConstants.OPTIONAL:
                        setFieldValue = true;
                        break;
                    case CQConstants.READONLY:
                        // [teyang] TODO conflict handling
                        TraceManager.TraceWarning("Field {0} is READONLY", fieldName);
                        setFieldValue = false;
                        break;
                    case CQConstants.USEHOOK:
                        // [teyang] TODO conflict handling
                        TraceManager.TraceWarning("Field {0} is USEHOOK", fieldName);
                        setFieldValue = false;
                        break;
                    default:
                        throw new InvalidOperationException();
                }

                if (setFieldValue)
                {
                    int attempt1Count = 0;
                    if (!SetFieldValue(action, entity, fieldName, stringVal, ref attempt1Count))
                    {
                        return;
                    }
                    AddFieldToUpdateLog(fieldName, stringVal, updateLog);
                    recordIsUpdated = true;
                }
            }

            if (!recordIsUpdated)
            {
                // no update has been made to the record, mark this action to be skipped
                CQWrapper.Revert(entity);
                if (action.State == ActionState.Pending)
                {
                    action.State = ActionState.Skipped;
                }

                return;
            }

            AddLineToUpdateLog(updateLog);
            int attempt2Count = 0;
            if (!string.IsNullOrEmpty(NoteEntryFieldName))
            {
                if (!SetFieldValue(action, entity, NoteEntryFieldName, updateLog.ToString(), ref attempt2Count))
                {
                    return;
                }
            }

            retVal = CQWrapper.Validate(entity);
            if (!string.IsNullOrEmpty(retVal))
            {
                IEnumerable<Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQTextParser.RecordValidationResult> validationResults;
                if (CQTextParser.RecordValidationTextParser.TryParse(retVal, out validationResults))
                {
                    foreach (CQTextParser.RecordValidationResult rslt in validationResults)
                    {
                        MigrationConflict conflict = ClearQuestInvalidFieldValueConflictType.CreateConflict(rslt, action);
                        List<MigrationAction> actions;
                        var resolutionRslt = m_conflictManagerService.TryResolveNewConflict(m_conflictManagerService.SourceId, conflict, out actions);
                        if (!resolutionRslt.Resolved)
                        {
                            action.ChangeGroup.ContainsBackloggedAction = true;
                            return;
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException(retVal);
                }
            }

            retVal = CQWrapper.Commmit(entity);
            if (!string.IsNullOrEmpty(retVal))
            {
                // [teyang] TODO: invalid update conflict handling
                throw new InvalidOperationException(retVal);
            }

            if (action.State == ActionState.Pending)
            {
                action.State = ActionState.Complete;
            }

            // *********
            // now comparing to the cache, so that we can clearly identify the item:version pairs
            // e.g. TargetCQRecordDisplayName : HistoryFieldName::LatestHistoryIndex
            Dictionary<string, int[]> updatedHistoryIndices = new Dictionary<string, int[]>();
            FindUpdatedHistoryIndices(entity, recordHistoryCountCache, updatedHistoryIndices);
            recordHistoryCountCache.Clear();

            // Get the record's ItemId for updating conversion history
            string recordItemId = UtilityMethods.CreateCQRecordMigrationItemId(entity);

            foreach (string histFieldName in updatedHistoryIndices.Keys)
            {
                foreach (int histIndex in updatedHistoryIndices[histFieldName])
                {
                    UpdateConversionHistory(action,
                                            recordItemId,
                                            CQHistoryMigrationItem.CreateHistoryItemVersion(histFieldName, histIndex),
                                            convRslt);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="action"></param>
        /// <param name="convRslt"></param>
        /// <param name="stateTransitFieldNode"></param>
        /// <param name="skipFields"></param>
        /// <returns></returns>
        private bool ChangeRecordState(
            OAdEntity entity,
            IMigrationAction action,
            ConversionResult convRslt,
            XmlNode stateTransitFieldNode,
            out List<string> processedFields)
        {
            processedFields = new List<string>();

            string destState = UtilityMethods.ExtractSingleFieldValue(stateTransitFieldNode);
            Debug.Assert(!string.IsNullOrEmpty(destState), "string.IsNullOrEmpty(newState)");

            string entityDefName = CQWrapper.GetEntityDefName(entity);

            // find the current state
            OAdFieldInfo aFldInfo = CQWrapper.GetEntityFieldValue(entity, m_migrationContext.GetStateField(entityDefName));
            string srcState = CQWrapper.GetFieldValue(aFldInfo);

            if (CQStringComparer.StateName.Equals(srcState, destState))
            {
                // state does not change, skip this action
                return false;
            }

            // find action def name
            OAdEntityDef entityDef = CQWrapper.GetEntityDef(m_userSession, entityDefName);
            string[] changeActionNames = CQUtilityMethods.FindAllActionNameByTypeAndStateTransition(entityDef, srcState, destState, CQConstants.ACTION_CHANGE_STATE);

            if (changeActionNames.Length == 0)
            {
                // [teyang] todo error handling 
                throw new InvalidOperationException();
            }

            string changeActionName = changeActionNames[0];

            // *********
            // cache the current history count for all "history fields"
            // i.e. pairs of HistoryFieldName, count
            Dictionary<string, int> recordHistoryCountCache = new Dictionary<string, int>();
            BuildRecordHistoryCountCache(entity, recordHistoryCountCache);

            StringBuilder updateLog = new StringBuilder();
            PrintUpdateLogHeader(action, updateLog);

            // mark entity to be editable with the desired state-change action)
            CQWrapper.EditEntity(m_userSession, entity, changeActionName);

            XmlNodeList columns = action.MigrationActionDescription.SelectNodes("/WorkItemChanges/Columns/Column");
            if (null == columns)
            {
                throw new MigrationException(ClearQuestResource.ClearQuest_Error_InvalidActionDescription, action.ActionId);
            }

            foreach (XmlNode columnData in columns)
            {
                string stringVal = columnData.FirstChild.InnerText;
                string fieldName = columnData.Attributes["ReferenceName"].Value;

                Debug.Assert(!string.IsNullOrEmpty(fieldName),
                             "Field ReferenceName is absent in the Migration Description");


                OAdFieldInfo aFieldInfo = CQWrapper.GetEntityFieldValue(entity, fieldName);
                int fieldRequiredness = CQWrapper.GetRequiredness(aFieldInfo);
                if (fieldRequiredness != CQConstants.MANDATORY)
                {
                    // skipping all non-mandatory fields
                    continue;
                }

                int attempt1Count = 0;
                if (!SetFieldValue(action, entity, fieldName, stringVal, ref attempt1Count))
                {
                    return false;
                }
                AddFieldToUpdateLog(fieldName, stringVal, updateLog);
                processedFields.Add(fieldName);
            }

            AddLineToUpdateLog(updateLog);
            int attempt2Count = 0;
            if (!string.IsNullOrEmpty(NoteEntryFieldName))
            {
                if (!SetFieldValue(action, entity, NoteEntryFieldName, updateLog.ToString(), ref attempt2Count))
                {
                    return false;
                }
            }

            string retVal = CQWrapper.Validate(entity);
            if (!string.IsNullOrEmpty(retVal))
            {
                // [teyang] TODO conflict handling
                throw new InvalidOperationException(retVal);
            }

            retVal = CQWrapper.Commmit(entity);
            if (!string.IsNullOrEmpty(retVal))
            {
                // [teyang] TODO conflict handling
                throw new InvalidOperationException(retVal);
            }

            if (action.State == ActionState.Pending)
            {
                action.State = ActionState.Complete;
            }

            // *********
            // now comparing to the cache, so that we can clearly identify the item:version pairs
            // e.g. TargetCQRecordDisplayName : HistoryFieldName::LatestHistoryIndex
            Dictionary<string, int[]> updatedHistoryIndices = new Dictionary<string, int[]>();
            FindUpdatedHistoryIndices(entity, recordHistoryCountCache, updatedHistoryIndices);
            recordHistoryCountCache.Clear();

            // Get the record's ItemId for updating conversion history
            string recordItemId = UtilityMethods.CreateCQRecordMigrationItemId(entity);

            foreach (string histFieldName in updatedHistoryIndices.Keys)
            {
                foreach (int histIndex in updatedHistoryIndices[histFieldName])
                {
                    UpdateConversionHistory(action,
                                            recordItemId,
                                            CQHistoryMigrationItem.CreateHistoryItemVersion(histFieldName, histIndex),
                                            convRslt);
                }
            }

            return true;
        }

        private bool SetFieldValue(
            IMigrationAction action,
            OAdEntity record,
            string fieldName,
            string stringVal,
            ref int numOfAttempts)
        {
            numOfAttempts++;

            OAdFieldInfo aFieldInfo = CQWrapper.GetEntityFieldValue(record, fieldName);
            string originalFieldValue = CQWrapper.GetFieldValue(aFieldInfo);

            // doing the real job: setting field value with CQ OM
            string cqRetVal = CQWrapper.SetFieldValue(record, fieldName, stringVal);

            // error handling
            if (!string.IsNullOrEmpty(cqRetVal))
            {
                MigrationConflict conflict = ClearQuestSetFieldValueConflictType.CreateConflict(
                                                UtilityMethods.ExtractSourceWorkItemId(action),
                                                UtilityMethods.ExtractSourceWorkItemRevision(action),
                                                fieldName, stringVal, cqRetVal);
                List<MigrationAction> migrationActions;
                var resolutionResult = m_conflictManagerService.TryResolveNewConflict(
                                            m_conflictManagerService.SourceId,
                                            conflict,
                                            out migrationActions);

                if (!resolutionResult.Resolved)
                {
                    // cannot resolve the conflict, move on to next MigrationAction
                    return false;
                }
                else if (numOfAttempts <= 3)
                {
                    // not reached maximum set value attempts yet
                    if (resolutionResult.ResolutionType == ConflictResolutionType.UpdatedConflictedChangeAction)
                    {
                        XmlNode column = UtilityMethods.ExtractSingleFieldNodeFromMigrationDescription(
                                            action.MigrationActionDescription, fieldName);

                        if (null == column)
                        {
                            // the field has been dropped during conflict resolution
                            // restore the "original" value
                            return SetFieldValue(action, record, fieldName, originalFieldValue, ref numOfAttempts);
                        }
                        else
                        {
                            string newFieldValue = UtilityMethods.ExtractSingleFieldValue(column);
                            return SetFieldValue(action, record, fieldName, stringVal, ref numOfAttempts);
                        }
                    }
                }
                else
                {
                    // reached max set value attempts WITH unresolved conflict
                    return false;
                }
            }

            return true;
        }

        private void AddRecord(IMigrationAction action, ConversionResult convRslt)
        {
            string recordType = UtilityMethods.ExtractRecordType(action);
            OAdEntity newRecord = CQWrapper.BuildEntity(m_userSession, recordType);

            string validationRsltString = string.Empty;
            List<string> processedFields = new List<string>();
            #region add new record with MANDATORY field values
            
                if (!SetMandatoryFields(action, ref newRecord, out processedFields))
                {
                    return;
                }

                bool unResolvedConflictExists = false;
                bool validationErrorExists = false;
                validationRsltString = CQWrapper.Validate(newRecord);
                if (!string.IsNullOrEmpty(validationRsltString))
                {
                    validationErrorExists = true;
                    IEnumerable<CQTextParser.RecordValidationResult> validationResults;
                    if (CQTextParser.RecordValidationTextParser.TryParse(validationRsltString, out validationResults))
                    {
                        foreach (CQTextParser.RecordValidationResult rslt in validationResults)
                        {
                            MigrationConflict conflict = ClearQuestInvalidFieldValueConflictType.CreateConflict(rslt, action);
                            List<MigrationAction> actions;
                            var resolutionRslt = m_conflictManagerService.TryResolveNewConflict(m_conflictManagerService.SourceId, conflict, out actions);
                            if (!resolutionRslt.Resolved)
                            {
                                unResolvedConflictExists = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException(validationRsltString);
                    }
                }

                if (unResolvedConflictExists)
                {
                    return;
                }
                else if (validationErrorExists)
                {
                    // All conflicts are resolved. Try re-applying the changes
                    newRecord.Revert();

                    SetRecordEditable(newRecord);

                    if (!SetMandatoryFields(action, ref newRecord, out processedFields))
                    {
                        return;
                    }
                    else
                    {
                        validationRsltString = CQWrapper.Validate(newRecord);
                        if (!string.IsNullOrEmpty(validationRsltString))
                        {
                            IEnumerable<CQTextParser.RecordValidationResult> validationResults;
                            if (CQTextParser.RecordValidationTextParser.TryParse(validationRsltString, out validationResults)
                                && validationResults.Count() > 0)
                            {
                                CQTextParser.RecordValidationResult rslt = validationResults.First();
                                MigrationConflict conflict = ClearQuestInvalidFieldValueConflictType.CreateConflict(rslt, action);
                                m_conflictManagerService.BacklogUnresolvedConflict(m_conflictManagerService.SourceId, conflict, false);
                                return;
                            }
                            else
                            {
                                throw new InvalidOperationException(validationRsltString);
                            }
                        }
                    }
                }
            

            validationRsltString = CQWrapper.Commmit(newRecord);
            if (!string.IsNullOrEmpty(validationRsltString))
            {
                // [teyang] TODO: invalid update conflict handling
                throw new InvalidOperationException(validationRsltString);
            }

            if (action.State == ActionState.Pending)
            {
                action.State = ActionState.Complete;
            }

            // Get the record's ItemId for updating conversion history
            string recordItemId = UtilityMethods.CreateCQRecordMigrationItemId(newRecord);

            UpdateConversionHistory(action, recordItemId, ClearQuestRecordItem.NewRecordVersion, convRslt);
            #endregion

            #region update the new record with remaining field values
            ModifyRecordContent(newRecord, action, convRslt, processedFields);
            #endregion
        }

        private void SetRecordEditable(OAdEntity cqEntity)
        {
            string entityDefName = CQWrapper.GetEntityDefName(cqEntity);
            OAdEntityDef entityDef = CQWrapper.GetEntityDef(m_userSession, entityDefName);
            string modifyActionDefName = FindCQActionDefName(entityDef, CQConstants.ACTION_MODIFY);
            // open the record with the modify action
            CQWrapper.EditEntity(m_userSession, cqEntity, modifyActionDefName);
        }
        
        private bool SetMandatoryFields(IMigrationAction action, ref OAdEntity newRecord, out List<string> processedFields)
        {
            XmlNodeList columns = action.MigrationActionDescription.SelectNodes("/WorkItemChanges/Columns/Column");
            if (null == columns)
            {
                throw new MigrationException(ClearQuestResource.ClearQuest_Error_InvalidActionDescription, action.ActionId);
            }

            StringBuilder updateLog = new StringBuilder();
            PrintUpdateLogHeader(action, updateLog);
            processedFields = new List<string>();

            foreach (XmlNode columnData in columns)
            {
                string stringVal = columnData.FirstChild.InnerText;
                string fieldName = columnData.Attributes["ReferenceName"].Value;

                Debug.Assert(!string.IsNullOrEmpty(fieldName),
                             "Field ReferenceName is absent in the Migration Description");


                OAdFieldInfo aFieldInfo = CQWrapper.GetEntityFieldValue(newRecord, fieldName);
                int fieldRequiredness = CQWrapper.GetRequiredness(aFieldInfo);
                if (fieldRequiredness != CQConstants.MANDATORY)
                {
                    // skipping all non-mandatory fields
                    continue;
                }

                string originalFieldValue = CQWrapper.GetFieldValue(aFieldInfo);

                int attempt1Count = 0;
                if (!SetFieldValue(action, newRecord, fieldName, stringVal, ref attempt1Count))
                {
                    return false;
                }
                AddFieldToUpdateLog(fieldName, stringVal, updateLog);
                processedFields.Add(fieldName);
            }

            AddLineToUpdateLog(updateLog);
            int attempt2Count = 0;
            if (!string.IsNullOrEmpty(NoteEntryFieldName))
            {
                if (!SetFieldValue(action, newRecord, NoteEntryFieldName, updateLog.ToString(), ref attempt2Count))
                {
                    return false;
                }
            }

            return true;
        }

        private void AddFieldToUpdateLog(string fieldName, string stringVal, StringBuilder updateLog)
        {
            string fieldListing = String.Format("  {0}: {1}\n", fieldName, stringVal);
            updateLog.Append(m_commentDecorationService.AddToChangeGroupCommentSuffix(string.Empty, fieldListing));
        }

        private void AddLineToUpdateLog(StringBuilder updateLog)
        {
            updateLog.Append(m_commentDecorationService.AddToChangeGroupCommentSuffix(string.Empty, "\n"));
        }

        private void PrintUpdateLogHeader(IMigrationAction action, StringBuilder updateLog)
        {
            string author = UtilityMethods.ExtractAuthor(action);
            string changedDate = UtilityMethods.ExtractChangeDate(action);
            string srcWorkItemId = UtilityMethods.ExtractSourceWorkItemId(action);
            string srcWorkItemRev = UtilityMethods.ExtractSourceWorkItemRevision(action);

            updateLog.Append(m_commentDecorationService.GetChangeGroupCommentSuffix(srcWorkItemId));
            string revDecoration = String.Format(ClearQuestResource.ClearQuest_Msg_UpdateLogRevFormat, srcWorkItemRev);
            updateLog.Append(m_commentDecorationService.AddToChangeGroupCommentSuffix(string.Empty, revDecoration));
            string updateLogDetailsHeader = String.Format(ClearQuestResource.ClearQuest_Msg_UpdateLogHeaderFormat, author, changedDate);
            updateLog.Append(m_commentDecorationService.AddToChangeGroupCommentSuffix(string.Empty, updateLogDetailsHeader));
        }

        private void UpdateConversionHistory(
            IMigrationAction action,
            string newRecordDisplayName,
            string newRecordVersion,
            ConversionResult convRslt)
        {
            string sourceWorkItemId = UtilityMethods.ExtractSourceWorkItemId(action);
            string sourceWorkItemRevision = UtilityMethods.ExtractSourceWorkItemRevision(action);

            // update conversion history cache
            if (action.Action.Equals(WellKnownChangeActionId.Add)
                || action.Action.Equals(WellKnownChangeActionId.Edit))
            {
                // insert conversion history for pushing to db
                convRslt.ItemConversionHistory.Add(
                    new ItemConversionHistory(sourceWorkItemId, sourceWorkItemRevision,
                                              newRecordDisplayName, newRecordVersion));
                convRslt.ChangeId = newRecordDisplayName + ":" + newRecordVersion;
            }
            else if (action.Action.Equals(WellKnownChangeActionId.AddAttachment)
                     || action.Action.Equals(WellKnownChangeActionId.DelAttachment))
            {
                // insert conversion history for pushing to db
                convRslt.ItemConversionHistory.Add(
                    new ItemConversionHistory(sourceWorkItemId, "Attachment",
                                              newRecordDisplayName,
                                              newRecordVersion));
                convRslt.ChangeId = newRecordDisplayName + ":" + newRecordVersion + " (Attachments)";
            }
        }

        private string FindTargetWorkItemId(IMigrationAction action)
        {
            string tgtWorkItemId = UtilityMethods.ExtractTargetWorkItemId(action);

            if (string.IsNullOrEmpty(tgtWorkItemId))
            {
                // translation phase didn't manage to translate the WorkItemId, which is possible
                // when the record was just created
                string srcWorkItemId = UtilityMethods.ExtractSourceWorkItemId(action);
                Debug.Assert(!string.IsNullOrEmpty(srcWorkItemId), "string.IsNullOrEmpty(srcWorkItemId)");

                tgtWorkItemId = TranslationService.TryGetTargetItemId(srcWorkItemId, m_configurationService.MigrationPeer);
            }

            return tgtWorkItemId;
        }

        private string ExtractDisplayNameFromItemId(string migrationItemId) 
        {
            string displayName = null;
            if (migrationItemId.Contains(UtilityMethods.MigrationItemDelimiter))
            {
                string[] identity = UtilityMethods.ParseCQRecordMigrationItemId(migrationItemId);
                if (identity.Length == 2)
                {
                    displayName = identity[1];
                }
            }
            if (displayName == null)
            {
                displayName = migrationItemId;
            }
            return displayName;
        }

        private string ExtractRecordTypeFromItemId(string migrationItemId)
        {
            string recordType = null;
            if (migrationItemId.Contains(UtilityMethods.MigrationItemDelimiter))
            {
                string[] identity = UtilityMethods.ParseCQRecordMigrationItemId(migrationItemId);
                if (identity.Length == 2)
                {
                    recordType = identity[0];
                }
            }
            if (recordType == null)
            {
                recordType = "Unknown";
            }
            return recordType;
        }
    }
}
