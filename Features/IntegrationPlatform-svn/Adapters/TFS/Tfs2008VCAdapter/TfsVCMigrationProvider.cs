// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon;

namespace Microsoft.TeamFoundation.Migration.Tfs2008VCAdapter
{
    public class TfsVCMigrationProvider : IMigrationProvider
    {
        IServiceContainer m_migrationServiceContainer;
        ChangeGroupService m_changeGroupService;
        ConfigurationService m_configurationService;
        EventService m_eventService;
        ICommentDecorationService m_commentDecorationService;
        HighWaterMark<int> m_lastHighWaterMark;
        HighWaterMark<int> m_deltaHighWaterMark;

        ConflictManager m_conflictManagementService;
        VersionControlServer m_tfsClient;
        VersionControlServer m_sourceTfsClient = null;
        bool m_sourceSideIsTfs = true;
        Workspace m_workspace;
        int m_retryLimit;
        int m_secondsToWait;
        int m_localWorkSpaceVersion = 0;


        /// <summary>
        /// Initialize TfsVCAdapter 
        /// </summary>
        public void InitializeServices(IServiceContainer migrationServiceContainer)
        {
            m_migrationServiceContainer = migrationServiceContainer;
            m_changeGroupService = (ChangeGroupService)m_migrationServiceContainer.GetService(typeof(ChangeGroupService));
            m_changeGroupService.RegisterDefaultSourceSerializer(new TfsMigrationItemSerialzier());
            Debug.Assert(m_changeGroupService != null, "Change group service is not initialized");
            m_configurationService = (ConfigurationService)m_migrationServiceContainer.GetService(typeof(ConfigurationService));
            Debug.Assert(m_configurationService != null, "Configuration service is not initialized");
            m_eventService = (EventService)m_migrationServiceContainer.GetService(typeof(EventService));
            //Debug.Assert(m_eventService != null, "Event service is not initialized");
            m_commentDecorationService = (ICommentDecorationService)m_migrationServiceContainer.GetService(typeof(ICommentDecorationService));
            Debug.Assert(m_commentDecorationService != null, "Comment decoration service is not initialized");
            m_lastHighWaterMark = new HighWaterMark<int>(Constants.HwmMigrated);
            m_deltaHighWaterMark = new HighWaterMark<int>(Constants.HwmDelta);
            m_configurationService.RegisterHighWaterMarkWithSession(m_lastHighWaterMark);
            m_configurationService.RegisterHighWaterMarkWithSession(m_deltaHighWaterMark);
            //m_hwmDelta = m_configurationService.CreateHighWaterMark(Constants.HwmDelta);            
        }

        /// <summary>
        /// Perform the adapter-specific initialization
        /// </summary>
        public virtual void InitializeClient()
        {
            initializeTfsClient();
            initializeConfiguration();
        }

        private void initializeConfiguration()
        {
            m_retryLimit = m_configurationService.GetValue<int>("RetryLimit", 10);
            m_secondsToWait = m_configurationService.GetValue<int>("RetryDelaySeconds", 30);
            m_workspace = CreateWorkspace();
        }

        private void initializeTfsClient()
        {
            TeamFoundationServer tfsServer = TeamFoundationServerFactory.GetServer(
                m_configurationService.ServerUrl);
            m_tfsClient = (VersionControlServer)tfsServer.GetService(typeof(VersionControlServer));
            m_tfsClient.NonFatalError += new ExceptionEventHandler(NonFatalError);
            m_migrationServiceContainer.AddService(typeof(VersionControlServer), m_tfsClient);
        }

        private void NonFatalError(object sender, ExceptionEventArgs e)
        {
            if (e.Exception != null)
            {
                processNonFatalErrorException(e.Exception);
            }

            if (e.Failure != null)
            {
                Trace.TraceWarning(e.Failure.Message);
            }
        }

        /// <summary>
        /// Process the exceptions in returned from nonfatalerror
        /// </summary>
        /// <param name="exception"></param>
        private void processNonFatalErrorException(Exception exception)
        {
            if (exception.Message.EndsWith(TfsVCAdapterResource.CantDeleteNonEmptyDirPath, false, TfsVCAdapterResource.Culture))
            {
                Trace.TraceWarning(string.Format(
                    TfsVCAdapterResource.Culture,
                    TfsVCAdapterResource.DeleteNonEmptyDirPathManually,
                    exception.Message));
                // Try delete the folder manually.
                TfsUtil.DeleteFile(exception.Message.Substring(
                    0, exception.Message.Length - TfsVCAdapterResource.CantDeleteNonEmptyDirPath.Length));
                return;
            }
            throw exception;
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

            m_conflictManagementService.RegisterConflictType(new TFSZeroCheckinConflictType());
            m_conflictManagementService.RegisterConflictType(new TFSDosShortNameConflictType());
            m_conflictManagementService.RegisterConflictType(new TFSHistoryNotFoundConflictType());
            m_conflictManagementService.RegisterConflictType(new TfsItemNotFoundConflictType());
            m_conflictManagementService.RegisterConflictType(new VCMissingItemConflictType());
            m_conflictManagementService.RegisterConflictType(new GenericConflictType());
            m_conflictManagementService.RegisterConflictType(new VCContentConflictType());
            m_conflictManagementService.RegisterConflictType(new VCNameSpaceContentConflictType());
            m_conflictManagementService.RegisterConflictType(new TfsCheckinConflictType());
            m_conflictManagementService.RegisterConflictType(new VCInvalidLabelNameConflictType());
            m_conflictManagementService.RegisterConflictType(new VCLabelAlreadyExistsConflictType());
        }

        /// <summary>
        /// Check wether the specified path is a valid Tfs path.
        /// Returns true if the path is valid, false if the path in invalid but can be resolved as skip, throw otherwise.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="action"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool verifyPath(ChangeGroup group, MigrationAction action, string path)
        {
            try
            {
                VersionControlPath.GetFullPath(path, true);
            }
            catch (Exception e)
            {
                MigrationConflict invalidPathConflict = VCInvalidPathConflictType.CreateConflict(action, e.Message, path);

                List<MigrationAction> returnActions;
                ConflictResolutionResult resolutionResult = m_conflictManagementService.TryResolveNewConflict(
                    group.SourceId, invalidPathConflict, out returnActions);
                if (resolutionResult.Resolved)
                {
                    switch (resolutionResult.ResolutionType)
                    {
                        case ConflictResolutionType.SkipConflictedChangeAction:
                            return false;
                        default:
                            Debug.Fail("Unknown resolution result");
                            throw new MigrationUnresolvedConflictException(invalidPathConflict);
                    }
                }
                else
                {
                    throw new MigrationUnresolvedConflictException(invalidPathConflict);
                }
            }
            return true;
        }

