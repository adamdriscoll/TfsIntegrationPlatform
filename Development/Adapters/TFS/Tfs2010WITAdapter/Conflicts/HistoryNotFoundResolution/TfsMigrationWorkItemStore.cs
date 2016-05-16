// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts.HistoryNotFoundResolution;
using System.Xml;
using System.Diagnostics;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    public partial class TfsMigrationWorkItemStore
    {
        private WorkItem m_targetWorkItem = null;
        private WorkItem TargetWorkItem
        {
            get
            {
                if (null == m_targetWorkItem)
                {
                    if (TargetWorkItemId > 0)
                    {
                        m_targetWorkItem = this.WorkItemStore.GetWorkItem(TargetWorkItemId);
                    }
                }
                return m_targetWorkItem;
            }
        }
        internal int TargetWorkItemId { get; set; }

        internal void SubmitChanges(MigrationAction[] changeGroup, ConversionResult changeResult)
        {
            SubmitChangesWithUpdateDoc(changeGroup, changeResult);
        }

        private void SubmitChangesWithUpdateDoc(
            MigrationAction[] changeGroup,
            ConversionResult changeResult)
        {
            // NOTE / TODO:
            //   Currently, work item revisions are submitted separately. To minimize server round-trips for
            //   performance improvement, we may want to submit changes in batch.
            foreach (MigrationAction action in changeGroup)
            {
                try
                {
                    XmlDocument updateDocument = CreateUpdateOperationDoc(action);

                    if (updateDocument == null)
                    {
                        throw new InvalidOperationException("updateDocument is null");
                    }

                    var updates = new XmlDocument[1] { updateDocument };
                    UpdateResult[] results = TfsBatchUpdateHelper.Submit(Core, WorkItemServer, updates);

                    if (results.Length != updates.Length)
                    {
                        string msg = string.Format(
                            TfsWITAdapterResources.Culture,
                            TfsWITAdapterResources.ErrorWrongNumberOfUpdateResults,
                            Core.ServerName,
                            StoreName,
                            updates.Length,
                            results.Length);
                        throw new Exception(msg);
                    }

                    for (int i = 0; i < results.Length; ++i)
                    {
                        UpdateResult rslt = results[i];

                        if (rslt.Exception != null)
                        {
                            throw rslt.Exception;
                        }
                        else
                        {
                            //TODO: Update watermark on pending update statements
                            UpdateConversionHistory(action, rslt.Watermark, changeResult);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new MigrationException(changeResult, ex);
                }
            }
        }

        private XmlDocument CreateUpdateOperationDoc(
            MigrationAction action)
        {
            XmlDocument updateDocument = null;
            if (action.ChangeActionId == TfsConstants.ChangeActionId.Add)
            {
                updateDocument = CreateNewWorkItemOperation(action);
            }
            else if (action.ChangeActionId == TfsConstants.ChangeActionId.Edit)
            {
                FindTargetWorkItemLatestRevision(action);
                updateDocument = CreateWorkItemUpdateOperation(action);
            }

            return updateDocument;
        }


        private void UpdateConversionHistory(
            MigrationAction action,
            Watermark targetItemWatermark,
            ConversionResult changeResult)
        {
            TargetWorkItemId = int.Parse(targetItemWatermark.Id);
            string sourceWorkItemRevision = GetSourceWorkItemRevision(action);

            // update work item mapping cache
            if (!m_mappedWorkItem.ContainsKey(action.SourceWorkItemId))
            {
                m_mappedWorkItem.Add(action.SourceWorkItemId, TargetWorkItemId);
            }

            // insert conversion history for pushing to db
            changeResult.ItemConversionHistory.Add(
                new ItemConversionHistory(action.SourceWorkItemId, sourceWorkItemRevision, targetItemWatermark.Id,
                                          targetItemWatermark.Revision.ToString()));
            changeResult.ChangeId = targetItemWatermark.Id + ":" + targetItemWatermark.Revision;
        }

        private XmlDocument CreateNewWorkItemOperation(MigrationAction action)
        {
            XmlDocument desc = action.RecordDetails.DetailsDocument;
            XmlElement rootNode = desc.DocumentElement;
            Debug.Assert(null != rootNode,
                        "Wit IMigrationAction.MigrationActionDescription is invalid.");
            Debug.Assert(null != rootNode.Attributes["WorkItemType"],
                        "WorkItemType is missing in MigrationActionDescription.");
            Debug.Assert(null != rootNode.Attributes["Author"],
                        "Author is missing in MigrationActionDescription.");
            Debug.Assert(null != rootNode.Attributes["ChangeDate"],
                        "ChangeDate is missing in MigrationActionDescription.");

            string workItemType = rootNode.Attributes["WorkItemType"].Value;
            string author = rootNode.Attributes["Author"].Value;
            string changedDate = rootNode.Attributes["ChangeDate"].Value;


            TfsUpdateDocument tfsUpdateDocument = InitializeUpdateDocument();

            tfsUpdateDocument.CreateWorkItemInsertDoc();
            tfsUpdateDocument.AddFields(action, workItemType, author, changedDate, true);

            // append a tracing comment to System.History
            tfsUpdateDocument.InsertConversionHistoryCommentToHistory(
                workItemType,
                GenerateMigrationHistoryComment(action));

            // insert source item Id to field TfsMigrationTool.ReflectedWorkItemId if it is in WITD
            tfsUpdateDocument.InsertConversionHistoryField(workItemType, action.SourceWorkItemId);

            return tfsUpdateDocument.UpdateDocument;
        }

        private string GenerateMigrationHistoryComment(MigrationAction action)
        {
            return string.Empty;
        }

        private XmlDocument CreateWorkItemUpdateOperation(MigrationAction action)
        {
            XmlDocument desc = action.RecordDetails.DetailsDocument;
            XmlElement rootNode = desc.DocumentElement;
            Debug.Assert(null != rootNode,
                        "Wit IMigrationAction.MigrationActionDescription is invalid.");
            Debug.Assert(null != rootNode.Attributes["WorkItemType"],
                        "WorkItemType is missing in MigrationActionDescription.");
            Debug.Assert(null != rootNode.Attributes["Author"],
                        "Author is missing in MigrationActionDescription.");
            Debug.Assert(null != rootNode.Attributes["ChangeDate"],
                        "ChangeDate is missing in MigrationActionDescription.");
            string workItemType = rootNode.Attributes["WorkItemType"].Value;
            string author = rootNode.Attributes["Author"].Value;
            string changeDate = rootNode.Attributes["ChangeDate"].Value;
            string targetRevision = rootNode.Attributes["TargetRevision"].Value;

            TfsUpdateDocument tfsUpdateDocument = InitializeUpdateDocument();
            tfsUpdateDocument.CreateWorkItemUpdateDoc(TargetWorkItem.Id.ToString(), targetRevision);
            tfsUpdateDocument.AddFields(action, workItemType, author, changeDate, false);

            tfsUpdateDocument.InsertConversionHistoryCommentToHistory(
                workItemType,
                GenerateMigrationHistoryComment(action));

            // insert source item Id to field TfsMigrationTool.ReflectedWorkItemId if it is in WITD
            tfsUpdateDocument.InsertConversionHistoryField(
                workItemType,
                action.SourceWorkItemId);

            return tfsUpdateDocument.UpdateDocument;
        }

        private void FindTargetWorkItemLatestRevision(
            MigrationAction action)
        {
            TargetWorkItem.Reset();
            TargetWorkItem.SyncToLatest();

            XmlDocument desc = action.RecordDetails.DetailsDocument;
            XmlElement rootNode = desc.DocumentElement;
            Debug.Assert(null != rootNode);
            rootNode.SetAttribute("TargetRevision", XmlConvert.ToString(TargetWorkItem.Rev));
        }

        internal string GetSourceWorkItemRevision(
            MigrationAction action)
        {
            XmlAttribute workItemRevAttr = action.RecordDetails.DetailsDocument.DocumentElement.Attributes["Revision"];
            return workItemRevAttr == null ? string.Empty : workItemRevAttr.Value;
        }

    }
}
