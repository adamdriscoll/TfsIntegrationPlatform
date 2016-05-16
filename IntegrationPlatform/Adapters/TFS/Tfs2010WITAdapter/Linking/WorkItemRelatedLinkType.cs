// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Linking
{
    [Serializable]
    // This class is for old style TFS 2008 related link types only
    public class WorkItemRelatedLinkType : LinkType, ILinkHandler
    {
        protected const string REFERENCE_NAME = "Microsoft.TeamFoundation.Migration.TFS.LinkType.WorkItemToWorkItem";
        protected const string FRIENDLY_NAME = "Team Foundation Server WorkItem-to-WorkItem link type";
        protected static readonly ArtifactType s_sourceArtifactType = new WorkItemArtifactType();
        protected static readonly ArtifactType s_targetArtifactType = new WorkItemArtifactType();
        protected static readonly ExtendedLinkProperties s_extendedProperties = new ExtendedLinkProperties(ExtendedLinkProperties.Topology.Network);

        public WorkItemRelatedLinkType()
            : base(REFERENCE_NAME, FRIENDLY_NAME, s_sourceArtifactType, s_targetArtifactType, s_extendedProperties)
        { }

        public virtual void ExtractLinkChangeActions(TfsMigrationWorkItem source, List<LinkChangeGroup> linkChangeGroups, WorkItemLinkStore store)
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

            foreach (Link l in source.WorkItem.Links)
            {
                RelatedLink rl = l as RelatedLink;

                if (rl != null)
                {
                    // v1 work item related link does not have direction info
                    // to avoid generating two link change actions for the same link
                    // we only pick one from the work item of smaller id
                    if (source.WorkItem.Id >= rl.RelatedWorkItemId)
                    {
                        continue;
                    }

                    var link = new Toolkit.Linking.ArtifactLink(
                        source.WorkItem.Id.ToString(CultureInfo.InvariantCulture),
                        new Toolkit.Linking.Artifact(source.Uri, s_sourceArtifactType),
                        new Toolkit.Linking.Artifact(TfsWorkItemHandler.UriFromId(rl.RelatedWorkItemId.ToString(CultureInfo.InvariantCulture)), s_targetArtifactType),
                        rl.Comment,
                        this);
                    linkChangeGroup.AddChangeAction(new LinkChangeAction(WellKnownChangeActionId.Add, link,
                                                                         LinkChangeAction.LinkChangeActionStatus.Created,
                                                                         false));
                }
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

            //if (!linkChangeAction.ChangeActionId.Equals(WellKnownChangeActionId.Add))
            //{
            //    Debug.Assert(false, "Unsupported action!");
            //    TraceManager.TraceError("Non-add action on external links is not supported.");
            //}

            string uri = linkChangeAction.Link.TargetArtifact.Uri;
            string comment = linkChangeAction.Link.Comment;

            if (linkChangeAction.ChangeActionId.Equals(WellKnownChangeActionId.Add))
            {
                updateDoc.AddWorkItemLink(TfsWorkItemHandler.IdFromUri(uri), comment);
            }
            else if (linkChangeAction.ChangeActionId.Equals(WellKnownChangeActionId.Delete))
            {
                updateDoc.RemoveWorkItemLink(TfsWorkItemHandler.IdFromUri(uri));
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
    }
}