        public ConversionResult ProcessChangeGroup(ChangeGroup group)
        {
            ConversionResult rslt;

            try
            {
                TfsUtil.CleanWorkspace(Workspace);
                m_deltaHighWaterMark.Reload();
                if (m_localWorkSpaceVersion < m_deltaHighWaterMark.Value)
                {
                    m_localWorkSpaceVersion = m_deltaHighWaterMark.Value;
                }

                if (m_localWorkSpaceVersion == 0)
                {
                    m_localWorkSpaceVersion = m_tfsClient.GetLatestChangesetId();
                }

                BatchingContext ctx = new BatchingContext(Workspace, m_configurationService, m_conflictManagementService, m_localWorkSpaceVersion);

                ctx.BatchedItemError += SingleItemError;
                ctx.BatchedItemWarning += SingleItemWarning;
                ctx.MergeError += MergeError;

                int processedActionCount = 0;
                HashSet<string> skippedActions = new HashSet<string>();

                foreach (MigrationAction action in group.Actions)
                {
                    if (processedActionCount > 50000)
                    {
                        TraceManager.TraceInformation("Processed 50,000 actions");
                        processedActionCount = 0;
                    }

                    processedActionCount++;

                    if (m_sourceSideIsTfs && (m_sourceTfsClient == null) && (action.SourceItem != null))
                    {
                        TfsMigrationItem tfsMigrationItem = action.SourceItem as TfsMigrationItem;
                        if (tfsMigrationItem != null)
                        {
                            m_sourceTfsClient = tfsMigrationItem.Server;
                        }
                        else
                        {
                            m_sourceSideIsTfs = false;
                        }
                    }

                    if (action.State != ActionState.Pending)
                    {
                        if (!skippedActions.Contains(action.Path))
                        {
                            skippedActions.Add(action.Path);
                        }
                        continue;
                    }

                    // Verify whether the action.Path and action.FromPath is a valid Tfs path.
                    if (!verifyPath(group, action, action.Path))
                    {
                        continue;
                    }


                    if (!string.IsNullOrEmpty(action.FromPath) && !verifyPath(group, action, action.FromPath))
                    {
                        continue;
                    }

                    if (action.Action == WellKnownChangeActionId.Add)
                    {
                        if (string.Equals(action.ItemTypeReferenceName, WellKnownContentType.VersionControlLabel.ReferenceName, StringComparison.Ordinal))
                        {
                            ctx.CacheLabel(action);
                        }    
                        else if(string.Equals(action.ItemTypeReferenceName, WellKnownContentType.VersionControlLabelItem.ReferenceName, StringComparison.Ordinal) ||
                                string.Equals(action.ItemTypeReferenceName, WellKnownContentType.VersionControlRecursiveLabelItem.ReferenceName, StringComparison.Ordinal))
                        {
                            action.Recursive = string.Equals(action.ItemTypeReferenceName, WellKnownContentType.VersionControlRecursiveLabelItem.ReferenceName, StringComparison.Ordinal);
                            ctx.CacheLabelItem(action);
                        }
                        else
                        {
                            if (VersionControlPath.GetFolderDepth(VersionControlPath.GetFullPath(action.Path)) == 1)
                            {
                                TraceManager.TraceWarning("Skipped the change action that creates team project");
                                continue;
                            }
                            Add(action, ctx);
                        }
                    }
                    else if (action.Action == WellKnownChangeActionId.Branch)
                    {
                        Branch(action, ctx);
                    }
                    else if (action.Action == WellKnownChangeActionId.Delete)
                    {
                        Delete(action, ctx);
                    }
                    else if (action.Action == WellKnownChangeActionId.Edit)
                    {
                        Edit(action, ctx);
                    }
                    else if (action.Action == WellKnownChangeActionId.Encoding)
                    {
                        Encoding(action, ctx);
                    }
                    else if (action.Action == WellKnownChangeActionId.Merge)
                    {
                        Merge(action, ctx);
                    }
                    else if (action.Action == WellKnownChangeActionId.BranchMerge)
                    {
                        BranchMerge(action, ctx);
                    }
                    else if (action.Action == WellKnownChangeActionId.Rename)
                    {
                        Rename(action, ctx);
                    }
                    else if (action.Action == WellKnownChangeActionId.Undelete)
                    {
                        Undelete(action, ctx);
                    }
                    else if (action.Action == WellKnownChangeActionId.Label)
                    {
                        Label(action, ctx);
                    }
                    else
                    {
                        Unknown(action, ctx);
                    }
                }

                Guid targetSideSourceId = m_configurationService.SourceId;
                Guid sourceSideSourceId = m_configurationService.MigrationPeer;
                rslt = new ConversionResult(sourceSideSourceId, targetSideSourceId);
                rslt.ChangeId = null;

                Flush(ctx);
                int changesetId;

                if (Checkin(group, ctx.ImplicitRenames, ctx.ImplicitAdds, skippedActions, out changesetId))
                {
                    if (changesetId == 0)
                    {
                        changesetId = m_tfsClient.GetLatestChangesetId();
                        MigrationConflict zeroCheckinConflict = TFSZeroCheckinConflictType.CreateConflict(group.Name);

                        List<MigrationAction> retActions;
                        ConflictResolutionResult resolutionResult =
                            m_conflictManagementService.TryResolveNewConflict(group.SourceId, zeroCheckinConflict, out retActions);
                        if (resolutionResult.Resolved)
                        {
                            // The changeset was skipped dring checkin on the target system. We will update the conversion history with the latest changeset number on the target system.
                            rslt.ContinueProcessing = true;
                            rslt.ChangeId = changesetId.ToString(CultureInfo.InvariantCulture);
                            rslt.ItemConversionHistory.Add(new ItemConversionHistory(group.Name, string.Empty, rslt.ChangeId, string.Empty));

                        }
                        else
                        {
                            rslt.ContinueProcessing = false;
                        }
                    }
                    else
                    {
                        if (changesetId == int.MinValue)
                        {
                            changesetId = m_tfsClient.GetLatestChangesetId();
                        }
                        m_localWorkSpaceVersion = changesetId;
                        rslt.ChangeId = changesetId.ToString(CultureInfo.InvariantCulture);
                        rslt.ItemConversionHistory.Add(new ItemConversionHistory(group.Name, string.Empty, rslt.ChangeId, string.Empty));
                        m_lastHighWaterMark.Update(changesetId);
                    }
                    if (rslt.ContinueProcessing)
                    {
                        ctx.CreateLabels();
                    }
                }
                else
                {
                    throw new MigrationException(TfsVCAdapterResource.CheckinFailedForUnknownReason);
                }
            }
            catch (MigrationUnresolvedConflictException)
            {
                rslt = new ConversionResult(Guid.Empty, group.SourceId);
                rslt.ContinueProcessing = false;
            }
            catch (Exception e)
            {
                TraceManager.TraceException(e);

                if ((e is VersionControlException) && (e.InnerException.Message.StartsWith("TF203013", StringComparison.InvariantCultureIgnoreCase)))
                {
                    Debug.Assert(m_conflictManagementService != null, "ConflictManager is not properly initialized");

                    MigrationConflict dosShortNameConflict = TFSDosShortNameConflictType.CreateConflict(e.InnerException.Message, group.Name);
                    List<MigrationAction> retActions;
                    m_conflictManagementService.TryResolveNewConflict(group.SourceId,
                        dosShortNameConflict,
                        out retActions);
                }
                else
                {
                    var errMgr = m_migrationServiceContainer.GetService(typeof(ErrorManager)) as ErrorManager;
                    Debug.Assert(errMgr != null, "Error Manager is not properly initialized");
                    errMgr.TryHandleException(e, m_conflictManagementService);
                }

                rslt = new ConversionResult(Guid.Empty, group.SourceId);
                rslt.ContinueProcessing = false;
            }

            return rslt;
        }
        
