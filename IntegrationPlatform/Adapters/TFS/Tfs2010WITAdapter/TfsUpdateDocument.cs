// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    public partial class TfsUpdateDocument
    {
        private TfsServerDataIntegrityChecker ServerDataIntegrityChecker { get; set; }
        private TfsMigrationWorkItemStore MigrationWorkItemStore { get; set; }
        private bool ByPassrules { get; set; }
        public XmlDocument UpdateDocument { get; set; }

        public TfsUpdateDocument(TfsMigrationWorkItemStore tfsMigrationWorkItemStore)
        {
            MigrationWorkItemStore = tfsMigrationWorkItemStore;
            ByPassrules = MigrationWorkItemStore.ByPassrules;
            ServerDataIntegrityChecker = new TfsServerDataIntegrityChecker();
        }

        public virtual void AddExternalLink(
            string linkType,
            string location,
            string comment)
        {
            if (string.IsNullOrEmpty(linkType))
            {
                throw new ArgumentNullException("linkType");
            }
            if (string.IsNullOrEmpty(location))
            {
                throw new ArgumentNullException("location");
            }

            Debug.Assert(null != UpdateDocument);

            XmlElement e = UpdateDocument.CreateElement("InsertResourceLink");
            e.SetAttribute("FieldName", "System.BISLinks");
            e.SetAttribute("LinkType", linkType);
            e.SetAttribute("Location", location);
            if (!string.IsNullOrEmpty(comment))
            {
                e.SetAttribute("Comment", comment);
            }
            UpdateDocument.FirstChild.AppendChild(e);
        }

        public virtual XmlDocument CreateWorkItemUpdateDoc(
            WorkItem workItem)
        {
            if (null == workItem)
            {
                throw new ArgumentNullException("workItem");
            }

            WorkItem = workItem;

            return CreateWorkItemUpdateDoc(workItem.Id.ToString(), workItem.Rev.ToString());
        }

        public virtual XmlDocument CreateWorkItemUpdateDoc(
            string workItemId,
            string workItemRevision)
        {
            UpdateDocument = new XmlDocument();
            XmlElement e = UpdateDocument.CreateElement("UpdateWorkItem");
            UpdateDocument.AppendChild(e);
            e.SetAttribute("ObjectType", "WorkItem");
            e.SetAttribute("BypassRules", ByPassrules ? "1" : "0");
            e.SetAttribute("WorkItemID", workItemId);
            e.SetAttribute("Revision", workItemRevision);

            return UpdateDocument;
        }
        
        public virtual XmlDocument CreateWorkItemInsertDoc()
        {
            UpdateDocument = new XmlDocument();
            XmlElement e = UpdateDocument.CreateElement("InsertWorkItem");
            UpdateDocument.AppendChild(e);
            e.SetAttribute("ObjectType", "WorkItem");
            e.SetAttribute("BypassRules", ByPassrules ? "1" : "0");
            return UpdateDocument;
        }

        public virtual void AddAttachment(
            string originalName,
            string fileName,
            string utcCreationDate,
            string utcLastWriteDate,
            string fileLength,
            string comment)
        {
            XmlElement insertElement = UpdateDocument.CreateElement("InsertFile");
            insertElement.SetAttribute("FieldName", "System.AttachedFiles");
            insertElement.SetAttribute("OriginalName", originalName);
            insertElement.SetAttribute("FileName", fileName);
            insertElement.SetAttribute("CreationDate", utcCreationDate);
            insertElement.SetAttribute("LastWriteDate", utcLastWriteDate);
            insertElement.SetAttribute("FileSize", fileLength);
            XmlElement ec = UpdateDocument.CreateElement("Comment");
            ec.SetAttribute("xml:space", "preserve");
            ec.InnerText = comment;
            insertElement.AppendChild(ec);
            UpdateDocument.FirstChild.AppendChild(insertElement);
        }

        public virtual void RemoveAttachment(int fileId)
        {
            XmlElement de = UpdateDocument.CreateElement("RemoveFile");
            de.SetAttribute("FileID", XmlConvert.ToString(fileId));
            UpdateDocument.FirstChild.AppendChild(de);
        }

        public virtual void AddWorkItemLink(
            string targetArtifactId, 
            string comment)
        {
            if (string.IsNullOrEmpty(targetArtifactId))
            {
                throw new ArgumentNullException("targetArtifactId");
            }

            Debug.Assert(null != UpdateDocument);

            XmlElement e = UpdateDocument.CreateElement("CreateRelation");
            e.SetAttribute("WorkItemID", targetArtifactId);
            if (!string.IsNullOrEmpty(comment))
            {
                e.SetAttribute("Comment", comment);
            }
            UpdateDocument.FirstChild.AppendChild(e);
        }

        public virtual void RemoveWorkItemLink(
            string targetArtifactId)
        {
            if (string.IsNullOrEmpty(targetArtifactId))
            {
                throw new ArgumentNullException("targetArtifactId");
            }

            Debug.Assert(null != UpdateDocument);

            XmlElement e = UpdateDocument.CreateElement("RemoveRelation");
            e.SetAttribute("WorkItemID", targetArtifactId);
            UpdateDocument.FirstChild.AppendChild(e);
        }

        public virtual void AddHyperLink(
            string location, 
            string comment)
        {
            if (string.IsNullOrEmpty(location))
            {
                throw new ArgumentNullException("location");
            }

            Debug.Assert(null != UpdateDocument);

            XmlElement e = UpdateDocument.CreateElement("InsertResourceLink");
            e.SetAttribute("FieldName", "System.LinkedFiles");
            e.SetAttribute("Location", location);
            if (!string.IsNullOrEmpty(comment))
            {
                e.SetAttribute("Comment", comment);
            }
            UpdateDocument.FirstChild.AppendChild(e);
        }

        public virtual void AddFields(
            IMigrationAction action,
            string workItemType,
            string author,
            string changedTime,
            bool insertWorkItem)
        {
            ServerDataIntegrityChecker.InitializeForUpdateAnalysis(workItemType, author);

            bool hasArea = false;
            bool hasIteration = false;
            XmlDocument desc = action.MigrationActionDescription;

            XmlElement cs = UpdateDocument.CreateElement("Columns");
            UpdateDocument.FirstChild.AppendChild(cs);
            XmlNodeList columns = desc.SelectNodes("/WorkItemChanges/Columns/Column");
            if (null == columns)
            {
                throw new MigrationException(TfsWITAdapterResources.ErrorInvalidActionDescription,
                                             action.ActionId);
            }

            foreach (XmlNode columnData in columns)
            {
                string fieldName = columnData.Attributes["DisplayName"].Value;
                string fieldReferenceName = columnData.Attributes["ReferenceName"].Value;
                string stringVal = columnData.FirstChild.InnerText;
                string fieldType = columnData.Attributes["Type"].Value;

                if (fieldReferenceName.Equals(CoreFieldReferenceNames.AreaPath)
                    || fieldReferenceName.Equals(CoreFieldReferenceNames.IterationPath))
                {
                    GetCSSNodeId(action, workItemType,
                                 ref fieldName, ref fieldReferenceName, ref stringVal, ref hasIteration, ref hasArea); ;
                }

                while (true)
                {
                    try
                    {
                        AddColumn(cs, workItemType, fieldName, fieldReferenceName, stringVal);
                        break;
                    }
                    catch (FieldNotExistException)
                    {
                        string sourceItemId = TfsMigrationWorkItemStore.GetSourceWorkItemId(action);
                        string sourceItemRevision = TfsMigrationWorkItemStore.GetSourceWorkItemRevision(action);
                        WorkItemType wit = MigrationWorkItemStore.GetWorkItemType(workItemType);

                        MigrationConflict conflict = new InvalidFieldConflictType().CreateConflict(
                            InvalidFieldConflictType.CreateConflictDetails(sourceItemId, sourceItemRevision, fieldReferenceName, wit),
                            InvalidFieldConflictType.CreateScopeHint(wit.Project.Name, wit.Name),
                            action);

                        var conflictMgrService = MigrationWorkItemStore.ServiceContainer.GetService(typeof(ConflictManager)) as ConflictManager;
                        Debug.Assert(null != conflictMgrService, "cannot get conflict management service.");

                        List<MigrationAction> actions;
                        ConflictResolutionResult resolutionRslt =
                            conflictMgrService.TryResolveNewConflict(conflictMgrService.SourceId, conflict, out actions);

                        if (!resolutionRslt.Resolved)
                        {
                            throw new MigrationUnresolvedConflictException();
                        }

                        if (resolutionRslt.ResolutionType == ConflictResolutionType.UpdatedConflictedChangeAction)
                        {
                            XmlDocument migrationDesc = conflict.ConflictedChangeAction.MigrationActionDescription;
                            XmlNode column = migrationDesc.SelectSingleNode(string.Format(
                                @"/WorkItemChanges/Columns/Column[@ReferenceName=""{0}""]", fieldReferenceName));

                            if (null == column)
                            {
                                // field has been dropped by the resolution rule
                                break;
                            }

                            fieldReferenceName = column.Attributes["ReferenceName"].Value;
                            fieldName = string.Empty;
                        }
                        else
                        {
                            // unrecognized resolution action was taken
                            throw new MigrationUnresolvedConflictException();
                        }
                    }
                }
            }

            if (insertWorkItem)
            {
                if (!hasArea)
                //if (!hasArea && !ByPassrules)
                {
                    AddColumn(cs, workItemType, string.Empty, CoreFieldReferenceNames.AreaId,
                              MigrationWorkItemStore.Core.DefaultAreaId.ToString());
                }
                if (!hasIteration)
                //if (!hasIteration && !ByPassrules)
                {
                    AddColumn(cs, workItemType, string.Empty, CoreFieldReferenceNames.IterationId,
                              MigrationWorkItemStore.Core.DefaultIterationId.ToString());
                }

                AddColumn(cs, workItemType, string.Empty, CoreFieldReferenceNames.WorkItemType, workItemType);
                AddColumn(cs, workItemType, string.Empty, CoreFieldReferenceNames.CreatedDate, string.Empty);
                AddColumn(cs, workItemType, string.Empty, CoreFieldReferenceNames.CreatedBy, author);
                AddColumn(cs, workItemType, string.Empty, CoreFieldReferenceNames.ChangedBy, author);

                foreach (string missingField in ServerDataIntegrityChecker.MissingFields)
                {
                    AddColumn(cs, workItemType, string.Empty, missingField, ServerDataIntegrityChecker[missingField]);
                }
            }
            else
            {
                AddColumn(cs, workItemType, string.Empty, CoreFieldReferenceNames.ChangedBy, author);
            }
        }

        internal void IncrementRevision()
        {
            Debug.Assert(null != UpdateDocument, "UpdateDocument is null");
            XmlAttribute revisionAttr = UpdateDocument.DocumentElement.Attributes["Revision"];
            if (null != revisionAttr && !string.IsNullOrEmpty(revisionAttr.Value))
            {
                int currentRev;
                if (int.TryParse(revisionAttr.Value, out currentRev))
                {
                    revisionAttr.Value = (currentRev + 1).ToString();
                }
            }
        }

        private void GetCSSNodeId(
            IMigrationAction action,
            string workItemType,
            ref string fieldName,
            ref string fieldReferenceName,
            ref string stringVal,
            ref bool hasIteration,
            ref bool hasArea)
        {
            string fieldNameBeforeConversion = fieldName;
            string fieldRefNameBeforeConversion = fieldReferenceName;

            try
            {
                if (fieldRefNameBeforeConversion.Equals(CoreFieldReferenceNames.AreaPath))
                {
                    // Substitute AreaPath with AreaId
                    fieldName = string.Empty;
                    fieldReferenceName = CoreFieldReferenceNames.AreaId;
                    stringVal = MigrationWorkItemStore.Core.TranslatePath(Node.TreeType.Area, stringVal).ToString();
                    hasArea = true;
                }
                else if (fieldRefNameBeforeConversion.Equals(CoreFieldReferenceNames.IterationPath))
                {
                    // Substitute IterationPath with IterationId
                    fieldName = string.Empty;
                    fieldReferenceName = CoreFieldReferenceNames.IterationId;
                    stringVal = MigrationWorkItemStore.Core.TranslatePath(Node.TreeType.Iteration, stringVal).ToString();
                    hasIteration = true;
                }
            }
            catch (MissingPathException)
            {
                WorkItemType wit = MigrationWorkItemStore.GetWorkItemType(workItemType);
                FieldDefinition fd = wit.FieldDefinitions[fieldNameBeforeConversion];

                MigrationConflict conflict = new InvalidFieldValueConflictType().CreateConflict(
                    InvalidFieldValueConflictType.CreateConflictDetails(action.FromPath, action.Version, fd.ReferenceName, fd.Name,
                                                                        string.Empty, stringVal, wit.Project.Name,
                                                                        wit.Name, wit.Store.TeamProjectCollection.Uri.AbsoluteUri),
                    InvalidFieldValueConflictType.CreateScopeHint(wit.Project.Name, wit.Name, fd.ReferenceName),
                    action);

                var conflictMgrService = MigrationWorkItemStore.ServiceContainer.GetService(typeof(ConflictManager)) as ConflictManager;
                Debug.Assert(null != conflictMgrService, "cannot get conflict management service.");

                List<MigrationAction> rsltActions;
                ConflictResolutionResult result = conflictMgrService.TryResolveNewConflict(
                    conflictMgrService.SourceId, conflict, out rsltActions);

                if (result.Resolved &&
                    result.ResolutionType == ConflictResolutionType.UpdatedConflictedChangeAction)
                {
                    // extract the mapped value in the action description doc
                    // and apply to the field
                    Debug.Assert(null != conflict.ConflictedChangeAction,
                                 "Invalid field value conflict does not contain a conflicted change action.");

                    XmlDocument mappedChangeData = conflict.ConflictedChangeAction.MigrationActionDescription;
                    XmlNode fieldCol = mappedChangeData.SelectSingleNode(
                        string.Format("/WorkItemChanges/Columns/Column[@ReferenceName='{0}']",
                                      fd.ReferenceName));

                    stringVal = fieldCol.FirstChild.InnerText;
                    GetCSSNodeId(action, workItemType,
                                 ref fieldNameBeforeConversion, ref fieldNameBeforeConversion, ref stringVal, ref hasIteration, ref hasArea);
                }
                else
                {
                    // we reach here because:
                    //  1. the conflict is not resolved
                    //  2. the conflict resolution action is unknown
                    throw new MigrationUnresolvedConflictException();
                }
            }
        }

        public virtual void InsertConversionHistoryField(
            string workItemType,
            string reflectedWorkItemId)
        {
            Debug.Assert(!string.IsNullOrEmpty(workItemType));

            if (string.IsNullOrEmpty(reflectedWorkItemId) || !MigrationWorkItemStore.EnableInsertReflectedWorkItemId)
            {
                return;
            }

            FieldDefinition fd = TryGetFieldDefinition(workItemType, ReflectedWorkItemIdFieldReferenceName);
            if (fd == null)
            {
                TraceManager.TraceInformation(
                    "WorkItem type '{0}' does not contain field '{1}'. Writing source item Id will be skipped.",
                    workItemType,
                    ReflectedWorkItemIdFieldReferenceName);
                return;
            }
            
            if (!fd.FieldType.Equals(TfsConstants.MigrationTracingFieldType))
            {
                TraceManager.TraceInformation(
                    "The field '{0}' is not defined with type '{1}'. Writing source item Id will be skipped.",
                    ReflectedWorkItemIdFieldReferenceName,
                    TfsConstants.MigrationTracingFieldType.ToString());
                return;
            }

            XmlElement cs = UpdateDocument.SelectSingleNode("//Columns") as XmlElement;
            Debug.Assert(null != cs);
            AddColumn(cs, workItemType,
                      string.Empty,
                      ReflectedWorkItemIdFieldReferenceName,
                      reflectedWorkItemId);
        }

        public virtual void InsertConversionHistoryCommentToHistory(
            string workItemType,
            string convHistComment)
        {
            Debug.Assert(null != UpdateDocument);

            FieldDefinition fd = TryGetFieldDefinition(workItemType, CoreFieldReferenceNames.History);
            if (fd == null)
            {
                throw new MigrationException("Field {0} is not defined in the Work Item Type {1}",
                                             CoreFieldReferenceNames.History, workItemType);
            }

            XmlNode root = UpdateDocument.FirstChild;
            XmlNode historyColNode = root.SelectSingleNode(string.Format("//InsertText[@FieldName='{0}']", fd.ReferenceName));
            if (null == historyColNode)
            {
                historyColNode = AddLargeTextColumn(root, fd, string.Empty);
            }

            string convHistCommentStr = string.IsNullOrEmpty(historyColNode.InnerText) ? convHistComment : "\r\n<p>" + convHistComment + "</p>";
                                        
            historyColNode.InnerText = historyColNode.InnerText + convHistCommentStr;
        }

        internal WorkItem WorkItem
        {
            get;
            private set;
        }

        private string ReflectedWorkItemIdFieldReferenceName
        {
            get
            {
                return MigrationWorkItemStore.ReflectedWorkItemIdFieldReferenceName;
            }
        }

        private XmlNode AddLargeTextColumn(
            XmlNode parentNode,
            FieldDefinition fieldDefinition,
            string fieldValue)
        {
            XmlElement e = parentNode.OwnerDocument.CreateElement("InsertText");

            e.SetAttribute("FieldName", fieldDefinition.ReferenceName);
            e.SetAttribute("FieldDisplayName", fieldDefinition.Name);
            e.InnerText = fieldValue;
            parentNode.AppendChild(e);

            return e;
        }

        private void AddColumn(
            XmlElement cs, 
            string workItemType, 
            string fieldName, 
            string fieldReferenceName, 
            string stringVal)
        {
            FieldDefinition fd = TryGetFieldDefinition(workItemType, fieldReferenceName);

            if (fd == null)
            {
                throw new FieldNotExistException();
            }

            if (fd.FieldType == FieldType.Html
                || fd.FieldType == FieldType.PlainText
                || fd.FieldType == FieldType.History)
            {
                // Large text are different
                AddLargeTextColumn(cs.ParentNode, fd, stringVal);
            }
            else
            {
                string typeName;
                if (fd.ReferenceName.Equals(CoreFieldReferenceNames.CreatedDate))
                {
                    typeName = "ServerDateTime";
                    stringVal = string.Empty;
                }
                else
                {
                    switch (fd.FieldType)
                    {
                        case FieldType.Integer:
                            typeName = "Number";
                            break;
                        case FieldType.Double:
                            typeName = "Double";
                            if (!string.IsNullOrEmpty(stringVal))
                            {
                                stringVal = XmlConvert.ToString((double)Convert.ToDouble(stringVal));
                            }
                            break;
                        case FieldType.DateTime:
                            typeName = "DateTime";
                            if (!string.IsNullOrEmpty(stringVal))
                            {
                                stringVal = XmlConvert.ToString(Convert.ToDateTime(stringVal).ToUniversalTime(),
                                                                XmlDateTimeSerializationMode.Unspecified);
                            }
                            break;

                        default:
                            Debug.Assert(fd.FieldType == FieldType.String, "Unsupported field type!");
                            typeName = null;
                            break;
                    }
                }

                XmlElement c = cs.OwnerDocument.CreateElement("Column");
                c.SetAttribute("Column", fieldReferenceName);
                if (!string.IsNullOrEmpty(typeName))
                {
                    c.SetAttribute("Type", typeName);
                }
                XmlElement v = cs.OwnerDocument.CreateElement("Value");
                v.InnerText = stringVal;
                c.AppendChild(v);
                cs.AppendChild(c);
            }

            ServerDataIntegrityChecker.RecordUpdatedField(fieldReferenceName);
        }

        private FieldDefinition TryGetFieldDefinition(
            string workItemType,
            string fieldReferenceName)
        {
            if (!MigrationWorkItemStore.WorkItemStore.Projects.Contains(MigrationWorkItemStore.Core.Config.Project))
            {
                return null;
            }
            Project p = MigrationWorkItemStore.WorkItemStore.Projects[MigrationWorkItemStore.Core.Config.Project];

            if (!p.WorkItemTypes.Contains(workItemType))
            {
                return null;
            }
            WorkItemType type = p.WorkItemTypes[workItemType];

            if (!type.FieldDefinitions.Contains(fieldReferenceName))
            {
                return null;
            }

            return type.FieldDefinitions[fieldReferenceName];
        }

        internal void DeleteExternalLink(int extId)
        {
            // Hyperlink ot ExternalLink
            XmlElement e = UpdateDocument.CreateElement("RemoveResourceLink");
            e.SetAttribute("LinkID", XmlConvert.ToString(extId));
            UpdateDocument.FirstChild.AppendChild(e);
        }
    }
}