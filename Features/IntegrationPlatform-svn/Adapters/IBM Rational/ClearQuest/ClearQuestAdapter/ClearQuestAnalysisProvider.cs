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
using ClearQuestOleServer;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.ConflictManagement;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.Exceptions;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.MigrationItem;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    internal class ClearQuestAnalysisProvider : IAnalysisProvider
    {
        private ClearQuestOleServer.Session m_userSession;          // user session; instantiated after InitializeClient()
        #region not need until context sync requires us to access schema info
        //private ClearQuestOleServer.AdminSession m_adminSession;    // admin session; may be NULL if login info is not provided in config file 
        #endregion
        private CQRecordFilters m_filters;

        private Dictionary<Guid, ChangeActionHandler> m_supportedChangeActions;
        private Collection<ContentType> m_supportedContentTypes;

        private IServiceContainer m_analysisServiceContainer;
        private ChangeGroupService m_changeGroupService;
        private ConfigurationService m_configurationService;
        private ConflictManager m_conflictManagerService;
        private ITranslationService m_translationService;
        private ChangeActionRegistrationService m_changeActionRegistrationService;
        private ClearQuestChangeActionHandlers m_mgrtChangeActionHandlers;
        private HighWaterMark<DateTime> m_hwmDelta;

        private CQDeltaComputationProgressLookupService m_deltaComputeProgressService;
        private ClearQuestMigrationContext m_migrationContext;

        private bool m_isLoginUserSQLEditor;
        private bool m_isLastRevisionAutoCorrectionEnabled;

        private ErrorManager ErrorManager
        {
            get
            {
                ErrorManager errMgr = null;
                if (m_analysisServiceContainer != null)
                {
                    errMgr = m_analysisServiceContainer.GetService(typeof(ErrorManager)) as ErrorManager;
                }

                return errMgr;
            }
        }

        #region IAnalysisProvider Members

        /// <summary>
        /// List of change actions supported by TfsWITAdapter.
        /// </summary>
        public Dictionary<Guid, ChangeActionHandler> SupportedChangeActions
        {
            get 
            { 
                return m_supportedChangeActions; 
            }
        }

        /// <summary>
        ///  List of change actions supported by the other side.
        /// </summary>
        public ICollection<Guid> SupportedChangeActionsOther
        {
            get;
            set;
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
            get;
            set;
        }

        /// <summary>
        /// Initialize the adapter services
        /// </summary>
        /// <param name="analysisServiceContainer"></param>
        public void InitializeServices(IServiceContainer analysisServiceContainer)
        {
            m_analysisServiceContainer = analysisServiceContainer;

            m_changeGroupService = (ChangeGroupService)m_analysisServiceContainer.GetService(typeof(ChangeGroupService));
            Debug.Assert(m_changeGroupService != null, "Change group service is not initialized");

            m_changeGroupService.RegisterDefaultSourceSerializer(new ClearQuestRecordItemSerializer());
            //m_changeGroupService.RegisterSourceSerializer(
            //    WellKnownChangeActionId.SyncContext, new WorkItemContextSyncMigrationItemSerializer());
            m_changeGroupService.RegisterSourceSerializer(
                WellKnownChangeActionId.AddAttachment, new ClearQuestAttachmentItemSerializer());
            m_changeGroupService.RegisterSourceSerializer(
                WellKnownChangeActionId.DelAttachment, new ClearQuestAttachmentItemSerializer());

            m_configurationService = (ConfigurationService)m_analysisServiceContainer.GetService(typeof(ConfigurationService));
            Debug.Assert(m_configurationService != null, "Configuration service is not initialized");

            m_conflictManagerService = m_analysisServiceContainer.GetService(typeof(ConflictManager)) as ConflictManager;

            m_hwmDelta = new HighWaterMark<DateTime>(ClearQuestConstants.CqRecordHwm);
            m_configurationService.RegisterHighWaterMarkWithSession(m_hwmDelta);
        }

        /// <summary>
        /// Perform the adapter-specific initialization
        /// </summary>
        public void InitializeClient()
        {
            MigrationSource migrSrcConfig = m_configurationService.MigrationSource;
            Debug.Assert(null != migrSrcConfig, "cannot get MigrationSource config from Session");

            // default to enable last revision auto-correction
            m_isLastRevisionAutoCorrectionEnabled = true;
            foreach (CustomSetting setting in migrSrcConfig.CustomSettings.CustomSetting)
            {
                if (setting.SettingKey.Equals(ClearQuestConstants.EnableLastRevisionAutoCorrection, StringComparison.OrdinalIgnoreCase))
                {
                    string settingStr = setting.SettingValue;
                    if (!bool.TryParse(settingStr, out m_isLastRevisionAutoCorrectionEnabled))
                    {
                        m_isLastRevisionAutoCorrectionEnabled = true;
                    }
                    break;
                }
            }

            InitializeCQClient();
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

            LoadSupportedChangeActions();

            m_changeActionRegistrationService = changeActionRegistrationService;
            foreach (KeyValuePair<Guid, ChangeActionHandler> supportedChangeAction in m_supportedChangeActions)
            {
                // note: for now, CQ adapter uses a single change action handler for all content types
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
            if (contentTypeRegistrationService == null)
            {
                throw new ArgumentNullException("contentTypeRegistrationService");
            }

            LoadSupportedContentTypes();

            foreach (ContentType contentType in m_supportedContentTypes)
            {
                contentTypeRegistrationService.RegisterContentType(contentType);
            }
        }

        /// <summary>
        /// Register all conflict handlers with ConflictManager
        /// </summary>
        /// <param name="conflictManager"></param>
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

            m_conflictManagerService.RegisterConflictType(new ClearQuestMissingCQDllConflictType(),
                                                          SyncOrchestrator.ConflictsSyncOrchOptions.StopConflictedSession);

            m_conflictManagerService.RegisterConflictType(new ClearQuestInsufficentPrivilegeConflictType(),
                                                          SyncOrchestrator.ConflictsSyncOrchOptions.StopConflictedSession);
        }

        public void GenerateContextInfoTable()
        {
            return;
        }

        public void GenerateDeltaTable()
        {
            try
            {
                if (!m_isLoginUserSQLEditor)
                {
                    ValidateIsSQLEditor();
                    m_isLoginUserSQLEditor = true;
                }

                // load high watermark; as for CQ, we store local time for queries
                m_hwmDelta.Reload();
                DateTime hwmDeltaValue = m_hwmDelta.Value;
                if (hwmDeltaValue.Equals(default(DateTime)))
                {
                    hwmDeltaValue = new DateTime(1900, 1, 1);
                }
                hwmDeltaValue = hwmDeltaValue.AddSeconds(-1);   // go back 1 second as we'll drop the millisec below

                m_migrationContext.CurrentHWMBaseLine = hwmDeltaValue;

                // HACK HACK HACK
                //string hwmDeltaValueStr = hwmDeltaValue.ToString("o"); // using "ISO 8601" DateTime string format
                string hwmDeltaValueStr = hwmDeltaValue.ToString("u").Replace("Z", ""); // using "ISO 8601" DateTime string format
                
                if (hwmDeltaValueStr.LastIndexOf('.') >= 0)
                {
                    hwmDeltaValueStr = hwmDeltaValueStr.Substring(0, hwmDeltaValueStr.LastIndexOf('.')); // drop the millisec
                }
                // HACK HACK HACK

                DateTime newHwmValue = DateTime.Now;

                foreach (CQRecordFilter filter in m_filters)
                {
                    ComputeDeltaPerRecordType(filter, hwmDeltaValueStr);
                }

                // persist results and hwm
                TraceManager.TraceInformation("Promote delta to pending.");
                m_changeGroupService.PromoteDeltaToPending();

                m_hwmDelta.Update(newHwmValue);
                TraceManager.TraceInformation("Persisted CQ HWM: {0}", ClearQuestConstants.CqRecordHwm);
                TraceManager.TraceInformation("Updated CQ HWM: {0}", newHwmValue.ToString());
            }
            catch (ClearQuestInsufficientPrivilegeException privEx)
            {
                ConflictResolutionResult rslt = UtilityMethods.HandleInsufficientPriviledgeException(privEx, m_conflictManagerService);
                if (rslt.Resolved)
                {
                    // todo: currently not expected, as we only enabled manual/skip resolution action
                }
            }
            catch (ClearQuestCOMDllNotFoundException cqComNotFoundEx)
            {
                UtilityMethods.HandleCOMDllNotFoundException(cqComNotFoundEx, ErrorManager, m_conflictManagerService);
            }
            catch (ClearQuestCOMCallException cqComCallEx)
            {
                UtilityMethods.HandleCQComCallException(cqComCallEx, ErrorManager, m_conflictManagerService);
            }
            catch (Exception ex)
            {
                ErrorManager errMgr = null;
                if (m_analysisServiceContainer != null)
                {
                    errMgr = m_analysisServiceContainer.GetService(typeof(ErrorManager)) as ErrorManager;
                }
                UtilityMethods.HandleGeneralException(ex, errMgr, m_conflictManagerService);
            }
        }

        public void DetectConflicts(ChangeGroup changeGroup)
        {
            return;
        }
        
        public string GetNativeId(MigrationSource migrationSourceConfig)
        {
            string dbSet = migrationSourceConfig.ServerUrl;
            string userDb = migrationSourceConfig.SourceIdentifier;
            return userDb + "@" + dbSet;
        }

        #endregion

        #region IServiceProvider Members

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IAnalysisProvider))
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
                m_analysisServiceContainer.GetService(typeof(ICredentialManagementService)) as ICredentialManagementService;

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

        protected virtual void LoadSupportedChangeActions()
        {
            m_mgrtChangeActionHandlers = new ClearQuestChangeActionHandlers(this);
            m_supportedChangeActions = new Dictionary<Guid, ChangeActionHandler>();
            m_supportedChangeActions.Add(WellKnownChangeActionId.Add, this.m_mgrtChangeActionHandlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Delete, this.m_mgrtChangeActionHandlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Edit, this.m_mgrtChangeActionHandlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.AddAttachment, this.m_mgrtChangeActionHandlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.DelAttachment, this.m_mgrtChangeActionHandlers.BasicActionHandler);
            //m_supportedChangeActions.Add(WellKnownChangeActionId.SyncContext, this.m_mgrtChangeActionHandlers.BasicActionHandler);
        }

        protected virtual void LoadSupportedContentTypes()
        {
            m_supportedContentTypes = new Collection<ContentType>();

            // basic work item content type
            m_supportedContentTypes.Add(WellKnownContentType.WorkItem);

            // work item metadata content type
            //m_supportedContentTypes.Add(WellKnownContentType.GenericWorkItemFieldMetadata);
            //m_supportedContentTypes.Add(WellKnownContentType.Tfs2005WorkItemFieldMetadata);
            //m_supportedContentTypes.Add(WellKnownContentType.Tfs2008WorkItemFieldMetadata);
            //m_supportedContentTypes.Add(WellKnownContentType.UserGroupList);
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
                if (null == m_translationService)
                {
                    m_translationService = m_analysisServiceContainer.GetService(typeof(ITranslationService)) as ITranslationService;
                    Debug.Assert(m_translationService != null, "Translation service is not initialized");
                }

                return m_translationService;
            }
        }

        private CQDeltaComputationProgressLookupService DeltaComputeProgressService
        {
            get
            {
                if (null == m_deltaComputeProgressService)
                {
                    m_deltaComputeProgressService = new CQDeltaComputationProgressLookupService(TranslationService,
                                                                                                m_configurationService.SourceId);
                }

                return m_deltaComputeProgressService;
            }
        }

        private void ComputeDeltaPerRecordType(
            CQRecordFilter filter,
            string hwmTime)
        {
            CQRecordQueryBase recordQuery = 
                CQRecordQueryFactory.CreatQuery(m_userSession, filter, hwmTime, this);

            foreach (OAdEntity record in recordQuery)
            {
                // HACK HACK
                if (record != null) // this if check is HACK
                {
                    try
                    {
                        ComputeDeltaPerRecord(record);
                    }
                    catch (Exception ex) // eating exception is HACK
                    {
                        TraceManager.TraceInformation(string.Format("Workaround for XML save exception : {0}",
                            ex.Message));
                        TraceManager.TraceException(ex);
                    }
                }
                // HACK HACK
            }
        }

        private void ComputeDeltaPerRecord(
            OAdEntity aRecord)
        {
            OAdEntityDef aEntityDef = CQWrapper.GetEntityDef(m_userSession, CQWrapper.GetEntityDefName(aRecord));
            string recordDispName = CQWrapper.GetEntityDisplayName(aRecord);
            string recordEntDefName = CQWrapper.GetEntityDefName(aRecord);

            #region process history

            bool recordContentIsModified = false;
            bool maybeNewRecord = false;
            Dictionary<string, List<ClearQuestRecordItem>> historyDelta = new Dictionary<string, List<ClearQuestRecordItem>>();
            Dictionary<string, int> perHistoryFieldLastIndex = new Dictionary<string, int>();   // needed for updating processed delta

            // find all history fields
            OAdHistoryFields aHistFields = CQWrapper.GetHistoryFields(aRecord);
            int historyFldCount = CQWrapper.HistoryFieldsCount(aHistFields);
            bool containsNewHistory = false;
            for (int histFldIndex = 0; histFldIndex < historyFldCount; histFldIndex++)
            {
                object ob = (object)histFldIndex;
                OAdHistoryField aHistoryField = CQWrapper.HistoryFieldsItem(aHistFields, ref ob);
                string historyFieldName = CQWrapper.GetHistoryFieldName(aHistoryField);

                // find last processed history entry for this history field
                string lookupItemId = CQDeltaComputationProgressLookupService.CreateHistoryItemId(recordEntDefName, recordDispName, historyFieldName);
                int startHistIndex = 1 + DeltaComputeProgressService.GetLastProcessedItemVersion(lookupItemId);

                // find all history in a particular history field
                int historyCount = CQWrapper.HistoryFieldHistoriesCount(aHistoryField);
                for (int histIndex = startHistIndex; histIndex < historyCount; histIndex++)
                {
                    object obHistIndex = (object)histIndex;
                    OAdHistory aHistory = CQWrapper.HistoryFieldHistoriesItem(aHistoryField, ref obHistIndex);
                    CQHistory cqHistory = new CQHistory(aHistory);

                    CQMigrationItem migrationItem = new CQHistoryMigrationItem(recordDispName, historyFieldName, histIndex);

                    if (TranslationService.IsSyncGeneratedItemVersion(ClearQuestRecordItem.GetMigrationRecordId(recordEntDefName, recordDispName),
                                                                      migrationItem.MigrationItemVersion,
                                                                      m_configurationService.SourceId))
                    {
                        continue;
                    }

                    if (histIndex == 0)
                    {
                        maybeNewRecord = true;
                    }

                    // add unprocessed history fields for processing
                    if (!historyDelta.ContainsKey(historyFieldName))
                    {
                        historyDelta.Add(historyFieldName, new List<ClearQuestRecordItem>(historyCount));
                    }
                    historyDelta[aHistoryField.fieldname].Add(new ClearQuestRecordItem(aRecord, aHistory, historyFieldName, histIndex.ToString()));
                    containsNewHistory = true;

                    // based on action type, we decide whether content change is needed
                    int actionType = CQWrapper.GetActionDefType(aEntityDef, cqHistory.Action);
                    switch (actionType)
                    {
                        case CQConstants.ACTION_SUBMIT:
                            break;
                        case CQConstants.ACTION_MODIFY:
                            recordContentIsModified = true;
                            break;
                        case CQConstants.ACTION_CHANGE:
                            break;
                        case CQConstants.ACTION_DUPLICATE:
                            break;
                        case CQConstants.ACTION_UNDUPLICATE:
                            break;
                        case CQConstants.ACTION_IMPORT:
                            break;
                        case CQConstants.ACTION_DELETE:
                            TraceManager.TraceInformation(ClearQuestResource.ClearQuest_Msg_RecordDeleted, recordEntDefName, recordDispName);
                            break;
                        case CQConstants.ACTION_BASE:
                            break;
                        case CQConstants.ACTION_RECORD_SCRIPT_ALIAS:
                            break;
                    }
                }

                perHistoryFieldLastIndex.Add(historyFieldName, historyCount - 1);
            }

            #endregion

            #region generate delta for content and history

            if (maybeNewRecord || recordContentIsModified)
            {
                // the first revision, i.e. "Submit", of a CQ record is always hard-coded to be '1'
                CQMigrationItem contentMigrationAction = new CQMigrationItem(recordDispName, ClearQuestRecordItem.NewRecordVersion);
                bool isNewRecord = false;
                if (maybeNewRecord)
                {
                    isNewRecord = !(DeltaComputeProgressService.IsMigrationItemProcessed(recordDispName, ClearQuestRecordItem.NewRecordVersionValue));
                }

                if (!isNewRecord)
                {
                    // all subsequent record "MODIFICATIONs" are hard-coded to be "update@<Now.Ticks>"
                    contentMigrationAction.MigrationItemVersion = ClearQuestRecordItem.RecordUpdateVersion + "@" + DateTime.Now.Ticks;
                }

                ClearQuestRecordItem recordContentItem = new ClearQuestRecordItem(aRecord, contentMigrationAction.MigrationItemVersion);
                recordContentItem.CQSession = m_userSession;
                recordContentItem.Version = contentMigrationAction.MigrationItemVersion;
                ChangeGroup contentChangeGroup = recordContentItem.CreateChangeGroup(
                    m_changeGroupService, m_migrationContext, isNewRecord && m_isLastRevisionAutoCorrectionEnabled);
                contentChangeGroup.Save();

                if (isNewRecord && !containsNewHistory)
                {
                    DeltaComputeProgressService.UpdateCache(recordDispName, ClearQuestRecordItem.NewRecordVersionValue);
                }
            }

            var lastHistoryRecordItem = historyDelta[historyDelta.Keys.Last()].Last();
            foreach (string histFieldName in historyDelta.Keys)
            {
                foreach (ClearQuestRecordItem recordHistItem in historyDelta[histFieldName])
                {
                    recordHistItem.CQSession = m_userSession;
                    ChangeGroup changeGroup = recordHistItem.CreateChangeGroup(
                        m_changeGroupService, 
                        m_migrationContext, 
                        (CQStringComparer.FieldName.Equals(recordHistItem.HistoryFieldName, lastHistoryRecordItem.HistoryFieldName)
                        && recordHistItem.Version.Equals(lastHistoryRecordItem.Version, StringComparison.OrdinalIgnoreCase)
                        && m_isLastRevisionAutoCorrectionEnabled));
                    changeGroup.Save();
                }

                Debug.Assert(perHistoryFieldLastIndex.ContainsKey(histFieldName), "perHistoryFieldLastIndex.ContainsKey(histFieldName) returns false");
                string deltaComputeProcessLookupId = CQDeltaComputationProgressLookupService.CreateHistoryItemId(recordEntDefName, recordDispName, histFieldName);
                DeltaComputeProgressService.UpdateCache(deltaComputeProcessLookupId, perHistoryFieldLastIndex[histFieldName]);
            } 

            #endregion

            #region process attachment

            OAdAttachmentFields aAttachmentFields = CQWrapper.GetAttachmentFields(aRecord);
            
            for (int aAttachmentFieldsIndex = 0;
                 aAttachmentFieldsIndex < CQWrapper.AttachmentsFieldsCount(aAttachmentFields);
                 aAttachmentFieldsIndex++)
            {
                object ob = (object)aAttachmentFieldsIndex;
                OAdAttachmentField aAttachmentField = CQWrapper.AttachmentsFieldsItem(aAttachmentFields, ref ob);
                string fieldName = CQWrapper.GetAttachmentFieldName(aAttachmentField);

                ChangeGroup changeGroup = m_changeGroupService.CreateChangeGroupForDeltaTable(
                        string.Format("{0}:{1}:{2}", recordDispName, "Attachments", fieldName));

                // process all attachments
                OAdAttachments attachments = CQWrapper.GetAttachments(aAttachmentField);
                for (int attachmentIndex = 0;
                     attachmentIndex < CQWrapper.AttachmentsCount(attachments);
                     attachmentIndex++)
                {
                    object obIndex = (object)attachmentIndex;
                    OAdAttachment aAttachment = CQWrapper.AttachmentsItem(attachments, ref obIndex);

                    ClearQuestAttachmentItem attachmentItem = new ClearQuestAttachmentItem(aRecord, aAttachmentField, aAttachment, UserSessionConnConfig);
                    attachmentItem.CQSession = m_userSession;
                    attachmentItem.CreateChangeAction(changeGroup, lastHistoryRecordItem.Version);                    
                } 

                if (changeGroup.Actions.Count > 0)
                {
                    changeGroup.Save();
                }
            } 

            #endregion
        }

        private void ValidateIsSQLEditor()
        {
            /*
             * CQ adapter builds SQL query and sends it to CQ server to retrieve the CQ records for sync/migration
             * 
             * SQL Editor
             * Perform all Active User tasks plus the following activity:
             *   Edit SQL for queries in the Rational ClearQuest client and build SQL statements for API calls in hooks.
             * 
             * Attention: Security context is implemented in the SQL code that builds queries. 
             *            A user who has the SQL Editor privilege can bypass the security context.
             */

            if (!CQWrapper.HasUserPrivilege(m_userSession, (int)CQConstants.UserPrivilegeMaskType._RAW_SQL_WRITER))
            {
                string userLoginName = CQWrapper.GetUserLoginName(m_userSession);
                throw new ClearQuestInsufficientPrivilegeException(userLoginName, CQConstants.UserPrivilegeMaskType._RAW_SQL_WRITER.ToString());
            }
        }
    }
}
