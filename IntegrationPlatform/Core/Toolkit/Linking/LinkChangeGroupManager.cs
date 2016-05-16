// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Linking
{
    internal class LinkChangeGroupManager
    {
        internal LinkChangeGroupManager(Guid sessionGroupId, Guid sessionId, Guid sourceId, LinkService service)
        {
            SessionGroupId = sessionGroupId;
            SessionId = sessionId;
            SourceId = sourceId;
            LinkService = service;
        }

        private LinkService LinkService { get; set; }

        internal Guid SessionGroupId
        {
            get;
            private set;
        }

        internal Guid SessionId
        {
            get;
            private set;
        }

        internal Guid SourceId
        {
            get;
            private set;
        }

        internal RTLinkChangeGroup AddLinkChangeGroup(LinkChangeGroup group)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var rtLinkChangeGroup = RTLinkChangeGroup.CreateRTLinkChangeGroup(
                    0, (int) group.Status, false, SessionGroupId, SessionId, SourceId);
                rtLinkChangeGroup.GroupName = group.GroupName;
                rtLinkChangeGroup.IsForcedSync = group.IsForcedSync;
                context.AddToRTLinkChangeGroupSet(rtLinkChangeGroup);

                int newActiveActionCount = 0;
                foreach (LinkChangeAction a in group.Actions)
                {
                    RTLinkChangeAction rtLinkChangeAction = AddChangeAction(a, context);
                    if (rtLinkChangeAction == null)
                    {
                        continue;
                    }

                    rtLinkChangeAction.LinkChangeGroup = rtLinkChangeGroup;
                    ++newActiveActionCount;
                }

                if (newActiveActionCount <= 0)
                {
                    return null;
                }
                
                context.TrySaveChanges();
                group.InternalId = rtLinkChangeGroup.Id;
                context.Detach(rtLinkChangeGroup);
                return rtLinkChangeGroup;
            }
        }

        internal ReadOnlyCollection<LinkChangeGroup> GetPagedLinkChangeGroups(
            long firstGroupId,
            int pageSize,
            LinkChangeGroup.LinkChangeGroupStatus status, 
            bool? getConflictedGroup,
            out long lastGroupId)
        {
            return GetPagedLinkChangeGroups(firstGroupId, pageSize, status, getConflictedGroup, int.MaxValue, out lastGroupId);
        }

        internal ReadOnlyCollection<LinkChangeGroup> GetPagedLinkChangeGroups(
            long firstGroupId,
            int pageSize,
            LinkChangeGroup.LinkChangeGroupStatus status,
            bool? getConflictedGroup,
            int maxAge,
            out long lastGroupId)
        {
            lastGroupId = firstGroupId - 1;
            var linkChangeGroups = new List<LinkChangeGroup>();
            int statusVal = (int)status;

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var linkGroupQuery = getConflictedGroup.HasValue
                                     ? (from g in context.RTLinkChangeGroupSet
                                        where g.Status == statusVal
                                           && g.ContainsConflictedAction == getConflictedGroup
                                           && g.SessionGroupUniqueId.Equals(SessionGroupId)
                                           && g.SessionUniqueId.Equals(SessionId)
                                           && g.SourceId.Equals(SourceId)
                                           && (g.Age == null || g.Age.Value <= maxAge)
                                           && g.Id >= firstGroupId
                                        orderby g.Id
                                        select g).Take(pageSize)
                                     : (from g in context.RTLinkChangeGroupSet
                                        where g.Status == statusVal
                                           && g.SessionGroupUniqueId.Equals(SessionGroupId)
                                           && g.SessionUniqueId.Equals(SessionId)
                                           && g.SourceId.Equals(SourceId)
                                           && (g.Age == null || g.Age.Value <= maxAge)
                                           && g.Id >= firstGroupId
                                        orderby g.Id
                                        select g).Take(pageSize);

                foreach (RTLinkChangeGroup rtLinkChangeGroup in linkGroupQuery)
                {
                    var group = new LinkChangeGroup(rtLinkChangeGroup.GroupName,
                                                    status,
                                                    rtLinkChangeGroup.ContainsConflictedAction,
                                                    rtLinkChangeGroup.Id,
                                                    rtLinkChangeGroup.Age ?? 0,
                                                    rtLinkChangeGroup.RetriesAtCurrAge ?? 0);
                    group.IsForcedSync = rtLinkChangeGroup.IsForcedSync.HasValue ? (bool)rtLinkChangeGroup.IsForcedSync : false;

                    GetLinkChangeActions(group, context);
                    linkChangeGroups.Add(group);

                    lastGroupId = rtLinkChangeGroup.Id;
                }
            }

            return linkChangeGroups.AsReadOnly();
        }

        internal LinkChangeAction LoadSingleLinkChangeAction(long actionId)
        {
            Debug.Assert(actionId != LinkChangeAction.INVALID_INTERNAL_ID && actionId > 0);
                        
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var linkChangeActionQuery =
                    from a in context.RTLinkChangeActionSet
                    where a.Id == actionId
                    select a;

                if (linkChangeActionQuery.Count() != 1)
                {
                    return null;
                }

                linkChangeActionQuery.First().LinkChangeGroupReference.Load();
                RTLinkChangeGroup linkChangeGroup = linkChangeActionQuery.First().LinkChangeGroup;
                Debug.Assert(null != linkChangeGroup);
                var group = new LinkChangeGroup(linkChangeGroup.GroupName,
                                                (LinkChangeGroup.LinkChangeGroupStatus)linkChangeGroup.Status,
                                                linkChangeGroup.ContainsConflictedAction,
                                                linkChangeGroup.Id,
                                                linkChangeGroup.Age ?? 0,
                                                linkChangeGroup.RetriesAtCurrAge ?? 0);
                group.IsForcedSync = linkChangeGroup.IsForcedSync.HasValue ? (bool)linkChangeGroup.IsForcedSync : false;
                return RealizeLinkChangeActionFromEDM(group, linkChangeActionQuery.First());
            }
        }

        private void GetLinkChangeActions(LinkChangeGroup linkChangeGroup, RuntimeEntityModel context)
        {
            int skippedWILinkValue = LinkChangeAction.GetStatusStorageValue(LinkChangeAction.LinkChangeActionStatus.SkipScopedOutWILinks);
            int skippedVCLinkValue = LinkChangeAction.GetStatusStorageValue(LinkChangeAction.LinkChangeActionStatus.SkipScopedOutVCLinks);
            var changeActionQuery = from a in context.RTLinkChangeActionSet
                                    where a.LinkChangeGroup.Id == linkChangeGroup.InternalId
                                       && a.Status != skippedVCLinkValue
                                       && a.Status != skippedWILinkValue
                                    select a;

            foreach (RTLinkChangeAction rtLinkChangeAction in changeActionQuery)
            {
                if (rtLinkChangeAction.SourceId.Equals(SourceId))
                {
                    linkChangeGroup.AddChangeAction(RealizeLinkChangeActionFromEDM(linkChangeGroup, rtLinkChangeAction));
                }
            }
        }

        private LinkChangeAction RealizeLinkChangeActionFromEDM(LinkChangeGroup linkChangeGroup, RTLinkChangeAction rtLinkChangeAction)
        {
            rtLinkChangeAction.ArtifactLinkReference.Load();
            ILink artifactLink = RealizeArtifactLinkFromEDM(rtLinkChangeAction.ArtifactLink);

            var linkChangeAction = new LinkChangeAction(rtLinkChangeAction.ActionId, artifactLink, (LinkChangeAction.LinkChangeActionStatus)rtLinkChangeAction.Status,
                                                        rtLinkChangeAction.Conflicted, rtLinkChangeAction.ExecutionOrder ?? 0, linkChangeGroup,
                                                        rtLinkChangeAction.Id);
            linkChangeAction.ServerLinkChangeId = rtLinkChangeAction.ServerLinkChangeId;
            return linkChangeAction;
        }

        private ILink RealizeArtifactLinkFromEDM(RTArtifactLink rtArtifactLink)
        {
            Debug.Assert(LinkService.LinkEngine.AllSessionServiceContainerPairs.ContainsKey(SessionId)
                && LinkService.LinkEngine.AllSessionServiceContainerPairs[SessionId].ContainsKey(SourceId));
            ServiceContainer serviceContainer = LinkService.LinkEngine.AllSessionServiceContainerPairs[SessionId][SourceId];
            var linkProvider = serviceContainer.GetService(typeof (ILinkProvider)) as ILinkProvider;
            Debug.Assert(null != linkProvider);

            rtArtifactLink.LinkTypeReference.Load();
            string linkTypeRefName = rtArtifactLink.LinkType.ReferenceName;
            
            if (!linkProvider.SupportedLinkTypes.ContainsKey(linkTypeRefName))
            {
                throw new MigrationException(MigrationToolkitResources.ErrorLinkTypeNotSupported, linkTypeRefName);
            }

            LinkType linkType = linkProvider.SupportedLinkTypes[linkTypeRefName];

            var link = new ArtifactLink(rtArtifactLink.SourceArtifactId,
                                        new Artifact(rtArtifactLink.SourceArtifactUri, linkType.SourceArtifactType),
                                        new Artifact(rtArtifactLink.TargetArtifactUri, linkType.TargetArtifactType),
                                        rtArtifactLink.Comment, linkType, rtArtifactLink.IsLocked ?? false);
            return link;
        }

        internal void SaveChangeGroupActionStatus(LinkChangeGroup linkGroup)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var groupQuery = from g in context.RTLinkChangeGroupSet
                                 where g.Id == linkGroup.InternalId
                                 select g;

                Debug.Assert(groupQuery.First() != null);
                RTLinkChangeGroup rtLinkChangeGroup = groupQuery.First();
                rtLinkChangeGroup.Status = (int)linkGroup.Status;
                rtLinkChangeGroup.ContainsConflictedAction = linkGroup.IsConflicted;
                rtLinkChangeGroup.IsForcedSync = linkGroup.IsForcedSync;

                foreach (LinkChangeAction linkAction in linkGroup.Actions)
                {
                    if (linkAction.InternalId == LinkChangeAction.INVALID_INTERNAL_ID)
                    {
                        throw new InvalidOperationException("Error updating link change action: action is not persisted in DB.");
                    }

                    RTLinkChangeAction rtLinkChangeAction = context.RTLinkChangeActionSet.Where
                        (lcg => lcg.Id == linkAction.InternalId).First();

                    rtLinkChangeAction.Status = (int)linkAction.Status;
                    rtLinkChangeAction.Conflicted = linkAction.IsConflicted;
                    rtLinkChangeAction.ServerLinkChangeId = linkAction.ServerLinkChangeId;
                }

                context.TrySaveChanges();
            }
        }

        private RTLinkChangeAction AddChangeAction(LinkChangeAction action, RuntimeEntityModel context)
        {
            if (LinkChangeActionMatchesExistingServerLinkChangeId(action, context))
            {
                TraceManager.TraceInformation(String.Format("LinkChangeGroupManager: Skipping generated link change ({0} link to work item {1}) because it was made by the sync process",
                        action.ChangeActionId == WellKnownChangeActionId.Add ? "Add" : "Delete", action.Link.SourceArtifactId));
                return null;
            }

            if (action.InternalId == LinkChangeAction.INVALID_INTERNAL_ID)
            {
                RTArtifactLink rtArtifactLink = FindCreateLink(action.Link, context);
                Debug.Assert(null != rtArtifactLink, "rtArtifactLink is null.");

                int status = LinkChangeAction.GetStatusStorageValue(action.Status);
                var duplicateActionInDeferralQuery =
                    from a in context.RTLinkChangeActionSet
                    where a.ActionId.Equals(action.ChangeActionId)
                          && a.SessionGroupUniqueId.Equals(SessionGroupId)
                          && a.SessionUniqueId.Equals(SessionId)
                          && a.SourceId.Equals(SourceId)
                          && a.ArtifactLink.Id == rtArtifactLink.Id
                          && a.Status == status 
                    select a.Id;

                if (duplicateActionInDeferralQuery.Count() > 0)
                {
                    // duplicate link change action in deferral status
                    return null;
                }

                var rtLinkChangeAction = RTLinkChangeAction.CreateRTLinkChangeAction(
                    0, SessionGroupId, SessionId, action.ChangeActionId,
                    status,
                    false, SourceId);
                rtLinkChangeAction.ArtifactLink = rtArtifactLink;
                if (!string.IsNullOrEmpty(action.ServerLinkChangeId))
                {
                    rtLinkChangeAction.ServerLinkChangeId = action.ServerLinkChangeId;
                }

                return rtLinkChangeAction;
            }
            
            throw new MigrationException(MigrationToolkitResources.ErrorSaveDuplicateLinkAction, action.InternalId);
        }

        private bool LinkChangeActionMatchesExistingServerLinkChangeId(LinkChangeAction linkChangeAction, RuntimeEntityModel context)
        {
            if (string.IsNullOrEmpty(linkChangeAction.ServerLinkChangeId))
            {
                TraceManager.TraceVerbose("LinkChangeActionMatchesExistingServerLinkChangeId returning false because ServerLinkChangeId is null or empty");
                return false;     
            }
            else
            {
                // Look for existing row in LINK_LINK_CHANGEACTION with linkChangeAction.ServerLinkChangeId;
                int completedStatus = LinkChangeAction.GetStatusStorageValue(LinkChangeAction.LinkChangeActionStatus.Completed);
                var serverLinkChangeIdQuery =
                    from a in context.RTLinkChangeActionSet
                    where a.ActionId.Equals(linkChangeAction.ChangeActionId)
                          && a.SessionGroupUniqueId.Equals(SessionGroupId)
                          && a.SessionUniqueId.Equals(SessionId)
                          && a.SourceId.Equals(SourceId)
                          && a.ServerLinkChangeId.Equals(linkChangeAction.ServerLinkChangeId)
                          && a.Status == completedStatus
                    select a.Id;

                // Return true only if row found
                return (serverLinkChangeIdQuery.Count() > 0);
            }
        }

        private string GetExtendedPropertyString(ILink link)
        {
            var serializer = new GenericSerializer<ExtendedLinkProperties>();
            string extendedProperty = serializer.Serialize(
                link.LinkType.ExtendedProperties ?? new ExtendedLinkProperties(ExtendedLinkProperties.Topology.Network));
            return extendedProperty;
        }

        private RTArtifactLink FindCreateLink(ILink link, RuntimeEntityModel context)
        {
            var rtLink = context.FindArtifactLink(
                link.SourceArtifactId,
                link.SourceArtifact.Uri,
                link.TargetArtifact.Uri,
                link.Comment,
                link.LinkType.ReferenceName,
                link.LinkType.FriendlyName,
                GetExtendedPropertyString(link),
                link.LinkType.SourceArtifactType.ReferenceName,
                link.LinkType.SourceArtifactType.FriendlyName,
                link.LinkType.SourceArtifactType.ContentTypeReferenceName,
                link.LinkType.TargetArtifactType.ReferenceName,
                link.LinkType.TargetArtifactType.FriendlyName,
                link.LinkType.TargetArtifactType.ContentTypeReferenceName,
                true
                ).First();

            rtLink.IsLocked = link.IsLocked;
            return rtLink;

            //var rtArtifactLink = RTArtifactLink.CreateRTArtifactLink(0, link.SourceArtifact.Uri, link.TargetArtifact.Uri);
            //rtArtifactLink.Comment = link.Comment;
            //rtArtifactLink.SourceArtifactId = link.SourceArtifactId;
            
            //var rtLinkTypeId = FindCreateLinkType(link.LinkType);

            //var rtLinkType = context.RTLinkTypeSet.Where(lt => lt.Id == rtLinkTypeId).First();
            //Debug.Assert(null != rtLinkType);

            //rtArtifactLink.LinkType = rtLinkType;
            //return rtArtifactLink;
        }

        private int FindCreateLinkType(LinkType linkType)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                int rtArtifactTypeSourceId = FindCreateArtifactType(linkType.SourceArtifactType);
                int rtArtifactTypeTargetId = FindCreateArtifactType(linkType.TargetArtifactType);

                var linkTypeQuery = from t in context.RTLinkTypeSet
                                    where t.DisplayName.Equals(linkType.FriendlyName)
                                          && t.ReferenceName.Equals(linkType.ReferenceName)
                                          && t.SourceArtifactType.Id == rtArtifactTypeSourceId
                                          && t.TargetArtifactType.Id == rtArtifactTypeTargetId
                                    select t;

                if (linkTypeQuery.Count() > 0)
                {
                    return linkTypeQuery.First().Id;
                }

                var serializer = new GenericSerializer<ExtendedLinkProperties>();
                string extendedProperty =
                    serializer.Serialize(linkType.ExtendedProperties ??
                                         new ExtendedLinkProperties(ExtendedLinkProperties.Topology.Network));

                var rtArtifactTypeSource = context.RTArtifactTypeSet.Where(sAT => sAT.Id == rtArtifactTypeSourceId).First();
                Debug.Assert(null != rtArtifactTypeSource);

                var rtArtifactTypeTarget = context.RTArtifactTypeSet.Where(tAT => tAT.Id == rtArtifactTypeTargetId).First();
                Debug.Assert(null != rtArtifactTypeTarget);

                RTLinkType rtLinkType = RTLinkType.CreateRTLinkType(0, linkType.ReferenceName, linkType.FriendlyName);
                rtLinkType.SourceArtifactType = rtArtifactTypeSource;
                rtLinkType.TargetArtifactType = rtArtifactTypeTarget;
                rtLinkType.ExtendedProperty = extendedProperty;
                context.AddToRTLinkTypeSet(rtLinkType);
                context.TrySaveChanges();
                return rtLinkType.Id;
            }
        }

        private int FindCreateArtifactType(ArtifactType artifactType)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                if (string.IsNullOrEmpty(artifactType.ReferenceName))
                {
                    throw new ArgumentException(string.Format(MigrationToolkitResources.MissingArtifactTypeRefName));
                }
            

                if (string.IsNullOrEmpty(artifactType.FriendlyName))
                {
                    throw new ArgumentException(string.Format(MigrationToolkitResources.MissingArtifactTypeDispName));
                }

                var artifactTypeQuery = from t in context.RTArtifactTypeSet
                                        where t.ReferenceName.Equals(artifactType.ReferenceName)
                                              && t.DisplayName.Equals(artifactType.FriendlyName)
                                              && t.ArtifactContentType.Equals(artifactType.ContentTypeReferenceName)
                                        select t;

                if (artifactTypeQuery.Count() > 0)
                {
                    return artifactTypeQuery.First().Id;
                }

                var newType = RTArtifactType.CreateRTArtifactType(0, artifactType.ReferenceName, artifactType.FriendlyName,
                                                           artifactType.ContentTypeReferenceName);
                context.AddToRTArtifactTypeSet(newType);
                context.TrySaveChanges();
                return newType.Id;
             }
        }

        public void PromoteCreatedLinkChangesToInAnalysis()
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                context.BatchUpdateLinkChangeGroupStatus(SessionGroupId, SessionId, SourceId, false,
                                                         (int)LinkChangeGroup.LinkChangeGroupStatus.Created,
                                                         (int)LinkChangeGroup.LinkChangeGroupStatus.InAnalysis);
            }
        }

        public void PromoteInAnalysisChangesToReadyForMigration()
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                //todo: convert to sproc
                int inAnalysisVal = (int) LinkChangeGroup.LinkChangeGroupStatus.InAnalysisTranslated;
                int translatedVal = LinkChangeAction.GetStatusStorageValue(LinkChangeAction.LinkChangeActionStatus.Translated);
                int readyForMigrVal = (int) LinkChangeGroup.LinkChangeGroupStatus.ReadyForMigration;
                var linkActionQuery = from a in context.RTLinkChangeActionSet
                                      where a.LinkChangeGroup.SessionGroupUniqueId.Equals(SessionGroupId)
                                            && a.LinkChangeGroup.SessionUniqueId.Equals(SessionId)
                                            && a.LinkChangeGroup.SourceId.Equals(SourceId)
                                            && a.LinkChangeGroup.Status == inAnalysisVal
                                            && !a.LinkChangeGroup.ContainsConflictedAction
                                            && a.Status == translatedVal
                                            && a.Conflicted == false
                                      select a;

                foreach (RTLinkChangeAction linkChangeAction in linkActionQuery)
                {
                    linkChangeAction.Status = LinkChangeAction.GetStatusStorageValue(LinkChangeAction.LinkChangeActionStatus.ReadyForMigration);
                }
                context.TrySaveChanges();

                context.BatchUpdateLinkChangeGroupStatus(SessionGroupId, SessionId, SourceId, false,
                                                         inAnalysisVal, readyForMigrVal);
            }
        }

        public void PromoteDeferredLinkChangesToInAnalysis()
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                context.BatchUpdateLinkChangeGroupStatus(SessionGroupId, SessionId, SourceId, false,
                                                         (int)LinkChangeGroup.LinkChangeGroupStatus.InAnalysisDeferred,
                                                         (int)LinkChangeGroup.LinkChangeGroupStatus.InAnalysis);
            }
        }

        public void SaveLinkChangeGroupTranslationResult(ReadOnlyCollection<LinkChangeGroup> linkChangeGroups)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                foreach (LinkChangeGroup linkChangeGroup in linkChangeGroups)
                {
                    SaveLinkChangeGroupTranslationResult(linkChangeGroup, context);
                }

                context.TrySaveChanges();
            }
        }

        private void SaveLinkChangeGroupTranslationResult(LinkChangeGroup linkChangeGroup, RuntimeEntityModel context)
        {
            Debug.Assert(linkChangeGroup.InternalId != LinkChangeGroup.INVALID_INTERNAL_ID);

            var linkChangeGroupQuery = from g in context.RTLinkChangeGroupSet
                                       where g.Id == linkChangeGroup.InternalId
                                       select g;
            Debug.Assert(linkChangeGroupQuery.Count() == 1);
            RTLinkChangeGroup rtLinkChangeGroup = linkChangeGroupQuery.First();
            
            // update the source side link change group status
            rtLinkChangeGroup.Status = (int)linkChangeGroup.Status;
            if (linkChangeGroup.Status == LinkChangeGroup.LinkChangeGroupStatus.InAnalysisDeferred)
            {
                // if the link change group cannot be translated in the current run
                // we check the number of translation attemps that have been made at the group's current age
                // if it has reached the max number of retries allowed for this age, we increment its age and reset the retry count
                rtLinkChangeGroup.RetriesAtCurrAge = rtLinkChangeGroup.RetriesAtCurrAge ?? 0;
                rtLinkChangeGroup.Age = rtLinkChangeGroup.Age ?? 0;
                if (++rtLinkChangeGroup.RetriesAtCurrAge >= LinkEngine.AgeInterveralSecAndRetries[rtLinkChangeGroup.Age.Value, 1])
                {
                    rtLinkChangeGroup.Age++;
                    rtLinkChangeGroup.RetriesAtCurrAge = 0;
                }
            }

            // identify translated actions in the group
            List<LinkChangeAction> translatedActions = new List<LinkChangeAction>();
            foreach (LinkChangeAction action in linkChangeGroup.Actions)
            {
                if (action.Status == LinkChangeAction.LinkChangeActionStatus.Translated)
                {
                    translatedActions.Add(action);
                    UpdateLinkChangeActionStatus(action.InternalId, LinkChangeAction.LinkChangeActionStatus.DeltaCompleted, context);
                }
                else
                {
                    UpdateLinkChangeActionStatus(action.InternalId, action.Status, context);
                }
            }

            if (AllActionsTranslated(linkChangeGroup))
            {
                // mark group completed when all its actions are successfully translated
                rtLinkChangeGroup.Status = (int)LinkChangeGroup.LinkChangeGroupStatus.Completed;
            }
            else
            {
                rtLinkChangeGroup.ContainsConflictedAction = linkChangeGroup.IsConflicted;
            }

            // move the translated actions to the target side (current side) and create a new group to store them
            LinkChangeGroup translatedGroup = new LinkChangeGroup(
                linkChangeGroup.GroupName, LinkChangeGroup.LinkChangeGroupStatus.InAnalysisTranslated, false, linkChangeGroup.IsForcedSync);
            foreach (LinkChangeAction action in translatedActions)
            {
                action.InternalId = LinkChangeAction.INVALID_INTERNAL_ID;
                translatedGroup.AddChangeAction(action);
            }

            RTLinkChangeGroup rtLinkChangeGroupTranslated = AddLinkChangeGroup(translatedGroup);
            if (rtLinkChangeGroupTranslated != null)
            {
                context.Attach(rtLinkChangeGroupTranslated);
            }
        }

        private bool AllActionsTranslated(LinkChangeGroup linkChangeGroup)
        {
            foreach (LinkChangeAction action in linkChangeGroup.Actions)
            {
                switch (action.Status)
                {
                    case LinkChangeAction.LinkChangeActionStatus.Created:
                    case LinkChangeAction.LinkChangeActionStatus.SkipScopedOutVCLinks:
                    case LinkChangeAction.LinkChangeActionStatus.SkipScopedOutWILinks:
                        if (action.IsConflicted)
                        {
                            linkChangeGroup.IsConflicted = true;
                        }
                        return false;
                    default:
                        continue;
                }
            }

            return true;
        }

        private void UpdateLinkChangeActionStatus(
            long internalId, 
            LinkChangeAction.LinkChangeActionStatus linkChangeActionStatus, 
            RuntimeEntityModel context)
        {
            Debug.Assert(internalId != LinkChangeAction.INVALID_INTERNAL_ID);

            var linkChangeActionQuery = from a in context.RTLinkChangeActionSet
                                        where a.Id == internalId
                                        select a;
            Debug.Assert(linkChangeActionQuery.Count() == 1);
            RTLinkChangeAction rtLinkChangeAction = linkChangeActionQuery.First();

            rtLinkChangeAction.Status = (int)linkChangeActionStatus;
        }

        public bool IsActionInDelta(LinkChangeAction linkChangeAction)
        {
            ILink link = linkChangeAction.Link;
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var duplicateActionInDelta = context.FindLinkChangeActionInDelta(
                    SessionGroupId,
                    SessionId,
                    SourceId,
                    linkChangeAction.ChangeActionId,
                    link.SourceArtifactId,
                    link.SourceArtifact.Uri,
                    link.TargetArtifact.Uri,
                    link.Comment,
                    link.LinkType.ReferenceName,
                    link.LinkType.FriendlyName,
                    GetExtendedPropertyString(link),
                    link.LinkType.SourceArtifactType.ReferenceName,
                    link.LinkType.SourceArtifactType.FriendlyName,
                    link.LinkType.SourceArtifactType.ContentTypeReferenceName,
                    link.LinkType.TargetArtifactType.ReferenceName,
                    link.LinkType.TargetArtifactType.FriendlyName,
                    link.LinkType.TargetArtifactType.ContentTypeReferenceName);

                return duplicateActionInDelta.Count() > 0;
            }
        }

        internal LinkChangeAction TryFindLastDeleteAction(LinkChangeAction queryAction)
        {
            ILink link = queryAction.Link;
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                int completedStatus = (int)LinkChangeAction.LinkChangeActionStatus.Completed;
                string extendedLinkProperties = GetExtendedPropertyString(link);
                var query =
                    (from lcAction in context.RTLinkChangeActionSet
                     where lcAction.SessionGroupUniqueId.Equals(SessionGroupId)
                     && lcAction.SessionUniqueId.Equals(SessionId)
                     && lcAction.SourceId.Equals(SourceId)
                     && lcAction.Status == completedStatus
                     && lcAction.ArtifactLink.SourceArtifactId.Equals(link.SourceArtifactId)
                     && lcAction.ArtifactLink.SourceArtifactUri.Equals(link.SourceArtifact.Uri)
                     && lcAction.ArtifactLink.TargetArtifactUri.Equals(link.TargetArtifact.Uri)
                     && lcAction.ArtifactLink.Comment.Equals(link.Comment)
                     && lcAction.ArtifactLink.LinkType.ReferenceName.Equals(link.LinkType.ReferenceName)
                     && lcAction.ArtifactLink.LinkType.ExtendedProperty.Equals(extendedLinkProperties)
                     && lcAction.ArtifactLink.LinkType.SourceArtifactType.ReferenceName.Equals(link.LinkType.SourceArtifactType.ReferenceName)
                     && lcAction.ArtifactLink.LinkType.SourceArtifactType.ArtifactContentType.Equals(link.LinkType.SourceArtifactType.ContentTypeReferenceName)
                     && lcAction.ArtifactLink.LinkType.TargetArtifactType.ReferenceName.Equals(link.LinkType.TargetArtifactType.ReferenceName)
                     && lcAction.ArtifactLink.LinkType.TargetArtifactType.ArtifactContentType.Equals(link.LinkType.TargetArtifactType.ContentTypeReferenceName)
                     orderby lcAction.Id descending
                     select lcAction).Take(1);

                if (query.Count() == 0
                    || !query.First().ActionId.Equals(WellKnownChangeActionId.Delete))
                {
                    return null;
                }

                return new LinkChangeAction(
                    WellKnownChangeActionId.Delete,
                    queryAction.Link,
                    LinkChangeAction.LinkChangeActionStatus.Completed,
                    query.First().Conflicted);
            }
        }

        internal void TryDeprecateActiveAddActionMigrationInstruction(LinkChangeAction queryAction)
        {
            ILink link = queryAction.Link;
            int translatedStatus = LinkChangeAction.GetStatusStorageValue(LinkChangeAction.LinkChangeActionStatus.Translated);
            int readyForMigrationStatus = LinkChangeAction.GetStatusStorageValue(LinkChangeAction.LinkChangeActionStatus.ReadyForMigration);
            
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                
                string extendedLinkProperties = GetExtendedPropertyString(link);
                var query =
                    (from lcAction in context.RTLinkChangeActionSet
                     where lcAction.ActionId.Equals(WellKnownChangeActionId.Add)
                     && lcAction.SessionGroupUniqueId.Equals(SessionGroupId)
                     && lcAction.SessionUniqueId.Equals(SessionId)
                     && lcAction.SourceId.Equals(SourceId)
                     && (lcAction.Status == translatedStatus || lcAction.Status == readyForMigrationStatus)
                     && lcAction.ArtifactLink.SourceArtifactId.Equals(link.SourceArtifactId)
                     && lcAction.ArtifactLink.SourceArtifactUri.Equals(link.SourceArtifact.Uri)
                     && lcAction.ArtifactLink.TargetArtifactUri.Equals(link.TargetArtifact.Uri)
                     && lcAction.ArtifactLink.Comment.Equals(link.Comment)
                     && lcAction.ArtifactLink.LinkType.ReferenceName.Equals(link.LinkType.ReferenceName)
                     && lcAction.ArtifactLink.LinkType.ExtendedProperty.Equals(extendedLinkProperties)
                     && lcAction.ArtifactLink.LinkType.SourceArtifactType.ReferenceName.Equals(link.LinkType.SourceArtifactType.ReferenceName)
                     && lcAction.ArtifactLink.LinkType.SourceArtifactType.ArtifactContentType.Equals(link.LinkType.SourceArtifactType.ContentTypeReferenceName)
                     && lcAction.ArtifactLink.LinkType.TargetArtifactType.ReferenceName.Equals(link.LinkType.TargetArtifactType.ReferenceName)
                     && lcAction.ArtifactLink.LinkType.TargetArtifactType.ArtifactContentType.Equals(link.LinkType.TargetArtifactType.ContentTypeReferenceName)
                     orderby lcAction.Id descending
                     select lcAction).Take(1);


                foreach (var action in query)
                {
                    action.Status = LinkChangeAction.GetStatusStorageValue(LinkChangeAction.LinkChangeActionStatus.Skipped);
                }

                context.TrySaveChanges();
            }
        }

        internal bool IsLinkMigratedBefore(ILink link)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                int completedStatus = (int)LinkChangeAction.LinkChangeActionStatus.Completed;
                string extendedLinkProperties = GetExtendedPropertyString(link);
                var query =
                    (from lcAction in context.RTLinkChangeActionSet
                     where lcAction.SessionGroupUniqueId.Equals(SessionGroupId)
                     && lcAction.SessionUniqueId.Equals(SessionId)
                     && lcAction.SourceId.Equals(SourceId)
                     && lcAction.Status == completedStatus
                     && lcAction.ArtifactLink.SourceArtifactId.Equals(link.SourceArtifactId)
                     && lcAction.ArtifactLink.SourceArtifactUri.Equals(link.SourceArtifact.Uri)
                     && lcAction.ArtifactLink.TargetArtifactUri.Equals(link.TargetArtifact.Uri)
                     && lcAction.ArtifactLink.Comment.Equals(link.Comment)
                     && lcAction.ArtifactLink.LinkType.ReferenceName.Equals(link.LinkType.ReferenceName)
                     && lcAction.ArtifactLink.LinkType.ExtendedProperty.Equals(extendedLinkProperties)
                     && lcAction.ArtifactLink.LinkType.SourceArtifactType.ReferenceName.Equals(link.LinkType.SourceArtifactType.ReferenceName)
                     && lcAction.ArtifactLink.LinkType.SourceArtifactType.ArtifactContentType.Equals(link.LinkType.SourceArtifactType.ContentTypeReferenceName)
                     && lcAction.ArtifactLink.LinkType.TargetArtifactType.ReferenceName.Equals(link.LinkType.TargetArtifactType.ReferenceName)
                     && lcAction.ArtifactLink.LinkType.TargetArtifactType.ArtifactContentType.Equals(link.LinkType.TargetArtifactType.ContentTypeReferenceName)
                     orderby lcAction.Id descending
                     select lcAction).Take(1);

                return query.Count() > 0;
            }
        }
    }
}