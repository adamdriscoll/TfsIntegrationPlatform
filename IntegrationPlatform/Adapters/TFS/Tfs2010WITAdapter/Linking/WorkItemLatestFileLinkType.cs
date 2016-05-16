// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Linking
{
    [Serializable]
    public class WorkItemLatestFileLinkType : LinkType, ILinkHandler
    {
        private const string REFERENCE_NAME = "Microsoft.TeamFoundation.Migration.TFS.LinkType.WorkItemToLatestFile";
        private const string FRIENDLY_NAME = "Team Foundation Server WorkItem-to-Latest-File link type";
        private static readonly ArtifactType s_sourceArtifactType = new WorkItemArtifactType();
        private static readonly ArtifactType s_targetArtifactType = new VersionControlledFileArtifactType();
        private static readonly ExtendedLinkProperties s_extendedProperties = new ExtendedLinkProperties(ExtendedLinkProperties.Topology.Network);

        public WorkItemLatestFileLinkType()
            : base(REFERENCE_NAME, FRIENDLY_NAME, s_sourceArtifactType, s_targetArtifactType, s_extendedProperties)
        { }

        public static bool IsMyLink(ExternalLink link)
        {
            return TFStringComparer.ArtifactType.Equals(link.ArtifactLinkType.Name, LinkingConstants.VcFileLinkType) &&
                TfsLatestFileHandler.IsMyUri(link.LinkedArtifactUri);
        }

        public void ExtractLinkChangeActions(TfsMigrationWorkItem source, List<LinkChangeGroup> linkChangeGroups, WorkItemLinkStore store)
        {
            if (null == source)
            {
                throw new ArgumentNullException("source");
            }

            if (null == source.WorkItem)
            {
                throw new ArgumentException("source.WorkItem is null");
            }

            var linkChangeGroup = new LinkChangeGroup(
                source.WorkItem.Id.ToString(CultureInfo.InvariantCulture), LinkChangeGroup.LinkChangeGroupStatus.Created, false);

            ReadOnlyCollection<ExternalLink> myLinks = ExtractMyLinks(source.WorkItem);

            foreach (ExternalLink el in myLinks)
            {
                var link = new Toolkit.Linking.ArtifactLink(
                        source.WorkItem.Id.ToString(CultureInfo.InvariantCulture),
                        new Toolkit.Linking.Artifact(source.Uri, s_sourceArtifactType),
                        new Toolkit.Linking.Artifact(el.LinkedArtifactUri, s_targetArtifactType),
                        el.Comment,
                        this);
                linkChangeGroup.AddChangeAction(new LinkChangeAction(WellKnownChangeActionId.Add, link,
                                                                     LinkChangeAction.LinkChangeActionStatus.Created,
                                                                     false));
            }

            linkChangeGroups.Add(linkChangeGroup);
        }

        public bool UpdateTfs(TfsUpdateDocument updateDoc, LinkChangeAction linkChangeAction)
        {
            if (null == updateDoc)
            {
                throw new ArgumentNullException("updateDoc");
            }

            if (null == linkChangeAction)
            {
                throw new ArgumentNullException("linkChangeAction");
            }

            if (!linkChangeAction.Link.LinkType.ReferenceName.Equals(REFERENCE_NAME))
            {
                throw new ArgumentException("Link type mismatch.");
            }

            string uri = linkChangeAction.Link.TargetArtifact.Uri;
            string comment = linkChangeAction.Link.Comment;

            if (linkChangeAction.ChangeActionId.Equals(WellKnownChangeActionId.Add))
            {
                updateDoc.AddExternalLink(LinkingConstants.VcFileLinkType, uri, comment);
            }
            else if (linkChangeAction.ChangeActionId.Equals(WellKnownChangeActionId.Delete))
            {
                Debug.Assert(updateDoc.WorkItem != null, "WorkItem is null in updateDoc");
                int? extId = ExtractFileLinkInfoExtId(updateDoc.WorkItem, uri);

                if (extId.HasValue)
                {
                    updateDoc.DeleteExternalLink(extId.Value);
                }
                else
                {
                    TraceManager.TraceInformation("Deleting link {0}-to-{1} failed - cannot find linked target artifact.",
                        linkChangeAction.Link.SourceArtifactId,
                        linkChangeAction.Link.TargetArtifact.Uri);
                    return false;
                }
            }
            else
            {
                throw new MigrationException(TfsWITAdapterResources.ErrorUnsupportedChangeAction);
            }

            return true;
        }

        private int? ExtractFileLinkInfoExtId(WorkItem workItem, string uri)
        {
            ReadOnlyCollection<ExternalLink> myLinks = ExtractMyLinks(workItem);

            foreach (ExternalLink link in myLinks)
            {
                if (TFStringComparer.ArtiFactUrl.Equals(link.LinkedArtifactUri, uri))
                {
                    return WorkItemExternalLinkType.ReflectFileLinkInfoExtId(link);
                }
            }

            return null;
        }

        private ReadOnlyCollection<ExternalLink> ExtractMyLinks(WorkItem workItem)
        {
            List<ExternalLink> myLinks = new List<ExternalLink>();

            foreach (Link l in workItem.Links)
            {
                ExternalLink el = l as ExternalLink;

                if (el != null && IsMyLink(el))
                {
                    myLinks.Add(el);
                }
            }

            return myLinks.AsReadOnly();
        }

        public override LinkChangeAction CreateLinkDeletionAction(string sourceItemUri, string targetArtifactUrl, string linkTypeReferenceName)
        {
            var link = new Toolkit.Linking.ArtifactLink(
                TfsWorkItemHandler.IdFromUri(sourceItemUri),
                new Toolkit.Linking.Artifact(sourceItemUri, s_sourceArtifactType),
                new Toolkit.Linking.Artifact(targetArtifactUrl, s_targetArtifactType),
                string.Empty,
                this);
            return new LinkChangeAction(WellKnownChangeActionId.Delete, link, LinkChangeAction.LinkChangeActionStatus.Created, false);
        }
    }
}