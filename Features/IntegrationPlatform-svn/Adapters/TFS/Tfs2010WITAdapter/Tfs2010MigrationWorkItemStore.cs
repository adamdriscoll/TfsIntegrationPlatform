// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Xml;
using Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts;
using Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Proxy;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    public class Tfs2010MigrationWorkItemStore : TfsMigrationWorkItemStore
    {
        public Tfs2010MigrationWorkItemStore(
            TfsCore core)
            : base(core)
        {
        }

        protected override TfsUpdateDocument InitializeUpdateDocument()
        {
            return new Tfs2010UpdateDocument(this);
        }

        protected override void SubmitLinkChangesWithUpdateDoc(
            LinkChangeGroup linkChanges,
            ServiceContainer serviceContainer,
            TfsLinkingProviderBase.LinkSubmissionPhase submissionPhase)
        {
            ConfigurationService configService = serviceContainer.GetService(typeof(ConfigurationService)) as ConfigurationService;
            ITranslationService translationService = serviceContainer.GetService(typeof(ITranslationService)) as ITranslationService;

            bool nonWorkItemLinkChangesAllSubmitted = SubmitNonWorkItemLinkChanges(
                linkChanges, serviceContainer, configService, translationService, submissionPhase);
            bool workItemLinkChangesAllSubmitted = SubmitWorkItemLinkChanges(
                linkChanges, serviceContainer, configService, translationService, submissionPhase);

            linkChanges.Status = (nonWorkItemLinkChangesAllSubmitted && workItemLinkChangesAllSubmitted && AllActionSubmitted(linkChanges))
                                     ? LinkChangeGroup.LinkChangeGroupStatus.Completed
                                     : LinkChangeGroup.LinkChangeGroupStatus.ReadyForMigration;
        }

        protected override Server.IGroupSecurityService GetGroupSecurityService(WorkItemStore workItemStore)
        {
            return workItemStore.TeamProjectCollection.GetService(typeof(IGroupSecurityService)) as IGroupSecurityService;
        }

        protected override TfsUpdateDocument SubmitAttachmentChanges(IMigrationAction action, ConflictManager conflictMgrService)
        {
            /*
             * retrieve change details
             */
            XmlDocument desc = action.MigrationActionDescription;
            XmlElement rootNode = desc.DocumentElement;
            Debug.Assert(null != rootNode);
            XmlNode attachmentNode = rootNode.FirstChild;
            string originalName = attachmentNode.Attributes["Name"].Value;
            string utcCreationDate = attachmentNode.Attributes["UtcCreationDate"].Value;
            string utcLastWriteDate = attachmentNode.Attributes["UtcLastWriteDate"].Value;
            string length = attachmentNode.Attributes["Length"].Value;
            string comment = attachmentNode.FirstChild.InnerText;
            int targetWorkItemId = FindTargetWorkItemId(action, conflictMgrService);
            string targetRevision = rootNode.Attributes["TargetRevision"].Value;

            /*
             * create operation document
             */
            TfsUpdateDocument tfsUpdateDocument = InitializeUpdateDocument();
            tfsUpdateDocument.CreateWorkItemUpdateDoc(targetWorkItemId.ToString(), targetRevision);

            /*
             * insert Connector specific comment
             */
            WorkItem item = WorkItemStore.GetWorkItem(targetWorkItemId);
            Debug.Assert(null != item, "target work item does not exist");
            tfsUpdateDocument.InsertConversionHistoryCommentToHistory(item.Type.Name, GenerateMigrationHistoryComment(action));

            int[] fileId = FindAttachmentFileId(targetWorkItemId, originalName,
                                                   utcCreationDate, utcLastWriteDate, length, comment);
            /*
             * delete attachment
             */
            if (action.Action == WellKnownChangeActionId.DelAttachment)
            {
                if (fileId.Length == 0)
                {
                    action.State = ActionState.Skipped;
                    return null;
                }
                else
                {
                    tfsUpdateDocument.RemoveAttachment(fileId[0]);
                    return tfsUpdateDocument;
                }
            }

            /*
             * add attachment
             */
            try
            {
                string sourceStoreCountString = attachmentNode.Attributes["CountInSourceSideStore"].Value;
                int sourceStoreCount;
                if (int.TryParse(sourceStoreCountString, out sourceStoreCount))
                {
                    if (sourceStoreCount <= fileId.Length)
                    {
                        action.State = ActionState.Skipped;
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                TraceManager.TraceVerbose(e.ToString());
                // for backward compatibility, just proceed
            }

            if (AttachmentIsOversized(length))
            {
                MigrationConflict conflict = new FileAttachmentOversizedConflictType().CreateConflict(
                    originalName, length, MaxAttachmentSize, targetWorkItemId.ToString(), Core.ServerName, Core.Config.Project, action);

                List<MigrationAction> actions;
                ConflictResolutionResult resolveRslt = conflictMgrService.TryResolveNewConflict(conflictMgrService.SourceId, conflict, out actions);

                if (!resolveRslt.Resolved)
                {
                    return null;
                }

                if (resolveRslt.ResolutionType == ConflictResolutionType.SuppressedConflictedChangeAction)
                {
                    action.State = ActionState.Skipped;
                    return null;
                }

                if (resolveRslt.ResolutionType == ConflictResolutionType.Other)
                {
                    // conflict resolved, just proceed
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            Guid fileGuid = Guid.NewGuid();
            tfsUpdateDocument.AddAttachment(originalName, XmlConvert.ToString(fileGuid),
                                            utcCreationDate, utcLastWriteDate, length, comment);

            // get areaNodeUri - required by Dev10 OM
            Project project = item.Project;
            string projectNodeUri = (project.Uri).ToString();
            string areaNodeUri = string.Empty;
            if (project.Id == item.AreaId)
            {
                areaNodeUri = Core.AreaNodeUri;
            }
            else
            {
                // Loop through the area root nodes looking for the one we're on
                foreach (Node node in project.AreaRootNodes)
                {
                    // It could be one of the root nodes
                    if (node.Id == item.AreaId)
                    {
                        areaNodeUri = node.Uri.ToString();
                        break;
                    }

                    // Now check if it is a child of the current area root node
                    try
                    {
                        Node node2 = node.FindNodeInSubTree(item.AreaId);
                        areaNodeUri = node2.Uri.ToString();
                        break;
                    }
                    catch (DeniedOrNotExistException)
                    {
                        // Ignore if not found, go onto the next area root node
                        continue;
                    }
                }
            }

            //Now upload the file since that has to be done before the Xml batch is executed.
            Debug.Assert(!string.IsNullOrEmpty(LocalWorkDir));
            string filePath = Path.Combine(LocalWorkDir, fileGuid.ToString());
            action.SourceItem.Download(filePath);
            using (var strm = File.OpenRead(filePath))
            {
                var f = new FileAttachment();
                f.AreaNodeUri = areaNodeUri;
                f.ProjectUri = projectNodeUri;
                f.FileNameGUID = fileGuid;
                f.LocalFile = strm; // attachment.GetFileContents();

                WorkItemServer.UploadFile(f);
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return tfsUpdateDocument;
        }

        private bool SubmitWorkItemLinkChanges(
            LinkChangeGroup linkChanges,
            ServiceContainer serviceContainer,
            ConfigurationService configService,
            ITranslationService translationService,
            TfsLinkingProviderBase.LinkSubmissionPhase submissionPhase)
        {
            var linkChangeActions = ExtractWorkItemLinkChanges(linkChanges);
            if (linkChangeActions.Count == 0)
            {
                return true;
            }

            ConflictManager conflictManageer = serviceContainer.GetService(typeof(ConflictManager)) as ConflictManager;
            var updatedocsForEditLinks = new List<XmlDocument>(linkChangeActions.Count);
            var updateDocsForAddLinks = new List<XmlDocument>(linkChangeActions.Count);
            var updateDocsForDeleteLinks = new List<XmlDocument>(linkChangeActions.Count);
            Dictionary<int, LinkChangeAction> docForEditlinksToActionMap = new Dictionary<int, LinkChangeAction>();
            Dictionary<int, LinkChangeAction> docForAddlinksToActionMap = new Dictionary<int, LinkChangeAction>();
            Dictionary<int, LinkChangeAction> docForDeletelinksToActionMap = new Dictionary<int, LinkChangeAction>();
            for (int i = 0; i < linkChangeActions.Count; ++i)
            {
                if (linkChangeActions[i].Status != LinkChangeAction.LinkChangeActionStatus.ReadyForMigration
                    || linkChangeActions[i].IsConflicted)
                {
                    continue;
                }

                if (!ProcessActionInCurrentSubmissionPhase(linkChangeActions[i], submissionPhase))
                {
                    continue;
                }

                var tfsUpdateDocument = InitializeUpdateDocument();

                var handler = linkChangeActions[i].Link.LinkType as ILinkHandler;
                Debug.Assert(null != handler);

                handler.UpdateTfs(tfsUpdateDocument, linkChangeActions[i]);

                if (linkChangeActions[i].ChangeActionId.Equals(WellKnownChangeActionId.Add))
                {
                    docForAddlinksToActionMap.Add(updateDocsForAddLinks.Count, linkChangeActions[i]);
                    updateDocsForAddLinks.Add(tfsUpdateDocument.UpdateDocument);
                }
                else if (linkChangeActions[i].ChangeActionId.Equals(WellKnownChangeActionId.Delete))
                {
                    docForDeletelinksToActionMap.Add(updateDocsForDeleteLinks.Count, linkChangeActions[i]);
                    updateDocsForDeleteLinks.Add(tfsUpdateDocument.UpdateDocument);
                }
                else if (linkChangeActions[i].ChangeActionId.Equals(WellKnownChangeActionId.Edit))
                {
                    docForEditlinksToActionMap.Add(updatedocsForEditLinks.Count, linkChangeActions[i]);
                    updatedocsForEditLinks.Add(tfsUpdateDocument.UpdateDocument);
                }
                else
                {
                    TraceManager.TraceVerbose("Change action '{0}' in Link Change Group '{1}' is not supported.",
                        linkChangeActions[i].ChangeActionId.ToString(), linkChanges.GroupName);
                    linkChangeActions[i].Status = LinkChangeAction.LinkChangeActionStatus.Completed;
                }
            }

            bool succeeded = true;
            if (updatedocsForEditLinks.Count > 0)
            {
                succeeded &= SubmitBatchedAddOrDeleteLinkChanges(
                    updatedocsForEditLinks, docForEditlinksToActionMap, translationService, configService, conflictManageer);
            }
            if (updateDocsForDeleteLinks.Count > 0)
            {
                succeeded &= SubmitBatchedAddOrDeleteLinkChanges(
                    updateDocsForDeleteLinks, docForDeletelinksToActionMap, translationService, configService, conflictManageer);
            }
            if (updateDocsForAddLinks.Count > 0)
            {
                succeeded &= SubmitBatchedAddOrDeleteLinkChanges(
                    updateDocsForAddLinks, docForAddlinksToActionMap, translationService, configService, conflictManageer);
            }
            return succeeded;
        }

        private bool SubmitBatchedAddOrDeleteLinkChanges(
            List<XmlDocument> updateDocuments,
            Dictionary<int, LinkChangeAction> updateDocIndexToLinkChangeActionMap,
            ITranslationService translationService,
            ConfigurationService configService,
            ConflictManager conflictManager)
        {
            bool succeeded = true;

            UpdateResult[] results = TfsBatchUpdateHelper.Submit(Core, WorkItemServer, updateDocuments.ToArray());
            if (results.Length != updateDocuments.Count)
            {
                throw new SynchronizationEngineException("Wrong number of link update results.");
            }

            for (int i = 0; i < results.Length; ++i)
            {
                UpdateResult rslt = results[i];

                if (rslt.Exception != null)
                {
                    if (rslt.Exception.Message.Contains("The specified link already exists"))
                    {
                        // it is ok to eat this exception
                    }
                    else if (rslt.Exception is System.Web.Services.Protocols.SoapException
                        && null != rslt.Exception.Message
                        && rslt.Exception.Message.StartsWith(
                            TFSMulitpleParentLinkConflictType.SingleParentViolationMessage, 
                            StringComparison.OrdinalIgnoreCase))
                    {
                        MigrationConflict conflict = TFSMulitpleParentLinkConflictType.CreateConflict(
                            updateDocIndexToLinkChangeActionMap[i], rslt.Exception);

                        List<MigrationAction> actions;
                        var resolutionRslt = conflictManager.TryResolveNewConflict(conflictManager.SourceId, conflict, out actions);
                        if (!resolutionRslt.Resolved)
                        {
                            updateDocIndexToLinkChangeActionMap[i].IsConflicted = true;
                            succeeded = false;
                        }
                    }
                    else if (rslt.Exception is System.Web.Services.Protocols.SoapException
                        && null != rslt.Exception.Message
                        && rslt.Exception.Message.StartsWith(
                            TFSCyclicLinkConflictType.CircularityLinkHierarchyViolationMessage, 
                            StringComparison.OrdinalIgnoreCase))
                    {
                        ILinkProvider linkProvider = ServiceContainer.GetService(typeof(ILinkProvider)) as ILinkProvider;
                        Debug.Assert(null != linkProvider, "linkProvider is NULL");

                        LinkChangeAction conflictedAction = updateDocIndexToLinkChangeActionMap[i];
                        NonCyclicReferenceClosure linkRefClosure =
                            linkProvider.CreateNonCyclicLinkReferenceClosure(conflictedAction.Link.LinkType, conflictedAction.Link.SourceArtifact);

                        MigrationConflict conflict = TFSCyclicLinkConflictType.CreateConflict(conflictedAction, rslt.Exception, linkRefClosure);

                        List<MigrationAction> actions;
                        var resolutionRslt = conflictManager.TryResolveNewConflict(conflictManager.SourceId, conflict, out actions);
                        if (!resolutionRslt.Resolved)
                        {
                            updateDocIndexToLinkChangeActionMap[i].IsConflicted = true;
                            succeeded = false;
                        }
                    }
                    else if (rslt.Exception is System.Web.Services.Protocols.SoapException
                        && null != rslt.Exception.Message
                        && rslt.Exception.Message.StartsWith(
                            TFSModifyLockedWorkItemLinkConflictType.ModifyLockedWorkItemLinkViolationMessage,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        MigrationConflict conflict = TFSModifyLockedWorkItemLinkConflictType.CreateConflict(
                            updateDocIndexToLinkChangeActionMap[i], rslt.Exception);

                        List<MigrationAction> actions;
                        var resolutionRslt = conflictManager.TryResolveNewConflict(conflictManager.SourceId, conflict, out actions);
                        if (!resolutionRslt.Resolved)
                        {
                            updateDocIndexToLinkChangeActionMap[i].IsConflicted = true;
                        }
                        // returning "not succeeded" so that the caller keeps this change group in "ReadyForMigration" status
                        succeeded = false;
                    }
                    else if (rslt.Exception is System.Web.Services.Protocols.SoapException
                        && null != rslt.Exception.Message
                        && (rslt.Exception.Message.StartsWith(TFSLinkAccessViolationConflictType.LinkAccessViolationMessage1, StringComparison.OrdinalIgnoreCase)
                            || rslt.Exception.Message.StartsWith(TFSLinkAccessViolationConflictType.LinkAccessViolationMessage2, StringComparison.OrdinalIgnoreCase)))
                    {
                        MigrationConflict conflict = TFSLinkAccessViolationConflictType.CreateConflict(
                            updateDocIndexToLinkChangeActionMap[i], rslt.Exception);

                        List<MigrationAction> actions;
                        var resolutionRslt = conflictManager.TryResolveNewConflict(conflictManager.SourceId, conflict, out actions);
                        if (!resolutionRslt.Resolved)
                        {
                            updateDocIndexToLinkChangeActionMap[i].IsConflicted = true;
                        }
                        // returning "not succeeded" so that the caller keeps this change group in "ReadyForMigration" status
                        succeeded = false;
                    }
                    else
                    {
                        TraceManager.TraceError(rslt.Exception.ToString());
                        succeeded = false;
                        // TODO
                        // Try resolve conflict and push to backlog if resolution fails
                        updateDocIndexToLinkChangeActionMap[i].IsConflicted = true;
                    }
                }
                else
                {
                    foreach (LinkChangeAction action in updateDocIndexToLinkChangeActionMap.Values)
                    {
                        MarkLinkChangeActionCompleted(action);
                    }

                    List<LinkChangeAction> updatedActions = new List<LinkChangeAction>(updateDocIndexToLinkChangeActionMap.Values);
                    if (rslt.Exception == null)
                    {
                        UpdateLinkConversionHistory(configService, translationService, rslt, updatedActions);
                    }
                    else if (rslt.Exception.Message.Contains("The specified link already exists"))
                    {
                        WorkItemLinkStore relatedArtifactsStore = new WorkItemLinkStore(configService.SourceId);
                        relatedArtifactsStore.UpdateSyncedLinks(updatedActions);
                    }
                }
            }
            return succeeded;
        }

        protected bool SubmitNonWorkItemLinkChanges(
            LinkChangeGroup linkChanges,
            ServiceContainer serviceContainer,
            ConfigurationService configService,
            ITranslationService translationService,
            TfsLinkingProviderBase.LinkSubmissionPhase submissionPhase)
        {
            // group non-WorkItemLink changes by work item Id
            Dictionary<int, List<LinkChangeAction>> perWorkItemLinkChanges = RegroupLinkChangeActions(linkChanges);
            var orderedWorkitemId = new Dictionary<int, int>();
            int index = 0;
            foreach (int workItemId in perWorkItemLinkChanges.Keys)
            {
                orderedWorkitemId.Add(index++, workItemId);
            }

            // batch-submit links of each work item
            var updateDocs = new List<XmlDocument>(perWorkItemLinkChanges.Count);
            foreach (var perWorkItemLinkChange in perWorkItemLinkChanges)
            {
                if (perWorkItemLinkChange.Value.Count == 0)
                {
                    continue;
                }

                WorkItem workItem = WorkItemStore.GetWorkItem(perWorkItemLinkChange.Key);

                var tfsUpdateDocument = InitializeUpdateDocument();
                tfsUpdateDocument.CreateWorkItemUpdateDoc(workItem);
                bool hasNonWorkItemLinkChanges = false;
                foreach (LinkChangeAction linkChangeAction in perWorkItemLinkChange.Value)
                {
                    if (linkChangeAction.Status != LinkChangeAction.LinkChangeActionStatus.ReadyForMigration
                        || linkChangeAction.IsConflicted
                        || linkChangeAction.Link.LinkType is WorkItemLinkTypeBase)
                    {
                        continue;
                    }

                    if (!ProcessActionInCurrentSubmissionPhase(linkChangeAction, submissionPhase))
                    {
                        continue;
                    }

                    hasNonWorkItemLinkChanges = true;
                    var handler = linkChangeAction.Link.LinkType as ILinkHandler;
                    Debug.Assert(null != handler);
                    handler.UpdateTfs(tfsUpdateDocument, linkChangeAction);
                }

                if (hasNonWorkItemLinkChanges)
                {
                    updateDocs.Add(tfsUpdateDocument.UpdateDocument);
                }
            }

            if (updateDocs.Count == 0)
            {
                return true;
            }

            UpdateResult[] results = TfsBatchUpdateHelper.Submit(Core, WorkItemServer, updateDocs.ToArray());

            if (results.Length != updateDocs.Count)
            {
                throw new SynchronizationEngineException("Wrong number of link update results.");
            }

            bool succeeded = true;
            for (int i = 0; i < results.Length; ++i)
            {
                UpdateResult rslt = results[i];

                if (rslt.Exception != null
                    && !rslt.Exception.Message.Contains("The specified link already exists"))
                {
                    TraceManager.TraceError(rslt.Exception.ToString());
                    succeeded = false;
                    // TODO
                    // Try resolve conflict and push to backlog if resolution fails
                    foreach (LinkChangeAction action in perWorkItemLinkChanges[orderedWorkitemId[i]])
                    {
                        action.IsConflicted = true;
                    }
                }
                else
                {
                    foreach (LinkChangeAction action in perWorkItemLinkChanges[orderedWorkitemId[i]])
                    {
                        if (ProcessActionInCurrentSubmissionPhase(action, submissionPhase))
                        {
                            MarkLinkChangeActionCompleted(action);
                        }
                    }

                    if (rslt.Exception == null)
                    {
                        UpdateLinkConversionHistory(configService, translationService, rslt, perWorkItemLinkChanges[orderedWorkitemId[i]]);
                    }
                    else if (rslt.Exception.Message.Contains("The specified link already exists"))
                    {
                        WorkItemLinkStore relatedArtifactsStore = new WorkItemLinkStore(configService.SourceId);
                        relatedArtifactsStore.UpdateSyncedLinks(perWorkItemLinkChanges[orderedWorkitemId[i]]);
                    }
                }
            }
            return succeeded;
        }

        private List<LinkChangeAction> ExtractWorkItemLinkChanges(
            LinkChangeGroup linkChangeGroup)
        {
            var extractedLinkChanges = new List<LinkChangeAction>();

            foreach (LinkChangeAction linkChangeAction in linkChangeGroup.Actions)
            {
                Debug.Assert(!string.IsNullOrEmpty(linkChangeAction.Link.SourceArtifactId));
                if (linkChangeAction.Link.LinkType is WorkItemLinkTypeBase)
                {
                    extractedLinkChanges.Add(linkChangeAction);
                }
            }
            return extractedLinkChanges;
        }

        protected override Dictionary<int, List<LinkChangeAction>> RegroupLinkChangeActions(
            LinkChangeGroup linkChangeGroup)
        {
            var perWorkItemLinkChanges = new Dictionary<int, List<LinkChangeAction>>();

            foreach (LinkChangeAction linkChangeAction in linkChangeGroup.Actions)
            {
                Debug.Assert(!string.IsNullOrEmpty(linkChangeAction.Link.SourceArtifactId));

                // skipping v2 WorkItemLinks, as they are submitted separately
                if (linkChangeAction.Link.LinkType is WorkItemLinkTypeBase)
                {
                    continue;
                }

                int sourceArtifactWorkItemId;
                bool idConversionResult = int.TryParse(linkChangeAction.Link.SourceArtifactId,
                                                       out sourceArtifactWorkItemId);
                Debug.Assert(idConversionResult);

                if (!perWorkItemLinkChanges.ContainsKey(sourceArtifactWorkItemId))
                {
                    perWorkItemLinkChanges.Add(sourceArtifactWorkItemId, new List<LinkChangeAction>());
                }

                if (!perWorkItemLinkChanges[sourceArtifactWorkItemId].Contains(linkChangeAction))
                {
                    perWorkItemLinkChanges[sourceArtifactWorkItemId].Add(linkChangeAction);
                }
            }

            return perWorkItemLinkChanges;
        }
    }
}