        protected virtual void Flush(BatchingContext context)
        {
            context.Flush();
        }

        protected virtual string GetChangeComment(ChangeGroup group)
        {
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
            string migrationComment = m_commentDecorationService.GetChangeGroupCommentSuffix(group.Name);
            if (!string.IsNullOrEmpty(resolutionDesc))
            {
                m_commentDecorationService.AddToChangeGroupCommentSuffix(migrationComment, resolutionDesc);
            }
            return group.Comment + " " + migrationComment;
        }

        protected virtual string GetChangeOwner(ChangeGroup group)
        {
            return group.Owner;
        }

        private bool codeReview(PendingChange[] pendChanges, int changeset, HashSet<string> implicitRenames, HashSet<string> implicitAdds, 
            HashSet<string> skippedActions, bool autoResolve)
        {
            VCTranslationService translationService = (VCTranslationService)((m_migrationServiceContainer.GetService(typeof(ITranslationService)) as ITranslationService));
            Guid otherSidesourceId = new Guid(m_configurationService.PeerMigrationSource.InternalUniqueId);

            Dictionary<string, Change> sourceChanges = new Dictionary<string, Change>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, Change> localDiffResults = new Dictionary<string, Change>();

            foreach (Change sourceChange in m_sourceTfsClient.GetChangeset(changeset).Changes)
            {
                string mappedPath =  translationService.GetMappedPath(sourceChange.Item.ServerItem, otherSidesourceId);
                if (!string.IsNullOrEmpty(mappedPath))
                {
                    if (sourceChanges.ContainsKey(mappedPath))
                    {
                        if (((sourceChange.ChangeType & ChangeType.Rename) == ChangeType.Rename) && (sourceChange.Item.DeletionId != 0))
                        {
                            // do nothing
                        }
                        else
                        {
                            sourceChanges[mappedPath] = sourceChange;
                        }
                    }
                    else
                    {
                        sourceChanges[mappedPath] = sourceChange;
                    }
                }
            }

            List<string> pendedAdds = new List<string>();
            foreach (PendingChange pendingChange in pendChanges)
            {
                bool changeTypeMatch = false;
                if ((pendingChange.ChangeType & ChangeType.Add) == ChangeType.Add)
                {
                    pendedAdds.Add(pendingChange.ServerItem);
                }
                if (sourceChanges.ContainsKey(pendingChange.ServerItem))
                {
                    ChangeType sourceChangeType = sourceChanges[pendingChange.ServerItem].ChangeType & ~ChangeType.Encoding & ~ChangeType.Lock;
                    if (implicitRenames.Contains(pendingChange.ServerItem))
                    {
                        if (sourceChangeType == ChangeType.Rename)
                        {
                            sourceChanges.Remove(pendingChange.ServerItem);
                            continue;
                        }
                        else
                        {
                            sourceChangeType = sourceChangeType & ~ChangeType.Rename;
                        }
                    }
                    ChangeType targetChangeType = pendingChange.ChangeType & ~ChangeType.Encoding & ~ChangeType.Lock;

                    if (sourceChangeType == targetChangeType)
                    {
                        changeTypeMatch = true;
                    }
                    else if (sourceChangeType == (targetChangeType | ChangeType.Merge))
                    {
                        // Merge from item or version not migrated.
                        changeTypeMatch = true; 
                    }
                    else
                    {
                        switch (sourceChangeType)
                        {
                            case ChangeType.Branch:
                                changeTypeMatch = (targetChangeType == (ChangeType.Branch | ChangeType.Merge) // We always pend merge for branch for performance improvement.
                                                || targetChangeType == ChangeType.Add // Branch may have been changed to Add if the branch source is not migrated
                                                || targetChangeType == (ChangeType.Add | ChangeType.Edit)); // Branch|Edit may becomes Add|Edit 
                                break;
                            case ChangeType.Branch | ChangeType.Merge:
                                changeTypeMatch = (targetChangeType == ChangeType.Add // Branch|Merge may be changed to Add if branch source is not migrated. 
                                                || targetChangeType == (ChangeType.Add | ChangeType.Edit)); // Same as Branch|Merge -> Add
                                break;
                            case ChangeType.Branch | ChangeType.Edit:
                                changeTypeMatch = (targetChangeType == ChangeType.Add // Branch may be changed to Add if branch source is not migrated. 
                                                || targetChangeType == (ChangeType.Add | ChangeType.Edit)) // Same as Branch -> Add
                                                || (targetChangeType == (ChangeType.Edit | ChangeType.Branch | ChangeType.Merge)); // We map Branch to Branch|Merge
                                break;

                            case ChangeType.Branch | ChangeType.Edit | ChangeType.Merge:
                                changeTypeMatch = (targetChangeType == ChangeType.Add // Branch|Merge|Edit may be changed to Add if branch source is not migrated. 
                                                || targetChangeType == (ChangeType.Add | ChangeType.Edit)); // Same as Branch|Merge|Edit -> Add
                                break;
                            case ChangeType.Delete | ChangeType.Merge:
                                changeTypeMatch = (targetChangeType == ChangeType.Delete); // We map Merge|Delete to Delete
                                break;
                            case ChangeType.Delete | ChangeType.Rename:
                                changeTypeMatch = (targetChangeType == ChangeType.Delete); // The rename is implicit as it is operated on the parent.
                                break;
                            case ChangeType.Delete | ChangeType.Rename |ChangeType.Undelete:
                                changeTypeMatch = (targetChangeType == ChangeType.Delete); // The rename is implicit as it is operated on the parent.
                                break;
                            case ChangeType.Delete | ChangeType.Merge | ChangeType.Rename: 
                                changeTypeMatch = (targetChangeType == ChangeType.Delete); // The combination of Merge|Delete and Rename|Delete
                                break;
                            case ChangeType.Delete | ChangeType.Merge | ChangeType.Undelete:
                            case ChangeType.Delete | ChangeType.Undelete:
                                changeTypeMatch = (targetChangeType == ChangeType.Delete); // TFS2008 only, recursive undelete won't show up on sub items.
                                break;
                            case ChangeType.Edit:
                                changeTypeMatch = (targetChangeType == (ChangeType.Add | ChangeType.Edit)); // Change to Add on missing for Edit
                                break;
                            case ChangeType.Edit | ChangeType.Merge:
                                changeTypeMatch = (targetChangeType == ChangeType.Edit); // Merge from item or version is not migrated
                                break;
                            case ChangeType.Edit | ChangeType.Merge | ChangeType.Rename:
                                changeTypeMatch = (targetChangeType == (ChangeType.Edit | ChangeType.Rename) // Merge from item or version is not migrated
                                                 | (targetChangeType == ChangeType.Edit)); // Merge from item or version is not migrated and rename is implicit from parent.
                                break;
                            case ChangeType.Edit | ChangeType.Merge | ChangeType.Undelete:
                                changeTypeMatch = (targetChangeType == (ChangeType.Add | ChangeType.Edit) // Undelete is from a version not migrated
                                                || targetChangeType == (ChangeType.Edit | ChangeType.Undelete) // Undelete is from a version not migrated
                                                || targetChangeType == (ChangeType.Branch | ChangeType.Merge) // Undelete is from a version not migrated
                                                || targetChangeType == (ChangeType.Branch | ChangeType.Merge | ChangeType.Edit)); // Undelete is from a version not migrated
                                break;
                            case ChangeType.Edit | ChangeType.Merge | ChangeType.Rename | ChangeType.Undelete: // Undelete is from a not migrated version
                                changeTypeMatch = (targetChangeType == (ChangeType.Add | ChangeType.Edit)
                                                || targetChangeType == ChangeType.Add);
                                                break;
                            case ChangeType.Edit | ChangeType.Undelete: // Undelete is from a version not migrated
                                                changeTypeMatch = (targetChangeType == (ChangeType.Add | ChangeType.Edit)
                                                                || (targetChangeType == ChangeType.Add)
                                                                || targetChangeType == ChangeType.Edit);
                                                break;
                            case ChangeType.Merge | ChangeType.Undelete:
                                changeTypeMatch = (targetChangeType == ChangeType.Undelete // Undelete is from a version migrated, but merge is from a verion not migrated
                                                || targetChangeType == ChangeType.Add // Both Merge and Undelete is from a version not migrated
                                                || targetChangeType == (ChangeType.Branch | ChangeType.Merge) // Undelete is from a version not migrated, and Merge is valid.
                                                || targetChangeType == (ChangeType.Add | ChangeType.Edit)); // Both Merge and Undelete is from a version not migrated
                                break;
                            case ChangeType.Merge | ChangeType.Rename:
                                changeTypeMatch = (targetChangeType == ChangeType.Rename); // Merge from item or version not migrated. 
                                break;
                            case ChangeType.Merge | ChangeType.Rename | ChangeType.Undelete:
                                changeTypeMatch = (targetChangeType == ChangeType.Add 
                                                || targetChangeType == (ChangeType.Add | ChangeType.Edit)); // Merge|Undelete from item or version not migrated.
                                break;

                            case ChangeType.Rename | ChangeType.Undelete:
                                changeTypeMatch = (targetChangeType == ChangeType.Undelete // Rename is implicit on the parent item. 
                                                || targetChangeType == ChangeType.Rename); // Undelete is an implicit undelete on the child item. 
                                break;
                            case ChangeType.Rename:
                                changeTypeMatch = (targetChangeType == ChangeType.Add) // Rename is from a source not mapped.
                                                || targetChangeType == (ChangeType.Add | ChangeType.Edit); // Rename is from a source not mapped. 
                                break;
                            case ChangeType.Rename | ChangeType.Edit:
                                changeTypeMatch = (targetChangeType == (ChangeType.Add | ChangeType.Edit)); // Rename is from a source not mapped. 
                                break;
                            case ChangeType.Undelete:
                                changeTypeMatch = (targetChangeType == ChangeType.Add // Undelete from before the snapshot
                                                || targetChangeType == (ChangeType.Add | ChangeType.Edit)); // Undelete from before the snapshot
                                break;
                            default:
                                break;
                        }
                    }
                    if (!changeTypeMatch)
                    {
                        localDiffResults.Add(pendingChange.ServerItem, sourceChanges[pendingChange.ServerItem]);
                    }
                    sourceChanges.Remove(pendingChange.ServerItem);
                }
                else
                {
                    if (pendingChange.ChangeType != ChangeType.Delete) // Target only delete. The deleted has an implicit rename from one of its parent.
                    {
                        localDiffResults.Add(pendingChange.ServerItem, null);
                    }
                }
            }

            List<ItemSpec> verifyDeletedItems = new List<ItemSpec>();

            foreach (KeyValuePair<string, Change> remaingChange in sourceChanges)
            {
                if (skippedActions.Contains(remaingChange.Key))
                {
                    continue;
                }
                if ((remaingChange.Value.ChangeType & ~ChangeType.Encoding & ~ChangeType.Lock) == ChangeType.Merge)
                {
                    continue;
                }
                else if (remaingChange.Value.ChangeType == ChangeType.Add || remaingChange.Value.ChangeType == ChangeType.Edit
                    || remaingChange.Value.ChangeType == (ChangeType.Add | ChangeType.Edit))
                {
                    // We don't check for missing changes of pure Add , Edit or Add|Edit. They are caused by contentconflict resolution 
                    continue;
                }
                else if ((remaingChange.Value.ChangeType & (ChangeType.Delete | ChangeType.Undelete)) == (ChangeType.Delete | ChangeType.Undelete))
                {
                    continue; //DeleteUndelete is ignored on target system.
                }
                else if (((remaingChange.Value.ChangeType & ChangeType.Delete) == ChangeType.Delete)
                    || ((remaingChange.Value.ChangeType & ChangeType.Merge) == ChangeType.Merge)
                    || ((remaingChange.Value.ChangeType & ChangeType.Rename) == ChangeType.Rename))
                {
                    if (implicitRenames.Contains(remaingChange.Key))
                    {
                        // Skip the implicit rename
                        continue;
                    }
                    verifyDeletedItems.Add(new ItemSpec(remaingChange.Key, RecursionType.None));
                }
                else if ((((remaingChange.Value.ChangeType & ChangeType.Add) == ChangeType.Add)
                             || ((remaingChange.Value.ChangeType & ChangeType.Undelete) == ChangeType.Undelete))
                         && implicitAdds.Contains(remaingChange.Key))
                {
                    // This is an implicit add created by rename a folder to below itself. 
                    // Or a Sub item undelete created by parent item undelete on TFS2008. 
                    continue;
                }
                else if (((remaingChange.Value.ChangeType & ~ChangeType.Encoding & ~ChangeType.Lock  & ~ChangeType.Merge) == ChangeType.Rename)
                    && implicitRenames.Contains(remaingChange.Key))
                {
                    // Skip the implicit rename.
                    continue;
                }
                else
                {
                    bool childAddPended = false;
                    if ((remaingChange.Value.ChangeType & ChangeType.Add) == ChangeType.Add)
                    {
                        // For the case of adding filter path itself, we don't generate a code review error.
                        if (isMappingRoot(remaingChange.Key))
                        {
                            continue;
                        }

                        // For the case of parent add where the child add is in the same changeset, we don't generate a code review error. 
                        foreach (string pendedAdd in pendedAdds)
                        {
                            if (VersionControlPath.IsSubItem(pendedAdd, remaingChange.Key))
                            {
                                childAddPended = true;
                                break;
                            }
                        }
                    }
                    if (childAddPended)
                    {
                        continue;
                    }
                    localDiffResults.Add(remaingChange.Key, remaingChange.Value);
                }
            }

            if (verifyDeletedItems.Count > 0)
            {
                int batchGetSize = 10000; // The max get size enforced on the target Tfs server.
                ItemSpec[] getBatch;
                int index = 0;
                int thisBatchSize = 0;
                while (index < verifyDeletedItems.Count)
                {
                    if (index + batchGetSize < verifyDeletedItems.Count)
                    {
                        thisBatchSize = batchGetSize;
                    }
                    else
                    {
                        thisBatchSize = verifyDeletedItems.Count - index;
                    }
                    getBatch = new ItemSpec[thisBatchSize];
                    verifyDeletedItems.CopyTo(index, getBatch, 0, thisBatchSize);
                    ItemSet[] itemSets = m_sourceTfsClient.GetItems(getBatch, new ChangesetVersionSpec(changeset), DeletedState.NonDeleted, ItemType.Any);
                    foreach (ItemSet itemSet in itemSets)
                    {
                        if (itemSet.Items.Length > 0)
                        {
                            localDiffResults.Add( itemSet.Items[0].ServerItem, sourceChanges[itemSet.Items[0].ServerItem]);
                        }
                    }
                    index = index + thisBatchSize;
                }
            }
 
            if (localDiffResults.Count > 0)
            {
                if (autoResolve)
                {
                    return autoResolveCheckinConflicts(localDiffResults, changeset);
                }
                else
                {
                    using (StreamWriter writer = new StreamWriter(string.Format("{0}.txt", changeset)))
                    {
                        foreach (string errorItem in localDiffResults.Keys)
                        {
                            writer.WriteLine(errorItem);
                        }
                    }
                    return false;
                }
            }

            return true;
        }


