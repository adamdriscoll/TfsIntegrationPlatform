// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Xml;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Proxy;
using WIT = Microsoft.TeamFoundation.Migration.BusinessModel.WIT;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    /// <summary>
    /// Describes kinds of work item's data
    /// </summary>
    [Flags]
    public enum MigrationWorkItemData
    {
        None = 0,                               // No data
        Revisions = 1 << 0,                     // Historical revisions
        Attachments = 1 << 1,                   // File attachments
        Links = 1 << 2,                         // Links between work items
        All = Revisions | Attachments | Links,  // All known kinds
    }

    /// <summary>
    /// TFS work item.
    /// </summary>
    public partial class TfsMigrationWorkItem
    {
        public delegate bool IsWorkItemRevisionProcessed(int wiId, int rev);

        /// <summary>
        /// Returns underlying work item from the WIT OM.
        /// </summary>
        public WorkItem WorkItem { get { return m_workItem; } }

        /// <summary>
        /// Returns work item's URI.
        /// </summary>
        public string Uri { get { return m_workItem.Uri.ToString(); } }

        /// <summary>
        /// Returns id of the work item.
        /// </summary>
        public Watermark Watermark { get { return m_watermark; } }

        /// <summary>
        /// Gets name of the work item type.
        /// </summary>
        public string WorkItemType { get { return m_workItemType; } }

        /// <summary>
        /// Tells the engine what kind of data the work item has.
        /// </summary>
        public MigrationWorkItemData Flags { get { return m_flags; } }

        /// <summary>
        /// The list of link changes made to the associated work item sorted by time, 
        /// with a list of WorkItemLinkChange objects for each unique time in the SortedList
        /// </summary>
        public ICollection<WorkItemLinkChange> LinkChanges { get; set; }

        /// <summary>
        /// Gets the most recent values of the given fields.
        /// </summary>
        /// <param name="fieldNames">Names of fields</param>
        /// <returns>Values</returns>
        public IEnumerable<MigrationField> GetLatestValues(
            IEnumerable<string> fieldNames)
        {
            if (fieldNames == null)
            {
                throw new ArgumentNullException("fieldNames");
            }

            foreach (string fieldName in fieldNames)
            {
                Field f = m_workItem.Fields[fieldName];
                object value = TranslateFieldValue(f);
                yield return new MigrationField(fieldName, value);
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="core">Shared TFS core</param>
        /// <param name="item">TFS work item</param>
        /// <param name="fieldForm">Tells what field name should be used</param>
        public TfsMigrationWorkItem(
            TfsCore core,
            WorkItem item)
        {
            m_core = core;
            m_watermark = new Watermark(
                item.Id.ToString(),
                item.Rev);
            m_workItemType = (string)item.Fields[CoreField.WorkItemType].Value;
            if ((int)item[CoreField.AttachedFileCount] > 0)
            {
                m_flags |= MigrationWorkItemData.Attachments;
            }
            int linkCount = (int)item[CoreField.ExternalLinkCount] + (int)item[CoreField.RelatedLinkCount] + (int)item[CoreField.HyperLinkCount];
            if (linkCount > 0)
            {
                m_flags |= MigrationWorkItemData.Links;
            }
            m_workItem = item;
        }

        internal void ComputeAttachmentDelta(
            ChangeGroupService changeGroupService,
            DateTime waterMarkChangeStartTime,
            ITranslationService translationService,
            Guid sourceId,
            List<ChangeGroup> groups)
        {
            List<TfsMigrationFileAttachment> files = new List<TfsMigrationFileAttachment>();
            if (WorkItem.Attachments.Count > 0)
            {

                List<Revision> revsToBeSynced = FindUnsyncedRevisions(waterMarkChangeStartTime,
                                                                      translationService,
                                                                      sourceId,
                                                                      null);

                bool hasAttachmentChanges = false;
                foreach (Revision rev in revsToBeSynced)
                {
                    if (rev.Index == 0 &&
                        (int)rev.Fields[CoreField.AttachedFileCount].Value > 0)
                    {
                        hasAttachmentChanges = true;
                        break;
                    }

                    if (rev.Index > 0)
                    {
                        int currAttchCount = (int)rev.Fields[CoreField.AttachedFileCount].Value;
                        Revision prevRev = rev.WorkItem.Revisions[rev.Index - 1];
                        int prevAttchCount = (int)prevRev.Fields[CoreField.AttachedFileCount].Value;
                        if (currAttchCount != prevAttchCount)
                        {
                            hasAttachmentChanges = true;
                            break;
                        }
                    }
                }

                if (!hasAttachmentChanges)
                {
                    return;
                }

                foreach (Attachment attachment in WorkItem.Attachments)
                {
                    if (attachment.AttachedTimeUtc <= waterMarkChangeStartTime
                        || !attachment.IsSaved)
                    {
                        continue;
                    }
                    files.Add(new TfsMigrationFileAttachment(attachment, m_core.TfsTPC.Uri.AbsoluteUri));
                }
            }

            Guid changeActionId = WellKnownChangeActionId.AddAttachment;
            ChangeGroup changeGroup = changeGroupService.CreateChangeGroupForDeltaTable(
                string.Format("{0}:{1}", WorkItem.Id, "Attachments"));
            foreach (TfsMigrationFileAttachment attachmentFile in files)
            {
                XmlDocument migrationActionDetails = CreateAttachmentDescriptionDoc(attachmentFile, WorkItem.Rev.ToString());
                changeGroup.CreateAction(
                    changeActionId,
                    attachmentFile,
                    WorkItem.Id.ToString(),
                    "",
                    "0",
                    "",
                    WellKnownContentType.WorkItem.ReferenceName,
                    migrationActionDetails);
                TraceManager.TraceVerbose(String.Format("Generating AddAttachment change action: Work Item: {0}, Attachment File: {1}",
                    WorkItem.Id.ToString(), attachmentFile.Name));
            }

            // VERY IMPORTANT: use the RelatedArtifactsStore to detect detailed attachment changes
            WorkItemAttachmentStore store = new WorkItemAttachmentStore(sourceId);
            List<FileAttachmentMetadata> additionalAttachmentToDelete;
            store.UpdatePerItemAttachmentChangesByCheckingRelatedItemRecords(
                WorkItem.Id.ToString(), changeGroup, out additionalAttachmentToDelete);

            foreach (FileAttachmentMetadata attch in additionalAttachmentToDelete)
            {
                TfsMigrationFileAttachment attachmentFile = new TfsMigrationFileAttachment(attch, m_core.TfsTPC.Uri.AbsoluteUri);
                XmlDocument migrationActionDetails = CreateAttachmentDescriptionDoc(attachmentFile, WorkItem.Rev.ToString());
                changeGroup.CreateAction(
                    WellKnownChangeActionId.DelAttachment,
                    attachmentFile,
                    WorkItem.Id.ToString(),
                    "",
                    "0",
                    "",
                    WellKnownContentType.WorkItem.ReferenceName,
                    migrationActionDetails);
                TraceManager.TraceVerbose(String.Format("Generating DeleteAttachment change action: Work Item: {0}, Attachment File: {1}",
                    WorkItem.Id.ToString(), attachmentFile.Name));
            }

            groups.Add(changeGroup);
        }

        internal ChangeGroup GetChangeGroupForLatestAttachments(ChangeGroupService changeGroupService)
        {
            List<TfsMigrationFileAttachment> files = new List<TfsMigrationFileAttachment>();
            if (WorkItem.Attachments.Count == 0)
            {
                return null;
            }
            else
            {
                foreach (Attachment attachment in WorkItem.Attachments)
                {
                    files.Add(new TfsMigrationFileAttachment(attachment, m_core.TfsTPC.Uri.AbsoluteUri));
                }

                Guid changeActionId = WellKnownChangeActionId.AddAttachment;
                ChangeGroup changeGroup = changeGroupService.CreateChangeGroupForDeltaTable(
                    string.Format("{0}:{1}", WorkItem.Id, "Attachments"));
                foreach (TfsMigrationFileAttachment attachmentFile in files)
                {
                    XmlDocument migrationActionDetails = CreateAttachmentDescriptionDoc(attachmentFile, WorkItem.Rev.ToString());
                    changeGroup.CreateAction(
                        changeActionId,
                        attachmentFile,
                        WorkItem.Id.ToString(),
                        "",
                        "0",
                        "",
                        WellKnownContentType.WorkItem.ReferenceName,
                        migrationActionDetails);
                    TraceManager.TraceVerbose(String.Format("Generating AddAttachment change action: Work Item: {0}, Attachment File: {1}",
                        WorkItem.Id.ToString(), attachmentFile.Name));
                }

                return changeGroup;
            }
        }

        internal void ComputeFieldDelta(
            ChangeGroupService changeGroupService,
            DateTime waterMarkChangeStartTime,
            FieldValueComparer tfsValueComparer,
            ITranslationService translationService,
            ConfigurationService configService,
            List<ChangeGroup> groups,
            IsWorkItemRevisionProcessed processedRevCallBack)
        {
            Guid sourceId = configService.SourceId;

            List<Revision> revsToBeSynced = FindUnsyncedRevisions(waterMarkChangeStartTime,
                                                                  translationService,
                                                                  sourceId,
                                                                  processedRevCallBack);

            Dictionary<int, object> fieldValueBaseline = new Dictionary<int, object>();
            TryEstablishFieldValueBaseline(fieldValueBaseline, revsToBeSynced);

            foreach (Revision rev in revsToBeSynced)
            {
                // get basic revision info: revision#, author#, and change time
                int revIndex = (int)rev.Fields[CoreField.Rev].Value;
                string author = (string)rev.Fields[CoreField.ChangedBy].Value;
                if (string.IsNullOrEmpty(author))
                {
                    author = (string)rev.Fields[CoreField.AuthorizedAs].Value;
                }
                DateTime changedDate = (DateTime)rev.Fields[CoreField.ChangedDate].Value;

                List<Field> fieldForSync = new List<Field>();
                List<Field> skippingFields = new List<Field>();
                foreach (Field f in rev.Fields)
                {
                    // filter out System.History with empty new value
                    if (TFStringComparer.FieldName.Equals(f.ReferenceName, CoreFieldReferenceNames.History))
                    {
                        if (string.IsNullOrEmpty(f.Value as string))
                        {
                            continue;
                        }
                    }

                    if (MustTakeField(f.FieldDefinition))
                    {
                        // find out fields that changed in this revision
                        object oldValue;
                        fieldValueBaseline.TryGetValue(f.Id, out oldValue);
                        //***Note: When it is a new work item (unmatched on the other side), 
                        //         we need to always include fields
                        if ((null != f.Value && (!tfsValueComparer.Equals(oldValue, f.Value) || revIndex == 1))
                            || (null == f.Value && null != oldValue && revIndex != 1))
                        {
                            fieldForSync.Add(f);
                            fieldValueBaseline[f.Id] = f.Value;
                        }
                        else if (IsReferencedField(f, configService))
                        {
                            skippingFields.Add(f);
                        }
                    }
                    else if (IsReferencedField(f, configService))
                    {
                        skippingFields.Add(f);
                    }
                }

                if (fieldForSync.Count == 0)
                {
                    continue;
                }

                XmlDocument migrationActionDetails = CreateFieldRevisionDescriptionDoc(
                    revIndex, author, changedDate, fieldForSync, skippingFields);

                Guid changeActionId = revIndex == 1 ?
                    WellKnownChangeActionId.Add : WellKnownChangeActionId.Edit;

                /// TODO: consider batching revs of different workitems in one change group
                ChangeGroup changeGroup = changeGroupService.CreateChangeGroupForDeltaTable(
                    string.Format("{0}:{1}", WorkItem.Id, revIndex));
                IMigrationAction action = changeGroup.CreateAction(
                    changeActionId,
                    new TfsWITMigrationItem(WorkItem, revIndex),
                    WorkItem.Id.ToString(),
                    "",
                    revIndex.ToString(),
                    "",
                    WellKnownContentType.WorkItem.ReferenceName,
                    migrationActionDetails);

                groups.Add(changeGroup);
            }
        }

        internal ChangeGroup GetChangeGroupForLatestFieldValues(ChangeGroupService changeGroupService)
        {
            string author = (string)WorkItem.Fields[CoreField.ChangedBy].Value;
            if (string.IsNullOrEmpty(author))
            {
                author = (string)WorkItem.Fields[CoreField.AuthorizedAs].Value;
            }
            DateTime changedDate = (DateTime)WorkItem.Fields[CoreField.ChangedDate].Value;

            List<Field> fieldsForSync = new List<Field>();
            foreach (Field f in WorkItem.Fields)
            {
                // filter out System.History with empty new value
                if (TFStringComparer.FieldName.Equals(f.ReferenceName, CoreFieldReferenceNames.History))
                {
                    if (string.IsNullOrEmpty(f.Value as string))
                    {
                        continue;
                    }
                }

                if (MustTakeField(f.FieldDefinition))
                {
                    fieldsForSync.Add(f);
                }
            }

            if (fieldsForSync.Count == 0)
            {
                return null;
            }

            XmlDocument migrationActionDetails = CreateFieldRevisionDescriptionDoc(
                WorkItem.Rev, author, changedDate, fieldsForSync, new List<Field>());

            ChangeGroup changeGroup = changeGroupService.CreateChangeGroupForDeltaTable(
                string.Format("{0}:{1}", WorkItem.Id, WorkItem.Rev));
            IMigrationAction action = changeGroup.CreateAction(
                // Always generate Edit even for rev 1 in force sync case
                // Action will be changed to Add later if history not found for rev 1
                WellKnownChangeActionId.Edit,  
                new TfsWITMigrationItem(WorkItem, WorkItem.Rev),
                WorkItem.Id.ToString(),
                "",
                WorkItem.Rev.ToString(),
                "",
                WellKnownContentType.WorkItem.ReferenceName,
                migrationActionDetails);

            return changeGroup;
        }

        private bool IsReferencedField(Field f, ConfigurationService configService)
        {
            WIT.SourceSideTypeEnum side =
                configService.IsLeftSideInConfiguration ? WIT.SourceSideTypeEnum.Left : WIT.SourceSideTypeEnum.Right;
            Debug.Assert(null != configService.WitCustomSetting, "WitCustomSetting is NULL");
            List<string> referencedFieldNames = configService.WitCustomSetting.GetReferencedFieldReferenceNames(side);

            foreach (string fieldName in referencedFieldNames)
            {
                if (TFStringComparer.WorkItemFieldReferenceName.Equals(f.ReferenceName, fieldName))
                {
                    return true;
                }
            }
            return false;
        }

        private void TryEstablishFieldValueBaseline(Dictionary<int, object> fieldValueBaseline, List<Revision> revsToBeSynced)
        {
            foreach (Revision firstUnsyncedRev in revsToBeSynced)
            {
                int firstUnsyncedRevIndex = (int)firstUnsyncedRev.Fields[CoreField.Rev].Value;
                if (firstUnsyncedRevIndex == 1)
                {
                    break;
                }

                foreach (Field f in firstUnsyncedRev.WorkItem.Revisions[firstUnsyncedRevIndex - 2].Fields)
                {
                    fieldValueBaseline.Add(f.Id, f.Value);
                }
                break;
            }
        }

        private XmlDocument CreateFieldRevisionDescriptionDoc(
            int revIndex, string author, DateTime changedDate,
            List<Field> fieldForSync, List<Field> skippingFields)
        {
            XmlDocument migrationActionDetails = new XmlDocument();
            XmlElement root = migrationActionDetails.CreateElement("WorkItemChanges");
            root.SetAttribute("WorkItemID", XmlConvert.ToString(WorkItem.Id));
            root.SetAttribute("Revision", XmlConvert.ToString(revIndex));
            root.SetAttribute("WorkItemType", WorkItem.Type.Name);
            root.SetAttribute("Author", author);
            root.SetAttribute("ChangeDate", XmlConvert.ToString(changedDate, XmlDateTimeSerializationMode.Unspecified));
            migrationActionDetails.AppendChild(root);

            XmlElement cs = migrationActionDetails.CreateElement("Columns");
            root.AppendChild(cs);
            foreach (Field f in fieldForSync)
            {
                cs.AppendChild(CreateFieldColumn(migrationActionDetails, f));
            }

            foreach (Field f in skippingFields)
            {
                cs.AppendChild(CreateFieldColumn(migrationActionDetails, f, true));
            }
            return migrationActionDetails;
        }

        internal static XmlElement CreateFieldColumn(XmlDocument migrationActionDetails, Field f)
        {
            return CreateFieldColumn(migrationActionDetails, f, f.Value);
        }

        internal static XmlElement CreateFieldColumn(XmlDocument migrationActionDetails, Field f, bool isSkippingField)
        {
            return CreateFieldColumn(migrationActionDetails, f, f.Value, isSkippingField);
        }

        internal static XmlElement CreateFieldColumn(XmlDocument migrationActionDetails, Field f, object fieldValue)
        {
            return CreateFieldColumn(migrationActionDetails, f, fieldValue, false);
        }

        internal static XmlElement CreateFieldColumn(XmlDocument migrationActionDetails, Field f, object fieldValue, bool isSkippingField)
        {
            XmlElement c = migrationActionDetails.CreateElement("Column");
            c.SetAttribute("DisplayName", f.Name);
            c.SetAttribute("ReferenceName", f.ReferenceName);
            c.SetAttribute("Type", f.FieldDefinition.FieldType.ToString());
            c.SetAttribute("IsSkippingField", isSkippingField.ToString());
            XmlElement v = migrationActionDetails.CreateElement("Value");
            object translatedValue = TranslateFieldValue(f, fieldValue);
            v.InnerText = translatedValue == null ? string.Empty : translatedValue.ToString();
            c.AppendChild(v);
            return c;
        }

        /// <summary>
        /// Determines whether given field should be included into revision.
        /// </summary>
        /// <param name="def">Field definition</param>
        /// <returns>true if the field should be included</returns>
        private bool MustTakeField(
           FieldDefinition def)
        {
            if (TFStringComparer.WorkItemFieldReferenceName.Equals(def.ReferenceName, m_core.ReflectedWorkItemIdFieldReferenceName))
            {
                // this field is specific to TfsWitAdapters and is used to track WorkItem Id reflection
                // we should NEVER expose it
                return false;
            }

            return MustTakeTfsField(def);
        }

        internal static bool MustTakeTfsField(FieldDefinition def)
        {
            switch (def.Id)
            {
                case (int)CoreField.AreaPath:
                case (int)CoreField.IterationPath:
                    return true;

                case (int)CoreField.WorkItemType:
                case (int)CoreField.IterationId:
                case (int)CoreField.AreaId:
                case (int)CoreField.AuthorizedAs:
                case (int)CoreField.ChangedBy:
                case (int)CoreField.ChangedDate:
                case (int)CoreField.CreatedDate:
                case (int)CoreField.CreatedBy:
                    return false;
                default:
                    return !def.IsComputed;
            }
        }

        /// <summary>
        /// Translates field value.
        /// </summary>
        /// <param name="field">Field whose value should be translated</param>
        /// <param name="newValue">value to be translated</param>
        /// <returns>Translated field value</returns>
        internal static object TranslateFieldValue(Field field, object newValue)
        {
            object value = newValue;

            if (null == value)
            {
                return null;
            }

            // Do mandatory translations
            switch (field.Id)
            {
                case (int)CoreField.AreaPath:
                case (int)CoreField.IterationPath:
                    string path = value as string;
                    Debug.Assert(path != null, "Path is not string!");

                    // Remove first part (which is project name)
                    int index = path.IndexOf('\\');
                    value = index == -1 ? string.Empty : path.Substring(index + 1);
                    break;

                case (int)CoreField.CreatedDate:
                case (int)CoreField.ChangedDate:
                    value = ((DateTime)value).ToUniversalTime();
                    break;

                default:
                    break;
            }

            return value;
        }

        /// <summary>
        /// Translates field value.
        /// </summary>
        /// <param name="field">Field whose value should be translated</param>
        /// <returns>Translated field value</returns>
        internal static object TranslateFieldValue(Field field)
        {
            return TranslateFieldValue(field, field.Value);
        }

        private List<Revision> FindUnsyncedRevisions(
            DateTime tfsWitWaterMark,
            ITranslationService translationService,
            Guid sourceId,
            IsWorkItemRevisionProcessed isRevisionProcessed)
        {
            int workItemId = WorkItem.Id;
            bool workItemHasReflectionOnTheOtherSide = WorkItemHasReflectionOnTheOtherSide(translationService, WorkItem.Id.ToString(), sourceId);

            List<Revision> retVal = new List<Revision>(WorkItem.Revisions.Count);
            foreach (Revision rev in WorkItem.Revisions)
            {
                int workItemRev = (int)rev.Fields[CoreField.Rev].Value;

                if (!workItemHasReflectionOnTheOtherSide)
                {
                    // NOTE: in the case that a work item is not migrated before, we should include all its revisions 
                    // (including Rev1) in the delta table. The only exception is that we recorded to have computed
                    // delta for a particular revision

                    // it's possible we are re-processing the rev as we go back 60 sec in history
                    // use the call back to eliminate the possible duplicates
                    if (null != isRevisionProcessed && isRevisionProcessed(workItemId, workItemRev))
                    {
                        continue;
                    }
                    else
                    {
                        retVal.Add(rev);
                    }
                }
                else
                {
                    // 1. skipping all revs that are prior to (HWM - 15sec)
                    DateTime revChangeDate = ((DateTime)rev.Fields[CoreField.ChangedDate].Value).ToUniversalTime();
                    if (revChangeDate <= tfsWitWaterMark)
                    {
                        continue;
                    }

                    // for work items that have been sync-ed by the tool, check if this revision is generated by it
                    if (workItemHasReflectionOnTheOtherSide
                        && translationService.IsSyncGeneratedItemVersion(workItemId.ToString(), workItemRev.ToString(), sourceId))
                    {
                        continue;
                    }

                    // it's possible we are re-processing the rev as we go back 60 sec in history
                    // use the call back to eliminate the possible duplicates
                    if (null != isRevisionProcessed && isRevisionProcessed(workItemId, workItemRev))
                    {
                        continue;
                    }

                    retVal.Add(rev);
                }
            }
            return retVal;
        }

        /// <summary>
        /// Determines whether a work item has been synced in the current session or not.
        /// </summary>
        /// <param name="translationService"></param>
        /// <param name="workItemId"></param>
        /// <param name="sourceId"></param>
        /// <returns></returns>
        /// <remarks>Look for the mirrored item id; if not found, we know the WI has not been synced by us.</remarks>
        private bool WorkItemHasReflectionOnTheOtherSide(ITranslationService translationService, string workItemId, Guid sourceId)
        {
            string reflectWorkItemId = translationService.TryGetTargetItemId(WorkItem.Id.ToString(), sourceId);
            return !string.IsNullOrEmpty(reflectWorkItemId);
        }

        private XmlDocument CreateAttachmentDescriptionDoc(TfsMigrationFileAttachment f, string revision)
        {
            Debug.Assert(!string.IsNullOrEmpty(revision));

            XmlDocument migrationActionDetails = new XmlDocument();
            XmlElement root = migrationActionDetails.CreateElement("WorkItemChanges");
            root.SetAttribute("WorkItemID", XmlConvert.ToString(WorkItem.Id));
            root.SetAttribute("Revision", revision);
            root.SetAttribute("WorkItemType", WorkItem.Type.Name);
            migrationActionDetails.AppendChild(root);

            XmlElement c = migrationActionDetails.CreateElement("Attachment");
            c.SetAttribute("Name", f.Name);
            c.SetAttribute("Length", XmlConvert.ToString(f.Length));
            c.SetAttribute("UtcCreationDate", XmlConvert.ToString(f.UtcCreationDate, XmlDateTimeSerializationMode.Unspecified));
            c.SetAttribute("UtcLastWriteDate", XmlConvert.ToString(f.UtcLastWriteDate, XmlDateTimeSerializationMode.Unspecified));
            XmlElement v = migrationActionDetails.CreateElement("Comment");
            v.InnerText = f.Comment;
            c.AppendChild(v);
            root.AppendChild(c);
            return migrationActionDetails;
        }

        private TfsCore m_core;                             // TFS shared core
        private Watermark m_watermark;                      // Watermark
        private string m_workItemType;                      // Name of the work item type
        private MigrationWorkItemData m_flags;              // Flags
        private WorkItem m_workItem;                        // Work item
    }
}
