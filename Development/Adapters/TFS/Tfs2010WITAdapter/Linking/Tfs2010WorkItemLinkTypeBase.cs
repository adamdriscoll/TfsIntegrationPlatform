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
using Microsoft.TeamFoundation.WorkItemTracking.Proxy;
using ToolkitLinking = Microsoft.TeamFoundation.Migration.Toolkit.Linking;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Linking
{
    public class WorkItemLinkTypeBase : LinkType, ILinkHandler
    {
        internal static readonly ArtifactType s_sourceArtifactType = new WorkItemArtifactType();
        internal static readonly ArtifactType s_targetArtifactType = new WorkItemArtifactType();
        internal static readonly ArtifactComparer s_artifactComparer = new ArtifactComparer();
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

        public override bool GetsActionsFromLinkChangeHistory
        {
            get
            {
                return true;
            }
        }

        public override LinkChangeAction CreateLinkDeletionAction(string sourceItemUri, string targetArtifactUrl, string linkTypeReferenceName)
        {
            return null;
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
            return;
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