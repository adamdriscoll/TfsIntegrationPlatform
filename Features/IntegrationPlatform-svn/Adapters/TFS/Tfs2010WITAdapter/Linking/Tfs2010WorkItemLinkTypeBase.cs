// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using ToolkitLinking = Microsoft.TeamFoundation.Migration.Toolkit.Linking;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Linking
{
    public class WorkItemLinkTypeBase : LinkType, ILinkHandler
    {
        private static readonly ArtifactType s_sourceArtifactType = new WorkItemArtifactType();
        private static readonly ArtifactType s_targetArtifactType = new WorkItemArtifactType();
        private static readonly ArtifactComparer s_artifactComparer = new ArtifactComparer();
        private WorkItemStore m_store;

        public WorkItemLinkTypeBase(
            string referenceName, 
            string friendlyName, 
            ExtendedLinkProperties extendedLinkProperties, 
            WorkItemStore store)
            : base(referenceName, friendlyName, s_sourceArtifactType, s_targetArtifactType, extendedLinkProperties)
        {
            m_store = store;
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

        public void ExtractDirectedLinksClosure(
            WorkItem workItem,
            NonCyclicReferenceClosure closure)
        {
            if (null == workItem)
            {
                throw new ArgumentNullException("workItem");
            }

            if (null == closure)
            {
                throw new ArgumentNullException("closure");
            }

            Debug.Assert(ExtendedProperties.Directed);
            ExtractDirectedLinks(workItem, closure);
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

            List<string> revertedLinkSourceWorkItemUris = new List<string>();
            if (null != store)
            {
                revertedLinkSourceWorkItemUris = store.GetRevertedLinkSourceItems(source.Uri, ReferenceName);
            }

            foreach (WorkItemLink l in source.WorkItem.WorkItemLinks)
            {
                #region obsolete
                //// always recognize the "ForwardLink"
                //if (!l.LinkTypeEnd.IsForwardLink)
                //{
                //    continue;
                //} 
                #endregion

                // always recognize the WorkItem with smaller Id for non-directional link
                if (!l.LinkTypeEnd.LinkType.IsDirectional && l.SourceId > l.TargetId)
                {
                    continue;
                }

                if (!TFStringComparer.LinkName.Equals(l.LinkTypeEnd.LinkType.ReferenceName, ReferenceName))
                {
                    continue;
                }

                var sourceIdStr = l.SourceId.ToString(CultureInfo.InvariantCulture);
                var targetIdStr = l.TargetId.ToString(CultureInfo.InvariantCulture);
                var sourceArtifact = new ToolkitLinking.Artifact(TfsWorkItemHandler.UriFromId(sourceIdStr), s_sourceArtifactType);
                var targetArtifact = new ToolkitLinking.Artifact(TfsWorkItemHandler.UriFromId(targetIdStr), s_targetArtifactType);

                ToolkitLinking.ArtifactLink link;
                if (l.LinkTypeEnd.IsForwardLink)
                {
                    link = new ToolkitLinking.ArtifactLink(sourceIdStr, sourceArtifact, targetArtifact, l.Comment, this, l.IsLocked);
                }
                else
                {
                    link = new ToolkitLinking.ArtifactLink(targetIdStr, targetArtifact, sourceArtifact, l.Comment, this, l.IsLocked);
                    if (revertedLinkSourceWorkItemUris.Contains(targetArtifact.Uri))
                    {
                        revertedLinkSourceWorkItemUris.Remove(targetArtifact.Uri);
                    }
                }

                var linkChangeAction = new LinkChangeAction(WellKnownChangeActionId.Add, 
                                                            link, 
                                                            LinkChangeAction.LinkChangeActionStatus.Created, 
                                                            false);

                linkChangeGroup.AddChangeAction(linkChangeAction);
            }

            foreach (string revertedLinkSrcItemUri in revertedLinkSourceWorkItemUris)
            {
                string sourceWorkItemId = TfsWorkItemHandler.IdFromUri(revertedLinkSrcItemUri);
                LinkChangeGroup group = 
                    new LinkChangeGroup(sourceWorkItemId, LinkChangeGroup.LinkChangeGroupStatus.Created, false);
                var deleteLinkChangeAction = new LinkChangeAction(
                    WellKnownChangeActionId.Delete,
                    new ToolkitLinking.ArtifactLink(sourceWorkItemId,
                        new ToolkitLinking.Artifact(revertedLinkSrcItemUri, s_sourceArtifactType),
                        new ToolkitLinking.Artifact(source.Uri, s_targetArtifactType),
                        string.Empty, this),
                    LinkChangeAction.LinkChangeActionStatus.Created, false);
                group.AddChangeAction(deleteLinkChangeAction);
                linkChangeGroups.Add(group);
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

            if (!linkChangeAction.Link.LinkType.ReferenceName.Equals(ReferenceName))
            {
                throw new ArgumentException("Link type mismatch.");
            }

            string targetId = TfsWorkItemHandler.IdFromUri(linkChangeAction.Link.TargetArtifact.Uri);
            string sourceId = TfsWorkItemHandler.IdFromUri(linkChangeAction.Link.SourceArtifact.Uri);
            int linkTypeId = WorkItemLinkTypeId(ReferenceName, true);
            string comment = linkChangeAction.Link.Comment ?? string.Empty;


            var tfs2010UpdateDoc = updateDoc as Tfs2010UpdateDocument;
            Debug.Assert(null != tfs2010UpdateDoc);

            if (linkChangeAction.ChangeActionId.Equals(WellKnownChangeActionId.Add))
            {
                tfs2010UpdateDoc.AddWorkItemLink(sourceId, targetId, linkTypeId, comment, linkChangeAction.Link.IsLocked);
            }
            else if (linkChangeAction.ChangeActionId.Equals(WellKnownChangeActionId.Edit))
            {
                tfs2010UpdateDoc.UpdateWorkItemLink(sourceId, targetId, linkTypeId, comment, linkChangeAction.Link.IsLocked);
            }
            else if (linkChangeAction.ChangeActionId.Equals(WellKnownChangeActionId.Delete))
            {
                tfs2010UpdateDoc.RemoveWorkItemLink(sourceId, targetId, linkTypeId, comment);
            }
            else
            {
                throw new MigrationException(TfsWITAdapterResources.ErrorUnsupportedChangeAction);
            }

            return true;
        }

        public List<IArtifact> GetDirectedLinkParents(WorkItem workItem)
        {
            if (null == workItem)
            {
                throw new ArgumentNullException("workItem");
            }

            var retVal = new List<IArtifact>();

            foreach (WorkItemLink l in workItem.WorkItemLinks)
            {
                if (l.LinkTypeEnd.IsForwardLink)
                {
                    continue;
                }

                if (!TFStringComparer.LinkName.Equals(l.LinkTypeEnd.LinkType.ReferenceName, ReferenceName))
                {
                    continue;
                }

                var targetIdStr = l.TargetId.ToString(CultureInfo.InvariantCulture);
                var targetUri = TfsWorkItemHandler.UriFromId(targetIdStr);
                var targetArtifact = new ToolkitLinking.Artifact(targetUri, s_targetArtifactType);

                var pos = retVal.BinarySearch(targetArtifact, s_artifactComparer);
                if (pos >= 0)
                {
                    continue;
                }

                retVal.Add(targetArtifact);
                retVal.Sort(s_artifactComparer);
            }

            return retVal;
        }

        private void ExtractDirectedLinks(
           WorkItem workItem,
           NonCyclicReferenceClosure closure)
        {
            List<int> workItemIdsToProcess = new List<int>();

            foreach (WorkItemLink l in workItem.WorkItemLinks)
            {
                if (!TFStringComparer.LinkName.Equals(l.LinkTypeEnd.LinkType.ReferenceName, ReferenceName))
                {
                    continue;
                }

                var sourceIdStr = l.SourceId.ToString(CultureInfo.InvariantCulture);
                var targetIdStr = l.TargetId.ToString(CultureInfo.InvariantCulture);
                var sourceUri = TfsWorkItemHandler.UriFromId(sourceIdStr);
                var targetUri = TfsWorkItemHandler.UriFromId(targetIdStr);
                var sourceArtifact = new ToolkitLinking.Artifact(sourceUri, s_sourceArtifactType);
                var targetArtifact = new ToolkitLinking.Artifact(targetUri, s_targetArtifactType);

                if (!closure.SourceArtifactUris.Contains(targetUri)
                     && !closure.TargetArtifactUris.Contains(targetUri))
                {
                    workItemIdsToProcess.Add(l.TargetId);
                }

                if (l.LinkTypeEnd.IsForwardLink)
                {
                    var link = new ToolkitLinking.ArtifactLink(sourceIdStr, sourceArtifact, targetArtifact, l.Comment, this);
                    closure.AddValidLink(link);
                }
            }

            foreach (var workItemId in workItemIdsToProcess)
            {
                var wi = m_store.GetWorkItem(workItemId);
                if (null == wi)
                {
                    continue;
                }

                ExtractDirectedLinks(wi, closure);
            }
        }

        public int WorkItemLinkTypeId(string workItemLinkTypeReferenceName, bool getForwardEnd)
        {
            Debug.Assert(null != m_store);
            Debug.Assert(!string.IsNullOrEmpty(workItemLinkTypeReferenceName));

            foreach (WorkItemLinkType type in m_store.WorkItemLinkTypes)
            {
                if (!TFStringComparer.LinkName.Equals(type.ReferenceName, workItemLinkTypeReferenceName))
                {
                    continue;
                }

                if (getForwardEnd)
                {
                    return type.ForwardEnd.Id;
                }
                else
                {
                    return type.ReverseEnd.Id;
                }
            }

            throw new InvalidOperationException(string.Format(
                TfsWITAdapterResources.WorkItemLinkTypeNotFound,
                workItemLinkTypeReferenceName));
        }
    }
}