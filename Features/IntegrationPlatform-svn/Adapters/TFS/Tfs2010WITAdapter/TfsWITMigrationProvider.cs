// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement.WITBasicConflicts;
using Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    enum WitMetadataSyncPolicy
    {
        DoNotTakeTheirs,
        TakeTheirs,
        Merge,
    }

    public partial class TfsWITMigrationProvider : IMigrationProvider, IComparer<Identity>
    {
        public TfsWITMigrationProvider()
        { }

        #region IMigrationProvider Members

        /// <summary>
        /// Establish the context based on the context info from the side of the pipeline
        /// </summary>
        public virtual void EstablishContext(ChangeGroupService sourceSystemChangeGroupService)
        {
            try
            {
                if (m_witMetadataSyncPolicy == WitMetadataSyncPolicy.DoNotTakeTheirs)
                {
                    return;
                }

                if (m_witMetadataSyncPolicy == WitMetadataSyncPolicy.Merge)
                {
                    throw new NotImplementedException("WitMetadataSyncPolicy.Merge has not been implemented");
                }

                int pageNumber = 0;
                int pageSize = 1000;
                IEnumerable<ChangeGroup> changeGroups = null;
                do
                {
                    changeGroups = sourceSystemChangeGroupService.NextDeltaTablePage(pageNumber++, pageSize, false);
                    foreach (ChangeGroup nextChangeGroup in changeGroups)
                    {
                        if (nextChangeGroup.ContainsBackloggedAction)
                        {
                            continue;
                        }

                        if (TryProcessContextSyncChangeGroup(nextChangeGroup))
                        {
                            break;
                        }
                    }
                } while (changeGroups.Count() == pageSize);
            }
            catch (Exception exception)
            {
                if (!(exception is MigrationUnresolvedConflictException))
                {
                    ErrorManager errMgr = m_migrationServiceContainer.GetService(typeof(ErrorManager)) as ErrorManager;
                    errMgr.TryHandleException(exception);
                }
                return;
            }
        }

        /// <summary>
        /// Try to process the change group, assuming it contains context sync instructions.
        /// </summary>
        /// <param name="nextChangeGroup"></param>
        /// <returns>true if the change group is a context sync group and it has been processed (either successfully or not)</returns>
        /// <exception cref="MigrationUnresolvedConflictException"></exception>
        /// <remarks>
        /// If sync succeeds, the change group will be marked as 'Completed'; otherwise, a conflict
        /// will be raised and we do not expect it to be auto-resolved.
        /// </remarks>
        private bool TryProcessContextSyncChangeGroup(ChangeGroup changeGroup)
        {
            // context sync steps must be strictly ordered, although they may not come in expected order
            // the following logic sorts them
            bool foundSyncContextAction = true;
            MigrationAction[] contextSyncActions = new MigrationAction[6];
            foreach (MigrationAction action in changeGroup.Actions)
            {
                if (action.Action.Equals(WellKnownChangeActionId.SyncContext))
                {
                    Debug.Assert(!string.IsNullOrEmpty(action.FromPath));
                    if (action.ItemTypeReferenceName.Equals(WellKnownContentType.UserGroupList.ReferenceName))
                    {
                        contextSyncActions[0] = action;
                    }
                    else if (action.ItemTypeReferenceName.Equals(WellKnownContentType.ValueListCollection.ReferenceName))
                    {
                        contextSyncActions[1] = action;
                    }
                    else if (action.ItemTypeReferenceName.Equals(WellKnownContentType.Tfs2005WorkItemFieldMetadata.ReferenceName)
                             || action.ItemTypeReferenceName.Equals(WellKnownContentType.Tfs2008WorkItemFieldMetadata.ReferenceName))
                    {
                        contextSyncActions[2] = action;
                    }
                    else if (action.ItemTypeReferenceName.Equals(TfsConstants.TfsAreaPathsContentTypeRefName))
                    {
                        contextSyncActions[3] = action;
                    }
                    else if (action.ItemTypeReferenceName.Equals(TfsConstants.TfsIterationPathsContentTypeRefName))
                    {
                        contextSyncActions[4] = action;
                    }
                    else if (action.ItemTypeReferenceName.Equals(TfsConstants.TfsCSSNodeChangesContentTypeRefName))
                    {
                        contextSyncActions[5] = action;
                    }
                }
                else
                {
                    foundSyncContextAction = false;
                    break;
                }
            }

            if (!foundSyncContextAction)
            {
                return false;
            }

            IMigrationAction currSyncAction = null;
            try
            {
                if (contextSyncActions[0] != null)
                {
                    currSyncAction = contextSyncActions[0];
                    SyncUserAccount(currSyncAction.MigrationActionDescription, m_witMetadataSyncPolicy);
                }

                if (contextSyncActions[1] != null)
                {
                    currSyncAction = contextSyncActions[1];
                    SyncGlobalList(currSyncAction.MigrationActionDescription, m_witMetadataSyncPolicy);
                }

                if (contextSyncActions[2] != null)
                {
                    currSyncAction = contextSyncActions[2];
                    SyncWorkItemTypeDefinition(currSyncAction.MigrationActionDescription,
                                               m_witMetadataSyncPolicy);
                }

                if (contextSyncActions[3] != null && !m_migrationSource.WorkItemStore.Core.DisableAreaPathAutoCreation)
                {
                    // keep for backward compatibility only (replaced by SyncCSSNodeChanges)
                    currSyncAction = contextSyncActions[3];
                    SyncAreaPaths(currSyncAction.MigrationActionDescription, m_witMetadataSyncPolicy);
                }

                if (contextSyncActions[4] != null && !m_migrationSource.WorkItemStore.Core.DisableIterationPathAutoCreation)
                {
                    // keep for backward compatibility only (replaced by SyncCSSNodeChanges)
                    currSyncAction = contextSyncActions[4];
                    SyncIterationPaths(currSyncAction.MigrationActionDescription, m_witMetadataSyncPolicy);
                }

                if (contextSyncActions[5] != null)
                {
                    currSyncAction = contextSyncActions[5];
                    SyncCSSNodeChanges(
                        currSyncAction.MigrationActionDescription,
                        m_migrationSource.WorkItemStore.Core.DisableAreaPathAutoCreation,
                        m_migrationSource.WorkItemStore.Core.DisableIterationPathAutoCreation);
                }

                changeGroup.Status = ChangeStatus.Complete;
                changeGroup.Save();

                return true;
            }
            catch (Exception ex)
            {
                if (!(ex is MigrationUnresolvedConflictException))
                {
                    // backlog the conflict context sync action/group
                    MigrationConflict genericeConflict = InvalidSubmissionConflictType.CreateConflict(
                        currSyncAction, ex, currSyncAction.ChangeGroup.Name, currSyncAction.ChangeGroup.Name);
                    var conflictManager = m_conflictManagementService.GetService(typeof(ConflictManager)) as ConflictManager;
                    Debug.Assert(null != conflictManager);
                    List<MigrationAction> resolutionActions;
                    ConflictResolutionResult resolveRslt =
                        conflictManager.TryResolveNewConflict(conflictManager.SourceId, genericeConflict, out resolutionActions);
                    Debug.Assert(!resolveRslt.Resolved);
                    return true;
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets a local work directory (for downloading attachment files).
        /// </summary>
        public virtual string LocalWorkDir
        {
            get
            {
                if (string.IsNullOrEmpty(m_localWorkDir))
                {
                    m_localWorkDir = m_configurationService.WorkspaceRoot;
                    if (!Directory.Exists(m_localWorkDir))
                    {
                        try
                        {
                            Directory.CreateDirectory(m_localWorkDir);
                        }
                        catch (Exception ex)
                        {
                            throw new MigrationException(
                                string.Format("Cannot create local directory at {0} to download attachment from source system", m_localWorkDir),
                                ex);
                        }
                    }
                }
                return m_localWorkDir;
            }
        }

        private void SyncAreaPaths(XmlDocument xmlDocument, WitMetadataSyncPolicy witMetadataSyncPolicy)
        {
            bool otherSideIsMaster = false;
            foreach (CustomSetting setting in m_configurationService.PeerMigrationSource.CustomSettings.CustomSetting)
            {
                if (setting.SettingKey.Equals(TfsConstants.DisableAreaPathAutoCreation, StringComparison.InvariantCultureIgnoreCase))
                {
                    otherSideIsMaster = true;
                }
            }

            if (witMetadataSyncPolicy == WitMetadataSyncPolicy.TakeTheirs)
            {
                byte[] newDocHash = new byte[0];
                bool hashMatched = m_md5Utility.CompareDocHash(xmlDocument, m_areaPathDocMD5, ref newDocHash);
                if (!hashMatched)
                {
                    m_migrationSource.WorkItemStore.SyncAreaPaths(xmlDocument, otherSideIsMaster);
                    m_md5Utility.UpdateDocHash(ref m_areaPathDocMD5, newDocHash);
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void SyncIterationPaths(XmlDocument xmlDocument, WitMetadataSyncPolicy witMetadataSyncPolicy)
        {
            bool otherSideIsMaster = false;
            foreach (CustomSetting setting in m_configurationService.PeerMigrationSource.CustomSettings.CustomSetting)
            {
                if (setting.SettingKey.Equals(TfsConstants.DisableIterationPathAutoCreation, StringComparison.InvariantCultureIgnoreCase))
                {
                    otherSideIsMaster = true;
                }
            }

            if (witMetadataSyncPolicy == WitMetadataSyncPolicy.TakeTheirs)
            {
                byte[] newDocHash = new byte[0];
                bool hashMatched = m_md5Utility.CompareDocHash(xmlDocument, m_iterationPathDocMD5, ref newDocHash);
                if (!hashMatched)
                {
                    m_migrationSource.WorkItemStore.SyncIterationPaths(xmlDocument, otherSideIsMaster);
                    m_md5Utility.UpdateDocHash(ref m_iterationPathDocMD5, newDocHash);
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void SyncCSSNodeChanges(XmlDocument cssNodeChangesDoc, bool disableAreaPathSync, bool disableIterationPathSync)
        {
            CSSAdapter adapter = new CSSAdapter(m_migrationSource.WorkItemStore.Core.Css, m_configurationService.SourceId);
            var store = m_migrationSource.WorkItemStore;
            adapter.SyncCSSNodeChanges(store.WorkItemStore.Projects[store.TeamProject], cssNodeChangesDoc, m_conflictManagementService);
        }

        private void SyncGlobalList(XmlDocument xmlDocument, WitMetadataSyncPolicy witMetadataSyncPolicy)
        {
            if (witMetadataSyncPolicy == WitMetadataSyncPolicy.TakeTheirs)
            {
                byte[] newDocHash = new byte[0];
                bool hashMatched = m_md5Utility.CompareDocHash(xmlDocument, m_globalListDocMD5, ref newDocHash);
                if (!hashMatched)
                {
                    m_migrationSource.WorkItemStore.UploadGlobalList(xmlDocument);
                    m_md5Utility.UpdateDocHash(ref m_globalListDocMD5, newDocHash);
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Comparer for identity type.
        /// </summary>
        /// <param name="i1">Identity 1</param>
        /// <param name="i2">Identity 2</param>
        /// <returns>Results of comparison</returns>
        int IComparer<Identity>.Compare(
            Identity x,
            Identity y)
        {
            Debug.Assert(x != null && y != null, "Null identity!");

            if (x.Deleted)
            {
                if (!y.Deleted) return -1;
            }
            else if (y.Deleted)
            {
                return 1;
            }

            return TFStringComparer.UserName.Compare(x.DisplayName, y.DisplayName);
        }

        private void SyncUserAccount(XmlDocument xmlDocument, WitMetadataSyncPolicy witMetadataSyncPolicy)
        {
            if (witMetadataSyncPolicy != WitMetadataSyncPolicy.TakeTheirs)
            {
                throw new NotImplementedException();
            }

            byte[] newDocHash = new byte[0];
            bool hashMatched = m_md5Utility.CompareDocHash(xmlDocument, m_userAccountsDocMD5, ref newDocHash);
            if (!hashMatched)
            {
                XmlNodeList globGroupNodes = xmlDocument.SelectNodes("/UserGroups/GlobalGroups/Identity");
                XmlNodeList projGroupNodes = xmlDocument.SelectNodes("/UserGroups/ProjectGroups/Identity");

                Identity[] globalGroupsOther = GetIdentities(globGroupNodes);
                Identity[] projectGroupsOther = GetIdentities(projGroupNodes);

                Project p = m_migrationSource.WorkItemStore.WorkItemStore.Projects[m_migrationSource.WorkItemStore.Core.Config.Project];
                Identity[] globalGroupsThis = m_migrationSource.WorkItemStore.GetGlobalGroups(p);
                Identity[] projectGroupsThis = m_migrationSource.WorkItemStore.GetProjectGroups(p);

                SymDiff<Identity> globs = new SymDiff<Identity>(globalGroupsOther, globalGroupsThis, this);
                SymDiff<Identity> projs = new SymDiff<Identity>(projectGroupsOther, projectGroupsThis, this);

                m_migrationSource.WorkItemStore.SyncAccounts(globs.LeftOnly, projs.LeftOnly);

                m_md5Utility.UpdateDocHash(ref m_userAccountsDocMD5, newDocHash);
            }
        }

        private Identity[] GetIdentities(XmlNodeList groupNodes)
        {
            List<Identity> groups = new List<Identity>(groupNodes.Count);
            foreach (XmlNode groupIdNode in groupNodes)
            {
                XmlDocument doc = new XmlDocument();
                XmlNode rootNode = doc.ImportNode(groupIdNode, true);
                doc.AppendChild(rootNode);

                XmlSerializer sr = new XmlSerializer(typeof(Identity));

                Guid guid = Guid.NewGuid();
                string tmpFilePath = Path.Combine(LocalWorkDir, guid.ToString() + ".xml");
                using (FileStream stream = new FileStream(tmpFilePath, FileMode.CreateNew, FileAccess.Write))
                {
                    doc.Save(stream);
                }

                using (FileStream stream = new FileStream(tmpFilePath, FileMode.Open, FileAccess.Read))
                {
                    Identity id = sr.Deserialize(stream) as Identity;
                    groups.Add(id);
                }

                File.Delete(tmpFilePath);
            }

            return groups.ToArray();
        }

        private void SyncWorkItemTypeDefinition(XmlDocument xmlDocument, WitMetadataSyncPolicy witMetadataSyncPolicy)
        {
            if (witMetadataSyncPolicy != WitMetadataSyncPolicy.TakeTheirs)
            {
                throw new NotImplementedException();
            }

            var customSettings = m_configurationService.MigrationSource.CustomSettings.CustomSetting;
            WITDUpdateCommands witdUpdateCommands = new WITDUpdateCommands(customSettings);
            AddReflectedWorkItemId(witdUpdateCommands);

            byte[] newDocHash = new byte[0];
            bool hashMatched = m_md5Utility.CompareDocHash(xmlDocument, m_witdDocMD5, ref newDocHash);
            if (!hashMatched)
            {
                UpdateSchemaNamespace(xmlDocument);

                XmlNode workItemTypes = xmlDocument.SelectSingleNode("WorkItemTypes");

                XmlNode workItemTypeDef = workItemTypes.FirstChild;
                while (null != workItemTypeDef)
                {
                    XmlDocument witdDoc = new XmlDocument();
                    XmlNode root = witdDoc.ImportNode(workItemTypeDef, true);
                    witdDoc.AppendChild(root);

                    IWITDSyncer witdSyncer = new TfsWITDSyncer(witdDoc, m_migrationSource.WorkItemStore);
                    witdUpdateCommands.Process(witdSyncer);
                    witdSyncer.Sync();

                    workItemTypeDef = workItemTypeDef.NextSibling;
                }

                m_md5Utility.UpdateDocHash(ref m_witdDocMD5, newDocHash);
            }
        }

        private void AddReflectedWorkItemId(WITDUpdateCommands witdUpdateCommands)
        {
            // <CustomSetting SettingKey="ContextSyncOp" SettingValue="Op1::InsertNode" />
            // <CustomSetting SettingKey="Op1::SearchPath" SettingValue="//FIELDS" />
            // <CustomSetting SettingKey="Op1::NewNodeContent" SettingValue="content_xml_goes_here" />
            // <CustomSetting SettingKey="Op1::DuplicateSearchPath" SettingValue="//FIELDS/FIELD[@refname='TfsMigrationTool.ReflectedWorkItemId']" />

            SyncUpdateCmdBase reflectedIdInsertCmd = new InsertNodeCmd();
            reflectedIdInsertCmd.AddParam(WITDUpdateCommands.ParamSearchPath, "//FIELDS");
            reflectedIdInsertCmd.AddParam(WITDUpdateCommands.ParamNewNodeContent, ReflectedIdFieldDef);
            reflectedIdInsertCmd.AddParam(WITDUpdateCommands.ParamDuplicateSearchPath, ReflectedIdSearchPath);
            witdUpdateCommands.AddCommand(ReflectedIdInsertCmdName, reflectedIdInsertCmd);
        }

        protected virtual void UpdateSchemaNamespace(XmlDocument xmlDocument)
        {
            return;
        }

        public ConversionResult ProcessChangeGroup(ChangeGroup changeGroup)
        {
            try
            {
                Guid targetSideSourceId = m_configurationService.SourceId;
                Guid sourceSideSourceId = m_configurationService.MigrationPeer;
                ConversionResult changeResult = new ConversionResult(sourceSideSourceId, targetSideSourceId);
                changeResult.ChangeId = string.Empty;

                if (TryProcessContextSyncChangeGroup(changeGroup))
                {
                    // by leaving the ChangeId as empty string, we instruct the platform 
                    // migration engine not to update the conversion history for this change group
                    return changeResult;
                }

                m_migrationSource.WorkItemStore.SubmitChanges(changeGroup, changeResult, sourceSideSourceId);
                if (!string.IsNullOrEmpty(changeResult.ChangeId))
                {
                    TraceManager.TraceInformation(string.Format(
                        "Completed migration, result change: {0}", changeResult.ChangeId));
                }

                return changeResult;
            }
            catch (Exception exception)
            {
                if (!(exception is MigrationUnresolvedConflictException))
                {
                    ErrorManager errMgr = m_migrationServiceContainer.GetService(typeof(ErrorManager)) as ErrorManager;
                    errMgr.TryHandleException(exception);
                }

                // by setting "ContinueProcessing" to FALSE, we instruct the platform migration engine to stop
                // processing the following change groups (migration instructions)
                var convRslt = new ConversionResult(m_configurationService.MigrationPeer, m_configurationService.SourceId);
                convRslt.ContinueProcessing = false;
                return convRslt;
            }
        }

        public void InitializeServices(IServiceContainer migrationServiceContainer)
        {
            m_migrationServiceContainer = migrationServiceContainer;

            m_changeGroupService = (ChangeGroupService)m_migrationServiceContainer.GetService(typeof(ChangeGroupService));
            Debug.Assert(m_changeGroupService != null, "Change group service is not initialized");

            m_changeGroupService.RegisterDefaultSourceSerializer(new MigrationItemSerializer<TfsWITMigrationItem>());
            m_changeGroupService.RegisterSourceSerializer(
                WellKnownChangeActionId.SyncContext, new MigrationItemSerializer<WorkItemContextSyncMigrationItem>());
            m_changeGroupService.RegisterSourceSerializer(
                WellKnownChangeActionId.AddAttachment, new MigrationItemSerializer<TfsMigrationFileAttachment>());
            m_changeGroupService.RegisterSourceSerializer(
                WellKnownChangeActionId.DelAttachment, new MigrationItemSerializer<TfsMigrationFileAttachment>());

            m_configurationService = (ConfigurationService)m_migrationServiceContainer.GetService(typeof(ConfigurationService));
            Debug.Assert(m_configurationService != null, "Configuration service is not initialized");
        }

        /// <summary>
        /// Perform the adapter-specific initialization
        /// </summary>
        public virtual void InitializeClient()
        {
            InitializeTfsClient();
        }

        /// <summary>
        /// Registers conflict types supported by the provider.
        /// </summary>
        /// <param name="conflictManager"></param>
        public virtual void RegisterConflictTypes(ConflictManager conflictManager)
        {
            if (null == conflictManager)
            {
                throw new ArgumentNullException("conflictManager");
            }
            m_conflictManagementService = conflictManager;

            m_conflictManagementService.RegisterConflictType(new InvalidFieldValueConflictType(), SyncOrchestrator.ConflictsSyncOrchOptions.Continue);
            m_conflictManagementService.RegisterConflictType(new WorkItemTypeNotExistConflictType(),
                                                             SyncOrchestrator.ConflictsSyncOrchOptions.StopConflictedSessionCurrentTrip);
            m_conflictManagementService.RegisterConflictType(new WITUnmappedWITConflictType(),
                                                          SyncOrchestrator.ConflictsSyncOrchOptions.StopConflictedSessionCurrentTrip);
            m_conflictManagementService.RegisterConflictType(new FileAttachmentOversizedConflictType(), SyncOrchestrator.ConflictsSyncOrchOptions.Continue);
            m_conflictManagementService.RegisterConflictType(new GenericConflictType());
            m_conflictManagementService.RegisterConflictType(new WitGeneralConflictType());
            m_conflictManagementService.RegisterConflictType(new InvalidFieldConflictType(), SyncOrchestrator.ConflictsSyncOrchOptions.Continue);
            m_conflictManagementService.RegisterConflictType(new ExcessivePathConflictType(), SyncOrchestrator.ConflictsSyncOrchOptions.Continue);
            m_conflictManagementService.RegisterConflictType(new WorkItemHistoryNotFoundConflictType(), SyncOrchestrator.ConflictsSyncOrchOptions.Continue);
            m_conflictManagementService.RegisterConflictType(new InvalidSubmissionConflictType(), SyncOrchestrator.ConflictsSyncOrchOptions.Continue);
        }

        #endregion

        #region IServiceProvider Members

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IMigrationProvider))
            {
                return this as IMigrationProvider;
            }
            else if (serviceType == typeof(ConflictManager))
            {
                return m_conflictManagementService;
            }
            else if (serviceType == typeof(ConfigurationService))
            {
                return m_configurationService;
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

        protected virtual TfsMigrationDataSource InitializeMigrationDataSource()
        {
            return new TfsMigrationDataSource();
        }

        private void InitializeTfsClient()
        {
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

            this.m_migrationSource = new TfsWITMigrationSource(
                migrationSourceConfiguration.InternalUniqueId,
                dataSourceConfig.CreateWorkItemStore());
            this.m_migrationSource.WorkItemStore.ServiceContainer = m_migrationServiceContainer;

            m_migrationSource.WorkItemStore.LocalWorkDir = GetLocalWorkDir();

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

            if (m_migrationSource.WorkItemStore.ByPassrules)
            {
                m_migrationSource.WorkItemStore.Core.CheckBypassRulePermission();
            }
        }

        private string GetLocalWorkDir()
        {
            return m_configurationService.WorkspaceRoot;
        }

        /// TODO: add conflict handling
        ChangeGroupService m_changeGroupService;
        ConfigurationService m_configurationService;
        ConflictManager m_conflictManagementService;
        ITranslationService m_translationService;

        IServiceContainer m_migrationServiceContainer;
        TfsWITMigrationSource m_migrationSource;
        string m_localWorkDir = string.Empty;

        // todo: change policy based on workflow!
        WitMetadataSyncPolicy m_witMetadataSyncPolicy = WitMetadataSyncPolicy.TakeTheirs;

        private readonly Md5HashUtility m_md5Utility = new Md5HashUtility();
        private byte[] m_globalListDocMD5 = new byte[0];
        private byte[] m_userAccountsDocMD5 = new byte[0];
        private byte[] m_witdDocMD5 = new byte[0];
        private byte[] m_areaPathDocMD5 = new byte[0];
        private byte[] m_iterationPathDocMD5 = new byte[0];

        private const string ReflectedIdInsertCmdName = "Microsoft.TeamFoundation.Migration.AddReflectedWorkItemId";
        private const string ReflectedIdFieldDef =
@"<FIELD type=""String"" 
         name=""Mirrored TFS ID"" 
         refname=""TfsMigrationTool.ReflectedWorkItemId"">
    <HELPTEXT>TFS ID from mirrored TFS server</HELPTEXT>
  </FIELD>";
        private const string ReflectedIdSearchPath = "//FIELDS/FIELD[@refname='TfsMigrationTool.ReflectedWorkItemId']";
    }
}
