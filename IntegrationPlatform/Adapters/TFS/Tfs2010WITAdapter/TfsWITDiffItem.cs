// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
        private List<IMigrationFileAttachment> m_fileAttachments;

        internal TfsWITDiffItem(TfsWITDiffProvider diffProvider, WorkItem tfsWorkItem)
        {
            m_diffProvider = diffProvider;
            m_tfsWorkItem = tfsWorkItem;
            m_tfsWorkItem.SyncToLatest();
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
                return new TfsWITRecordDetails(m_tfsWorkItem, true).DetailsDocument;
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
                        m_fileAttachments.Add(new TfsMigrationFileAttachment(tfsAttachment, m_tfsWorkItem.Project.Store.TeamProjectCollection.Uri.AbsoluteUri));
                    }
                }

                return m_fileAttachments.AsReadOnly();
            }
        }

        public int LinkCount
        {
            get { return LinkUris.Count; }
        }

        /// <summary>
        /// Returns the list of links for this item
        /// </summary>
        public ReadOnlyCollection<string> LinkUris
        {
            get
            {
                List<string> linkUris = new List<string>();

                if (m_tfsWorkItem.Links.Count > 0 && m_diffProvider != null && m_diffProvider.LinkProvider != null)
                {
                    // There are two foreach loops here to build up the list of link URIs because TFS has two different types of link that the
                    // TFS WIT adapter processes differenly:
                    //      1. Work item to artifact links (processed by the first foreach loop)
                    //      2. Work item to work item links (processed by the second foreach loop)
                    foreach (LinkType linkType in m_diffProvider.LinkProvider.SupportedLinkTypes.Values)
                    {
                        // This type won't return anything on the call to m_diffProvider.LinkProvider.GetLinks, so skip it.
                        // These will be counted by the 2nd loop for RelatedLinks below.
                        if (linkType is WorkItemLinkTypeBase)
                        {
                            continue;
                        }

                        // Skip changeset links for now because they only get migrated for WIT & VC sessions and then only if the changeset is in the
                        // VC session's filter scope.
                        if (linkType is WorkItemChangeListLinkType)
                        {
                            continue;
                        }

                        IArtifact artifact = null;
                        if (m_diffProvider.LinkProvider.TryGetArtifactById(linkType.SourceArtifactType.ReferenceName, TfsWorkItem.Id.ToString(), out artifact))
                        {
                            foreach (ILink link in m_diffProvider.LinkProvider.GetLinks(artifact, linkType))
                            {
                                linkUris.Add(link.TargetArtifact.Uri);
                            }
                        }
                    }

                    foreach (Link link in m_tfsWorkItem.Links)
                    {
                        RelatedLink relatedLink = link as RelatedLink;
                        if (relatedLink != null)
                        {
                            // Exclude TestedBy links since the target work item types ("Test Case" and "Shared Steps") cannot be 
                            // properly migrated
                            if (!string.Equals(relatedLink.LinkTypeEnd.LinkType.ReferenceName, "Microsoft.VSTS.Common.TestedBy") &&
                                m_diffProvider.LinkProvider != null &&
                                m_diffProvider.LinkProvider.SupportedLinkTypes != null &&
                                m_diffProvider.LinkProvider.SupportedLinkTypeReferenceNamesOther != null &&
                                m_diffProvider.LinkProvider.SupportedLinkTypes.ContainsKey(relatedLink.LinkTypeEnd.LinkType.ReferenceName) &&
                                m_diffProvider.LinkProvider.SupportedLinkTypeReferenceNamesOther.Contains(relatedLink.LinkTypeEnd.LinkType.ReferenceName))
                            {
                                if (m_diffProvider.HasWorkItemEverBeenInScope(relatedLink.RelatedWorkItemId))
                                {
                                    linkUris.Add(String.Format("({0}){1}",
                                        relatedLink.LinkTypeEnd.LinkType.ReferenceName,
                                        TfsWorkItemHandler.UriFromId(relatedLink.RelatedWorkItemId.ToString())));
                                }
                                else
                                {
                                    Debug.WriteLine(String.Format(CultureInfo.InvariantCulture,
                                        "Not counting related link to work item {0} of type {1} because it is not in the filter scope",
                                        relatedLink.RelatedWorkItemId, relatedLink.LinkTypeEnd.LinkType.ReferenceName));

                                }
                            }
                            else
                            {
                                Debug.WriteLine("Not counting unsupported related link of type: " + relatedLink.LinkTypeEnd.LinkType.ReferenceName);
                            }
                        }
                    }
                }

                return linkUris.AsReadOnly();
            }
        }

        public bool HasBeenModifiedSince(DateTime aUtcDateTime)
        {
            try
            {
                return m_tfsWorkItem.ChangedDate.ToUniversalTime() > aUtcDateTime.ToUniversalTime();
            }
            catch (DeniedOrNotExistException)
            {
                return false;
            }
        }

        public bool HasBeenModifiedMoreRecentlyThan(MigrationItemId migrationItemId)
        {
            try
            {
                TfsWITDiffItem diffItemToCompare = m_diffProvider.GetWITDiffItem(migrationItemId.ItemId) as TfsWITDiffItem;
                if (diffItemToCompare == null)
                {
                    return false;
                }

                return m_tfsWorkItem.ChangedDate > diffItemToCompare.TfsWorkItem.ChangedDate;
            }
            catch(DeniedOrNotExistException)
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the latest version string for the work item represented by the IWITDiffItem
        /// </summary>
        public string LatestVersion
        {
            get
            {
                return m_tfsWorkItem.Rev.ToString();
            }
        }

        /// <summary>
        /// Returns the date changed for the last version of the work item represented by the IWITDiffItem
        /// that was changed by a user (not the Integration Platform).   
        /// Returns DateTime.MinValue if the work item has only been modified by the Integration Platform.
        /// </summary>
        public DateTime ChangedByUserDate
        {
            get
            {
                DateTime changedByUserDate = DateTime.MinValue;

                for (int revNumber = m_tfsWorkItem.Rev; revNumber >= 1; revNumber--)
                {
                    if (!m_diffProvider.TranslationService.IsSyncGeneratedItemVersion(m_tfsWorkItem.Id.ToString(), revNumber.ToString(), m_diffProvider.MigrationSourceGuid, true))
                    {
                        if (revNumber == 1)
                        {
                            changedByUserDate = m_tfsWorkItem.CreatedDate;
                        }
                        else
                        {
                            Revision rev = m_tfsWorkItem.Revisions[revNumber - 1];
                            Debug.Assert(rev != null);
                            if (rev != null)
                            {
                                Field revisedDateField = rev.Fields[CoreField.RevisedDate];
                                Debug.Assert(revisedDateField != null);
                                if (revisedDateField != null && revisedDateField.OriginalValue != null)
                                {
                                    if (!DateTime.TryParse(revisedDateField.OriginalValue.ToString(), out changedByUserDate))
                                    {
                                        Debug.Fail("DateTime.TryParse failed to parse RevisedDate field value");
                                    }
                                }
                            }
                        }
                        break;
                    }
                }

                return changedByUserDate;
            }
        }

        /// <summary>
        /// Returns true if the first version of this work item was created by a user rather than the Integration Platform
        /// </summary>
        public bool CreatedByUser
        {
            get
            {
                return !m_diffProvider.TranslationService.IsSyncGeneratedItemVersion(m_tfsWorkItem.Id.ToString(), "1", m_diffProvider.MigrationSourceGuid, true);
            }
        }
    }
}
