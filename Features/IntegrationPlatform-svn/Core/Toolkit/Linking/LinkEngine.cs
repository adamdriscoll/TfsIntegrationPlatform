// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Linking
{
    internal class LinkEngine
    {
        internal static readonly double[,] AgeInterveralSecAndRetries = 
            {
             {          5 * 60, 12},     // age 0: wait for 5 mins, retry 12 times
             {    24 * 60 * 60, 7},     // age 1: wait for 1 day, retry max 7 times
             {7 * 24 * 60 * 60, 2}      // age 2: wait for 7 day, retry max twice
            };

        private enum LinkEnd
        {
            SourceArtifact,
            TargetArtifact,
        }

        private readonly LinkConfigurationLookupService m_configLookupService;
        private const int MaxChangeActionsInSlice = 10000;
        private readonly Dictionary<SessionTypeEnum, Dictionary<Guid, Dictionary<Guid, ServiceContainer>>> m_perSessionTypeServiceContainerPairs;
        private readonly Dictionary<Guid, Dictionary<Guid, ServiceContainer>> m_allServiceContainerPairs;
        private readonly Dictionary<string, List<NonCyclicReferenceClosure>> m_nonCyclicRefClosures;
        private PerSourceTranslationTimestamp m_perSourceTranslationTimestamp = new PerSourceTranslationTimestamp();

        internal LinkEngine(
            Guid sessionGroupId, 
            ConflictManager conflictManager, 
            LinkingElement linkingElement)
        {
            SessionGroupId = sessionGroupId;
            m_perSessionTypeServiceContainerPairs = 
                new Dictionary<SessionTypeEnum, Dictionary<Guid, Dictionary<Guid, ServiceContainer>>>();
            m_allServiceContainerPairs = new Dictionary<Guid, Dictionary<Guid, ServiceContainer>>();
            ConflictManager = conflictManager;
            ConflictManager.LinkEngine = this;
            m_configLookupService = new LinkConfigurationLookupService(linkingElement);
            m_nonCyclicRefClosures = new Dictionary<string, List<NonCyclicReferenceClosure>>();

            RegisterLinkConflicts();
        }

        internal ErrorManager ErrorManager
        {
            get;
            set;
        }

        private void RegisterLinkConflicts()
        {
            ConflictManager.RegisterToolkitConflictType(new CyclicLinkReferenceConflictType(),
                SyncOrchestrator.ConflictsSyncOrchOptions.Continue);
            ConflictManager.RegisterToolkitConflictType(new GenericConflictType(), SyncOrchestrator.ConflictsSyncOrchOptions.StopConflictedSessionCurrentTrip);
        }

        internal void AddSessionServiceContainers(
            Guid sessionId, 
            Dictionary<Guid, ServiceContainer> serviceContainers, 
            SessionTypeEnum sessionType)
        {
            if (serviceContainers == null)
            {
                throw new ArgumentNullException("serviceContainers");
            }

            if (!m_perSessionTypeServiceContainerPairs.ContainsKey(sessionType))
            {
                m_perSessionTypeServiceContainerPairs.Add(
                    sessionType, 
                    new Dictionary<Guid, Dictionary<Guid, ServiceContainer>>());
            }

            Debug.Assert(!m_perSessionTypeServiceContainerPairs[sessionType].ContainsKey(sessionId));
            m_perSessionTypeServiceContainerPairs[sessionType].Add(sessionId, serviceContainers);

            Debug.Assert(!m_allServiceContainerPairs.ContainsKey(sessionId));
            m_allServiceContainerPairs.Add(sessionId, serviceContainers);
        }
        
        public LinkConfigurationLookupService LinkConfigurationLookupService
        {
            get { return m_configLookupService; }
        } 

        internal ConflictManager ConflictManager
        {
            get; 
            private set;
        }

        /// <summary>
        /// Gets the session group identifier.
        /// </summary>
        internal Guid SessionGroupId
        {
            get; 
            private set;
        }

        internal Dictionary<Guid, Dictionary<Guid, ServiceContainer>> AllSessionServiceContainerPairs
        {
            get
            {
                return m_allServiceContainerPairs;
            }
        }

        /// <summary>
        /// Process the links detected from a particular side of the session.
        /// </summary>
        /// <param name="sessionId">The session unique id.</param>
        /// <param name="sourceId">Source-side Migration Source of the link.</param>
        internal void AnalyzeLinkDelta(
            Guid sessionId,
            Guid sourceId,
            bool bidirection)
        {
            try
            {
                if (m_configLookupService.IsLinkingDisabled)
                {
                    return;
                }

                var sourceLinkService = GetService(sessionId, sourceId, typeof(LinkService)) as LinkService;
                if (null == sourceLinkService)
                {
                    return;
                }

                var configService = GetService(sessionId, sourceId, typeof(ConfigurationService)) as ConfigurationService;
                Debug.Assert(null != configService);

                var targetLinkService = GetService(sessionId, configService.MigrationPeer, typeof(LinkService)) as LinkService;
                if (null == targetLinkService)
                {
                    return;
                }
                var targetLinkProvider = GetService(sessionId, configService.MigrationPeer, typeof(ILinkProvider)) as ILinkProvider;
                if (null == targetLinkProvider)
                {
                    return;
                }

                TraceManager.TraceInformation("Start translating link change actions");
                TranslatePhase1(sourceLinkService, targetLinkService);
                TranslatePhase2(targetLinkService, sourceLinkService, bidirection);
                TraceManager.TraceInformation("Finish translating link change actions");

                #region obsolete - using TFS OM to detect link rule violation
                //TraceManager.TraceInformation("Start detecting basic link change conflicts");
                //BasicConflictDetection(
                //    sessionId,
                //    sourceLinkService, sourceId,
                //    targetLinkService, configService.MigrationPeer,
                //    targetLinkProvider);
                //TraceManager.TraceInformation("Finish detecting basic link change conflicts");                
                #endregion

                TraceManager.TraceInformation("Start detecting system-specific link change conflicts");
                targetLinkProvider.Analyze(targetLinkService, sessionId, sourceId);
                TraceManager.TraceInformation("Finish detecting system-specific link change conflicts");

                // mark analyzed target-side link migration instruction ready
                // DO NOT mark target-side delta completed
                targetLinkService.PromoteInAnalysisChangesToReadyForMigration();
            }
            catch (Exception e)
            {
                ErrorManager.TryHandleException(e, ConflictManager);
            }
        }

        private void BasicConflictDetection(
            Guid sessionId, 
            LinkService sourceLinkService, Guid sourceSourceId, 
            LinkService targetLinkService, Guid targetSourceId,
            ILinkProvider targetLinkProvider)
        {
            NonCyclicReferenceDetection(targetLinkService, targetLinkProvider);

            SingleParentAnalysis(
                sessionId,
                sourceLinkService,
                sourceSourceId,
                targetLinkService,
                targetSourceId,
                targetLinkProvider);
        }

        private void SingleParentAnalysis(
            Guid sessionId, 
            LinkService sourceLinkService, 
            Guid sourceSourceId, 
            LinkService targetLinkService, 
            Guid targetSourceId,
            ILinkProvider targetLinkProvider)
        {
            bool hasSingleParentLinkType = false;
            foreach (var linkType in targetLinkProvider.SupportedLinkTypes)
            {
                if (linkType.Value.ExtendedProperties.HasOnlyOneParent)
                {
                    hasSingleParentLinkType = true;
                    break;
                }
            }
            if (!hasSingleParentLinkType) return;

            long firstChangeGroupId = 0;
            const LinkChangeGroup.LinkChangeGroupStatus status = LinkChangeGroup.LinkChangeGroupStatus.InAnalysisTranslated;

            while (true)
            {
                long lastChangeGroupId;
                ReadOnlyCollection<LinkChangeGroup> linkChangeGroups =
                    targetLinkService.GetLinkChangeGroups(firstChangeGroupId, MaxChangeActionsInSlice, status, null, out lastChangeGroupId);

                if (linkChangeGroups.Count <= 0 || lastChangeGroupId < firstChangeGroupId) break;
                firstChangeGroupId = lastChangeGroupId + 1;

                foreach (LinkChangeGroup linkChangeGroup in linkChangeGroups)
                {
                    bool actionConflictDetected = false;
                    foreach (LinkChangeAction action in linkChangeGroup.Actions)
                    {
                        if (!action.Link.LinkType.ExtendedProperties.HasOnlyOneParent)
                        {
                            continue;
                        }

                        if (!ActionIsReadyForAnalysis(action))
                        {
                            continue;
                        }

                        if (!action.ChangeActionId.Equals(WellKnownChangeActionId.Add))
                        {
                            continue;
                        }

                        LinkType linkType = action.Link.LinkType;
                        IArtifact targetArtifact = action.Link.TargetArtifact;
                        IArtifact[] parentArtifacts;
                        bool typedLinkParentsRslt = targetLinkProvider.TryGetSingleParentLinkSourceArtifacts(
                            linkType, targetArtifact, out parentArtifacts);
                        
                        if (!typedLinkParentsRslt || null == parentArtifacts)
                        {
                            continue;
                        }

                        if (parentArtifacts.Length > 0)
                        {
                            // found cyclic referencing link
                            // todo: raise a conflict
                            actionConflictDetected = true;
                            action.Status = LinkChangeAction.LinkChangeActionStatus.Skipped;
                        }
                    }

                    if (actionConflictDetected)
                    {
                        targetLinkService.SaveChangeGroupActionStatus(linkChangeGroup);
                    }
                }
            }
        }

        private bool ActionIsReadyForAnalysis(LinkChangeAction action)
        {
            return !action.IsConflicted
                   && action.Status == LinkChangeAction.LinkChangeActionStatus.Translated;
        }

        private bool TryFindClosureInCache(
            ILink link, 
            out NonCyclicReferenceClosure closure)
        {
            Debug.Assert(null != link && null != link.LinkType);

            closure = null;
            if (!m_nonCyclicRefClosures.ContainsKey(link.LinkType.ReferenceName))
            {
                return false;
            }

            var closures = m_nonCyclicRefClosures[link.LinkType.ReferenceName];
            foreach (NonCyclicReferenceClosure cachedClosure in closures)
            {
                if (cachedClosure.SourceArtifactUris.Contains(link.SourceArtifact.Uri)
                    || cachedClosure.SourceArtifactUris.Contains(link.TargetArtifact.Uri)
                    || cachedClosure.TargetArtifactUris.Contains(link.SourceArtifact.Uri)
                    || cachedClosure.TargetArtifactUris.Contains(link.TargetArtifact.Uri))
                {
                    closure = cachedClosure;
                    return true;
                }
            }

            return false;
        }

        private void AddClosureToCache(
            NonCyclicReferenceClosure closure)
        {
            string linkTypeRefName = closure.LinkType.ReferenceName;
            if (!m_nonCyclicRefClosures.ContainsKey(linkTypeRefName))
            {
                m_nonCyclicRefClosures.Add(linkTypeRefName, new List<NonCyclicReferenceClosure>());
            }

            m_nonCyclicRefClosures[linkTypeRefName].Add(closure);
        }

        private void NonCyclicReferenceDetection(
            LinkService targetLinkService, 
            ILinkProvider targetLinkProvider)
        {
            bool hasNonCyclicRefLinkType = false;
            foreach (var linkType in targetLinkProvider.SupportedLinkTypes)
            {
                if (linkType.Value.ExtendedProperties.NonCircular)
                {
                    hasNonCyclicRefLinkType = true;
                    break;
                }
            }
            if (!hasNonCyclicRefLinkType) return;

            long firstChangeGroupId = 0;
            const LinkChangeGroup.LinkChangeGroupStatus status = LinkChangeGroup.LinkChangeGroupStatus.InAnalysisTranslated;

            while (true)
            {
                long lastChangeGroupId;
                ReadOnlyCollection<LinkChangeGroup> linkChangeGroups =
                    targetLinkService.GetLinkChangeGroups(firstChangeGroupId++, MaxChangeActionsInSlice, status, null, out lastChangeGroupId);

                if (linkChangeGroups.Count <= 0 || lastChangeGroupId < firstChangeGroupId) break;
                firstChangeGroupId = lastChangeGroupId + 1;

                Dictionary<string, Dictionary<string, NonCyclicReferenceClosure>> perSrcWorkItemPerLinkTypeClosure =
                        new Dictionary<string, Dictionary<string, NonCyclicReferenceClosure>>();
                Dictionary<ILink, LinkChangeAction> perGroupLinkToActionDict = new Dictionary<ILink, LinkChangeAction>();

                foreach (LinkChangeGroup linkChangeGroup in linkChangeGroups)
                {
                    perSrcWorkItemPerLinkTypeClosure.Clear();
                    perGroupLinkToActionDict.Clear();

                    foreach (LinkChangeAction action in linkChangeGroup.Actions)
                    {
                        if (!action.Link.LinkType.ExtendedProperties.NonCircular)
                        {
                            continue;
                        }

                        if (!ActionIsReadyForAnalysis(action))
                        {
                            continue;
                        }

                        if (!perSrcWorkItemPerLinkTypeClosure.ContainsKey(action.Link.SourceArtifact.Uri))
                        {
                            perSrcWorkItemPerLinkTypeClosure.Add(action.Link.SourceArtifact.Uri, new Dictionary<string,NonCyclicReferenceClosure>());
                        }
                        if (!perSrcWorkItemPerLinkTypeClosure[action.Link.SourceArtifact.Uri].ContainsKey(action.Link.LinkType.ReferenceName))
                        {
                            NonCyclicReferenceClosure closure = targetLinkProvider.CreateNonCyclicLinkReferenceClosure(
                                action.Link.LinkType, action.Link.SourceArtifact);
                            Debug.Assert(closure.InvalidLinks.Count == 0, "closure.InvalidLinks.Count != 0");
                            perSrcWorkItemPerLinkTypeClosure[action.Link.SourceArtifact.Uri].Add(action.Link.LinkType.ReferenceName, closure);
                        }

                        if (action.ChangeActionId.Equals(WellKnownChangeActionId.Add))
                        {
                            perSrcWorkItemPerLinkTypeClosure[action.Link.SourceArtifact.Uri][action.Link.LinkType.ReferenceName].AddLinkForAnalysis(action.Link);
                            perGroupLinkToActionDict.Add(action.Link, action);
                        }
                        else if (action.ChangeActionId.Equals(WellKnownChangeActionId.Delete))
                        {
                            perSrcWorkItemPerLinkTypeClosure[action.Link.SourceArtifact.Uri][action.Link.LinkType.ReferenceName].DeleteLinkForAnaysis(action.Link);
                            perGroupLinkToActionDict.Add(action.Link, action);
                        }
                    }

                    foreach (string sourceArtifactUri in perSrcWorkItemPerLinkTypeClosure.Keys)
                    {
                        foreach (var linkTypeClosure in perSrcWorkItemPerLinkTypeClosure[sourceArtifactUri])
                        {
                            int invalidLinksAfterAdd = linkTypeClosure.Value.InvalidLinks.Count;
                            if (invalidLinksAfterAdd > 0)
                            {
                                foreach (ILink invalidLink in linkTypeClosure.Value.InvalidLinks)
                                {
                                    LinkChangeAction conflictedAction;
                                    if (perGroupLinkToActionDict.TryGetValue(invalidLink, out conflictedAction))
                                    {
                                        // action.Status = LinkChangeAction.LinkChangeActionStatus.Skipped;
                                        string details = CyclicLinkReferenceConflictType.CreateConflictDetails(conflictedAction);
                                        string scopeHint = CyclicLinkReferenceConflictType.CreateScopeHint(SessionGroupId, conflictedAction);
                                        MigrationConflict conflict = new MigrationConflict(new CyclicLinkReferenceConflictType(),
                                                                                           MigrationConflict.Status.Unresolved,
                                                                                           details,
                                                                                           scopeHint);
                                        conflict.ConflictedLinkChangeAction = conflictedAction;

                                        List<MigrationAction> actions;
                                        ConflictManager.TryResolveNewConflict(ConflictManager.SourceId,
                                                                              conflict,
                                                                              out actions);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


        public void GenerateLinkDelta(
            Guid sessionId, 
            Guid sourceId)
        {
            try
            {
                if (m_configLookupService.IsLinkingDisabled)
                {
                    return;
                }

                var linkProvider = GetService(sessionId, sourceId, typeof(ILinkProvider)) as ILinkProvider;
                if (null == linkProvider)
                {
                    return;
                }
                var linkService = GetService(sessionId, sourceId, typeof(LinkService)) as LinkService;
                if (null == linkService)
                {
                    return;
                }

                ReadOnlyCollection<LinkChangeGroup> changeGroups = linkProvider.GenerateNextLinkDeltaSlice(linkService, MaxChangeActionsInSlice);
                while (changeGroups.Count > 0)
                {
                    changeGroups = linkProvider.GenerateNextLinkDeltaSlice(linkService, MaxChangeActionsInSlice);
                }
            }
            catch (Exception e)
            {
                ErrorManager.TryHandleException(e, ConflictManager);
            }
        }

        public void MigrateLinks(
            Guid sessionId, 
            Guid sourceId)
        {
            try
            {
                if (m_configLookupService.IsLinkingDisabled)
                {
                    return;
                }

                var linkProvider = GetService(sessionId, sourceId, typeof(ILinkProvider)) as ILinkProvider;
                if (null == linkProvider)
                {
                    return;
                }
                var linkService = GetService(sessionId, sourceId, typeof(LinkService)) as LinkService;
                if (null == linkService)
                {
                    return;
                }

                long firstChangeGroupId = 0;
                const LinkChangeGroup.LinkChangeGroupStatus status = LinkChangeGroup.LinkChangeGroupStatus.ReadyForMigration;

                while (true)
                {
                    long lastChangeGroupId;
                    ReadOnlyCollection<LinkChangeGroup> pagedChangeGroups = linkService.GetLinkChangeGroups(
                        firstChangeGroupId, MaxChangeActionsInSlice, status, false, out lastChangeGroupId);

                    if (pagedChangeGroups.Count <= 0 || lastChangeGroupId < firstChangeGroupId) break;
                    firstChangeGroupId = lastChangeGroupId + 1;

                    foreach (LinkChangeGroup changeGroup in pagedChangeGroups)
                    {
                        if (ChangeGroupIsCompleted(changeGroup))
                        {
                            changeGroup.Status = LinkChangeGroup.LinkChangeGroupStatus.Completed;
                        }
                        else if (AllLinkMigrationInstructionsAreConflicted(changeGroup))
                        {
                            changeGroup.IsConflicted = true;
                        }
                        else
                        {
                            LinkChangeGroup.LinkChangeGroupStatus statusCache = changeGroup.Status;

                            WorkItemLinkStore store = new WorkItemLinkStore(sourceId);
                            store.ValidateLinkChangeMigrationInstructions(changeGroup);
                        }
                        linkService.SaveChangeGroupActionStatus(changeGroup);
                    }
                }


                #region call out to adapter
                linkProvider.SubmitLinkChange(linkService);
                #endregion
            }
            catch (Exception e)
            {
                ErrorManager.TryHandleException(e, ConflictManager);
            }
        }
        
        private bool ChangeGroupIsCompleted(LinkChangeGroup changeGroup)
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

        private bool AllLinkMigrationInstructionsAreConflicted(LinkChangeGroup changeGroup)
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

        private bool ContainsSpecialSkipActions(LinkChangeGroup changeGroup)
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

        private string LookupReflectedArtifactId(
            string artifactId, 
            string contentTypeReferenceName, 
            bool isSourceArtifactFromLeftSideInConfig)
        {
            SessionTypeEnum? sessionType = SessionContentTypeBinding.GetSessionType(contentTypeReferenceName);
            if (!sessionType.HasValue)
            {
                return artifactId;
            }

            if (!m_perSessionTypeServiceContainerPairs.ContainsKey(sessionType.Value))
            {
                return artifactId;
            }

            foreach (var serviceContainerPair in m_perSessionTypeServiceContainerPairs[sessionType.Value])
            {
                foreach (var serviceContainerEntry in serviceContainerPair.Value)
                {
                    var configService = serviceContainerEntry.Value.GetService(typeof (ConfigurationService)) as ConfigurationService;
                    Debug.Assert(null != configService);

                    if (isSourceArtifactFromLeftSideInConfig != configService.IsLeftSideInConfiguration)
                    {
                        continue;
                    }

                    var translationService =
                        serviceContainerEntry.Value.GetService(typeof(ITranslationService)) as ITranslationService;
                    Debug.Assert(null != translationService);

                    string targetArtifactId = translationService.TryGetTargetItemId(artifactId, serviceContainerEntry.Key);
                    if (!string.IsNullOrEmpty(targetArtifactId))
                    {
                        return targetArtifactId;
                    }
                }
            }

            return string.Empty;
        }

        internal object GetService(
            Guid sessionId, 
            Guid sourceId, 
            Type serviceType)
        {
            ServiceContainer serviceContainer = GetServiceContainer(sessionId, sourceId);
            if (null == serviceContainer)
            {
                return null;
            }

            return serviceContainer.GetService(serviceType);
        }

        private ServiceContainer GetServiceContainer(
            Guid sessionId, 
            Guid sourceId)
        {
            if (!m_allServiceContainerPairs.ContainsKey(sessionId)
                || !m_allServiceContainerPairs[sessionId].ContainsKey(sourceId))
            {
                return null;
            }

            return m_allServiceContainerPairs[sessionId][sourceId];
        }

        /// <summary>
        /// Translates URI and extracts tool-specific id.
        /// </summary>
        /// <param name="sessionId">The session unique Id.</param>
        /// <param name="sourceId">Side (Migration Source) of the artifact.</param>
        /// <param name="artifact"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool TryTranslateUri(
            Guid sessionId,
            Guid sourceId,
            IArtifact artifact,
            out string id)
        {
            id = string.Empty;

            var linkProvider = GetService(sessionId, sourceId, typeof(ILinkProvider)) as ILinkProvider;
            if (null == linkProvider)
            {
                return false;
            }

            return linkProvider.TryExtractArtifactId(artifact, out id);
        }

        private void TranslatePhase2(
            LinkService targetLinkService,
            LinkService sourceLinkService,
            bool bidireciton)
        {
            // phase 2 analysis is covered in bidirectional sync
            // after the pipeline swap the direction
            if (bidireciton)
            {
                return;
            }

            TraceManager.TraceInformation("Promote new link changes to InAnalysis");
            targetLinkService.PromoteCreatedLinkChangesToInAnalysis();
            TraceManager.TraceInformation("Promote Deferred link changes to InAnalysis");
            targetLinkService.PromoteDeferredLinkChangesToInAnalysis();

            Guid sessionId = targetLinkService.SessionId;
            Guid sourceId = targetLinkService.SourceId;

            int maxAge = m_perSourceTranslationTimestamp.GetMaxAgeForTranslation(sessionId, sourceId);

            long firstChangeGroupId = 0;
            const LinkChangeGroup.LinkChangeGroupStatus status = LinkChangeGroup.LinkChangeGroupStatus.InAnalysis;
            while (true)
            {
                TraceManager.TraceInformation("Loading paged link change groups");
                
                long lastChangeGroupId;
                ReadOnlyCollection<LinkChangeGroup> linkChangeGroups =
                    targetLinkService.GetLinkChangeGroups(firstChangeGroupId, MaxChangeActionsInSlice, status, false, maxAge, out lastChangeGroupId);
                if (linkChangeGroups.Count <= 0) break;
                firstChangeGroupId = lastChangeGroupId + 1;

                // translate each group
                List<LinkChangeGroup> deleteLinkChangeGroups = new List<LinkChangeGroup>();
                foreach (LinkChangeGroup linkChangeGroup in linkChangeGroups)
                {
                    TraceManager.TraceInformation("Translating link change group: {0} ({1} actions",
                        linkChangeGroup.InternalId,
                        linkChangeGroup.Actions.Count);
                    TranslateChangeGroup(targetLinkService.SessionId,
                                         targetLinkService.SourceId,
                                         linkChangeGroup,
                                         deleteLinkChangeGroups);
                }

                TraceManager.TraceInformation("Saving translation result");
                if (deleteLinkChangeGroups.Count > 0)
                {
                    targetLinkService.AddChangeGroups(deleteLinkChangeGroups);
                }
                sourceLinkService.SaveLinkChangeGroupTranslationResult(linkChangeGroups);
            }
        }

        private void TranslatePhase1(
            LinkService linkService, 
            LinkService targetLinkService)
        {
            TraceManager.TraceInformation("Promote new link changes to InAnalysis");
            linkService.PromoteCreatedLinkChangesToInAnalysis();
            TraceManager.TraceInformation("Promote Deferred link changes to InAnalysis");
            linkService.PromoteDeferredLinkChangesToInAnalysis();

            Guid sessionId = linkService.SessionId;
            Guid sourceId = linkService.SourceId;

            int maxAge = m_perSourceTranslationTimestamp.GetMaxAgeForTranslation(sessionId, sourceId);

            long firstChangeGroupId = 0;
            const LinkChangeGroup.LinkChangeGroupStatus status = LinkChangeGroup.LinkChangeGroupStatus.InAnalysis;
            while (true)
            {
                TraceManager.TraceInformation("Loading paged link change groups");
                long lastChangeGroupId;
                ReadOnlyCollection<LinkChangeGroup> linkChangeGroups =
                    linkService.GetLinkChangeGroups(firstChangeGroupId, MaxChangeActionsInSlice, status, false, maxAge, out lastChangeGroupId);
                if (linkChangeGroups.Count <= 0) break;
                firstChangeGroupId = lastChangeGroupId + 1;

                // translate each group
                List<LinkChangeGroup> deleteLinkChangeGroups = new List<LinkChangeGroup>();
                foreach (LinkChangeGroup linkChangeGroup in linkChangeGroups)
                {
                    TraceManager.TraceInformation("Translating link change group: {0} ({1} actions",
                        linkChangeGroup.InternalId,
                        linkChangeGroup.Actions.Count);
                    TranslateChangeGroup(linkService.SessionId, 
                                         linkService.SourceId, 
                                         linkChangeGroup,
                                         deleteLinkChangeGroups);
                }

                TraceManager.TraceInformation("Saving translation result");
                if (deleteLinkChangeGroups.Count > 0)
                {
                    linkService.AddChangeGroups(deleteLinkChangeGroups);
                }
                targetLinkService.SaveLinkChangeGroupTranslationResult(linkChangeGroups);
            }
        }

        private void TranslateChangeGroup(
            Guid sessionId, 
            Guid sourceId, 
            LinkChangeGroup linkChangeGroup,
            List<LinkChangeGroup> deleteLinkChangeGroups)
        {            
            Debug.Assert(linkChangeGroup.InternalId != LinkChangeGroup.INVALID_INTERNAL_ID);

            var configService =
                    GetService(sessionId, sourceId, typeof(ConfigurationService)) as ConfigurationService;
            Debug.Assert(null != configService);
            var targetLinkProvider =
                GetService(sessionId, configService.MigrationPeer, typeof(ILinkProvider)) as ILinkProvider;
            Debug.Assert(null != targetLinkProvider);

            var targetLinkService = GetService(sessionId, configService.MigrationPeer, typeof(LinkService)) as LinkService;
            Debug.Assert(null != targetLinkService, "target link service is null");

            var sourceLinkService = GetService(sessionId, sourceId, typeof(LinkService)) as LinkService;
            Debug.Assert(null != sourceLinkService, "source link service is null");

            bool allActionsTranslated = true;

            foreach (LinkChangeAction action in linkChangeGroup.Actions)
            {
                try
                {
                    Debug.Assert(action.InternalId != LinkChangeAction.INVALID_INTERNAL_ID);

                    if (!ActionNeedsTranslation(action))
                    {
                        continue;
                    }

                    // reflect source artifact
                    IArtifact reflectedSourceArtifact;
                    if (!TryFindReflection(sessionId, sourceId, action.Link, LinkEnd.SourceArtifact, out reflectedSourceArtifact))
                    {
                        // source is not present, keep action's status unchanged
                        allActionsTranslated = false;
                        continue;
                    }

                    // reflect target artifact
                    IArtifact reflectedTargetArtifact;
                    try
                    {
                        if (!TryFindReflection(sessionId, sourceId, action.Link, LinkEnd.TargetArtifact, out reflectedTargetArtifact))
                        {
                            // target is not present, keep action's status unchanged
                            allActionsTranslated = false;
                            continue;
                        }
                    }
                    catch (VCPathNotMappedException)
                    {
                        // target artifact is a version controlled artifact
                        // that is not in the mapped paths of any VC sessions
                        action.Status = LinkChangeAction.LinkChangeActionStatus.Skipped;
                        continue;
                    }

                    // translate link change action
                    Debug.Assert(targetLinkProvider.SupportedChangeActions.Contains(action.ChangeActionId));

                    // translate link type
                    string reflectedLinkTypeName = m_configLookupService.FindMappedLinkType(sourceId, action.Link.LinkType.ReferenceName);
                    if (!targetLinkProvider.SupportedLinkTypes.ContainsKey(reflectedLinkTypeName))
                    {
                        action.Status = LinkChangeAction.LinkChangeActionStatus.Skipped;
                        continue;
                    }
                    LinkType reflectedLinkType = targetLinkProvider.SupportedLinkTypes[reflectedLinkTypeName];

                    // translate reflected source artifact ID
                    string reflectedSourceArtifactId;
                    bool getIdResult = targetLinkProvider.TryExtractArtifactId(reflectedSourceArtifact, out reflectedSourceArtifactId);
                    Debug.Assert(getIdResult);

                    string preTransSourceArtifactId = action.Link.SourceArtifactId;
                    LinkType preTransLinkType = action.Link.LinkType;
                    IArtifact preTransSourceArtifact = action.Link.SourceArtifact;
                    IArtifact preTransTargetArtifact = action.Link.TargetArtifact;

                    action.Link.SourceArtifactId = reflectedSourceArtifactId;
                    action.Link.LinkType = reflectedLinkType;
                    action.Link.SourceArtifact = reflectedSourceArtifact;
                    action.Link.TargetArtifact = reflectedTargetArtifact;
                    action.Status = LinkChangeAction.LinkChangeActionStatus.Translated;

                    SkipAddLinkActionExistingOnTarget(action, targetLinkProvider, targetLinkService);

                    DeprecatePrevAddLinkActionIfNewActionIsToDelete(action, targetLinkProvider, targetLinkService);

                    #region Obsolete - now using RelatedArtifactsStore to record the link changes on each side
                    //if (action.Status != LinkChangeAction.LinkChangeActionStatus.Skipped)
                    //{
                    //    // the link is not on the target side, we need to decide whether
                    //    // -- it is an existing link but deleted by a user; or
                    //    // -- it is a new link introduced from the source
                    //    //
                    //    // we determine by finding the last completed migration instruction to
                    //    // the target system that involves a "delete":
                    //    // -- if there is one, the link was deleted by our tool - keep 'Add'
                    //    // -- if there isn't, we check the last completed migration instruction
                    //    // to the source system that involves a "delete"
                    //    // ---- if there isn't one or there is a conflicted one (i.e. we tried to delete
                    //    //      it but failed with conflict), the link was deleted by a user, we should
                    //    //      reflect this change
                    //    // ---- otherwise, the link is a brand new one
                    //    LinkChangeAction queryAction = new LinkChangeAction(
                    //        WellKnownChangeActionId.Delete,
                    //        action.Link,
                    //        LinkChangeAction.LinkChangeActionStatus.Created,
                    //        false);

                    //    LinkChangeAction resultAction = targetLinkService.TryFindLastDeleteAction(queryAction);
                    //    if (null == resultAction)
                    //    {
                    //        ILink preTranslationLink = new ArtifactLink(preTransSourceArtifactId,
                    //                                                    preTransSourceArtifact,
                    //                                                    preTransTargetArtifact,
                    //                                                    action.Link.Comment,
                    //                                                    preTransLinkType);
                    //        if (targetLinkService.IsLinkMigratedBefore(action.Link)
                    //            || sourceLinkService.IsLinkMigratedBefore(preTranslationLink))
                    //        {
                    //            LinkChangeGroup deleteLinkGroup = new LinkChangeGroup(
                    //                linkChangeGroup.GroupName,
                    //                LinkChangeGroup.LinkChangeGroupStatus.ReadyForMigration,
                    //                false);

                    //            LinkChangeAction deleteLinkAction = new LinkChangeAction(
                    //                WellKnownChangeActionId.Delete,
                    //                preTranslationLink,
                    //                LinkChangeAction.LinkChangeActionStatus.ReadyForMigration,
                    //                false);
                    //            deleteLinkGroup.AddChangeAction(deleteLinkAction);

                    //            resultAction = sourceLinkService.TryFindLastDeleteAction(deleteLinkAction);
                    //            if (null == resultAction
                    //                || resultAction.IsConflicted)
                    //            {
                    //                // the link on target server was deleted by a user
                    //                // mark source side action to be delete       

                    //                // add the new delete-link group for being added to source side
                    //                deleteLinkChangeGroups.Add(deleteLinkGroup);

                    //                // mark the current action skipped
                    //                action.Status = LinkChangeAction.LinkChangeActionStatus.Skipped;
                    //            }
                    //        }
                    //    }
                    //} 
                    #endregion
                }
                catch (Exception ex)
                {
                    TraceManager.TraceException(ex);
                    allActionsTranslated = false;
                    continue;
                }
            }

            linkChangeGroup.Status = allActionsTranslated
                                     ? LinkChangeGroup.LinkChangeGroupStatus.InAnalysisTranslated
                                     : LinkChangeGroup.LinkChangeGroupStatus.InAnalysisDeferred;
        }

        private bool ActionNeedsTranslation(LinkChangeAction action)
        {
            switch (action.Status)
            {
                case LinkChangeAction.LinkChangeActionStatus.Translated:
                case LinkChangeAction.LinkChangeActionStatus.Skipped:
                case LinkChangeAction.LinkChangeActionStatus.SkipScopedOutVCLinks:
                case LinkChangeAction.LinkChangeActionStatus.SkipScopedOutWILinks:
                case LinkChangeAction.LinkChangeActionStatus.DeltaCompleted:
                    return false;
                case LinkChangeAction.LinkChangeActionStatus.Created:
                    return true;
                default:
                    Debug.Assert(false, "Error - never reach here");
                    return false;
            }
        }

        private void SkipAddLinkActionExistingOnTarget(
            LinkChangeAction action, 
            ILinkProvider targetLinkProvider, 
            LinkService targetLinkService)
        {
            if (!action.ChangeActionId.Equals(WellKnownChangeActionId.Add))
            {
                return;
            }

            if (targetLinkService.IsActionInDelta(action))
            {
                action.Status = LinkChangeAction.LinkChangeActionStatus.Skipped;
                return;
            }

            List<ILink> typedLinksOfSameSourceArtifact =
                targetLinkProvider.GetLinks(action.Link.SourceArtifact, action.Link.LinkType);

            LinkComparer linkComparer = new LinkComparer();
            typedLinksOfSameSourceArtifact.Sort(linkComparer);
            int pos = typedLinksOfSameSourceArtifact.BinarySearch(action.Link, linkComparer);
            if (pos >= 0)
            {
                if (typedLinksOfSameSourceArtifact[pos].IsLocked == action.Link.IsLocked)
                {
                    action.Status = LinkChangeAction.LinkChangeActionStatus.Skipped;
                }
                else
                {
                    // we need to update the lock property of the link
                    action.ChangeActionId = WellKnownChangeActionId.Edit;
                }
            }
        }

        private void DeprecatePrevAddLinkActionIfNewActionIsToDelete(
            LinkChangeAction action, 
            ILinkProvider targetLinkProvider, 
            LinkService targetLinkService)
        {
            if (action.ChangeActionId.Equals(WellKnownChangeActionId.Delete))
            {
                targetLinkService.TryDeprecateActiveAddActionMigrationInstruction(action);
            }
        }

        /// <summary>
        /// Find reflection of an artifact.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="sourceId"></param>
        /// <param name="link"></param>
        /// <param name="linkEnd"></param>
        /// <param name="reflectedArtifact"></param>
        /// <returns></returns>
        private bool TryFindReflection(
            Guid sessionId, 
            Guid sourceId, 
            ILink link, 
            LinkEnd linkEnd, 
            out IArtifact reflectedArtifact)
        {
            reflectedArtifact = null;
            IArtifact artifact = (linkEnd == LinkEnd.SourceArtifact ? link.SourceArtifact : link.TargetArtifact);
            
            // get target provider
            var configService = GetService(sessionId, sourceId, typeof(ConfigurationService)) as ConfigurationService;
            Debug.Assert(null != configService);
            var targetProvider = GetService(sessionId, configService.MigrationPeer, typeof(ILinkProvider)) as ILinkProvider;
            if (null == targetProvider)
            {
                return false;
            }

             // find reflected artifact type
            string reflectedLinkTypeName = m_configLookupService.FindMappedLinkType(sourceId, link.LinkType.ReferenceName);
            if (!targetProvider.SupportedLinkTypes.ContainsKey(reflectedLinkTypeName))
            {
                return false;
            }
            LinkType reflectedLinkType = targetProvider.SupportedLinkTypes[reflectedLinkTypeName];
            ArtifactType reflectedArtifactType = (linkEnd == LinkEnd.SourceArtifact
                                                 ? reflectedLinkType.SourceArtifactType
                                                 : reflectedLinkType.TargetArtifactType);

            string artifactContentTypeRefName = artifact.ArtifactType.ContentTypeReferenceName;
            if (artifactContentTypeRefName == WellKnownContentType.VersionControlledArtifact.ReferenceName
                || artifactContentTypeRefName == WellKnownContentType.VersionControlledFile.ReferenceName
                || artifactContentTypeRefName == WellKnownContentType.VersionControlChangeGroup.ReferenceName
                || artifactContentTypeRefName == WellKnownContentType.VersionControlledFolder.ReferenceName)
            {
                SessionTypeEnum? sessionType = SessionContentTypeBinding.GetSessionType(artifactContentTypeRefName);
                if (!sessionType.HasValue ||
                    !m_perSessionTypeServiceContainerPairs.ContainsKey(sessionType.Value))
                {
                    return false;
                }

                return TryFindReflectionOfVersionControlledArtifact(
                        sessionId, sourceId, artifact, reflectedArtifactType, targetProvider, configService,
                        out reflectedArtifact);
            }
            else
            {
                return TryFindReflectionOfNonVersionControlledArtifact(
                    sessionId, sourceId, artifact, reflectedArtifactType, targetProvider, configService, out reflectedArtifact);
            }            
        }

        private bool TryFindReflectionOfVersionControlledArtifact(
            Guid sessionId,
            Guid sourceId,
            IArtifact artifact,
            ArtifactType reflectedArtifactType,
            ILinkProvider targetProvider,
            ConfigurationService configService,
            out IArtifact reflectedArtifact)
        {
            reflectedArtifact = null;
            
            var sourceLinkProvider = GetService(sessionId, sourceId, typeof(ILinkProvider)) as ILinkProvider;
            if (null == sourceLinkProvider)
            {
                return false;
            }

            
            bool isSourceArtifactFromLeftSideInConfig = configService.IsLeftSideInConfiguration;

            if (artifact.ArtifactType.ContentTypeReferenceName == WellKnownContentType.VersionControlChangeGroup.ReferenceName)
            {
                string changeId;
                bool idExtractionResult = sourceLinkProvider.TryExtractArtifactId(artifact, out changeId);
                if (!idExtractionResult)
                {
                    return false;
                }

                // find reflected change id           
                string reflectedChangeId = LookupReflectedArtifactId(changeId,
                                                                     WellKnownContentType.VersionControlChangeGroup.ReferenceName,
                                                                     isSourceArtifactFromLeftSideInConfig);
                if (string.IsNullOrEmpty(reflectedChangeId))
                {
                    return false;
                }

                string reflectedUri = targetProvider.GetVersionControlledArtifactUri(string.Empty, reflectedChangeId);
                if (string.IsNullOrEmpty(reflectedUri))
                {
                    return false;
                }

                reflectedArtifact = new Artifact(reflectedUri, reflectedArtifactType);
                return true;
            }
            else
            {
                string path, changeId;
                if (!sourceLinkProvider.TryGetVersionControlledArtifactDetails(artifact, out path, out changeId))
                {
                    return false;
                }

                //todo: path translation
                string reflectedPath = LookupReflectedVersionControlledItemPath(path, sourceId, isSourceArtifactFromLeftSideInConfig);
                if (string.IsNullOrEmpty(reflectedPath))
                {
                    // path is not in any VC's session's mapped path
                    throw new VCPathNotMappedException();
                }

                // find reflected change id           
                string reflectedChangeId = LookupReflectedArtifactId(changeId,
                                                                     WellKnownContentType.VersionControlChangeGroup.ReferenceName,
                                                                     isSourceArtifactFromLeftSideInConfig);
                if (string.IsNullOrEmpty(reflectedChangeId))
                {
                    return false;
                }

                string reflectedUri = targetProvider.GetVersionControlledArtifactUri(reflectedPath, reflectedChangeId);
                if (string.IsNullOrEmpty(reflectedUri))
                {
                    return false;
                }

                reflectedArtifact = new Artifact(reflectedUri, reflectedArtifactType);
                return true;
            }   
        }

        private string LookupReflectedVersionControlledItemPath(
            string path, 
            Guid sourceId, 
            bool isSourceArtifactFromLeftSideInConfig)
        {
            foreach (var serviceContainerPair in m_perSessionTypeServiceContainerPairs[SessionTypeEnum.VersionControl])
            {
                foreach (var serviceContainerEntry in serviceContainerPair.Value)
                {
                    var configService = serviceContainerEntry.Value.GetService(typeof(ConfigurationService)) as ConfigurationService;
                    Debug.Assert(null != configService);

                    if (isSourceArtifactFromLeftSideInConfig != configService.IsLeftSideInConfiguration)
                    {
                        continue;
                    }

                    var translationService = serviceContainerEntry.Value.GetService(typeof(ITranslationService)) as VCTranslationService;
                    if (null == translationService)
                    {
                        continue;
                    }

                    string reflectedPath = translationService.GetMappedPath(path, serviceContainerEntry.Key);
                    if (!string.IsNullOrEmpty(reflectedPath))
                    {
                        return reflectedPath;
                    }
                }
            }

            return string.Empty;
        }

        private bool TryFindReflectionOfNonVersionControlledArtifact(
            Guid sessionId, 
            Guid sourceId, 
            IArtifact artifact, 
            ArtifactType reflectedArtifactType, 
            ILinkProvider targetProvider, 
            ConfigurationService configService,
            out IArtifact reflectedArtifact)
        {
            reflectedArtifact = null;

            // ask source provider for artifact id
            string artifactId;
            if (!TryTranslateUri(sessionId, sourceId, artifact, out artifactId))
            {
                return false;
            }
            Debug.Assert(!string.IsNullOrEmpty(artifactId));

            // find reflected artifact id
            bool isSourceArtifactFromLeftSideInConfig = configService.IsLeftSideInConfiguration;
            string reflectedArtifactId = LookupReflectedArtifactId(artifactId,
                                                                   artifact.ArtifactType.ContentTypeReferenceName,
                                                                   isSourceArtifactFromLeftSideInConfig);

            // ask target provider for the reflected artifact
            return targetProvider.TryGetArtifactById(reflectedArtifactType.ReferenceName, reflectedArtifactId, out reflectedArtifact);
        }

        public bool LinkTypeSupportedByOtherSide(
            string linkTypeReferenceName, 
            Guid sessionId, 
            Guid sourceId)
        {
            var configService =
                GetService(sessionId, sourceId, typeof(ConfigurationService)) as ConfigurationService;
            Debug.Assert(null != configService);
            
            var targetLinkProvider =
                GetService(sessionId, configService.MigrationPeer, typeof(ILinkProvider)) as ILinkProvider;
            Debug.Assert(null != targetLinkProvider);

            string reflectedLinkTypeName = m_configLookupService.FindMappedLinkType(sourceId, linkTypeReferenceName);
            return targetLinkProvider.SupportedLinkTypes.ContainsKey(reflectedLinkTypeName);
        }


        class PerSourceTranslationTimestamp : Dictionary<Guid, Dictionary<Guid, DateTime>>
        {
            private Dictionary<Guid, Dictionary<Guid, Dictionary<int, DateTime>>> m_lastTimestamp = 
                new Dictionary<Guid,Dictionary<Guid,Dictionary<int,DateTime>>>();

            
            public int GetMaxAgeForTranslation(Guid sessionId, Guid sourceId)
            {               
                int maxAge = 0;
                bool maxAgeFound = false;
                
                for (int age = AgeInterveralSecAndRetries.GetLength(0) - 1; age >= 0; --age)
                {
                    if (!maxAgeFound)
                    {
                        DateTime lastTranslationTimeStamp = this.GetLastTranslationTimestamp(sessionId, sourceId, age);
                        var timeSpan = DateTime.Now.Subtract(lastTranslationTimeStamp);

                        if (timeSpan.TotalSeconds >= AgeInterveralSecAndRetries[age, 0])
                        {
                            maxAge = age;
                            maxAgeFound = true;
                        }
                    }
                    else
                    {
                        m_lastTimestamp[sessionId][sourceId][age] = DateTime.Now;
                    }
                }

                if (!maxAgeFound)
                {
                    m_lastTimestamp[sessionId][sourceId][0] = DateTime.Now;
                }

                return maxAge;
            }

            private DateTime GetLastTranslationTimestamp(Guid sessionId, Guid sourceId, int age)
            {
                TryCreateDataEntry(sessionId, sourceId);
                return m_lastTimestamp[sessionId][sourceId][age];
            }

            private void TryCreateDataEntry(Guid sessionId, Guid sourceId)
            {
                if (!m_lastTimestamp.ContainsKey(sessionId))
                {
                    m_lastTimestamp.Add(sessionId, new Dictionary<Guid, Dictionary<int, DateTime>>());
                }

                if (!m_lastTimestamp[sessionId].ContainsKey(sourceId))
                {
                    m_lastTimestamp[sessionId].Add(sourceId, new Dictionary<int, DateTime>());                

                    for (int ageIndex = 0; ageIndex < AgeInterveralSecAndRetries.GetLength(0); ageIndex++)
                    {
                        m_lastTimestamp[sessionId][sourceId].Add(ageIndex, DateTime.Now);
                    }
                }
            }
        }
    }
}