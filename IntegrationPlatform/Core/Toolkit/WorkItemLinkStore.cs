// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;


namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class WorkItemLinkStore : RelatedArtifactStoreBase
    {
        public enum LinkLockStatus
        {
            Unlocked = 0,
            Locked = 1,
        }

        public WorkItemLinkStore(Guid migrationSourceId)
            : base(migrationSourceId)
        { }

        public void UpdateSyncedLinks(
            List<LinkChangeAction> syncedLinkActions)
        {            
            foreach (LinkChangeAction action in syncedLinkActions)
            {
                bool relationExistsOnServer = action.ChangeActionId.Equals(WellKnownChangeActionId.Add);
                UpdateLink(action, relationExistsOnServer);
            }

            m_context.TrySaveChanges();            
        }

        public void UpdatePerItemLinkChangeGroupsByCheckingRelatedItemRecordsWithoutImplicitDelete(
            string sourceItemUri,
            LinkChangeGroup group,
            ILinkProvider linkProvider)
        {
            UpdatePerItemLinkChangeGroupsByCheckingRelatedItemRecords(sourceItemUri, group, linkProvider, false);
        }
       
        public void UpdatePerItemLinkChangeGroupsByCheckingRelatedItemRecords(
            string sourceItemUri,
            LinkChangeGroup group,
            ILinkProvider linkProvider)
        {
            UpdatePerItemLinkChangeGroupsByCheckingRelatedItemRecords(sourceItemUri, group, linkProvider, true);
        }

        public List<string> GetRevertedLinkSourceItems(string targetItemUri, string linkTypeReferenceName)
        {
            var queryByTargetItem = from link in m_context.RTRelatedArtifactsRecordsSet
                                    where link.MigrationSource.Id == RuntimeMigrationSource.Id
                                    && link.RelatedArtifactId.Equals(targetItemUri)
                                    select link;

            var queryPerTypeExistingLinks = from link in queryByTargetItem
                                            where link.Relationship.Equals(linkTypeReferenceName)
                                            && link.RelationshipExistsOnServer
                                            select link.ItemId;
            return queryPerTypeExistingLinks.ToList();
        }

        public void MarkLinkNoLongerExists(string sourceItemUri, string targetItemUri, string linkTypeReferenceName)
        {
            MarkRelationshipNoLongerExists(sourceItemUri, targetItemUri, linkTypeReferenceName);
        }

        private void UpdatePerItemLinkChangeGroupsByCheckingRelatedItemRecords(
            string sourceItemUri,
            LinkChangeGroup group,
            ILinkProvider linkProvider,
            bool createDeleteActionImplicitly)
        {
            var queryByItem = QueryByItem(sourceItemUri);
            var perItemExistingLinks = from link in queryByItem
                                       where link.RelationshipExistsOnServer
                                       select link;
            if (perItemExistingLinks.Count() == 0)
            {
                // no link existed before, all 'add' link actions should be pushed to the other side
                // AND we are going to record these links as existing on this side now
                AddLinks(group.Actions);
            }
            else
            {
                // check the delta link change actions

                // this list contains all the actions that do not need to push into the pipeline
                List<LinkChangeAction> actionThatEstablishExistingLinkRelationship = new List<LinkChangeAction>(); 
                foreach (LinkChangeAction action in group.Actions)
                {
                    if (action.Link.LinkType.GetsActionsFromLinkChangeHistory)
                    {
                        // For link types that can provide a history of link changes, we don't need to keep track of related artifact metadata
                        // so continue to the next LinkChangeAction
                        continue;
                    }
                    Debug.Assert(sourceItemUri.Equals(action.Link.SourceArtifact.Uri), "link of different action exists in the same group");
                    var linkQuery = from l in perItemExistingLinks
                                    where l.Relationship.Equals(action.Link.LinkType.ReferenceName)
                                    && l.RelatedArtifactId.Equals(action.Link.TargetArtifact.Uri)
                                    select l;

                    if (action.ChangeActionId.Equals(WellKnownChangeActionId.Add))
                    {
                        if (linkQuery.Count() > 0)
                        {
                            if (!linkQuery.First().OtherProperty.HasValue)
                            {
                                // link lock property is not available - required by backward-compability
                                UpdateLink(action, true);
                            }
                            else
                            {
                                bool linkInStoreHasLock = (((WorkItemLinkStore.LinkLockStatus)linkQuery.First().OtherProperty.Value) == WorkItemLinkStore.LinkLockStatus.Locked);

                                if (action.Link.IsLocked == linkInStoreHasLock)
                                {
                                    // link already exist and lock-property matches - no need to push to the other side
                                    actionThatEstablishExistingLinkRelationship.Add(action);
                                }
                                else
                                {
                                    UpdateLink(action, true);
                                }
                            }
                            
                        }
                        else
                        {
                            // link does not exist, keep it and push through the pipeline
                            // AND we are going to record these links as existing on this side now
                            UpdateLink(action, true);
                        }
                    }
                    else // delete
                    {
                        if (linkQuery.Count() > 0)
                        {
                            // link exists, so we will mark in our store that it no longer exists
                            UpdateLink(action, false); 
                        }
                        else
                        {
                            // link does not exist, no need to migrate this action
                            actionThatEstablishExistingLinkRelationship.Add(action);
                        }
                    }
                }

                // make sure we generate "Delete Link" action for ones that exist in the our recorded link table but is not included
                // in delta link actions
                List<LinkChangeAction> deletionActions = new List<LinkChangeAction>();
                if (createDeleteActionImplicitly)
                {
                    foreach (var recordedExistingLink in perItemExistingLinks)
                    {
                        Debug.Assert(linkProvider.SupportedLinkTypes.ContainsKey(recordedExistingLink.Relationship),
                            "linkProvider.SupportedLinkTypes.ContainsKey(recordedExistingLink.Relationship) returns false");
                        LinkType linkType = linkProvider.SupportedLinkTypes[recordedExistingLink.Relationship];
                        if (linkType.GetsActionsFromLinkChangeHistory)
                        {
                            // The link type is one that support link change history, so we ignore the contents of the
                            // RelatedArtifactTable and rely on the link change history instead.
                            continue;
                        }

                        bool recordedActionInGroup = false;
                        foreach (LinkChangeAction action in group.Actions)
                        {
                            if (action.Link.LinkType.ReferenceName.Equals(recordedExistingLink.Relationship, StringComparison.OrdinalIgnoreCase)
                                && action.Link.TargetArtifact.Uri.Equals(recordedExistingLink.RelatedArtifactId, StringComparison.OrdinalIgnoreCase))
                            {
                                recordedActionInGroup = true;
                                break;
                            }
                        }

                        if (!recordedActionInGroup)
                        {
                            TraceManager.TraceInformation("Link '{0}'->'{1}' ({2}) appears to have been deleted - generating link deletion action to be migrated",
                                recordedExistingLink.ItemId, recordedExistingLink.RelatedArtifactId, recordedExistingLink.Relationship);
                            
                            LinkChangeAction linkDeleteAction = linkType.CreateLinkDeletionAction(
                                recordedExistingLink.ItemId, recordedExistingLink.RelatedArtifactId, recordedExistingLink.Relationship);

                            if (null != linkDeleteAction)
                            {
                                deletionActions.Add(linkDeleteAction);
                            }

                            recordedExistingLink.RelationshipExistsOnServer = false;
                        }
                    }
                }

                if (actionThatEstablishExistingLinkRelationship.Count > 0)
                {
                    foreach (LinkChangeAction actionToDelete in actionThatEstablishExistingLinkRelationship)
                    {
                        group.DeleteChangeAction(actionToDelete);
                    }
                }

                if (deletionActions.Count > 0)
                {
                    group.PrependActions(deletionActions);
                }
            }

            m_context.TrySaveChanges();
        }

        private void AddLinks(List<LinkChangeAction> linkActions)
        {
            foreach (LinkChangeAction action in linkActions)
            {
                UpdateLink(action, true);
            }
        }

        private void UpdateLink(LinkChangeAction action, bool linkExistAfterUpdate)
        {
            string itemId = action.Link.SourceArtifact.Uri;
            string relatedArtifact = action.Link.TargetArtifact.Uri;
            string linkType = action.Link.LinkType.ReferenceName;

            if (!action.Link.LinkType.GetsActionsFromLinkChangeHistory)
            {
                var rtMigrationSource = RuntimeMigrationSource;

                var queryByItem = QueryByItem(itemId);
                var relationshipQuery =
                    from r in queryByItem
                    where r.RelatedArtifactId.Equals(relatedArtifact)
                    && r.Relationship.Equals(linkType)
                    select r;

                Debug.Assert(relationshipQuery.Count() <= 1,
                    "More than two identical related artifacts relationship exist");

                RTRelatedArtifactsRecords record;
                if (relationshipQuery.Count() == 0)
                {
                    record = RTRelatedArtifactsRecords.CreateRTRelatedArtifactsRecords(
                        0, itemId, linkType, relatedArtifact, linkExistAfterUpdate);
                    record.MigrationSource = m_rtMigrationSource;
                }
                else
                {
                    record = relationshipQuery.First();
                    record.RelationshipExistsOnServer = linkExistAfterUpdate;
                }
                record.OtherProperty = action.Link.IsLocked ? (int)WorkItemLinkStore.LinkLockStatus.Locked : (int)WorkItemLinkStore.LinkLockStatus.Unlocked;
            }
        }
    }
}
