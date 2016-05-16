// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using System.Xml;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    [Serializable]
    public class FileAttachmentMetadata : IMigrationFileAttachment
    {
        public FileAttachmentMetadata()
        { }

        public FileAttachmentMetadata(
            string name,
            long length,
            DateTime utcCreationDate,
            DateTime utcLastWriteDate,
            string comment)
        {
            Name = name;
            Length = length;
            UtcCreationDate = utcCreationDate;
            UtcLastWriteDate = utcLastWriteDate;
            Comment = comment;
        }

        public string Name { get; set; }

        public long Length { get; set; }
        
        public DateTime UtcCreationDate { get; set; }
        
        public DateTime UtcLastWriteDate { get; set; }
        
        public string Comment { get; set; }
        
        public System.IO.Stream GetFileContents()
        {
            return null;
        }

        public override string ToString()
        {
            GenericSerializer<FileAttachmentMetadata> serializer = new GenericSerializer<FileAttachmentMetadata>();
            return serializer.Serialize(this);
        }

        internal static string CreateAttachmentStorageId(System.Xml.XmlDocument updateDocument)
        {
            FileAttachmentMetadata metadata = Create(updateDocument);
            if (null == metadata)
            {
                return string.Empty;
            }
            else
            {
                return metadata.ToString();
            }
        }

        internal static FileAttachmentMetadata Create(System.Xml.XmlDocument updateDocument)
        {
            try
            {
                XmlElement rootNode = updateDocument.DocumentElement;
                XmlNode attachmentNode = rootNode.SelectSingleNode("/WorkItemChanges/Attachment");
                return new FileAttachmentMetadata(
                    attachmentNode.Attributes["Name"].Value,
                    long.Parse(attachmentNode.Attributes["Length"].Value),
                    DateTime.Parse(attachmentNode.Attributes["UtcCreationDate"].Value),
                    DateTime.Parse(attachmentNode.Attributes["UtcLastWriteDate"].Value),
                    attachmentNode.FirstChild.InnerText);
            }
            catch (Exception e)
            {
                TraceManager.TraceVerbose(e.ToString());
                return null;
            }
        }
    }

    public class WorkItemAttachmentStore : RelatedArtifactStoreBase
    {
        public const string AttachmentRelationship = "Microsoft.TeamFoundation.Migration.Toolkit.AttachmentRelationship";

        public WorkItemAttachmentStore(Guid migrationSourceId)
            : base(migrationSourceId)
        { }

        public void UpdatePerItemAttachmentChangesByCheckingRelatedItemRecords(
            string workItemId, 
            ChangeGroup attachmentChangeGroup,
            out List<FileAttachmentMetadata> additionalAttachmentToDelete)
        {
            if (string.IsNullOrEmpty(workItemId))
            {
                throw new ArgumentNullException("workItemId");
            }

            additionalAttachmentToDelete = new List<FileAttachmentMetadata>();

            var queryByItem = QueryByItem(workItemId);
            var perItemExistingAttachments =
                from attch in queryByItem
                where attch.RelationshipExistsOnServer
                select attch;

            if (attachmentChangeGroup.Actions == null
                || attachmentChangeGroup.Actions.Count == 0)
            {
                GenericSerializer<FileAttachmentMetadata> serializer = new GenericSerializer<FileAttachmentMetadata>();
                foreach (var attch in perItemExistingAttachments)
                {
                    if (!attch.OtherProperty.HasValue) 
                    {
                        continue;
                    }
                    for (int i = 0; i < attch.OtherProperty.Value; ++i)
                    {
                        try
                        {
                            additionalAttachmentToDelete.Add(serializer.Deserialize(attch.RelatedArtifactId));
                        }
                        catch (Exception e)
                        {
                            TraceManager.TraceVerbose(e.ToString());
                        }
                    }
                }

                foreach (var attch in perItemExistingAttachments)
                {
                    attch.OtherProperty = 0;
                    attch.RelationshipExistsOnServer = false;
                }

                m_context.TrySaveChanges();
            }
            else
            {
                Dictionary<IMigrationFileAttachment, List<IMigrationAction>> perAttachmentChangeActions =
                    GroupAttachmentByMetadata(attachmentChangeGroup);

                List<IMigrationAction> skippedActions = new List<IMigrationAction>(attachmentChangeGroup.Actions.Count);
                foreach (var attchSpecificActions in perAttachmentChangeActions)
                {
                    if (attchSpecificActions.Value.Count == 0)
                    {
                        continue;
                    }

                    string attachmentStorageId = attchSpecificActions.Key.ToString();
                    int deltaAttachmentCount = GetAttachmentCountInDelta(attchSpecificActions.Value);
                    var attachmentInStore = QueryItemSpecificAttachment(perItemExistingAttachments, attachmentStorageId);
                    int attachmentInStoreCount = GetAttachmentInStoreCount(attachmentInStore);

                    int serverStoreCountDiff = deltaAttachmentCount - attachmentInStoreCount;
                    if (serverStoreCountDiff >= 0)
                    {
                        int redundantAttachmentActionCount = deltaAttachmentCount - serverStoreCountDiff;
                        var addAttachmentActionToSkip =
                            attchSpecificActions.Value.Where(a => a.Action == WellKnownChangeActionId.AddAttachment).Take(redundantAttachmentActionCount);
                        skippedActions.AddRange(addAttachmentActionToSkip);
                    }
                    else if (serverStoreCountDiff < 0)
                    {
                        IMigrationAction action = attchSpecificActions.Value[0];
                        do
                        {
                            attachmentChangeGroup.Actions.Add(CreateDeleteAttachmentAction(action));
                        }
                        while (++serverStoreCountDiff < 0);
                    }

                    int countAfterUpdateStore = UpdateStore(workItemId, attachmentStorageId, serverStoreCountDiff);
                    foreach (IMigrationAction action in attchSpecificActions.Value)
                    {
                        XmlElement attachmentNode =
                            action.MigrationActionDescription.DocumentElement.SelectSingleNode("/WorkItemChanges/Attachment") as XmlElement;
                        System.Diagnostics.Debug.Assert(null != attachmentNode, "attachmentNode is NULL");
                        attachmentNode.SetAttribute("CountInSourceSideStore", countAfterUpdateStore.ToString());
                    }
                }

                foreach (IMigrationAction skippedAction in skippedActions)
                {
                    attachmentChangeGroup.Actions.Remove(skippedAction);
                }
            }
        }

        public int GetWorkItemAttachmentSpecificCount(string workItemId, XmlDocument updateDocument)
        {
            var queryByItem = QueryByItem(workItemId);
            var perItemExistingAttachments =
                from attch in queryByItem
                where attch.RelationshipExistsOnServer
                select attch;
            string attachmentStorageId = FileAttachmentMetadata.CreateAttachmentStorageId(updateDocument);
            var attachmentInStore = QueryItemSpecificAttachment(perItemExistingAttachments, attachmentStorageId);
            return GetAttachmentInStoreCount(attachmentInStore);
        }


        private static IQueryable<EntityModel.RTRelatedArtifactsRecords> QueryItemSpecificAttachment(
            IQueryable<EntityModel.RTRelatedArtifactsRecords> perItemExistingAttachments, 
            string attachmentStorageId)
        {
            var attachmentInStore =
                from attch in perItemExistingAttachments
                where attch.Relationship == AttachmentRelationship
                && attch.RelatedArtifactId == attachmentStorageId
                select attch;
            return attachmentInStore;
        }

        /// <summary>
        /// Update the attachment count in the store
        /// </summary>
        /// <param name="workItemId"></param>
        /// <param name="attachmentStorageId"></param>
        /// <param name="serverStoreCountDiff">Positive if there are more attachments on endpoint (server);
        /// Negative if there are more in the store; Zero if the count is equal</param>
        private int UpdateStore(string workItemId, string attachmentStorageId, int serverStoreCountDiff)
        {
            int countAfterUpdate = 0;
            if (serverStoreCountDiff != 0)
            {
                var queryByItem = QueryByItem(workItemId);
                var queryAttachment = QueryItemSpecificAttachment(queryByItem, attachmentStorageId);
                if (serverStoreCountDiff > 0)
                {
                    if (queryAttachment.Count() > 0)
                    {
                        queryAttachment.First().OtherProperty =
                            (queryAttachment.First().OtherProperty.HasValue
                            ? queryAttachment.First().OtherProperty.Value + serverStoreCountDiff
                            : serverStoreCountDiff);
                        countAfterUpdate = queryAttachment.First().OtherProperty.Value;
                    }
                    else
                    {
                        var attchRecord = CreateNewAttachmentStoreRecord(workItemId, attachmentStorageId, serverStoreCountDiff);
                        countAfterUpdate = attchRecord.OtherProperty.Value;
                    }
                    m_context.TrySaveChanges();
                }
                else if (serverStoreCountDiff < 0)
                {
                    if (queryAttachment.Count() > 0)
                    {
                        queryAttachment.First().OtherProperty =
                            (queryAttachment.First().OtherProperty.HasValue
                            ? queryAttachment.First().OtherProperty.Value + serverStoreCountDiff
                            : 0);
                        if (queryAttachment.First().OtherProperty.Value <= 0)
                        {
                            queryAttachment.First().OtherProperty = 0;
                            queryAttachment.First().RelationshipExistsOnServer = false;
                        }

                        countAfterUpdate = queryAttachment.First().OtherProperty.Value;
                        m_context.TrySaveChanges();
                    }
                }
            }
            
            return countAfterUpdate;
        }

        private EntityModel.RTRelatedArtifactsRecords CreateNewAttachmentStoreRecord(
            string workItemId, 
            string attachmentStorageId, 
            int numberOfIdenticalAttachments)
        {
            var attchRecord = EntityModel.RTRelatedArtifactsRecords.CreateRTRelatedArtifactsRecords(
                0, workItemId, AttachmentRelationship, attachmentStorageId, true);
            attchRecord.MigrationSource = this.RuntimeMigrationSource;
            attchRecord.OtherProperty = numberOfIdenticalAttachments;
            m_context.AddToRTRelatedArtifactsRecordsSet(attchRecord);
            return attchRecord;
        }

        private IMigrationAction CreateDeleteAttachmentAction(IMigrationAction action)
        {
            SqlMigrationAction sqlMigrationAction = action as SqlMigrationAction;
            System.Diagnostics.Debug.Assert(null != sqlMigrationAction, "cannot convert action to SqlMigrationAction");

            SqlMigrationAction copy = new SqlMigrationAction(
                action.ChangeGroup, 0, WellKnownChangeActionId.DelAttachment, action.SourceItem,
                action.FromPath, action.Path, action.Version, action.MergeVersionTo, action.ItemTypeReferenceName,
                action.MigrationActionDescription, action.State);
            return copy;
        }

        private static int GetAttachmentInStoreCount(IQueryable<EntityModel.RTRelatedArtifactsRecords> attachmentInStore)
        {
            int attachmentInStoreCount;
            if (attachmentInStore.Count() == 0
                || !attachmentInStore.First().OtherProperty.HasValue)
            {
                attachmentInStoreCount = 0;
            }
            else
            {
                attachmentInStoreCount = attachmentInStore.First().OtherProperty.Value;
            }
            return attachmentInStoreCount;
        }

        private int GetAttachmentCountInDelta(List<IMigrationAction> migrationActions)
        {
            int count = 0;
            foreach (var action in migrationActions)
            {
                if (action.Action == WellKnownChangeActionId.AddAttachment)
                {
                    ++count;
                }
                else if (action.Action == WellKnownChangeActionId.DelAttachment)
                {
                    --count;
                }
            }

            return count;
        }

        private static Dictionary<IMigrationFileAttachment, List<IMigrationAction>> GroupAttachmentByMetadata(ChangeGroup attachmentChangeGroup)
        {
            var metadataComparer = new BasicFileAttachmentComparer();
            Dictionary<IMigrationFileAttachment, List<IMigrationAction>> perAttachmentChangeActions =
                new Dictionary<IMigrationFileAttachment, List<IMigrationAction>>(new BasicFileAttachmentComparer());

            foreach (IMigrationAction migrationAction in attachmentChangeGroup.Actions)
            {
                if (IsAttachmentSpecificChangeAction(migrationAction.Action))
                {
                    FileAttachmentMetadata attchMetadata = FileAttachmentMetadata.Create(migrationAction.MigrationActionDescription);
                    if (null != attchMetadata)
                    {
                        IMigrationFileAttachment existingKey = null;
                        foreach (IMigrationFileAttachment key in perAttachmentChangeActions.Keys)
                        {
                            if (metadataComparer.Equals(key, attchMetadata))
                            {
                                existingKey = key;
                                break;
                            }
                        }
                        if (null == existingKey)
                        {
                            existingKey = attchMetadata;
                            perAttachmentChangeActions.Add(existingKey, new List<IMigrationAction>());
                        }
                        perAttachmentChangeActions[existingKey].Add(migrationAction);
                    }
                }
            }

            return perAttachmentChangeActions;
        }

        /// <summary>
        /// Determines if a change action is related to work item attachment
        /// </summary>
        /// <param name="changeActionId"></param>
        /// <returns></returns>
        private static bool IsAttachmentSpecificChangeAction(Guid changeActionId)
        {
            return changeActionId == WellKnownChangeActionId.AddAttachment
                || changeActionId == WellKnownChangeActionId.DelAttachment;
        }

        public void Update(string workItemId, IMigrationAction action)
        {
            FileAttachmentMetadata attchMetadata = FileAttachmentMetadata.Create(action.MigrationActionDescription);
            if (null != attchMetadata)
            {
                var queryByItem = QueryByItem(workItemId);                
                if (action.Action == WellKnownChangeActionId.AddAttachment)
                {
                    if (queryByItem.Count() > 0)
                    {
                        queryByItem.First().RelationshipExistsOnServer = true;
                        if (queryByItem.First().OtherProperty.HasValue)
                        {
                            queryByItem.First().OtherProperty = queryByItem.First().OtherProperty.Value + 1;
                        }
                        else
                        {
                            queryByItem.First().OtherProperty = 1;
                        }
                    }
                    else
                    {
                        var newAttchRecord = CreateNewAttachmentStoreRecord(
                            workItemId, FileAttachmentMetadata.CreateAttachmentStorageId(action.MigrationActionDescription), 1);
                    }
                }
                else if (action.Action == WellKnownChangeActionId.DelAttachment)
                {
                    if (queryByItem.Count() > 0)
                    {
                        if (queryByItem.First().RelationshipExistsOnServer)
                        {
                            if (queryByItem.First().OtherProperty.HasValue && queryByItem.First().OtherProperty.Value > 0)
                            {
                                queryByItem.First().OtherProperty = queryByItem.First().OtherProperty.Value - 1;
                                if (queryByItem.First().OtherProperty == 0)
                                {
                                    queryByItem.First().RelationshipExistsOnServer = false;
                                }
                            }
                            else
                            {
                                queryByItem.First().OtherProperty = 0;
                                queryByItem.First().RelationshipExistsOnServer = false;
                            }
                        }
                    }
                }

                m_context.TrySaveChanges();
            }
        }
    }
}
