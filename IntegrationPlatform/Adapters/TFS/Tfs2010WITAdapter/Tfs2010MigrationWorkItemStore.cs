// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
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
        HighWaterMark<long> m_hwmSubmittedLinkChangeId;

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

            if (m_hwmSubmittedLinkChangeId == null)
            {
                m_hwmSubmittedLinkChangeId = new HighWaterMark<long>(TfsConstants.HwmSubmittedLinkChangeId);
                configService.RegisterHighWaterMarkWithSession(m_hwmSubmittedLinkChangeId);
            }

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
            if (action.ChangeGroup.IsForcedSync && fileId.Length > 0)
            {
                // We are trying to force sync the attachment but it already exists on the target, so skip this action
                action.State = ActionState.Skipped;
                return null;
            }

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
                f.LocalFile = strm;

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
             
            // Collect list of successful LinkChangeActions (for LinkTypes with GetsActionsFromLinkChangeHistory true) to pass to SetServerLinkChangeIds()
            List<LinkChangeAction> actionsNeedingServerLinkIdSet = new List<LinkChangeAction>();

            for (int i = 0; i < results.Length; ++i)
            {
                if (results[i] == null) continue;

                UpdateResult rslt = results[i];

                if (rslt.Exception != null)
                {
                    MigrationConflict conflict = null;
                    ConflictResolutionResult resolutionRslt = null;
                    List<MigrationAction> actions;
                    bool createWitGeneralConflict = false;

                    System.Web.Services.Protocols.SoapException soapException = rslt.Exception as System.Web.Services.Protocols.SoapException;
                    if (soapException != null)
                    {
                        int? tfsErrorNumber = GetTfsErrorNumberFromSoapException(soapException);

                        if (tfsErrorNumber.HasValue)
                        {
                            LinkChangeAction linkChangeAction = updateDocIndexToLinkChangeActionMap[i];
                            switch (tfsErrorNumber)
                            {
                                case TfsConstants.TfsError_AddLink_LinkExists:
                                case TfsConstants.TfsError_DeleteLink_LinkNotFound:
                                    // it is ok to eat these exception and skip the action

                                    // mark the change action completed so it is not retried later
                                    linkChangeAction.Status = LinkChangeAction.LinkChangeActionStatus.Skipped;

                                    if (tfsErrorNumber == TfsConstants.TfsError_AddLink_LinkExists)
                                    {
                                        TraceManager.TraceInformation("Tried to add a link that already exists so skipping it: " + GetLinkChangeActionDescription(linkChangeAction));
                                    }
                                    else
                                    {
                                        TraceManager.TraceInformation("Tried to delete a link that does not exist so skipping it: " + GetLinkChangeActionDescription(linkChangeAction));
                                    }

                                    if (soapException.Detail != null)
                                    {
                                        TraceManager.TraceVerbose("SoapException.Detail.InnerXml for ignored exception: " + soapException.Detail.InnerXml);
                                    }
                                    break;

                                case TfsConstants.TfsError_AddLink_TooManyParents:
                                    if (linkChangeAction.Group.IsForcedSync)
                                    {
                                        if (DeleteExistingParentLinkToForceSyncAddLink(linkChangeAction, updateDocuments[i]))
                                        {
                                            break;
                                        }
                                    }

                                    conflict = TFSMulitpleParentLinkConflictType.CreateConflict(
                                        updateDocIndexToLinkChangeActionMap[i], rslt.Exception);

                                    resolutionRslt = conflictManager.TryResolveNewConflict(conflictManager.SourceId, conflict, out actions);
                                    if (!resolutionRslt.Resolved)
                                    {
                                        updateDocIndexToLinkChangeActionMap[i].IsConflicted = true;
                                        succeeded = false;
                                    }

                                    break;

                                case TfsConstants.TfsError_AddLink_Circular:
                                case TfsConstants.TfsError_AddLink_ChildIsAncestor:
                                    ILinkProvider linkProvider = ServiceContainer.GetService(typeof(ILinkProvider)) as ILinkProvider;
                                    Debug.Assert(null != linkProvider, "linkProvider is NULL");

                                    LinkChangeAction conflictedAction = updateDocIndexToLinkChangeActionMap[i];
                                    NonCyclicReferenceClosure linkRefClosure =
                                        linkProvider.CreateNonCyclicLinkReferenceClosure(conflictedAction.Link.LinkType, conflictedAction.Link.SourceArtifact);

                                    conflict = TFSCyclicLinkConflictType.CreateConflict(conflictedAction, rslt.Exception, linkRefClosure);

                                    resolutionRslt = conflictManager.TryResolveNewConflict(conflictManager.SourceId, conflict, out actions);
                                    if (!resolutionRslt.Resolved)
                                    {
                                        updateDocIndexToLinkChangeActionMap[i].IsConflicted = true;
                                        succeeded = false;
                                    }
                                    break;

                                case TfsConstants.TfsError_LinkAuthorizationFailedLinkLocked:
                                    conflict = TFSModifyLockedWorkItemLinkConflictType.CreateConflict(
                                        updateDocIndexToLinkChangeActionMap[i], rslt.Exception);

                                    resolutionRslt = conflictManager.TryResolveNewConflict(conflictManager.SourceId, conflict, out actions);
                                    if (!resolutionRslt.Resolved)
                                    {
                                        updateDocIndexToLinkChangeActionMap[i].IsConflicted = true;
                                    }
                                    // returning "not succeeded" so that the caller keeps this change group in "ReadyForMigration" status
                                    succeeded = false;
                                    break;

                                case TfsConstants.TfsError_LinkAuthorizationFailed:
                                case TfsConstants.TfsError_LinkAuthorizationFailedNotServiceAccount:
                                    conflict = TFSLinkAccessViolationConflictType.CreateConflict(
                                        updateDocIndexToLinkChangeActionMap[i], rslt.Exception);

                                    resolutionRslt = conflictManager.TryResolveNewConflict(conflictManager.SourceId, conflict, out actions);
                                    if (!resolutionRslt.Resolved)
                                    {
                                        updateDocIndexToLinkChangeActionMap[i].IsConflicted = true;
                                    }
                                    // returning "not succeeded" so that the caller keeps this change group in "ReadyForMigration" status
                                    succeeded = false;
                                    break;

                                default:
                                    // TFS error doesn't match any that we explicitly handle
                                    TraceManager.TraceError("SubmitBatchedAddOrDeleteLinkChanges:TFS error number in SoapException not explictly handled: {0}", tfsErrorNumber);
                                    createWitGeneralConflict = true;
                                    break;
                            }
                        }
                        else
                        {
                            TraceManager.TraceError("SubmitBatchedAddOrDeleteLinkChanges: Unable to get TFS error number from SoapException: {0}", soapException.ToString());
                            createWitGeneralConflict = true;
                        }
                    }
                    else // Exception is not SoapException
                    {
                        TraceManager.TraceError("SubmitBatchedAddOrDeleteLinkChanges: Exception returned is not SoapException: {0}", rslt.Exception.ToString());
                        createWitGeneralConflict = true;
                    }

                    if (createWitGeneralConflict)
                    {
                        conflict = WitGeneralConflictType.CreateConflict(rslt.Exception);

                        resolutionRslt = conflictManager.TryResolveNewConflict(conflictManager.SourceId, conflict, out actions);
                        if (!resolutionRslt.Resolved)
                        {
                            updateDocIndexToLinkChangeActionMap[i].IsConflicted = true;
                            succeeded = false;
                        }
                    }
                }
                else // rslt.Exception == null
                {
                    LinkChangeAction successfulAction = updateDocIndexToLinkChangeActionMap[i];
                    MarkLinkChangeActionCompleted(successfulAction);

                    TraceManager.TraceVerbose("Successful " + GetLinkChangeActionDescription(successfulAction));

                    List<LinkChangeAction> updatedActions = new List<LinkChangeAction>();
                    updatedActions.Add(successfulAction);

                    if (successfulAction.Link.LinkType.GetsActionsFromLinkChangeHistory)
                    {
                        actionsNeedingServerLinkIdSet.Add(successfulAction);
                    }

                    UpdateLinkConversionHistory(configService, translationService, rslt, updatedActions);
                }
            }

            SetServerLinkChangeIds(actionsNeedingServerLinkIdSet);

            return succeeded;
        }


        private static int? GetTfsErrorNumberFromSoapException(System.Web.Services.Protocols.SoapException soapException)
        {
            int tfsErrorNumber;
            if (soapException.Detail != null)
            {
                foreach (XmlNode childNode in soapException.Detail.ChildNodes)
                {
                    XmlElement childElement = childNode as XmlElement;
                    if (childElement != null && string.Equals(childElement.Name, "details", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (XmlAttribute attribute in childElement.Attributes)
                        {
                            if (string.Equals(attribute.Name, "id", StringComparison.OrdinalIgnoreCase))
                            {
                                if (int.TryParse(attribute.Value, out tfsErrorNumber))
                                {
                                    return tfsErrorNumber;
                                }
                                else
                                {
                                    Debug.Fail("Unable to parse TFS error id to int: " + attribute.Value);
                                    return null;
                                }
                            }
                        }
                        Debug.Fail("The 'id' attribute was not found in the 'details' element of the SoapException: {0}" + soapException.ToString());
                        return null;
                    }
                }
                Debug.Fail("Did not a find the 'details' element of the SoapException: {0}" + soapException.ToString());
            }
            else
            {
                Debug.Fail("The SoapException Detail property is null");
            }

            return null;
        }

        private string GetLinkChangeActionDescription(LinkChangeAction linkChangeAction)
        {
            return (String.Format(CultureInfo.InvariantCulture, "link change action: {0} link of type {1} from '{2}' to '{3}'",
                linkChangeAction.ChangeActionId == WellKnownChangeActionId.Add ? "Add" : "Delete",
                linkChangeAction.Link.LinkType.ReferenceName, linkChangeAction.Link.SourceArtifact.Uri, linkChangeAction.Link.TargetArtifact.Uri));
        }

        private void SetServerLinkChangeIds(List<LinkChangeAction> linkChangeActions)
        {
            if (linkChangeActions.Count == 0)
            {
                return;
            }
            m_hwmSubmittedLinkChangeId.Reload();
            long hwmSubmittedValue = m_hwmSubmittedLinkChangeId.Value;

            Dictionary<int, List<LinkChangeAction>> linkChangeActionsByLowerId = new Dictionary<int, List<LinkChangeAction>>();
            foreach(LinkChangeAction action in linkChangeActions)
            {
                int workItemIdKey = GetLowerWorkItemId(action);
                if (!linkChangeActionsByLowerId.ContainsKey(workItemIdKey))
                {
                    linkChangeActionsByLowerId.Add(workItemIdKey, new List<LinkChangeAction>());
                }
                linkChangeActionsByLowerId[workItemIdKey].Add(action);
            }

            /*
            TraceManager.TraceVerbose(String.Format(
                "SetServerLinkChangeIds calling WorkItemServer.GetWorkItemLinkChanges to find Ids for {0} successful link actions starting with RowNumber {1}",
                linkChangeActions.Count, hwmSubmittedValue));
             */
            Stopwatch stopwatch = Stopwatch.StartNew();

            int serverLinkChangeIdsSet = 0;
            foreach(WorkItemLinkChange linkChange in WorkItemServer.GetWorkItemLinkChanges(Guid.NewGuid().ToString(), hwmSubmittedValue))
            {
                int linkChangeKey = GetLowerWorkItemId(linkChange);
                List<LinkChangeAction> actions;
                if (linkChangeActionsByLowerId.TryGetValue(linkChangeKey, out actions))
                {
                    foreach(LinkChangeAction action in actions)
                    {
                        if (LinkChangeMatchesLinkAction(linkChange, action))
                        {
                            action.ServerLinkChangeId = linkChange.RowVersion.ToString();
                            serverLinkChangeIdsSet++;
                            hwmSubmittedValue = linkChange.RowVersion;
                            // Don't break here because there could be more than one LinkChangeAction in the list that matches the WorkItemLinkChange
                            // break;
                        }
                    }
                }
            }

            stopwatch.Stop();
            TraceManager.TraceVerbose("Time to call GetWorkItemLinkChanges() and process return values: {0} seconds", stopwatch.Elapsed.TotalSeconds);

            if (serverLinkChangeIdsSet < linkChangeActions.Count)
            {
                // Look for any LinkChangeActions that did not get the ServerLinkChangeId set
                foreach (LinkChangeAction linkChangeAction in linkChangeActions)
                {
                    if (linkChangeAction.ServerLinkChangeId == null)
                    {
                        string msg = String.Format(
                            "Unable to set ServerLinkChangeId on at least one LinkChangeAction: {0} '{1}'->'{2}' ({3})",
                            linkChangeAction.ChangeActionId == WellKnownChangeActionId.Add ? "Add" : "Delete",
                            linkChangeAction.Link.SourceArtifact.Uri, linkChangeAction.Link.TargetArtifact.Uri, linkChangeAction.Link.LinkType.ReferenceName);
                        TraceManager.TraceWarning(msg);
                        break;
                    }
                }
            }

            /*
            else
            {
                TraceManager.TraceVerbose(String.Format("SetServerLinkChangeIds set values for {0} link actions ending with RowNumber {1}",
                    serverLinkChangeIdsSet, hwmSubmittedValue));
            }
            */

            m_hwmSubmittedLinkChangeId.Update(hwmSubmittedValue);
            
            TraceManager.TraceInformation("Persisted WIT linking sequence HWM: {0} (MigrationSourceId: {1})",
                TfsConstants.HwmSubmittedLinkChangeId, m_hwmSubmittedLinkChangeId.SourceUniqueId.ToString());
            TraceManager.TraceInformation(TfsWITAdapterResources.UpdatedHighWatermark, hwmSubmittedValue);
        }

        private int GetLowerWorkItemId(LinkChangeAction action)
        {
            int sourceId = int.Parse(action.Link.SourceArtifactId);
            int targetId = int.Parse(TfsWorkItemHandler.IdFromUri(action.Link.TargetArtifact.Uri));
            return Math.Min(sourceId, targetId);
        }

        private int GetLowerWorkItemId(WorkItemLinkChange linkChange)
        {
            return Math.Min(linkChange.SourceID, linkChange.TargetID);
        }

        private bool LinkChangeMatchesLinkAction(WorkItemLinkChange linkChange, LinkChangeAction linkChangeAction)
        {
            // bool actionIsAdd = (linkChangeAction.ChangeActionId == WellKnownChangeActionId.Add);
            if (// Don't match on Add/Delete against IsActive because GetWorkItemLinkChanges() combines add/delete and IsActive is just latest status: 
                // actionIsAdd == linkChange.IsActive &&
                string.Equals(linkChange.LinkType, linkChangeAction.Link.LinkType.ReferenceName, StringComparison.Ordinal))
            {
                string linkActionTargetIdString = TfsWorkItemHandler.IdFromUri(linkChangeAction.Link.TargetArtifact.Uri);

                if (string.Equals(linkChange.SourceID.ToString(), linkChangeAction.Link.SourceArtifactId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(linkChange.TargetID.ToString(), linkActionTargetIdString, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (string.Equals(linkChange.SourceID.ToString(), linkActionTargetIdString, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(linkChange.TargetID.ToString(), linkChangeAction.Link.SourceArtifactId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// This method is called when an attempt to add a link in the force sync case fails because a parent link already exists
        /// In this case we attempt to delete the existing parent link and retry the add link operation
        /// </summary>
        /// <param name="linkChangeAction">The LinkChangeAction object</param>
        /// <param name="updateDocumentThatFailed">The XmlDocument that was submitted to add the link that resulted in the error</param>
        /// <returns>True if successful at removing the existing parent and adding the new parent; false if not</returns>
        private bool DeleteExistingParentLinkToForceSyncAddLink(LinkChangeAction linkChangeAction, XmlDocument updateDocumentThatFailed)
        {
            // Get target work item ...
            string targetItem = TfsWorkItemHandler.IdFromUri(linkChangeAction.Link.TargetArtifact.Uri);

            WorkItem targetWorkItem = null;
            try
            {
                int targetWorkItemId = int.Parse(targetItem);
                targetWorkItem = base.WorkItemStore.GetWorkItem(targetWorkItemId);
            }
            catch (FormatException)
            {
            }
            catch (DeniedOrNotExistException)
            {
            }

            if (targetWorkItem != null)
            {
                // Delete its parent link ...
                string linkType = linkChangeAction.Link.LinkType.ReferenceName;
                WorkItemLink linkToRemove = null;
                foreach (WorkItemLink link in targetWorkItem.WorkItemLinks)
                {
                    if (!link.LinkTypeEnd.IsForwardLink &&
                        string.Equals(link.LinkTypeEnd.LinkType.ReferenceName, linkType))
                    {
                        linkToRemove = link;
                        break;
                    }
                }
                if (linkToRemove != null)
                {
                    try
                    {
                        // Create a delete LinkChangeAction by copying from the add LinkChangeAction and
                        // changing the SourceId which is the parent in the add parent link case.
                        LinkChangeAction deletelinkChangeAction = new LinkChangeAction(WellKnownChangeActionId.Delete, linkChangeAction.Link, linkChangeAction.Status, false);
                        deletelinkChangeAction.Link.SourceArtifact.Uri = TfsWorkItemHandler.UriFromId(linkToRemove.TargetId.ToString());
                        TfsUpdateDocument tfsUpdateDocument = InitializeUpdateDocument();

                        var handler = deletelinkChangeAction.Link.LinkType as ILinkHandler;
                        Debug.Assert(null != handler);

                        handler.UpdateTfs(tfsUpdateDocument, deletelinkChangeAction);
                        XmlDocument[] xmlUpdateDocuments = new XmlDocument[] { tfsUpdateDocument.UpdateDocument };

                        UpdateResult[] deleteExistingParentResults = TfsBatchUpdateHelper.Submit(Core, WorkItemServer, xmlUpdateDocuments);
                        Debug.Assert(deleteExistingParentResults.Length == 1);
                        if (deleteExistingParentResults[0].Exception != null)
                        {
                            throw deleteExistingParentResults[0].Exception;
                        }
                    }
                    catch (Exception ex)
                    {
                        TraceManager.TraceWarning(String.Format(CultureInfo.InvariantCulture,
                            "An exception occurred while trying to remove the parent link to force sync a link change to target work item {0}: {1}",
                            targetWorkItem.Id, ex.ToString()));
                        targetWorkItem = null;
                    }

                    if (targetWorkItem != null)
                    {
                        // Retry the operation
                        XmlDocument[] retryDocuments = new XmlDocument[1] { updateDocumentThatFailed };
                        UpdateResult[] retryResults = TfsBatchUpdateHelper.Submit(Core, WorkItemServer, retryDocuments);
                        if (retryResults.Length != retryDocuments.Length)
                        {
                            throw new SynchronizationEngineException("Wrong number of link update results.");
                        }

                        if (retryResults[0].Exception == null)
                        {
                            // Successfully retried after removing existing parent; don't create conflict
                            return true;
                        }
                        else
                        {
                            TraceManager.TraceWarning(String.Format(CultureInfo.InvariantCulture,
                                "An exception occurred while trying to force sync a link change to target work item {0}: {1}",
                                targetWorkItem.Id, retryResults[0].Exception.ToString()));
                        }
                    }
                }
            }
            return false;
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