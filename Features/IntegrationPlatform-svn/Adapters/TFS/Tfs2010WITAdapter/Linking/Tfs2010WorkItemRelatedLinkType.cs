// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Linking
{
    [Serializable]
    public class Tfs2010WorkItemRelatedLinkType : WorkItemRelatedLinkType
    {
        public const string Tfs2010RelatedLinkTypeReferenceName = "System.LinkTypes.Related"; // do not localize

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

        public override void ExtractLinkChangeActions(TfsMigrationWorkItem source, List<LinkChangeGroup> linkChangeGroups, WorkItemLinkStore store)
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

                    #region TFS 2010 specific logic
                    if (rl.LinkTypeEnd != null
                        && rl.LinkTypeEnd.LinkType != null)
                    {
                        if (rl.LinkTypeEnd.LinkType.ReferenceName.Equals(Tfs2010RelatedLinkTypeReferenceName, StringComparison.OrdinalIgnoreCase))
                        {
                    #endregion

                            var link = new Toolkit.Linking.ArtifactLink(
                                source.WorkItem.Id.ToString(CultureInfo.InvariantCulture),
                                new Toolkit.Linking.Artifact(source.Uri, s_sourceArtifactType),
                                new Toolkit.Linking.Artifact(TfsWorkItemHandler.UriFromId(rl.RelatedWorkItemId.ToString(CultureInfo.InvariantCulture)), s_targetArtifactType),
                                rl.Comment, this, rl.IsLocked);
                            linkChangeGroup.AddChangeAction(new LinkChangeAction(WellKnownChangeActionId.Add, link,
                                                                                 LinkChangeAction.LinkChangeActionStatus.Created,
                                                                                 false));
                        }
                    }
                    else
                    {
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
            }

            linkChangeGroups.Add(linkChangeGroup);
        }
    }
}
