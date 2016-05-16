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
    public class WorkItemHyperlinkLinkType : LinkType, ILinkHandler
    {
        private const string REFERENCE_NAME = "Microsoft.TeamFoundation.Migration.TFS.LinkType.WorkItemToHyperlink";
        private const string FRIENDLY_NAME = "Team Foundation Server WorkItem-to-Hyperlink link type";
        private static readonly ArtifactType s_sourceArtifactType = new WorkItemArtifactType();
        private static readonly ArtifactType s_targetArtifactType = new HyperlinkArtifactType();
        private static readonly ExtendedLinkProperties s_extendedProperties = new ExtendedLinkProperties(ExtendedLinkProperties.Topology.Network);

        public WorkItemHyperlinkLinkType()
            : base(REFERENCE_NAME, FRIENDLY_NAME, s_sourceArtifactType, s_targetArtifactType, s_extendedProperties)
        { }

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

            ReadOnlyCollection<Hyperlink> myLinks = ExtractMyLinks(source.WorkItem);

            foreach (var hl in myLinks)
            {
                var link = new Toolkit.Linking.ArtifactLink(
                         source.WorkItem.Id.ToString(CultureInfo.InvariantCulture),
                         new Toolkit.Linking.Artifact(source.Uri, s_sourceArtifactType),
                         new Toolkit.Linking.Artifact(TfsHyperlinkHandler.UriFromId(hl.Location), s_targetArtifactType),
                         hl.Comment,
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

            if (linkChangeAction.ChangeActionId.Equals(WellKnownChangeActionId.Add))
            {
                updateDoc.AddHyperLink(TfsHyperlinkHandler.IdFromUri(linkChangeAction.Link.TargetArtifact.Uri),
                                   linkChangeAction.Link.Comment);
            }
            else if (linkChangeAction.ChangeActionId.Equals(WellKnownChangeActionId.Delete))
            {
                Debug.Assert(updateDoc.WorkItem != null, "WorkItem is null in updateDoc");
                int? extId = ExtractFileLinkInfoExtId(updateDoc.WorkItem, linkChangeAction.Link.TargetArtifact.Uri);

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

        private int? ExtractFileLinkInfoExtId(WorkItem workItem, string uri)
        {
            ReadOnlyCollection<Hyperlink> myLinks = ExtractMyLinks(workItem);

            foreach (Hyperlink link in myLinks)
            {
                if (TFStringComparer.ArtiFactUrl.Equals(TfsHyperlinkHandler.UriFromId(link.Location), uri))
                {
                    return WorkItemExternalLinkType.ReflectFileLinkInfoExtId(link);
                }
            }

            return null;
        }

        private ReadOnlyCollection<Hyperlink> ExtractMyLinks(WorkItem workItem)
        {
            List<Hyperlink> myLinks = new List<Hyperlink>();

            foreach (var l in workItem.Links)
            {
                Hyperlink hl = l as Hyperlink;

                if (hl != null)
                {
                    myLinks.Add(hl);
                }
            }

            return myLinks.AsReadOnly();
        }
    }
}