// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using ClearQuestOleServer;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.MigrationItem
{
    [Serializable]
    public sealed class ClearQuestAttachmentItem : IMigrationItem, IMigrationFileAttachment
    {
        private MD5Producer m_HashProducer;
        private Session m_cqSession;

        /// <summary>
        /// Creates the MD5 based on the metadata of an attachment
        /// </summary>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        /// <param name="displayName"></param>
        /// <param name="fileSize"></param>
        /// <returns></returns>
        public static byte[] HashAttachmentMetadata(
            string name,
            string comment,
            string displayName,
            long fileSize)
        {
            MD5Producer hashProducer = new MD5Producer();
            StringBuilder sb = new StringBuilder(name.Trim());
            sb.Append(comment.Trim());
            sb.Append(displayName.Trim());
            sb.Append(fileSize.ToString());

            return hashProducer.CalculateMD5(new MemoryStream(ASCIIEncoding.Default.GetBytes(sb.ToString())));
        }

        /// <summary>
        /// Default ctor that's needed by serialization.
        /// </summary>
        public ClearQuestAttachmentItem()
        {
        }

        public ClearQuestAttachmentItem(
            OAdEntity aHostRecord,
            OAdAttachmentField aHostField,
            OAdAttachment aAttachment,
            ClearQuestConnectionConfig connectionConfiguration)
        {
            // gather info to query for the record
            EntityDefName = CQWrapper.GetEntityDefName(aHostRecord);
            EntityDispName = CQWrapper.GetEntityDisplayName(aHostRecord);

            // gather info to query for attachment field
            FieldName = CQWrapper.GetAttachmentFieldName(aHostField);

            string name;
            string comment;
            string dispName;
            int fileSize;
            CQWrapper.GetAttachmentMetadata(aAttachment,
                                            out name,
                                            out comment,
                                            out dispName,
                                            out fileSize);

            Name = name;
            Comment = comment;
            DisplayName = dispName;
            Length = (long)fileSize; // fileSize returned by CQ API is in bytes
            ConnectionConfiguration = connectionConfiguration;
        }

        /// <summary>
        /// Host record Entity Type Definition Name
        /// </summary>
        public string EntityDefName
        {
            get;
            set;
        }

        /// <summary>
        /// Host record display name
        /// </summary>
        /// <remarks>
        /// In CQ, for stateful=>id else dbid
        /// </remarks>
        public string EntityDispName
        {
            get;
            set;
        }

        /// <summary>
        /// Host attachment field name
        /// </summary>
        public string FieldName
        {
            get;
            set;
        }

        /// <summary>
        /// Connection configuration to get a CQ session
        /// </summary>
        public ClearQuestConnectionConfig ConnectionConfiguration
        {
            get;
            set;
        }

        /// <summary>
        /// Current CQ session for accessing the record
        /// </summary>
        [XmlIgnore]
        public Session CQSession
        {
            get
            {
                if (m_cqSession == null)
                {
                    m_cqSession = CQConnectionFactory.GetUserSession(ConnectionConfiguration);
                }
                return m_cqSession; 
            }
            set
            {
                m_cqSession = value;
            }
        }

        #region IMigrationItem Members

        public void Download(string localPath)
        {
            byte[] metadataHash = HashAttachmentMetadata(Name, Comment, DisplayName, Length);

            if (null == CQSession)
            {
                throw new InvalidOperationException("CQSession == NULL");
            }

            // [teyang] TODO: validation on localPath
            if (File.Exists(localPath))
            {
                File.Delete(localPath);
            }

            OAdEntity aRecord = CQWrapper.GetEntity(CQSession, EntityDefName, EntityDispName);

            OAdAttachmentFields aAllAttFields = CQWrapper.GetAttachmentFields(aRecord);

            bool attachmentFound = false;
            for (int attFieldsIndex = 0;
                 attFieldsIndex < CQWrapper.AttachmentsFieldsCount(aAllAttFields);
                 attFieldsIndex++)
            {
                object ob = (object)attFieldsIndex;
                OAdAttachmentField aAttField = CQWrapper.AttachmentsFieldsItem(aAllAttFields, ref ob);

                string fieldName = CQWrapper.GetAttachmentFieldName(aAttField);

                if (!CQStringComparer.FieldName.Equals(fieldName, this.FieldName))
                {
                    // not the hosting attachment field, try next one
                    continue;
                }

                // attachment field is found, now look for the attachment
                OAdAttachments aAttachments = CQWrapper.GetAttachments(aAttField);
                OAdAttachment aAttachment = null;
                for (int attachmentIndex = 0;
                     attachmentIndex < CQWrapper.AttachmentsCount(aAttachments);
                     attachmentIndex++)
                {
                    object obIndex = (object)attachmentIndex;
                    aAttachment = CQWrapper.AttachmentsItem(aAttachments, ref obIndex);

                    string name;
                    string comment;
                    string dispName;
                    int fileSize;
                    CQWrapper.GetAttachmentMetadata(aAttachment, out name, out comment, out dispName, out fileSize);

                    byte[] hash = HashAttachmentMetadata(name, comment, dispName, (long)fileSize);
                    if (HashProducer.CompareMD5(metadataHash, hash) == 0)
                    {
                        attachmentFound = true;
                        break;
                    }
                }

                if (attachmentFound)
                {
                    Debug.Assert(null != aAttachment, "null == aAttachment");
                    CQWrapper.LoadAttachment(aAttachment, localPath);
                }

                // we've checked the host att field already, no need to proceed
                break;
            }

            if (!attachmentFound)
            {
                // [teyang] TODO: typed exception, AttachmentNotFound conflict handling
                throw new Exception(string.Format("attachment '{0}' is not found", Name)); 
            }
        }

        /// <summary>
        /// DisplayName of the attachment item
        /// </summary>
        public string DisplayName
        {
            get;
            set;
        }

        #endregion

        #region IMigrationFileAttachment Members

        /// <summary>
        /// attachmen file name
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// attachment file size in byte
        /// </summary>
        public long Length
        {
            get;
            set;
        }

        /// <summary>
        /// attachment creation time
        /// </summary>
        public DateTime UtcCreationDate
        {
            get;
            set;
        }

        /// <summary>
        /// attachment last update time
        /// </summary>
        public DateTime UtcLastWriteDate
        {
            get;
            set;
        }

        /// <summary>
        /// comment stored with the attachment
        /// </summary>
        public string Comment
        {
            get;
            set;
        }

        #endregion

        internal IMigrationAction CreateChangeAction(ChangeGroup hostChangeGroup, string lastVersion)
        {
            IMigrationAction action = hostChangeGroup.CreateAction(WellKnownChangeActionId.AddAttachment,
                                                                   this,
                                                                   MigrationRecordId,
                                                                   "",
                                                                   null,
                                                                   "",
                                                                   WellKnownContentType.WorkItem.ReferenceName,
                                                                   CreateAttachmentDescriptionDoc(lastVersion));
            return action;
        }

        private string MigrationRecordId
        {
            get
            {
                return UtilityMethods.CreateCQRecordMigrationItemId(EntityDefName, EntityDispName);
            }
        }

        private XmlDocument CreateAttachmentDescriptionDoc(string lastVersion)
        {
            if (string.IsNullOrEmpty(lastVersion))
            {
                Trace.TraceInformation("lastVersion is empty for CQ Attachment Delta Action creation, defaulting to use current time stamp");
                lastVersion = DateTime.Now.ToString();
            }

            XmlDocument migrationActionDetails = new XmlDocument();
            XmlElement root = migrationActionDetails.CreateElement("WorkItemChanges");
            root.SetAttribute("WorkItemID", MigrationRecordId);
            root.SetAttribute("Revision", lastVersion);
            root.SetAttribute("WorkItemType", EntityDefName);
            migrationActionDetails.AppendChild(root);

            XmlElement c = migrationActionDetails.CreateElement("Attachment");
            c.SetAttribute("Name", Name);
            c.SetAttribute("Length", XmlConvert.ToString(Length));
            c.SetAttribute("UtcCreationDate", string.Empty);
            c.SetAttribute("UtcLastWriteDate", string.Empty);
            XmlElement v = migrationActionDetails.CreateElement("Comment");
            v.InnerText = Comment;
            c.AppendChild(v);
            root.AppendChild(c);
            return migrationActionDetails;
        }

        [XmlIgnore]
        private MD5Producer HashProducer
        {
            get
            {
                if (null == m_HashProducer)
                {
                    m_HashProducer = new MD5Producer();
                }

                return m_HashProducer;
            }
        }
    }
}
