// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Xml;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    public class Tfs2010UpdateDocument : TfsUpdateDocument
    {
        private const string InsertWorkItemLinkElem = "InsertWorkItemLink"; // DO NOT localize
        private const string DeleteWorkItemLinkElem = "DeleteWorkItemLink"; // DO NOT localize
        private const string UpdateWorkItemLinkElem = "UpdateWorkItemLink"; // DO NOT localize
        private const string WorkItemLinkSourceID = "SourceID";             // DO NOT localize
        private const string WorkItemLinkTargetID = "TargetID";             // DO NOT localize
        private const string WorkItemLinkType = "LinkType";                 // DO NOT localize
        private const string WorkItemLinkComment = "Comment";               // DO NOT localize
        private const string WorkItemLinkAutoMerge = "AutoMerge";           // DO NOT localize
        private const string WorkItemLinkLock = "Lock";                     // DO NOT localize

        public Tfs2010UpdateDocument(
            Tfs2010MigrationWorkItemStore tfsMigrationWorkItemStore)
            : base(tfsMigrationWorkItemStore)
        {
        }

        public void AddWorkItemLink(
            string sourceWorkItemId, 
            string targetWorkItemId, 
            int linkTypeId, 
            string comment,
            bool isLocked)
        {
            CreateEmptyUpdateDoc();

            XmlElement xwi = UpdateDocument.CreateElement(InsertWorkItemLinkElem);
            UpdateDocument.AppendChild(xwi);

            xwi.SetAttribute(WorkItemLinkSourceID, sourceWorkItemId);
            xwi.SetAttribute(WorkItemLinkTargetID, targetWorkItemId);
            xwi.SetAttribute(WorkItemLinkType, XmlConvert.ToString(linkTypeId));

            var autoMergeOption = XmlConvert.ToString(true);
            var lockOption = XmlConvert.ToString(isLocked);
            xwi.SetAttribute(WorkItemLinkAutoMerge, autoMergeOption);

            xwi.SetAttribute(WorkItemLinkComment, comment);
            xwi.SetAttribute(WorkItemLinkLock, lockOption);
        }

        public void RemoveWorkItemLink(
            string sourceWorkItemId,
            string targetWorkItemId,
            int linkTypeId, 
            string comment)
        {
            CreateEmptyUpdateDoc();

            XmlElement xwi = UpdateDocument.CreateElement(DeleteWorkItemLinkElem);
            UpdateDocument.AppendChild(xwi);

            xwi.SetAttribute(WorkItemLinkSourceID, sourceWorkItemId);
            xwi.SetAttribute(WorkItemLinkTargetID, targetWorkItemId);
            xwi.SetAttribute(WorkItemLinkType, XmlConvert.ToString(linkTypeId));

            var autoMergeOption = XmlConvert.ToString(true);
            xwi.SetAttribute(WorkItemLinkAutoMerge, autoMergeOption);
        }

        public void UpdateWorkItemLink(
            string sourceWorkItemId,
            string targetWorkItemId,
            int linkTypeId, 
            string comment,
            bool isLocked)
        {
            CreateEmptyUpdateDoc();

            XmlElement xwi = UpdateDocument.CreateElement(UpdateWorkItemLinkElem);
            UpdateDocument.AppendChild(xwi);

            xwi.SetAttribute(WorkItemLinkSourceID, sourceWorkItemId);
            xwi.SetAttribute(WorkItemLinkTargetID, targetWorkItemId);
            xwi.SetAttribute(WorkItemLinkType, XmlConvert.ToString(linkTypeId));

            var autoMergeOption = XmlConvert.ToString(false);
            var lockOption = XmlConvert.ToString(isLocked);
            xwi.SetAttribute(WorkItemLinkAutoMerge, autoMergeOption);

            xwi.SetAttribute(WorkItemLinkComment, comment);
            xwi.SetAttribute(WorkItemLinkLock, lockOption);
        }

        private void CreateEmptyUpdateDoc()
        {
            if (null == UpdateDocument)
            {
                UpdateDocument = new XmlDocument();
            }
        }
    }
}