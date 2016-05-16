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
    public class ClearCaseMigrationProvider : IMigrationProvider
    {
        ConflictManager m_conflictManagementService;
        IServiceContainer m_migrationServiceContainer;
        ChangeGroupService m_changeGroupService;
        ConfigurationService m_configurationService;
        EventService m_eventService;
        ICommentDecorationService m_commentDecorationService;
        HighWaterMark<long> m_changeGroupHighWaterMark;
        HighWaterMark<int> m_hwmLastSyncedTfsChangeset;

        ClearCaseServer m_clearCaseServer;

        CCConfiguration m_ccConfiguration;
        List<string> m_vobList = new List<string>();
        IMigrationItemSerializer m_alternativeSerializer;
        // When disableTargetAnalysis is set, the changes from source will be migrated to target even the target had changes. 
        bool m_overrideTargetChange = false;
        #region Interface method

        /// <summary>
        /// Constructor. 
        /// Initialize a clearCaseV6MigrationProvider with an alternative serializer.
        /// This is used for ClearTFSAdapter
        /// </summary>
        /// <param name="alternativeSerializer"></param>
        public ClearCaseMigrationProvider(IMigrationItemSerializer alternativeSerializer, string hwmName)
        {
            m_alternativeSerializer = alternativeSerializer;
            m_hwmLastSyncedTfsChangeset = new HighWaterMark<int>(hwmName);
        }

        public ClearCaseMigrationProvider()
        {
            m_alternativeSerializer = null;
            m_hwmLastSyncedTfsChangeset = null;
        }

        /// <summary>
        /// Perform the adapter-specific initialization
        /// </summary>
        public virtual void InitializeClient()
        {
            initializeConfiguration();
            initializeClearCaseServer();
            undoAllCheckouts();
        }

        /// <summary>
        /// Undo all checkouts on this clearcase server
        /// </summary>
        private void undoAllCheckouts()
        {
            foreach (MappingEntry mappingEntry in m_configurationService.Filters)
            {
                m_clearCaseServer.UndoCheckoutRecursive(mappingEntry.Path);
            }
        }

        private void initializeConfiguration()
        {
            m_ccConfiguration = CCConfiguration.GetInstance(m_configurationService.MigrationSource);
        }

        private void initializeClearCaseServer()
        {
            m_clearCaseServer = ClearCaseServer.GetInstance(m_ccConfiguration, m_ccConfiguration.GetViewName("Migration"));
            m_clearCaseServer.Initialize();
        }

        /// <summary>
        /// Initialize method. 
        /// </summary>
        public void InitializeServices(IServiceContainer migrationServiceContainer)
        {
            m_migrationServiceContainer = migrationServiceContainer;
            m_changeGroupService = (ChangeGroupService)m_migrationServiceContainer.GetService(typeof(ChangeGroupService));
            if (m_alternativeSerializer == null)
            {
                m_changeGroupService.RegisterDefaultSourceSerializer(new ClearCaseV6MigrationItemSerialzier());
            }
            else
            {
                m_changeGroupService.RegisterDefaultSourceSerializer(m_alternativeSerializer);
            }
            Debug.Assert(m_changeGroupService != null, "Change group service is not initialized");
            m_configurationService = (ConfigurationService)m_migrationServiceContainer.GetService(typeof(ConfigurationService));
            Debug.Assert(m_configurationService != null, "Configuration service is not initialized");
            m_eventService = (EventService)m_migrationServiceContainer.GetService(typeof(EventService));
            m_commentDecorationService = (ICommentDecorationService)m_migrationServiceContainer.GetService(typeof(ICommentDecorationService));
            Debug.Assert(m_commentDecorationService != null, "Comment decoration service is not initialized");
            m_changeGroupHighWaterMark = new HighWaterMark<long>("LastChangeGroupMigratedHighWaterMark");
            m_configurationService.RegisterHighWaterMarkWithSession(m_changeGroupHighWaterMark);
            if (m_hwmLastSyncedTfsChangeset != null)
            {
                m_configurationService.RegisterHighWaterMarkWithSession(m_hwmLastSyncedTfsChangeset);
            }

            foreach (BusinessModel.VC.Setting setting in m_configurationService.VcCustomSetting.Settings.Setting)
            {
                if (string.Equals(
                    setting.SettingKey, MigrationToolkitResources.VCSetting_DisableTargetAnalysis, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(setting.SettingValue, "True", StringComparison.OrdinalIgnoreCase))
                    {
                        m_overrideTargetChange = true;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Establish the context based on the context info from the side of the pipeline
        /// </summary>
        public void EstablishContext(ChangeGroupService sourceSystemChangeGroupService)
        {
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

            m_conflictManagementService.RegisterConflictType(new GenericConflictType());
            m_conflictManagementService.RegisterConflictType(new CCAttrTypeNotFoundConflictType());
            m_conflictManagementService.RegisterConflictType(new VCFilePropertyCreationConflictType());
            m_conflictManagementService.RegisterConflictType(new VCInvalidLabelNameConflictType());
            m_conflictManagementService.RegisterConflictType(new VCLabelCreationConflictType());
            m_conflictManagementService.RegisterConflictType(new VCContentConflictType());
            m_conflictManagementService.RegisterConflictType(new VCNameSpaceContentConflictType());
            m_conflictManagementService.RegisterConflictType(new VCInvalidPathConflictType());
            m_conflictManagementService.RegisterConflictType(new CCCheckinConflictType());
        }

        /// <summary>
        /// Process the change group. 
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public ConversionResult ProcessChangeGroup(ChangeGroup group)
        {
            ConversionResult rslt;
            CCBatchingContext ctx = new CCBatchingContext(m_clearCaseServer, GetChangeComment(group),
                group, m_ccConfiguration.DownloadFolder, m_conflictManagementService, m_ccConfiguration, m_overrideTargetChange);
            
            int processedActionCount = 0;
            m_changeGroupHighWaterMark.Reload();

            if (m_changeGroupHighWaterMark.Value == group.ChangeGroupId)
            {
                // Todo get the last action id from ClearCase history
            }

            try
            {
                foreach (MigrationAction action in group.Actions)
                {
                    if (processedActionCount > 50000)
                    {
                        TraceManager.TraceInformation("Processed 50,000 actions");
                        processedActionCount = 0;
                    }

                    processedActionCount++;

                    if (action.State != ActionState.Pending)
                    {
                        continue;
                    }

                    if (action.Action == WellKnownChangeActionId.Add)
                    {
                        if (string.Equals(action.ItemTypeReferenceName, WellKnownContentType.VersionControlLabel.ReferenceName, StringComparison.Ordinal))
                        {
                            ctx.CacheLabel(action);
                        }
                        else if (string.Equals(action.ItemTypeReferenceName, WellKnownContentType.VersionControlLabelItem.ReferenceName, StringComparison.Ordinal) ||
                                 string.Equals(action.ItemTypeReferenceName, WellKnownContentType.VersionControlRecursiveLabelItem.ReferenceName, StringComparison.Ordinal))
                        {
                            ctx.CacheLabelItem(action);
                        }
                        else
                        {
                            // Todo handle branch and undelete seperately.
                            if (ClearCasePath.IsVobRoot(ClearCasePath.GetFullPath(action.Path)))
                            {
                                TraceManager.TraceWarning("Skipped the change action that creates vob itself");
                                continue;
                            }
                            ctx.AddSingleItem(action, WellKnownChangeActionId.Add);
                        }
                    }
                    else if ((action.Action == WellKnownChangeActionId.Branch)
                         || (action.Action == WellKnownChangeActionId.BranchMerge)
                         || (action.Action == WellKnownChangeActionId.Undelete))
                    {
                        // Todo handle branch and undelete seperately.
                        if (ClearCasePath.IsVobRoot(ClearCasePath.GetFullPath(action.Path)))
                        {
                            TraceManager.TraceWarning("Skipped the change action that creates vob itself");
                            continue;
                        }
                        ctx.AddSingleItem(action, WellKnownChangeActionId.Add);
                    }
                    else if (action.Action == WellKnownChangeActionId.Edit)
                    {
                        ctx.AddSingleItem(action, WellKnownChangeActionId.Edit);
                    }
                    else if (action.Action == WellKnownChangeActionId.Delete)
                    {
                        ctx.AddSingleItem(action, WellKnownChangeActionId.Delete);
                    }
                    else if (action.Action == WellKnownChangeActionId.Rename)
                    {
                        if (ClearCasePath.Equals(action.Path, action.FromPath))
                        {
                            // Skip case-only rename.
                            continue;
                        }
                        ctx.AddSingleItem(action, WellKnownChangeActionId.Rename);
                    }
                    else if (action.Action == WellKnownChangeActionId.Merge)
                    {
                        continue;
                    }
                    else if (action.Action == WellKnownChangeActionId.AddFileProperties)
                    {
                        ctx.AddSingleItem(action, WellKnownChangeActionId.AddFileProperties);
                    }
                }

                rslt = new ConversionResult(m_configurationService.SourceId, m_configurationService.MigrationPeer);
                rslt.ChangeId = m_clearCaseServer.GetRelativePathFromVobAbsolutePath(ctx.Flush());
                rslt.ItemConversionHistory.Add(new ItemConversionHistory(group.Name, string.Empty, rslt.ChangeId, string.Empty));
                rslt.ContinueProcessing = true;
                m_changeGroupHighWaterMark.Update(group.ChangeGroupId);
                if (m_hwmLastSyncedTfsChangeset != null)
                {
                    m_hwmLastSyncedTfsChangeset.Update(int.Parse(group.Name));
                }
            }
            catch (Exception e)
            {
                // Undo any pending checkouts that would have been checked in by the call to ctx.Flush() if no exception occurred.
                // (If ctx.Flush() has already been called, this will just return.)
                // It will catch and log any exception as an error, but will not throw it so that the original exception is raised as the conflict.
                ctx.CancelCachedCheckouts();

                if (!(e is MigrationUnresolvedConflictException))
                {
                    TraceManager.TraceInformation("Raising generic conflict for exception: {0}", e.Message);
                    createGenericConflict(e, group);
                }

                rslt = new ConversionResult(Guid.Empty, group.SourceId);
                rslt.ContinueProcessing = false;
            }

            return rslt;
        }

        internal string GetChangeComment(ChangeGroup group)
        {
            string migrationComment = m_commentDecorationService.GetChangeGroupCommentSuffix(group.Name);

            // ToDo, embed conflict resolution into comment
            /*
            string resolutionDesc = string.Empty;             
            StringBuilder resolutionDescSB = new StringBuilder();
            resolutionDescSB.Append("Applied conflict resolution rules: ");
            string resolutionDescFormat = "{0}; ";
            List<ConflictResolutionRule> rulesAppliedOnGroup;
            if (m_conflictManagementService.TryGetResolutionRulesAppliedToGroup(group, out rulesAppliedOnGroup))
            {
                foreach (ConflictResolutionRule rule in rulesAppliedOnGroup)
                {
                    resolutionDescSB.AppendFormat(resolutionDescFormat, rule.RuleReferenceName);
                }
                resolutionDesc = resolutionDescSB.ToString();
            }
            migrationComment = m_commentDecorationService.AddToChangeGroupCommentSuffix(migrationComment, resolutionDesc);             
             * */

            return group.Comment + " " + migrationComment;
        }

        /// <summary>
        /// Create a generic conflict.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="group"></param>
        /// <returns></returns>
        private ConflictResolutionResult createGenericConflict(Exception e, ChangeGroup group)
        {
            Debug.Assert(e != null, "Exception e for creating generic conflict is NULL");
            Debug.Assert(group != null, "Conflicted change group is NULL");
            Debug.Assert(m_conflictManagementService != null, "ConflictManager is not properly initialized");

            MigrationConflict genericConflict = GenericConflictType.CreateConflict(e);

            List<MigrationAction> retActions;
            ConflictResolutionResult resolutionResult =
                m_conflictManagementService.TryResolveNewConflict(group.SourceId, genericConflict, out retActions);

            return resolutionResult;
        }
        #endregion


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