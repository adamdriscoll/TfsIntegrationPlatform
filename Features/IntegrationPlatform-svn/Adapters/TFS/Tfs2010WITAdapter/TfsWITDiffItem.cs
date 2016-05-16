// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Xml;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Linking;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    public class TfsWITDiffItem : IWITDiffItem
    {
        private TfsWITDiffProvider m_diffProvider;
        private WorkItem m_tfsWorkItem;
        private TfsWITRecordDetails m_workItemDetails;
        private List<IMigrationFileAttachment> m_fileAttachments;
        private List<ILink> m_links;

        internal TfsWITDiffItem(TfsWITDiffProvider diffProvider, WorkItem tfsWorkItem)
        {
            m_diffProvider = diffProvider;
            m_tfsWorkItem = tfsWorkItem;
        }

        public WorkItem TfsWorkItem
        {
            get { return m_tfsWorkItem; }
        }

        /// <summary>
        /// A string that uniquely identifies the work itme on the work item server (in the format specific to the adapter)
        /// </summary>
        public string WorkItemId
        {
            get
            {
                return m_tfsWorkItem.Id.ToString(CultureInfo.InvariantCulture);
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
                    m_workItemDetails = new TfsWITRecordDetails(m_tfsWorkItem);
                }
                return m_workItemDetails.DetailsDocument;
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
                    foreach (Attachment tfsAttachment in m_tfsWorkItem.Attachments)
                    {
                        m_fileAttachments.Add(new TfsMigrationFileAttachment(tfsAttachment));
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
                    if (m_tfsWorkItem.Links.Count > 0 && m_diffProvider != null && m_diffProvider.LinkProvider != null)
                    {
                        foreach (LinkType linkType in m_diffProvider.LinkProvider.SupportedLinkTypes.Values)
                        {
                            IArtifact artifact = null;
                            if (m_diffProvider.LinkProvider.TryGetArtifactById(linkType.SourceArtifactType.ReferenceName, TfsWorkItem.Id.ToString(), out artifact))
                            {
                                foreach (ILink link in m_diffProvider.LinkProvider.GetLinks(artifact, linkType))
                                {
                                    m_links.Add(link);
                                }
                            }
                            if (m_links.Count >= m_tfsWorkItem.Links.Count)
                            {
                                break;
                            }
                        }
                    }
                }

                return m_links.AsReadOnly();
            }
        }

        public bool HasBeenModifiedSince(DateTime someDateTime)
        {
            return m_tfsWorkItem.ChangedDate > someDateTime;
        }

        public bool HasBeenModifiedMoreRecentlyThan(MigrationItemId migrationItemId)
        {
            TfsWITDiffItem diffItemToCompare = m_diffProvider.GetWITDiffItem(migrationItemId.ItemId) as TfsWITDiffItem;
            if (diffItemToCompare == null)
            {
                return false;
            }

            return m_tfsWorkItem.ChangedDate > diffItemToCompare.TfsWorkItem.ChangedDate;
        }
    }
}