        private bool isMappingRoot(string path)
        {
            foreach (MappingEntry mappingEntry in m_configurationService.Filters)
            {
                if (VersionControlPath.Equals(mappingEntry.Path, mappingEntry.Path))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Auto resolve checkin conflicts
        /// </summary>
        /// <param name="sourceChanges"></param>
        /// <param name="changesetId"></param>
        /// <returns></returns>
        private bool autoResolveCheckinConflicts(Dictionary<string, Change> sourceChanges, int changesetId)
        {
            VCTranslationService translationService = (VCTranslationService)((m_migrationServiceContainer.GetService(typeof(ITranslationService)) as ITranslationService));
            Guid otherSidesourceId = new Guid(m_configurationService.PeerMigrationSource.InternalUniqueId);

            List<string> itemsToBeDeleted = new List<string>();

            // We wrap each auto-resolve operation in a try-catch to fix as many code review failures as possible.
            foreach (string pendedItem in sourceChanges.Keys)
            {
                try
                {
                    Workspace.Undo(pendedItem);
                }
                catch (Exception ex)
                {
                    TraceManager.TraceError(
                        string.Format("Unable to undo change for item {0}. Error - {1}", pendedItem, ex.Message));
                }
            }

            foreach (KeyValuePair<string, Change> sourceChange in sourceChanges)
            {
                try
                {
                    if (sourceChange.Value == null)
                    {
                        // Changes only exist on Target system. Undo the local change. 
                        continue;
                    }

                    string mappedLocalPath = Workspace.GetLocalItemForServerItem(sourceChange.Key);

                    if ((sourceChange.Value.ChangeType & ChangeType.Delete) == ChangeType.Delete)
                    {
                        itemsToBeDeleted.Add(mappedLocalPath);
                    }
                    else
                    {
                        if ((sourceChange.Value.ChangeType & ChangeType.Rename) == ChangeType.Rename)
                        {
                            Item previous = null;
                            string sourceRenameFromPath = translationService.GetMappedPath(sourceChange.Key, m_configurationService.SourceId);
                            try
                            {
                                foreach (Changeset previousChange in m_sourceTfsClient.QueryHistory(
                                   sourceRenameFromPath,
                                   new ChangesetVersionSpec(changesetId),
                                   0,
                                   RecursionType.None,
                                   null,
                                   null,
                                   new ChangesetVersionSpec(changesetId - 1),
                                   1,
                                   true,
                                   false,
                                   false))
                                {
                                    previous = previousChange.Changes[0].Item;
                                }
                            }
                            catch (ItemNotFoundException)
                            {
                                // rename from item doesnot exist.
                            }
                            if (previous != null)
                            {
                                string mappedPreviousPath = translationService.GetMappedPath(previous.ServerItem, otherSidesourceId);
                                if (!string.IsNullOrEmpty(mappedPreviousPath))
                                {
                                    itemsToBeDeleted.Add(mappedPreviousPath);
                                }
                            }
                        }

                        if (sourceChange.Value.Item.ItemType == ItemType.File)
                        {
                            sourceChange.Value.Item.DownloadFile(mappedLocalPath);
                        }
                        else
                        {
                            Directory.CreateDirectory(mappedLocalPath);
                        }

                        // Todo, detect item on target location.
                        ItemSet itemSet = Workspace.VersionControlServer.GetItems(sourceChange.Key, RecursionType.None);
                        if (itemSet.Items.Length > 0)
                        {
                            // Pend an Edit
                            Workspace.PendEdit(sourceChange.Key);
                        }
                        else
                        {
                            Workspace.PendAdd(sourceChange.Key);
                        }
                    }
                }
                catch (Exception ex)
                {
                    TraceManager.TraceError(
                        string.Format("Unable to pend change for item {0}. Error - {1}", sourceChange.Key, ex.Message));
                }
            }

            foreach (string itemToBeDeleted in itemsToBeDeleted)
            {
                try
                {
                    Workspace.PendDelete(itemToBeDeleted);
                }
                catch (Exception ex)
                {
                    TraceManager.TraceError(
                        string.Format("Unable to pend delete for item {0}. Error - {1}", itemToBeDeleted, ex.Message));
                }
            }
            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        protected virtual bool Checkin(ChangeGroup group, HashSet<string> implicitRenames, HashSet<string> implicitAdds, 
            HashSet<string> skippedActions, out int changesetId)
        {
            changesetId = -1;
            PendingChange[] changes = Workspace.GetPendingChanges();
            MigrationConflict checkinConflict = null;

            if (!group.Comment.Contains("Initial Check-in as snapshot at changeset") && m_sourceSideIsTfs)
            {
                if (!codeReview(changes, int.Parse(group.Name), implicitRenames, implicitAdds, skippedActions, false))
                {
                    checkinConflict = TfsCheckinConflictType.CreateConflict(group.Name);
                    m_conflictManagementService.BacklogUnresolvedConflict(group.SourceId, checkinConflict, false);

                    while (!checkinConflict.Reload() || (checkinConflict.ConflictStatus != MigrationConflict.Status.Resolved))
                    {
                        changes = Workspace.GetPendingChanges();
                        if (codeReview(changes, int.Parse(group.Name), implicitRenames, implicitAdds, skippedActions, false))
                        {
                            TraceManager.TraceInformation("All code review conflicts have been resolved after retry.");
                            break;
                        }
                        else
                        {
                            TraceManager.TraceWarning("Code review failed, please resolve the code review conflicts.");
                        }

                        Thread.Sleep(30 * 1000);
                    }
                    codeReview(changes, int.Parse(group.Name), implicitRenames, implicitAdds, skippedActions, true);
                    changes = Workspace.GetPendingChanges();
                }
             }

             if (changes.Length > 0)
             {
                 string comment = GetChangeComment(group);
                 string owner = GetChangeOwner(group);

                 //PendingChange[] remainingChanges = ValidatePendingChanges(group, changes);

                 bool retryWithDefaultOwner = false;
                 try
                 {
                     TraceManager.TraceInformation("Checking in {0} items, owner {1}", changes.Length, owner);

                     changesetId = Workspace.CheckIn(
                         changes,
                         owner,
                         comment, null, null, null);

                     TraceManager.TraceInformation("Checked in change {0}", changesetId);
                 }
                 catch (IdentityNotFoundException inf)
                 {
                     TraceManager.TraceInformation(inf.Message);
                     retryWithDefaultOwner = true;
                 }
                 catch (ChangesetAuthorMustBeNonGroupException ca)
                 {
                     TraceManager.TraceInformation(ca.Message);
                     retryWithDefaultOwner = true;
                 }
                 catch (OutOfMemoryException)
                 {
                     // Let this be handled by the general exception handlers in ProcessChangeGroup rather than retrying
                     // as is done in the "catch (Exception ge)" block below.
                     throw;
                 }
                 catch (Exception ge)
                 {
                     TraceManager.TraceInformation(ge.Message);
                     retryWithDefaultOwner = true;
                 }
                 if (retryWithDefaultOwner)
                 {
                     comment = m_commentDecorationService.AddToChangeGroupCommentSuffix(comment, 
                         string.Format(
                         TfsVCAdapterResource.Culture,
                         TfsVCAdapterResource.AuthorFallbackNote,
                         owner));

                     TraceManager.TraceInformation("Unable to checkin to TFS using the identity {0}.  Converting to default credentials.", group.Owner);

                     changesetId = Workspace.CheckIn(
                         changes,
                         comment, null, null, null);

                     TraceManager.TraceInformation("Checked in change {0}", changesetId);
                 }
             }
             else
             {
                 TraceManager.TraceInformation("After processing there were 0 pending changes for group id {0}", group.ChangeGroupId);
                 // Nothing to be checked in, int.MinValue will be returned. We will query the latest changesetid and store it in conversionhistory
                 // We will mark this change group as migrated.
                 changesetId = int.MinValue;
                 return true;
             }

             return changesetId > -1;
        }

        protected virtual PendingChange[] ValidatePendingChanges(ChangeGroup group, PendingChange[] changes)
        {
            string comment = string.Format(CultureInfo.InvariantCulture, "{0}-{1}", group.SessionId, group.Name);
            string shelvesetName = TfsUtil.CorrectShelvesetname(comment); // TFS will throw if shelveset name is longer than 64 characters or contains illegal characters.

            // Consider the situation when new changesets coming in during migration checkin. 
            /*List<PendingChange> conflicts = new List<PendingChange>();
            for (int i = 0; i < changes.Length; i++)
            {
                PendingChange pc = changes[i];

                if (pc.Version > lastMigrationHWM && pc.Version > m_completedTfs2SourceMigrationHWM.Value &&
                    !IsOurTfsChanges(pc.Version))
                {
                    TraceManager.TraceWarning("Detected conflict on file {0} version {1}",
                        pc.ServerItem, pc.Version);
                    conflicts.Add(pc);
                }
            }

            if (conflicts.Count > 0)
            {
                shelveConflicts(group, conflicts, shelvesetName, lastMigrationHWM, comment);
            }*/

            return m_workspace.GetPendingChanges();
        }

        private void shelveConflicts(ChangeGroup group, List<PendingChange> pendingChanges, string shelvesetName, int lastTfsChange, string comment)
        {
            VersionSpec versionSpec = null;

            if (lastTfsChange == 1)
            {
                versionSpec = VersionSpec.Latest;
            }
            else
            {
                versionSpec = new ChangesetVersionSpec(lastTfsChange);
            }

            if (pendingChanges.Count > 0)
            {
                for (int i = 0; i < pendingChanges.Count; i++)
                {
                    ItemSet set = m_tfsClient.GetItems(pendingChanges[i].LocalItem,
                        versionSpec,
                        RecursionType.None,
                        DeletedState.NonDeleted,
                        Microsoft.TeamFoundation.VersionControl.Client.ItemType.File,
                        false);

                    if (set.Items.Length == 1)
                    {
                        // Rebase file to conflicted version spec
                        m_workspace.Get(new string[] { pendingChanges[i].LocalItem }, versionSpec, RecursionType.None, GetOptions.None);
                    }
                    else
                    {
                        string msg = string.Format(CultureInfo.InvariantCulture, "{0} exists in TFS but not at version {1} (still adding to shelve)", pendingChanges[i].LocalItem, versionSpec.DisplayString);

                        // warningEmail.WriteLine(msg);
                        TraceManager.TraceWarning(msg);
                    }
                }
            }

            Conflict[] conflicts = m_workspace.QueryConflicts(null, false);
            foreach (Conflict conflict in conflicts)
            {
                TraceManager.TraceWarning(conflict.ToString());
                conflict.Resolution = Resolution.AcceptYours;
                m_workspace.ResolveConflict(conflict);
                int i = conflict.ConflictId;
            }

            m_eventService.OnMigrationWarning(
                new VersionControlEventArgs(group, string.Format(TfsVCAdapterResource.Culture, TfsVCAdapterResource.ConflictShelvesetCreated, shelvesetName), SystemType.Other));

            Shelveset shelveset = new Shelveset(m_tfsClient, shelvesetName, ".");
            shelveset.Comment = comment;
            m_workspace.Shelve(shelveset, pendingChanges.ToArray(), ShelvingOptions.Move);
        }

        protected virtual void Download(IMigrationItem downloadItem, IMigrationAction action, string localPath)
        {
            downloadItem.Download(localPath);
        }

        protected virtual void Add(IMigrationAction action, BatchingContext context)
        {
            try
            {
                context.PendAdd(action.Path, action.SourceItem);
            }
            catch (MappingNotFoundException)
            {
                // the item is not mapped so skip it.
                action.State = ActionState.Skipped;
            }
        }

        protected virtual void Edit(IMigrationAction action, BatchingContext context)
        {
            try
            {
                context.PendEdit(action.FromPath, action.Path, action.Version, action.SourceItem);
            }
            catch (MappingNotFoundException)
            {
                // the item is not mapped so skip it.
                action.State = ActionState.Skipped;
            }
        }

        protected virtual void Delete(IMigrationAction action, BatchingContext context)
        {
            context.PendDelete(action.FromPath, action.Path);
        }

        protected virtual void Rename(IMigrationAction action, BatchingContext context)
        {
            // Download on rename is not necessary in TFS
            if (!string.Equals(action.FromPath, action.Path, StringComparison.Ordinal))
            {
                context.PendRename(action.FromPath, action.Path);
            }
            else
            {
                // Skip rename to itself. Also skip it during code review check.
                context.ImplicitRenames.Add(action.Path);   
            }
        }

        protected virtual void Branch(IMigrationAction action, BatchingContext context)
        {
            bool contentChanged;
            string branchedFromVersion = m_changeGroupService.GetChangeIdFromConversionHistory(
                action.Version,
                m_configurationService.MigrationPeer,
                out contentChanged);

            if (string.IsNullOrEmpty(branchedFromVersion))
            {
                branchedFromVersion = findHistoryInfoFromResolutionRules(action.Version, action);
                if (string.IsNullOrEmpty(branchedFromVersion))
                {
                    // Change to Add, 
                    context.PendAdd(action.Path, action.SourceItem);
                    return;
                }
            }

            context.PendBranch(
                action.FromPath,
                action.Path,
                branchedFromVersion);

            // This is a changegroup resulted from a conflict resolution. Pend an extra edit to the item. 
            if (contentChanged)
            {
                context.PendEdit(action.Path, action.Path, string.Empty, action.SourceItem);
            }
        }

        private string findHistoryInfoFromResolutionRules(string Id, IMigrationAction action)
        {
            MigrationConflict historyNotFoundConflict = TFSHistoryNotFoundConflictType.CreateConflict(Id, action);

            List<MigrationAction> retActions;
            ConflictResolutionResult resolutionResult =
                m_conflictManagementService.TryResolveNewConflict(action.ChangeGroup.SourceId, historyNotFoundConflict, out retActions);
            if (resolutionResult.Resolved && resolutionResult.ResolutionType == ConflictResolutionType.SuppressedConflictedChangeGroup)
            {
                // The changeset was skipped during checkin on the target system. 
                return resolutionResult.Comment;
                // todo, the item may not exist on the target system at that version.
            }
            else if (resolutionResult.Resolved && resolutionResult.ResolutionType == ConflictResolutionType.SkipConflictedChangeAction)
            {
                return string.Empty;
            }
            else
            {
                throw new MigrationUnresolvedConflictException(historyNotFoundConflict);
            }
        }

        protected virtual void Merge(IMigrationAction action, BatchingContext context)
        {
            try
            {
                RecursionType recurse = action.Recursive ? RecursionType.Full : RecursionType.None;
                bool versionFromContentChanged;
                bool versionToContentChanged;
                string mergeVersionFrom = m_changeGroupService.GetChangeIdFromConversionHistory(action.Version, m_configurationService.MigrationPeer, out versionFromContentChanged);

                if (string.IsNullOrEmpty(mergeVersionFrom))
                {
                    mergeVersionFrom = findHistoryInfoFromResolutionRules(action.Version, action);
                    if (string.IsNullOrEmpty(mergeVersionFrom))
                    {
                        // Just skip the merge bit
                        return;
                    }
                }
                string mergeVersionTo = m_changeGroupService.GetChangeIdFromConversionHistory(action.MergeVersionTo, m_configurationService.MigrationPeer, out versionToContentChanged);
                if (string.IsNullOrEmpty(mergeVersionTo))
                {
                    mergeVersionTo = findHistoryInfoFromResolutionRules(action.MergeVersionTo, action);
                    if (string.IsNullOrEmpty(mergeVersionTo))
                    {
                        // Just skip the merge bit
                        return;
                    }
                }
                context.PendMerge(
                    action.FromPath,
                    action.Path,
                    recurse,
                    mergeVersionFrom,
                    mergeVersionTo,
                    action.SourceItem);
            }
            catch (MappingNotFoundException)
            {
                // the item is not mapped so skip it.
                action.State = ActionState.Skipped;
            }
        }

        protected virtual void BranchMerge(IMigrationAction action, BatchingContext context)
        {
            try
            {
                RecursionType recurse = action.Recursive ? RecursionType.Full : RecursionType.None;
                bool versionFromContentChanged;
                bool versionToContentChanged;
                string mergeVersionFrom = m_changeGroupService.GetChangeIdFromConversionHistory(action.Version, m_configurationService.MigrationPeer, out versionFromContentChanged);

                if (string.IsNullOrEmpty(mergeVersionFrom))
                {
                    mergeVersionFrom = findHistoryInfoFromResolutionRules(action.Version, action);
                    if (string.IsNullOrEmpty(mergeVersionFrom))
                    {
                        // Change to add
                        context.PendAdd(action.Path, action.SourceItem);
                        context.AddChangedFromUndelete.Add(action.Path, false);
                        return;
                    }
                }
                string mergeVersionTo = m_changeGroupService.GetChangeIdFromConversionHistory(action.MergeVersionTo, m_configurationService.MigrationPeer, out versionToContentChanged);
                if (string.IsNullOrEmpty(mergeVersionTo))
                {
                    mergeVersionTo = findHistoryInfoFromResolutionRules(action.MergeVersionTo, action);
                    if (string.IsNullOrEmpty(mergeVersionTo))
                    {
                        // Change to add
                        context.PendAdd(action.Path, action.SourceItem);
                        context.AddChangedFromUndelete.Add(action.Path, false);
                        return;
                    }
                }
                context.PendMerge(
                    action.FromPath,
                    action.Path,
                    recurse,
                    mergeVersionFrom,
                    mergeVersionTo,
                    action.SourceItem);
            }
            catch (MappingNotFoundException)
            {
                // the item is not mapped so skip it.
                action.State = ActionState.Skipped;
            }
        }

        protected virtual void Undelete(IMigrationAction action, BatchingContext context)
        {
            bool contentChanged;
            string deletedVersion = m_changeGroupService.GetChangeIdFromConversionHistory(
                action.Version,
                m_configurationService.MigrationPeer,
                out contentChanged);

            if (string.IsNullOrEmpty(deletedVersion))
            {
                deletedVersion = findHistoryInfoFromResolutionRules(action.Version, action);
                if (string.IsNullOrEmpty(deletedVersion))
                {
                    // change to Add
                    context.PendAdd(action.Path, action.SourceItem);
                    context.AddChangedFromUndelete.Add(action.Path, false);
                    return;
                }
            }
            context.PendUndelete(action.FromPath, action.Path, deletedVersion, action.SourceItem);
        }

        protected virtual void Encoding(IMigrationAction action, BatchingContext context)
        {
            // do nothing
        }

        protected virtual void Label(IMigrationAction action, BatchingContext context)
        {
            RecursionType recurse = action.Recursive ? RecursionType.Full : RecursionType.None;

            LabelItemSpec labelItem = new LabelItemSpec(
                new ItemSpec(action.Path, recurse),
                VersionSpec.Latest, false);

            Workspace.VersionControlServer.CreateLabel(
                new VersionControlLabel(Workspace.VersionControlServer,
                    action.Label,
                    action.ChangeGroup.Owner, null, action.ChangeGroup.Comment),
                    new LabelItemSpec[1] { labelItem },
                    LabelChildOption.Merge);
        }

        protected virtual void Unknown(IMigrationAction action, BatchingContext context)
        {
            Debug.Assert(false, "Unknown migration action!");

            TraceManager.TraceWarning("While processing migration action {0} an unknown action ({1}) has been encountered - skipping this action.",
                action.ChangeGroup.ChangeGroupId,
                action.Action);


            return;
        }


        protected virtual Workspace Workspace
        {
            get
            {
                return m_workspace;
            }
            set
            {
                m_workspace = value;
            }
        }


        protected virtual Workspace CreateWorkspace()
        {
            TraceManager.EnterMethod();

            Workspace ws;

            Workstation.Current.UpdateWorkspaceInfoCache(m_tfsClient, m_tfsClient.AuthenticatedUser);

            try
            {


                ws = m_tfsClient.GetWorkspace(
                    m_configurationService.Workspace,
                    m_tfsClient.AuthenticatedUser);

                /* the folder enum should be in the try/catch because the GetWorkspace can
                 * find a cached workspace.  if the workspace is deleted on the server the
                 * Folders call will throw WorkspaceNotFoundException which will do what
                 * we want
                 */

                // clear the existing mappings to make sure the latest ones are being used
                foreach (WorkingFolder wf in ws.Folders)
                {
                    try
                    {
                        ws.DeleteMapping(wf);
                    }
                    catch (ItemNotMappedException)
                    {
                        /* the item is no longer mapped in the workspace - ignore */
                    }
                }

                TraceManager.WriteLine(TraceManager.Engine, "Loaded existing workspace: {0}", m_configurationService.Workspace);
            }
            catch (WorkspaceNotFoundException)
            {
                TraceManager.WriteLine(TraceManager.Engine, "Creating new workspace: {0}", m_configurationService.Workspace);

                ws = m_tfsClient.CreateWorkspace(
                    m_configurationService.Workspace,
                    m_tfsClient.AuthenticatedUser,
                    m_configurationService.GetValue<string>("TargetWorkspaceComment",
                        "Migration Toolkit Generated Workspace")
                    );
            }


            // Determine the local root and add the mapping to the workspace
            List<string> mappedLocalPaths = new List<string>();
            foreach (MappingEntry mapping in m_configurationService.Filters)
            {
                string localPath;
                int allowedLocalPathLength = mapping.Path.Length - m_configurationService.WorkspaceRoot.Length - 1;
                if (allowedLocalPathLength > 0)
                {
                    localPath = Path.Combine(m_configurationService.WorkspaceRoot,
                        mapping.Path.Substring(2).Replace('/', '\\').Substring(m_configurationService.WorkspaceRoot.Length-1));
                    if (!mappedLocalPaths.Contains(localPath))
                    {
                        mappedLocalPaths.Add(localPath);
                    }
                    else
                    {
                        throw new MigrationException("Unable to find a unique local path");
                    }
                }
                else
                {
                    TraceManager.TraceWarning("The workspace root is too long. The migration may fail due to a InvalidPath exception if mapped local path is longer than 259 characters.");
                    localPath = Path.Combine(m_configurationService.WorkspaceRoot, mapping.Path.Substring(2).Replace('/', '\\'));
                }

                if (mapping.Cloak)
                {
                    ws.Cloak(mapping.Path);
                }
                else
                {
                    ws.Map(mapping.Path, localPath);
                }
            }

            TraceManager.WriteLine(TraceManager.Engine, "Created workspace {0}", ws.Name);

            TraceManager.ExitMethod(ws);

            return ws;

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers")]
        protected virtual void SingleItemError(object sender, BatchedItemEventArgs e)
        {
            throw new VersionControlMigrationException(
                string.Format(
                    TfsVCAdapterResource.Culture,
                    TfsVCAdapterResource.SingleItemActionFailed,
                    e.Target.Action, e.Target.Target), e.Exception);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers")]
        protected virtual void SingleItemWarning(object sender, BatchedItemEventArgs e)
        {
            TraceManager.TraceInformation(e.Message);
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers")]
        protected virtual void MergeError(object sender, BatchedItemEventArgs e)
        {
            TraceManager.TraceWarning("Failed to pend merge on item {0}", e.Target.Target);
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
