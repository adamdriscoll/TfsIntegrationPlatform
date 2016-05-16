// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Linking
{
    public class LinkService : ILinkTranslationService, IServiceProvider
    {
        readonly LinkChangeGroupManager m_linkChangeGroupManager;

        internal LinkService(LinkEngine linkEngine, Guid sessionId, Guid sourceId)
        {
            if (null == linkEngine)
            {
                throw new ArgumentNullException("linkEngine");
            }

            LinkEngine = linkEngine;
            SessionId = sessionId;
            SourceId = sourceId;
            m_linkChangeGroupManager = new LinkChangeGroupManager(SessionGroupId, SessionId, SourceId, this);
        }

        #region IServiceProvider Members

        public object GetService(Type serviceType)
        {
            if (!serviceType.Equals(typeof(LinkService)))
            {
                return null;
            }

            return this;
        }

        #endregion

        public void AddChangeGroups(List<LinkChangeGroup> groups)
        {
            foreach (LinkChangeGroup g in groups)
            {
                m_linkChangeGroupManager.AddLinkChangeGroup(g);
            }
        }

        /// <summary>
        /// Batch saves status changes of the change group and its actions
        /// </summary>
        /// <param name="group"></param>
        public void SaveChangeGroupActionStatus(LinkChangeGroup group)
        {
            if (group.InternalId == LinkChangeGroup.INVALID_INTERNAL_ID)
            {
                throw new InvalidOperationException("Error updating link change group: Group is not persisted in DB.");
            }
            m_linkChangeGroupManager.SaveChangeGroupActionStatus(group);
        }
        
        public LinkConfigurationLookupService LinkConfigurationLookupService
        {
            get
            {
                return LinkEngine.LinkConfigurationLookupService;
            }
        }

        public bool LinkTypeSupportedByOtherSide(string linkTypeReferenceName)
        {
            return LinkEngine.LinkTypeSupportedByOtherSide(linkTypeReferenceName, SessionId, SourceId);
        }

        public bool IsActionInDelta(LinkChangeAction linkChangeAction)
        {
            return m_linkChangeGroupManager.IsActionInDelta(linkChangeAction);
        }

        public ReadOnlyCollection<LinkChangeGroup> GetLinkChangeGroups(
            long firstGroupId,
            int pageSize,
            LinkChangeGroup.LinkChangeGroupStatus status,
            bool? includeConflicted,
            out long lastGroupId)
        {
            return m_linkChangeGroupManager.GetPagedLinkChangeGroups(firstGroupId, pageSize, status, includeConflicted, out lastGroupId);
        }

        public ReadOnlyCollection<LinkChangeGroup> GetLinkChangeGroups(
            long firstGroupId,
            int pageSize,
            LinkChangeGroup.LinkChangeGroupStatus status,
            bool? includeConflicted,
            int maxAge,
            out long lastGroupId)
        {
            return m_linkChangeGroupManager.GetPagedLinkChangeGroups(firstGroupId, pageSize, status, includeConflicted, maxAge, out lastGroupId);
        }

        public bool ChangeGroupIsCompleted(LinkChangeGroup changeGroup)
        {
            foreach (LinkChangeAction action in changeGroup.Actions)
            {
                if (action.Status != LinkChangeAction.LinkChangeActionStatus.Skipped
                    && action.Status != LinkChangeAction.LinkChangeActionStatus.Completed
                    && action.Status != LinkChangeAction.LinkChangeActionStatus.DeltaCompleted)
                {
                    return false;
                }
            }

            return true;
        }

        public bool AllLinkMigrationInstructionsAreConflicted(LinkChangeGroup changeGroup)
        {
            bool allMigrationInstructionAreConflicted = true;
            bool containsConflictedMigrationInstruction = false;
            foreach (LinkChangeAction action in changeGroup.Actions)
            {
                if (action.Status == LinkChangeAction.LinkChangeActionStatus.ReadyForMigration)
                {
                    if (action.IsConflicted)
                    {
                        containsConflictedMigrationInstruction = true;
                    }
                    else
                    {
                        allMigrationInstructionAreConflicted = false;
                        break;
                    }
                }
            }

            return allMigrationInstructionAreConflicted & containsConflictedMigrationInstruction;
        }

        public bool ContainsSpecialSkipActions(LinkChangeGroup changeGroup)
        {
            foreach (LinkChangeAction action in changeGroup.Actions)
            {
                if (action.Status == LinkChangeAction.LinkChangeActionStatus.SkipScopedOutVCLinks
                    || action.Status == LinkChangeAction.LinkChangeActionStatus.SkipScopedOutWILinks)
                {
                    return true;
                }
            }

            return false;
        }

        internal LinkEngine LinkEngine
        {
            get;
            private set;
        }

        internal Guid SessionGroupId
        {
            get
            {
                return LinkEngine.SessionGroupId;
            }
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

        internal void PromoteCreatedLinkChangesToInAnalysis()
        {
            m_linkChangeGroupManager.PromoteCreatedLinkChangesToInAnalysis();
        }

        internal void PromoteDeferredLinkChangesToInAnalysis()
        {
            m_linkChangeGroupManager.PromoteDeferredLinkChangesToInAnalysis();
        }

        internal void SaveLinkChangeGroupTranslationResult(ReadOnlyCollection<LinkChangeGroup> linkChangeGroups)
        {
            m_linkChangeGroupManager.SaveLinkChangeGroupTranslationResult(linkChangeGroups);
        }

        internal void PromoteInAnalysisChangesToReadyForMigration()
        {
            m_linkChangeGroupManager.PromoteInAnalysisChangesToReadyForMigration();
        }

        internal void TryDeprecateActiveAddActionMigrationInstruction(LinkChangeAction queryAction)
        {
            m_linkChangeGroupManager.TryDeprecateActiveAddActionMigrationInstruction(queryAction);
        }

        internal LinkChangeAction LoadSingleLinkChangeAction(long linkChangeActionId)
        {
            return m_linkChangeGroupManager.LoadSingleLinkChangeAction(linkChangeActionId);
        }

        internal LinkChangeAction TryFindLastDeleteAction(LinkChangeAction queryAction)
        {
            return m_linkChangeGroupManager.TryFindLastDeleteAction(queryAction);
        }

        internal bool IsLinkMigratedBefore(ILink link)
        {
            return m_linkChangeGroupManager.IsLinkMigratedBefore(link);
        }
    }
}