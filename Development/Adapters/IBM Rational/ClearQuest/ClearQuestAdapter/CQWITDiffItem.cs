// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using ClearQuestOleServer;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.MigrationItem;

using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    public class CQWITDiffItem : IWITDiffItem
    {
        private ClearQuestDiffProvider m_diffProvider;
        private OAdEntity m_record;
        private int m_versionNumber;
        private XmlDocument m_workItemDetails;
        private DateTime m_lastChangeDate = DateTime.MinValue;
        private List<IMigrationFileAttachment> m_fileAttachments;
        private List<ILink> m_links;

        internal CQWITDiffItem(ClearQuestDiffProvider diffProvider, OAdEntity record, int versionNumber)
        {
            m_diffProvider = diffProvider;
            m_record = record;
            m_versionNumber = versionNumber;
        }

        internal OAdEntity Record
        {
            get { return m_record; }
        }

        internal DateTime LastChangedDate
        {
            get
            {
                if (m_lastChangeDate == DateTime.MinValue)
                {
                    string lastAuthor;

                    ClearQuestRecordItem.FindLastRevDtls(this.Record, out lastAuthor, out m_lastChangeDate);
                }
                return m_lastChangeDate;
            }
        }

        #region IWITDiffItem implementation
        /// <summary>
        /// A string that uniquely identifies the work itme on the work item server (in the format specific to the adapter)
        /// </summary>
        public string WorkItemId
        {
            get
            {
                return UtilityMethods.CreateCQRecordMigrationItemId(CQWrapper.GetEntityDefName(m_record), CQWrapper.GetEntityDisplayName(m_record));
            }
        }

        /// <summary>
        /// An XML document that fully describes a work item
        /// May be null in the case where the contents of work items do not need to be compared
        /// </summary>
        public XmlDocument WorkItemDetails
        {
            get
            {
                if (m_workItemDetails == null)
                {
                    ClearQuestRecordItem recordItem = new ClearQuestRecordItem(m_record, m_versionNumber.ToString(CultureInfo.InvariantCulture));
                    recordItem.CQSession = m_diffProvider.MigrationContext.UserSession;
                    recordItem.Version = m_versionNumber.ToString(CultureInfo.InvariantCulture);
                    m_workItemDetails = recordItem.CreateRecordDesc(m_record, m_versionNumber.ToString(CultureInfo.InvariantCulture), m_diffProvider.MigrationContext, false);
                }
                return m_workItemDetails;
            }
        }

        /// <summary>
        /// Returns the list of attachments for this item
        /// </summary>
        public ReadOnlyCollection<IMigrationFileAttachment> Attachments
        {
            get
            {
                if (m_fileAttachments == null)
                {
                    m_fileAttachments = new List<IMigrationFileAttachment>();
                    OAdAttachmentFields aAttachmentFields = CQWrapper.GetAttachmentFields(m_record);

                    for (int aAttachmentFieldsIndex = 0;
                         aAttachmentFieldsIndex < CQWrapper.AttachmentsFieldsCount(aAttachmentFields);
                         aAttachmentFieldsIndex++)
                    {
                        object ob = (object)aAttachmentFieldsIndex;
                        OAdAttachmentField aAttachmentField = CQWrapper.AttachmentsFieldsItem(aAttachmentFields, ref ob);
                        string fieldName = CQWrapper.GetAttachmentFieldName(aAttachmentField);

                        // Get all attachments
                        OAdAttachments attachments = CQWrapper.GetAttachments(aAttachmentField);
                        for (int attachmentIndex = 0;
                             attachmentIndex < CQWrapper.AttachmentsCount(attachments);
                             attachmentIndex++)
                        {
                            object obIndex = (object)attachmentIndex;
                            OAdAttachment aAttachment = CQWrapper.AttachmentsItem(attachments, ref obIndex);

                            m_fileAttachments.Add(new ClearQuestAttachmentItem(m_record, aAttachmentField, aAttachment, null));
                        }
                    }
                }

                return m_fileAttachments.AsReadOnly();
            }
        }

        /// <summary>
        /// Returns the list of links for this item
        /// </summary>
        public ReadOnlyCollection<ILink> Links
        {
            get
            {
                if (m_links == null)
                {
                    m_links = new List<ILink>();
                    if (m_diffProvider != null && m_diffProvider.LinkProvider != null)
                    {
                        foreach (LinkType linkType in m_diffProvider.LinkProvider.SupportedLinkTypes.Values)
                        {
                            IArtifact artifact = null;
                            if (m_diffProvider.LinkProvider.TryGetArtifactById(linkType.SourceArtifactType.ReferenceName, this.WorkItemId, out artifact))
                            {
                                foreach (ILink link in m_diffProvider.LinkProvider.GetLinks(artifact, linkType))
                                {
                                    m_links.Add(link);
                                }
                            }
                        }
                    }
                }

                return m_links.AsReadOnly();
            }
        }

        public bool HasBeenModifiedSince(DateTime someDateTime)
        {
            return this.LastChangedDate > someDateTime;
        }

        public bool HasBeenModifiedMoreRecentlyThan(MigrationItemId migrationItemId)
        {
            CQWITDiffItem diffItemToCompare = m_diffProvider.GetWITDiffItem(migrationItemId.ItemId) as CQWITDiffItem;
            if (diffItemToCompare == null)
            {
                return false;
            }

            return this.LastChangedDate > diffItemToCompare.LastChangedDate;
        }
        #endregion
    }
}
