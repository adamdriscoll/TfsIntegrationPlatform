// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using System.Data.Objects;
using Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class ConflictManager : IServiceProvider
    {
        internal delegate void ConflictUnresolvedEventHandler(object sender, ConflictUnresolvedEventArgs e);
        internal event ConflictUnresolvedEventHandler ConflictUnresolvedEvent;        
        private int m_internalConflictCollectionId;
        private IServiceContainer m_serviceContainer;
        private ChangeGroupService m_changeGroupService;
        private readonly Dictionary<long, Dictionary<Guid, int>> m_cachedPerGroupResolvedConflictTypes =
            new Dictionary<long, Dictionary<Guid, int>>();
        private readonly Dictionary<Guid, Dictionary<Guid, int>> m_cachedPerScopeResolvedConflictTypes =
            new Dictionary<Guid, Dictionary<Guid, int>>();
        
        private ConflictTypeRegistry m_conflictTypeRegistry;

        internal ConflictManager(Guid sourceId)
        {
            this.SourceId = sourceId;
            m_conflictTypeRegistry = new ConflictTypeRegistry(sourceId);
        }

        internal void InitializePhase1(IServiceContainer serviceContainer)
        {
            m_serviceContainer = serviceContainer;
            m_changeGroupService = serviceContainer.GetService(typeof(ChangeGroupService)) as ChangeGroupService;
            Debug.Assert(null != m_changeGroupService, "ChangeGroupService is NULL");
        }

        internal void InitializePhase2(
            int internalConflictCollectionId)
        {
            if (internalConflictCollectionId <= 0)
            {
                throw new ArgumentException("internalConflictCollectionId");
            }

            m_internalConflictCollectionId = internalConflictCollectionId;
        }

        /// <summary>
        /// Gets the collection of registered conflict types
        /// </summary>
        public Dictionary<Guid, ConflictType> RegisteredConflictTypes
        {
            get 
            { 
                return m_conflictTypeRegistry.RegisteredConflictTypes; 
            }            
        }

        private Dictionary<Guid, ConflictType> RegisteredToolkitConflictTypes
        {
            get
            {
                return m_conflictTypeRegistry.RegisteredToolkitConflictTypes;
            }
        }

        private Dictionary<Guid, SyncOrchestrator.ConflictsSyncOrchOptions> RegisteredConflictTypeWithExplictSyncOrchOption
        {
            get
            {
                return m_conflictTypeRegistry.RegisteredConflictTypeWithExplictSyncOrchOption;
            }
        }

        /// <summary>
        /// Gets and sets the scope unique Id, i.e. SessionGroup Id or Session Id
        /// </summary>
        public Guid ScopeId
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets and sets the source unique Id, i.e. Framework unique Id or MigrationSource Id
        /// </summary>
        public Guid SourceId
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets and sets the link engine.
        /// </summary>
        internal LinkEngine LinkEngine
        {
            get;
            set;
        }

        internal int ConflictCollectionInternalId
        {
            get
            {
                return m_internalConflictCollectionId;
            }
            set
            {
                m_internalConflictCollectionId = value;
            }
        }

        #region IServiceProvider Members

        public object GetService(Type serviceType)
        {
            if (!serviceType.Equals(typeof(ConflictManager)))
            {
                return null;
            }

            return this;
        }

        #endregion

        #region Conflict Type Registration
        /// <summary>
        /// Register a conflict type to the manager
        /// </summary>
        /// <param name="type"></param>
        public void RegisterConflictType(ConflictType type)
        {
            m_conflictTypeRegistry.RegisterConflictType(type);
        }

        /// <summary>
        /// Register a conflict type to the manager
        /// </summary>
        /// <param name="type"></param>
        public void RegisterConflictType(Guid migrationSourceId, ConflictType type)
        {
            m_conflictTypeRegistry.RegisterConflictType(migrationSourceId, type);
        }

        public void RegisterConflictType(
            ConflictType type,
            SyncOrchestrator.ConflictsSyncOrchOptions syncOrchestrationOption)
        {
            m_conflictTypeRegistry.RegisterConflictType(type, syncOrchestrationOption);
        }

        public void RegisterConflictType(
            Guid migrationSourceId,
            ConflictType type,
            SyncOrchestrator.ConflictsSyncOrchOptions syncOrchestrationOption)
        {
            m_conflictTypeRegistry.RegisterConflictType(migrationSourceId, type, syncOrchestrationOption);
        }

        internal List<ConflictType> GetSourceSpecificConflictTypes(Guid sourceId)
        {
            return m_conflictTypeRegistry.GetSourceSpecificConflictTypes(sourceId);
        }

        internal void ValidateAndSaveProviderConflictRegistration(int providerInternalId)
        {
            m_conflictTypeRegistry.ValidateAndSaveProviderConflictRegistration(providerInternalId);
        }

        internal void RegisterToolkitConflictType(ConflictType type)
        {
            m_conflictTypeRegistry.RegisterToolkitConflictType(type);
        }

        internal void RegisterToolkitConflictType(
            ConflictType type,
            SyncOrchestrator.ConflictsSyncOrchOptions syncOrchestrationOption)
        {
            m_conflictTypeRegistry.RegisterToolkitConflictType(type, syncOrchestrationOption);
        } 
        #endregion

        public ConflictResolutionResult TryResolveNewConflict(
            Guid sourceId, 
            MigrationConflict conflict, 
            out List<MigrationAction> actions)
        {
            ConflictResolutionResult rslt = new ConflictResolutionResult(false, ConflictResolutionType.Other);
            actions = null;

            if (this.RegisteredConflictTypes.ContainsKey(conflict.ConflictType.ReferenceName))
            {
                IConflictHandler handler = this.RegisteredConflictTypes[conflict.ConflictType.ReferenceName].Handler;

                var rules = GetPersistedRules(conflict.ConflictType);

                ConflictResolutionRule resolutionRule = null;
                foreach (ConflictResolutionRule rule in rules)
                {
                    if (handler.CanResolve(conflict, rule))
                    {
                        rslt = handler.Resolve(m_serviceContainer, conflict, rule, out actions);
                        resolutionRule = rule;
                        break;
                    }
                }

                if (rslt.Resolved)
                {
                    Debug.Assert(resolutionRule != null, "resolutionRule is NULL");
                    // todo: remove persisting to db resolved conflicts
                    // persist at first resolved conflict ==> (changegroupid::conflicttype)
                    ResolvedNewConflict(sourceId, conflict, resolutionRule, rslt);
                }
                else
                {
                    BacklogUnresolvedConflict(sourceId, conflict, false);
                    rslt.ConflictInternalId = conflict.InternalId;
                    RaiseConflictUnresolvedEvent(sourceId, conflict, "Cannot find applicable resolution rule.");
                }
            }
            else
            {
                BacklogUnresolvedConflict(sourceId, conflict, false);
                rslt.ConflictInternalId = conflict.InternalId;
                RaiseConflictUnresolvedEvent(sourceId, conflict, "Unrecognized conflict type");
            }            

            // we reach here either:
            // 1. when a conflict is resolved
            // 2. when the unresolved conflict does not result in immediate stop of the session thread
            return rslt;
        }

        public ConflictResolutionResult ResolveExistingConflictWithNewRule(
            int internalConflictId, 
            ConflictResolutionRule newRule)
        {
            ConflictResolutionResult result = new ConflictResolutionResult(false, ConflictResolutionType.Other);

            // find conflict from backlog
            RTConflict rtConflict = null;
            Guid conflictTypeRefName = Guid.Empty;
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var conflicts =
                    from c in context.RTConflictSet
                    where c.Id == internalConflictId
                    select c;

                if (conflicts.Count() > 0)
                {
                    rtConflict = conflicts.First();
                    conflicts.First().ConflictTypeReference.Load();
                    conflictTypeRefName = conflicts.First().ConflictType.ReferenceName;
                }
                else
                {
                    result.Comment = string.Format(MigrationToolkitResources.ErrorInvalidConflictInternalId, 
                                                   internalConflictId);
                    return result;
                }

                return ResolveExistingConflict(context, newRule, result, rtConflict, conflictTypeRefName, true);
            }            
        }

        public ReadOnlyCollection<ConflictResolutionResult> ResolveExistingConflictWithExistingRule(
            ConflictResolutionRule existingRule)
        {
            List<ConflictResolutionResult> successes = new List<ConflictResolutionResult>();

            List<RTConflict> rtConflictsToResolve = new List<RTConflict>();
            Guid conflictTypeRefName = Guid.Empty;
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                RTResolutionRule resolutionRule = FindResolutionRule(context, existingRule);
                resolutionRule.ConflictTypeReference.Load();

                conflictTypeRefName = resolutionRule.ConflictType.ReferenceName;

                var conflicts =
                    from c in context.RTConflictSet
                    where c.Status == 0
                    && c.SourceSideMigrationSource.UniqueId.Equals(SourceId)
                    && (c.InCollection.SessionGroupRun.Config.SessionGroup.GroupUniqueId.Equals(ScopeId)
                        || c.InCollection.SessionRun.Config.SessionUniqueId.Equals(ScopeId))
                    && c.ConflictType.Id == resolutionRule.ConflictType.Id
                    select c;

                foreach (RTConflict rtConflict in conflicts)
                {
                    context.Detach(rtConflict);
                    rtConflictsToResolve.Add(rtConflict);
                }
            }

            foreach (RTConflict rtConflict in rtConflictsToResolve)
            {
                using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
                {
                    context.Attach(rtConflict);
                    ConflictResolutionResult result = new ConflictResolutionResult(false, ConflictResolutionType.Other);
                    result = ResolveExistingConflict(context, existingRule, result, rtConflict, conflictTypeRefName, false);

                    if (result.Resolved)
                    {
                        successes.Add(result);
                    }
                }
            }


            return successes.AsReadOnly();
        }

        public bool TryGetResolutionRulesAppliedToGroup(
            ChangeGroup changeGroup,
            out List<ConflictResolutionRule> rules)
        {
            rules = null;

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                // find change group in db
                if (changeGroup.ChangeGroupId <= 0)
                {
                    return false;
                }

                var ruleQuery = 
                    (from c in context.RTConflictSet
                     where c.ConflictedChangeAction.ChangeGroupId == changeGroup.ChangeGroupId
                        && c.Status == 1
                     select c.ResolvedByRule);

                if (ruleQuery.Count() <= 0)
                {
                    return false;
                }

                rules = new List<ConflictResolutionRule>();
                foreach (RTResolutionRule rtRule in ruleQuery)
                {
                    if (rtRule == null)
                    {
                        continue;
                    }

                    ConflictResolutionRule rule = RealizeRuleFromEDM(rtRule);
                    rules.Add(rule);
                }
            }

            return true;
        }

        private ConflictResolutionResult ResolveExistingConflict(
            RuntimeEntityModel context,
            ConflictResolutionRule newRule, 
            ConflictResolutionResult defaultResult, 
            RTConflict rtConflict, 
            Guid conflictTypeRefName,
            bool newResolutionRule)
        {
            // check if conflict is resolved
            if (rtConflict.Status != 0)
            {
                defaultResult.Comment = string.Format("Conflict has already been resolved.");
                defaultResult.Resolved = true;
                return defaultResult;
            }

            // check if manager support the conflict type of conflict (#internalConflictId)
            if (!RegisteredConflictTypes.ContainsKey(conflictTypeRefName))
            {
                defaultResult.Comment = string.Format(
                    MigrationToolkitResources.ConflictTypeNotRegistered,
                    conflictTypeRefName, this.ScopeId.ToString(), this.SourceId.ToString());
                return defaultResult;
            }

            // check if the registered conflict type supports the action mentioned in the rule
            ConflictType type = RegisteredConflictTypes[conflictTypeRefName];
            if (!type.SupportedResolutionActions.ContainsKey(newRule.ActionRefNameGuid))
            {
                string errorMessage = string.Format(
                    MigrationToolkitResources.InvalidConflictResolutionAction,
                    newRule.ActionReferenceName, type.ReferenceName.ToString());
                TraceManager.TraceError(errorMessage);

                defaultResult.Comment = errorMessage;
                return defaultResult;
            }

            //
            // start resolving the conflict
            //
            IConflictHandler handler = type.Handler;
            MigrationConflict conflict = new MigrationConflict(
                type, MigrationConflict.Status.Unresolved, rtConflict.ConflictDetails, rtConflict.ScopeHint);
            conflict.InternalId = rtConflict.Id;
            
            rtConflict.ConflictedChangeActionReference.Load();
            if (null != m_changeGroupService && null != rtConflict.ConflictedChangeAction)
            {
                conflict.ConflictedChangeAction = m_changeGroupService.LoadSingleAction(rtConflict.ConflictedChangeAction.ChangeActionId);
            }

            rtConflict.ConflictedLinkChangeActionReference.Load();
            if (null != rtConflict.ConflictedLinkChangeAction)
            {
                rtConflict.SourceSideMigrationSourceReference.Load();
                var linkService = LinkEngine.GetService(rtConflict.ConflictedLinkChangeAction.SessionUniqueId,
                                                        rtConflict.ConflictedLinkChangeAction.SourceId,
                                                        typeof(LinkService)) as LinkService;
                if (null != linkService)
                {
                    conflict.ConflictedLinkChangeAction = 
                        linkService.LoadSingleLinkChangeAction(rtConflict.ConflictedLinkChangeAction.Id);
                }
            }

            if (!handler.CanResolve(conflict, newRule))
            {
                defaultResult.Comment = string.Format(MigrationToolkitResources.ResolutionRuleNotApplicable,
                                                      newRule.RuleDescription, rtConflict.Id);
                return defaultResult;
            }

            List<MigrationAction> actions;
            defaultResult = handler.Resolve(m_serviceContainer, conflict, newRule, out actions);
            if (!defaultResult.Resolved)
            {
                return defaultResult;
            }

            // save resolution results and the new rule
            ResolvedExistingConflict(context, SourceId, conflict, rtConflict, type, newRule, defaultResult, newResolutionRule);

            return defaultResult;
        }

        private void ResolvedExistingConflict(
            RuntimeEntityModel context,
            Guid sourceId, 
            MigrationConflict conflict,
            RTConflict rtConflict, 
            ConflictType type,
            ConflictResolutionRule resolutionRule, 
            ConflictResolutionResult defaultRslt,
            bool newResolutionRule)
        {
            List<long> changeGroupIdsForReEvaluation = new List<long>();

            int ruleInternalId = resolutionRule.InternalId;
            // save the new rule
            if (newResolutionRule)
            {
                ruleInternalId = SaveNewResolutionRule(type, resolutionRule);
            }

            if (defaultRslt.ResolutionType == ConflictResolutionType.ScheduledForRetry)
            {
                rtConflict.Status = 2;
                rtConflict.ConflictCount = (rtConflict.ConflictCount.HasValue ? rtConflict.ConflictCount++ : 1);
                context.TrySaveChanges();
                resolutionRule.InternalId = ruleInternalId;
                return;
            }

            if (rtConflict.ConflictedChangeAction != null)
            {
                Debug.Assert(rtConflict.ConflictedChangeAction.ChangeActionId > 0,
                    "conflict.ConflictedChangeAction.ActionId has invalid value");
                RTChangeAction conflictedAction = context.RTChangeActionSet.Where
                    (ca => ca.ChangeActionId == rtConflict.ConflictedChangeAction.ChangeActionId).First<RTChangeAction>();

                if (conflictedAction != null)
                {
                    // save changes made on MigrationConflict(conflict)
                    if (defaultRslt.ResolutionType == ConflictResolutionType.UpdatedConflictedChangeAction ||
                        defaultRslt.ResolutionType == ConflictResolutionType.SkipConflictedChangeAction)
                    {
                        SaveChangesOnConflictedMigrationAction(conflict.ConflictedChangeAction, rtConflict.ConflictedChangeAction);
                    }

                    // update change action and (potentially) change group status
                    UnblockChangeGroupWithNoConflicts(context, conflictedAction, changeGroupIdsForReEvaluation);

                    // unblock the chained item updates in backlog
                    UnblockChainedItemUpdates(context, rtConflict, sourceId, changeGroupIdsForReEvaluation);
                }
            }
            else if (conflict.ConflictedLinkChangeAction != null)
            {
                Debug.Assert(
                    conflict.ConflictedLinkChangeAction.InternalId != LinkChangeAction.INVALID_INTERNAL_ID,
                    "conflict.ConflictedLinkChangeAction.ActionId has invalid value");

                var conflictedLinkAction = context.RTLinkChangeActionSet.Where
                    (ca => ca.Id == conflict.ConflictedLinkChangeAction.InternalId).First();

                if (conflictedLinkAction != null)
                {
                    if (defaultRslt.ResolutionType == ConflictResolutionType.UpdatedConflictedLinkChangeAction)
                    {
                        SaveChangesOnConflictedLinkChangeAction(conflict.ConflictedLinkChangeAction, conflictedLinkAction);
                    }
                    else if (defaultRslt.ResolutionType == ConflictResolutionType.CreatedUnlockLinkChangeActions)
                    {
                        conflictedLinkAction = context.RTLinkChangeActionSet.Where
                            (ca => ca.Id == conflict.ConflictedLinkChangeAction.InternalId).First();

                        if (conflictedLinkAction == null)
                        {
                            throw new MigrationException(MigrationToolkitResources.ErrorNonexistConflictChangeAction);
                        }

                        conflictedLinkAction.ArtifactLinkReference.Load();
                        conflictedLinkAction.ArtifactLink.IsLocked = false;
                        RTLinkChangeAction unlockAction = RTLinkChangeAction.CreateRTLinkChangeAction(
                            0, conflictedLinkAction.SessionGroupUniqueId, conflictedLinkAction.SessionUniqueId,
                            Microsoft.TeamFoundation.Migration.Toolkit.Services.WellKnownChangeActionId.Edit,
                            conflictedLinkAction.Status, false, conflictedLinkAction.SourceId);
                        unlockAction.ArtifactLink = conflictedLinkAction.ArtifactLink;
                        conflictedLinkAction.LinkChangeGroupReference.Load();
                        unlockAction.LinkChangeGroup = conflictedLinkAction.LinkChangeGroup;

                        context.TrySaveChanges();
                    }

                    UnblockLinkChangeGroupWithNoConflicts(context, conflictedLinkAction);
                }
            }

            UnblockChainonConflictConflicts(context, rtConflict, changeGroupIdsForReEvaluation);

            // mark conflict as resolved
            rtConflict.Status = 1;

            // associate the rule with the conflict
            var ruleQuery = (context.RTResolutionRuleSet.Where(r => r.Id == ruleInternalId));
            if (ruleQuery.Count() > 0)
            {
                // in case of generic conflict, they can only be manually resolved for now and there is no
                // specific resolution rule created for them
                Debug.Assert(null != ruleQuery);
                rtConflict.ResolvedByRule = ruleQuery.First();
            }

            context.TrySaveChanges();

            UnblockChangeGroupWithNoConflicts(changeGroupIdsForReEvaluation);

            defaultRslt.Resolved = true;
            defaultRslt.ConflictInternalId = rtConflict.Id;
            defaultRslt.Comment = string.Format(
                "Conflict has been resolved by rule '{0}' - {1}.)",
                resolutionRule.RuleRefNameGuid.ToString(), resolutionRule.RuleDescription);

            resolutionRule.InternalId = ruleInternalId;
        }

        internal static bool DoesSessionHaveUnresolvedConflicts(Guid leftMigrationSourceId, Guid rightMigrationSourceId)
        {
            bool sessionHasUnresolvedConflicts = false;

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var unresolvedConflicts =
                    from c in context.RTConflictSet
                    where (c.SourceSideMigrationSource.UniqueId == leftMigrationSourceId || c.SourceSideMigrationSource.UniqueId == rightMigrationSourceId)
                       && c.Status == 0
                    select c.Id;

                sessionHasUnresolvedConflicts = unresolvedConflicts.Count() > 0;
            }

            return sessionHasUnresolvedConflicts;
        }

        private static void UnblockChangeGroupWithNoConflicts(
           List<long> changeGroupIdsForReEvaluation)
        {
            if (changeGroupIdsForReEvaluation.Count == 0)
            {
                return;
            }

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                bool updatesNeeded = false;
                foreach (long groupId in changeGroupIdsForReEvaluation)
                {
                    int conflictedActionCount =
                        (from a in context.RTChangeActionSet
                        where a.ChangeGroupId == groupId && a.Backlogged
                        select a.ActionId).Count();

                    if (conflictedActionCount == 0)
                    {
                        var changeGroupQuery =
                            from g in context.RTChangeGroupSet
                            where g.Id == groupId
                            select g;

                        if (changeGroupQuery.Count() > 0)
                        {
                            changeGroupQuery.First().ContainsBackloggedAction = false;
                            updatesNeeded = true;
                        }
                    }
                }

                if (updatesNeeded)
                {
                    context.TrySaveChanges();
                }
            }
        }

        private void UnblockChainonConflictConflicts(
            RuntimeEntityModel context, 
            RTConflict rtConflict,
            List<long> changeGroupIdsForReEvaluation)
        {
            string chainOnConflictConflictScopeHint = rtConflict.Id.ToString();
            Guid chainOnConflictConflictTypeRefName = new ChainOnConflictConflictType().ReferenceName;
            var chainonConflictConflictsQuery =
                from c in context.RTConflictSet
                where c.ConflictType.ReferenceName.Equals(chainOnConflictConflictTypeRefName)
                && c.ScopeHint.Equals(chainOnConflictConflictScopeHint)  // ChainOnConflictConflictType uses ConflictId as scope hint
                && c.Status == 0
                select c;

            foreach (RTConflict chainedConflict in chainonConflictConflictsQuery)
            {
                chainedConflict.Status = 1;

                // unblock the chained change actions/groups
                chainedConflict.ConflictedChangeActionReference.Load();
                UnblockChangeGroupWithNoConflicts(context, chainedConflict.ConflictedChangeAction, changeGroupIdsForReEvaluation);

                UnblockChainedItemUpdates(context, chainedConflict, 
                                          chainedConflict.ConflictedChangeAction.ChangeGroup.SourceUniqueId,
                                          changeGroupIdsForReEvaluation);
            }
        }

        public int SaveNewResolutionRule(
            ConflictType conflictType,
            ConflictResolutionRule resolutionRule)
        {
            return SaveNewResolutionRule(this.ScopeId, this.SourceId, conflictType, resolutionRule);
        }

        public static int SaveNewResolutionRule(
            Guid scopeId,
            Guid sourceId, 
            ConflictType conflictType, 
            ConflictResolutionRule resolutionRule)
        {
            if (sourceId.Equals(Guid.Empty))
            {
                throw new ArgumentException("sourceId");
            }

            if (null == conflictType)
            {
                throw new ArgumentNullException("conflictType");
            }

            if (null == resolutionRule)
            {
                throw new ArgumentNullException("resolutionRule");
            }

            if (null == resolutionRule.ApplicabilityScope)
            {
                throw new ArgumentNullException("resolutionRule.ApplicableScope");
            }

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                if (context.RTResolutionRuleSet.Where(r => r.ReferenceName.Equals(resolutionRule.RuleRefNameGuid)).Count<RTResolutionRule>() > 0)
                {
                    throw new MigrationException(string.Format(
                        MigrationToolkitResources.ErrorDuplicateResolutionRule,
                        resolutionRule.RuleRefNameGuid.ToString()));
                }

                ResolutionAction action = conflictType[resolutionRule.ActionRefNameGuid];
                if (null == action)
                {
                    throw new MigrationException(string.Format(
                        MigrationToolkitResources.InvalidConflictResolutionAction,
                        resolutionRule.ActionReferenceName.ToString(),
                        conflictType.FriendlyName));
                }

                RTResolutionRule rtRule = RTResolutionRule.CreateRTResolutionRule(
                    0,
                    new ConfliceResolutionRuleSerializer().Serialize(resolutionRule), 
                    ConflictResolutionRuleState.Valid.StorageValue, // TODO: consult session mode to determine status? overload with different status?
                    resolutionRule.RuleRefNameGuid,
                    scopeId,
                    sourceId,
                    DateTime.Now);

                rtRule.ConflictType = FindOrCreateConflictType(context, conflictType);
                rtRule.ResolutionAction = FindOrCreateResolutionAction(context, action);
                rtRule.Scope = FindOrCreateScope(context, resolutionRule.ApplicabilityScope);

                if (action.ReferenceName.Equals(new UpdatedConfigurationResolutionAction().ReferenceName))
                {
                    // conflict resolution rule that updates the configuration only should not be preserved
                    // as active rules, because they cannot be re-applied.
                    rtRule.Status = ConflictResolutionRuleState.Deprecated.StorageValue;
                }

                if (resolutionRule.ActionRefNameGuid.Equals(new ManualConflictResolutionAction().ReferenceName))
                {
                    //rule to allow batch conflict resolution(with Manual Retry as the selected resolution action) 
                    //this rule should not be re-applied to ensure pipeline never gets into infinite loop.
                    rtRule.Status = ConflictResolutionRuleState.Deprecated.StorageValue;
                }

                context.TrySaveChanges();

                return rtRule.Id;
            }
        }

        private static RTConflictRuleScope FindOrCreateScope(RuntimeEntityModel context, string scopeString)
        {
            if (scopeString == null)
            {
                throw new ArgumentNullException("scopeString");
            }

            var scopes = context.RTConflictRuleScopeSet.Where(s => s.Scope == scopeString);
            if (scopes.Count<RTConflictRuleScope>() > 0)
            {
                return scopes.First<RTConflictRuleScope>();;
            }

            RTConflictRuleScope scope = RTConflictRuleScope.CreateRTConflictRuleScope(0, scopeString);
            context.AddToRTConflictRuleScopeSet(scope);
            return scope;
        }

        private static RTResolutionAction FindOrCreateResolutionAction(RuntimeEntityModel context, ResolutionAction resolutionAction)
        {

            var actions = context.RTResolutionActionSet.Where(a => a.ReferenceName.Equals(resolutionAction.ReferenceName));

            if (actions.Count<RTResolutionAction>() > 0)
            {
                return actions.First<RTResolutionAction>();
            }

            RTResolutionAction action = RTResolutionAction.CreateRTResolutionAction(0, resolutionAction.ReferenceName, resolutionAction.FriendlyName);
            context.AddToRTResolutionActionSet(action);
            return action;
        }

        private void ResolvedNewConflict(
            Guid sourceId,
            MigrationConflict conflict,
            ConflictResolutionRule resolutionRule,
            ConflictResolutionResult rslt)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                RTChangeAction rtConflictedAction = null;
                RTLinkChangeAction rtConflictedLinkAction = null;
                if (conflict.ConflictedChangeAction != null)
                {
                    Debug.Assert(conflict.ConflictedChangeAction.ActionId > 0,
                        "conflict.ConflictedChangeAction.ActionId has invalid value");

                    rtConflictedAction = context.RTChangeActionSet.Where
                            (ca => ca.ChangeActionId == conflict.ConflictedChangeAction.ActionId).First();

                    if (rtConflictedAction != null)
                    {
                        if (rslt.ResolutionType == ConflictResolutionType.UpdatedConflictedChangeAction
                            || rslt.ResolutionType == ConflictResolutionType.SuppressedConflictedChangeAction)
                        {
                            if (rslt.ResolutionType == ConflictResolutionType.UpdatedConflictedChangeAction)
                            {
                                SaveChangesOnConflictedMigrationAction(conflict.ConflictedChangeAction, rtConflictedAction);
                            }

                            // if resolution suppresses the action and it is the only one in its parent group
                            // mark the parent group as completed
                            if (rslt.ResolutionType == ConflictResolutionType.SuppressedConflictedChangeAction)
                            {
                                // mark the the suppressed action to be Substituted, so that it will not be processed any more
                                rtConflictedAction.IsSubstituted = true;

                                rtConflictedAction.ChangeGroupReference.Load();

                                ObjectQuery<RTChangeAction> changeGroupsObjectQuery = rtConflictedAction.ChangeGroup.ChangeActions.CreateSourceQuery();

                                if (changeGroupsObjectQuery.Where(x => !x.IsSubstituted).Count() == 0)
                                {
                                    conflict.ConflictedChangeAction.ChangeGroup.Complete();
                                }
                            }

                            context.TrySaveChanges();
                        }
                    }                    
                }
                else if (conflict.ConflictedLinkChangeAction != null)
                {
                    Debug.Assert(
                        conflict.ConflictedLinkChangeAction.InternalId != LinkChangeAction.INVALID_INTERNAL_ID
                        && conflict.ConflictedLinkChangeAction.InternalId > 0,
                        "conflict.ConflictedLinkChangeAction.ActionId has invalid value");

                    rtConflictedLinkAction = context.RTLinkChangeActionSet.Where
                            (ca => ca.Id == conflict.ConflictedLinkChangeAction.InternalId).First();

                    if (rtConflictedLinkAction != null)
                    {
                        if (rslt.ResolutionType == ConflictResolutionType.UpdatedConflictedLinkChangeAction)
                        {
                            SaveChangesOnConflictedLinkChangeAction(conflict.ConflictedLinkChangeAction, rtConflictedLinkAction);
                            context.TrySaveChanges();
                        }
                        else if (rslt.ResolutionType == ConflictResolutionType.CreatedUnlockLinkChangeActions)
                        {
                            rtConflictedLinkAction.ArtifactLinkReference.Load();
                            rtConflictedLinkAction.ArtifactLink.IsLocked = false;
                            RTLinkChangeAction unlockAction = RTLinkChangeAction.CreateRTLinkChangeAction(
                                0, rtConflictedLinkAction.SessionGroupUniqueId, rtConflictedLinkAction.SessionUniqueId,
                                Microsoft.TeamFoundation.Migration.Toolkit.Services.WellKnownChangeActionId.Edit,
                                rtConflictedLinkAction.Status, false, rtConflictedLinkAction.SourceId);
                            unlockAction.ArtifactLink = rtConflictedLinkAction.ArtifactLink;
                            rtConflictedLinkAction.LinkChangeGroupReference.Load();
                            unlockAction.LinkChangeGroup = rtConflictedLinkAction.LinkChangeGroup;

                            context.TrySaveChanges();
                        }
                    }
                }
          
                // persisting auto-resolved conflict
                if (rtConflictedAction != null)
                {
                    Debug.Assert(null != conflict.ConflictedChangeAction.ChangeGroup);
                    var changeGroupInternalId = conflict.ConflictedChangeAction.ChangeGroup.ChangeGroupId;
                    var conflictTypeRefName = conflict.ConflictType.ReferenceName;
                    int conflictId;
                    if (TryGetPerGroupResolvedConflictInCache(changeGroupInternalId, conflictTypeRefName, out conflictId))
                    {
                        rslt.ConflictInternalId = conflictId;
                        return;
                    }
                }
                else
                {
                    var conflictTypeRefName = conflict.ConflictType.ReferenceName;
                    int conflictId;
                    if (TryGetPerScopeResolvedConflictInCache(ScopeId, conflictTypeRefName, out conflictId))
                    {
                        rslt.ConflictInternalId = conflictId;
                        return;
                    }
                }

                RTConflictCollection conflictCollection = context.RTConflictCollectionSet.Where
                    (cc => cc.Id == m_internalConflictCollectionId).First();
                Debug.Assert(conflictCollection != null, "Conflict collection is not created for the session run");

                var sourceSideMigrationSource = FindOrCreateSource(context, sourceId);
                var conflictType = FindOrCreateConflictType(context, conflict.ConflictType);
                var rule = FindResolutionRule(context, resolutionRule);

                RTConflict rtConflictResolved = null;
                if (rslt.ResolutionType == ConflictResolutionType.ScheduledForRetry)
                {
                    var rtConflictResolvedQuery = from c in context.RTConflictSet
                                                  where c.SourceSideMigrationSource == sourceSideMigrationSource
                                                  && c.ConflictDetails == conflict.ConflictDetails
                                                  && c.ConflictedChangeAction == rtConflictedAction
                                                  && c.ConflictedLinkChangeAction == rtConflictedLinkAction
                                                  && c.ConflictType == conflictType
                                                  && c.ResolvedByRule == rule
                                                  && c.ScopeHint == conflict.ScopeHint
                                                  && c.Status != 1
                                                  select c;
                    if (rtConflictResolvedQuery.Count() > 0)
                    {
                        rtConflictResolved = rtConflictResolvedQuery.First();
                        if (!rtConflictResolved.ConflictCount.HasValue)
                        {
                            rtConflictResolved.ConflictCount = 1;
                        }
                        else
                        {
                            rtConflictResolved.ConflictCount++;
                        }
                    }
                }
                
                if (null == rtConflictResolved)
                {
                    rtConflictResolved = RTConflict.CreateRTConflict(0, 1, ScopeId);
                    rtConflictResolved.SourceSideMigrationSource = sourceSideMigrationSource;
                    rtConflictResolved.ConflictDetails = conflict.ConflictDetails;
                    rtConflictResolved.ConflictedChangeAction = rtConflictedAction;
                    rtConflictResolved.ConflictedLinkChangeAction = rtConflictedLinkAction;
                    rtConflictResolved.ConflictType = conflictType;
                    rtConflictResolved.InCollection = conflictCollection;
                    rtConflictResolved.ResolvedByRule = rule;
                    rtConflictResolved.ScopeHint = conflict.ScopeHint;
                    if (rslt.ResolutionType == ConflictResolutionType.ScheduledForRetry)
                    {
                        rtConflictResolved.Status = 2;
                        rtConflictResolved.ConflictCount = 1;
                    }
                    else
                    {
                        rtConflictResolved.Status = 1;
                        rtConflictResolved.CreationTime = DateTime.Now;
                    }
                }

                if (null != rtConflictedAction)
                {
                    long conflictParentGroupId = conflict.ConflictedChangeAction.ChangeGroup.ChangeGroupId;
                    List<long> groupIds = new List<long>();
                    groupIds.Add(conflictParentGroupId);
                    UnblockChangeGroupWithNoConflicts(context, rtConflictedAction, groupIds);
                }

                context.TrySaveChanges();

                if (null != rtConflictedAction)
                {
                    Debug.Assert(null != conflict.ConflictedChangeAction.ChangeGroup);
                    var changeGroupInternalId = conflict.ConflictedChangeAction.ChangeGroup.ChangeGroupId;
                    var conflictTypeRefName = conflict.ConflictType.ReferenceName;
                    AddResolvedPerGroupConflictIdToCache(changeGroupInternalId, conflictTypeRefName, rtConflictResolved.Id);
                }
                else
                {
                    var conflictTypeRefName = conflict.ConflictType.ReferenceName;
                    AddResolvedPerScopeConflictIdToCache(ScopeId, conflictTypeRefName, rtConflictResolved.Id);
                }

                rslt.ConflictInternalId = rtConflictResolved.Id;
            }
        }

        private void AddResolvedPerGroupConflictIdToCache(
           long changeGroupInternalId,
           Guid conflictTypeRefName,
           int resolvedConflictId)
        {
            if (!m_cachedPerGroupResolvedConflictTypes.ContainsKey(changeGroupInternalId))
            {
                m_cachedPerGroupResolvedConflictTypes.Add(changeGroupInternalId, new Dictionary<Guid, int>());
            }

            if (!m_cachedPerGroupResolvedConflictTypes[changeGroupInternalId].ContainsKey(conflictTypeRefName))
            {
                m_cachedPerGroupResolvedConflictTypes[changeGroupInternalId].Add(conflictTypeRefName, resolvedConflictId);
            }

            // we process change groups ordered by their id (in incremental order)
            // hence, we will drop the cache if the current change group has a larger id
            if (m_cachedPerGroupResolvedConflictTypes.Count > 1)
            {
                var obsoleteChangeGroupId = GetObsoleteChangeGroupId(m_cachedPerGroupResolvedConflictTypes.Keys,
                                                                      changeGroupInternalId);
                m_cachedPerGroupResolvedConflictTypes.Remove(obsoleteChangeGroupId);
            }
        }

        private void AddResolvedPerScopeConflictIdToCache(
           Guid scopeId,
           Guid conflictTypeRefName,
           int resolvedConflictId)
        {
            if (!m_cachedPerScopeResolvedConflictTypes.ContainsKey(scopeId))
            {
                m_cachedPerScopeResolvedConflictTypes.Add(scopeId, new Dictionary<Guid, int>());
            }

            if (!m_cachedPerScopeResolvedConflictTypes[scopeId].ContainsKey(conflictTypeRefName))
            {
                m_cachedPerScopeResolvedConflictTypes[scopeId].Add(conflictTypeRefName, resolvedConflictId);
            }
        }

        private long GetObsoleteChangeGroupId(
            ICollection<long> changeGroupIdsInCache,
            long currChangeGroupInternalId)
        {
            Debug.Assert(changeGroupIdsInCache.Count == 2);

            if (changeGroupIdsInCache.ElementAt(0) == currChangeGroupInternalId)
            {
                return changeGroupIdsInCache.ElementAt(1);
            }

            return changeGroupIdsInCache.ElementAt(0);
        }

        private bool TryGetPerScopeResolvedConflictInCache(
            Guid scopeId,
            Guid conflictTypeRefName,
            out int conflictId)
        {
            conflictId = int.MinValue;
            if (!m_cachedPerScopeResolvedConflictTypes.ContainsKey(scopeId))
            {
                return false;
            }

            if (!m_cachedPerScopeResolvedConflictTypes[scopeId].ContainsKey(conflictTypeRefName))
            {
                return false;
            }

            conflictId = m_cachedPerScopeResolvedConflictTypes[scopeId][conflictTypeRefName];
            return true;
        }

        private bool TryGetPerGroupResolvedConflictInCache(
            long changeGroupInternalId,
            Guid conflictTypeRefName,
            out int conflictId)
        {
            conflictId = int.MinValue;
            if (!m_cachedPerGroupResolvedConflictTypes.ContainsKey(changeGroupInternalId))
            {
                return false;
            }

            if (!m_cachedPerGroupResolvedConflictTypes[changeGroupInternalId].ContainsKey(conflictTypeRefName))
            {
                return false;
            }

            conflictId = m_cachedPerGroupResolvedConflictTypes[changeGroupInternalId][conflictTypeRefName];
            return true;
        }

        private void UnblockLinkChangeGroupWithNoConflicts(
            RuntimeEntityModel context, 
            RTLinkChangeAction conflictedLinkAction)
        {
            if (null == conflictedLinkAction)
            {
                return;
            }

            conflictedLinkAction.Conflicted = false;

            conflictedLinkAction.LinkChangeGroupReference.Load();
            RTLinkChangeGroup group = conflictedLinkAction.LinkChangeGroup;
            long groupId = group.Id;
            var backloggedActionsInGroup =
                from a in context.RTLinkChangeActionSet
                where a.LinkChangeGroup.Id == groupId
                   && a.Conflicted == true
                   && a.Id != conflictedLinkAction.Id
                select a;
            if (backloggedActionsInGroup.Count() == 0)
            {
                group.ContainsConflictedAction = false;
            }
        }

        private void SaveChangesOnConflictedLinkChangeAction(
            LinkChangeAction linkChangeAction, 
            RTLinkChangeAction conflictedLinkAction)
        {
            // only expects status to change for now
            conflictedLinkAction.Status = (int)linkChangeAction.Status;
        }

        private void SaveChangesOnConflictedMigrationAction(
            IMigrationAction migrationAction, 
            RTChangeAction conflictedAction)
        {
            conflictedAction.ActionData = 
                    (null == migrationAction.MigrationActionDescription ? null : migrationAction.MigrationActionDescription.OuterXml);
            conflictedAction.ActionId = migrationAction.Action;
            conflictedAction.FromPath = migrationAction.FromPath;
            conflictedAction.ItemTypeReferenceName = migrationAction.ItemTypeReferenceName;
            conflictedAction.Label = migrationAction.Label;
            conflictedAction.MergeVersionTo = migrationAction.MergeVersionTo;
            conflictedAction.Recursivity = migrationAction.Recursive;
            conflictedAction.Version = migrationAction.Version;
            conflictedAction.ToPath = migrationAction.Path;
            conflictedAction.ChangeGroupReference.Load();
            conflictedAction.ChangeGroup.Status = (int)migrationAction.ChangeGroup.Status;
        }

        /// <summary>
        /// In-memory update a ChangeGroup to "not contain conflicted changed actions".
        /// </summary>
        /// <remarks>
        /// The ConflictManager submit conflict resolution changes in one commit. In the case
        /// where a change group has multiple conflict change actions, and they are all resolved in one 
        /// commit, the linq query in this method won't return expected result. We need to return
        /// the parent group, and re-evaluate after committing the changes.
        /// </remarks>
        /// <param name="context"></param>
        /// <param name="conflictedAction"></param>
        /// <param name="changeGroupIdsForReEvaluation"></param>
        private static void UnblockChangeGroupWithNoConflicts(
            RuntimeEntityModel context, 
            RTChangeAction conflictedAction,
            List<long> changeGroupIdsForReEvaluation)
        {
            if (null == conflictedAction)
            {
                return;
            }

            conflictedAction.Backlogged = false;

            conflictedAction.ChangeGroupReference.Load();
            RTChangeGroup group = conflictedAction.ChangeGroup;
            long groupId = group.Id;
            int backloggedActionsInGroup =
                (from a in context.RTChangeActionSet
                where a.ChangeGroupId == groupId
                   && a.Backlogged
                   && a.ChangeActionId != conflictedAction.ChangeActionId
                select a.ChangeActionId).Count();
            if (backloggedActionsInGroup == 0)
            {
                group.ContainsBackloggedAction = false;
            }
            else
            {
                changeGroupIdsForReEvaluation.Add(groupId);
            }
        }

        private void UnblockChainedItemUpdates(
            RuntimeEntityModel context, 
            RTConflict rTConflict, 
            Guid sourceId, 
            List<long> changeGroupIdsForReEvaluation)
        {
            string sourceItemId = rTConflict.ConflictedChangeAction.FromPath;
            if (string.IsNullOrEmpty(sourceItemId))
            {
                return;
            }
            string sourceItemScopeHint = ChainOnBackloggedItemConflictType.CreateScopeHint(sourceItemId);

            // In a two-way sync chained item updates from both sides need to be unblocked
            string targetItemScopeHint = string.Empty;
            ITranslationService translationService = m_serviceContainer.GetService(typeof(ITranslationService)) as ITranslationService;
            Debug.Assert(translationService != null);
            if (translationService != null)
            {
                string targetItemId = translationService.TryGetTargetItemId(sourceItemId, sourceId);
                if (!string.IsNullOrEmpty(targetItemId))
                {
                    targetItemScopeHint = ChainOnBackloggedItemConflictType.CreateScopeHint(targetItemId);
                }
            }

            Guid chainOnBackloggedItemConflictTypeRefName = new ChainOnBackloggedItemConflictType().ReferenceName;
            var chainedItemUpdatesInBacklog =
                from c in context.RTConflictSet
                where c.SourceSideMigrationSource != null
                    // todo: uncomment and run EditEditConflictTakeTargetAfterMoreDeltaFromSourceTest
                //&& c.SourceSideMigrationSource.UniqueId.Equals(sourceId)  
                && c.ConflictType.ReferenceName.Equals(chainOnBackloggedItemConflictTypeRefName)
                && c.ScopeHint != null
                && (c.ScopeHint.Equals(sourceItemScopeHint) || c.ScopeHint.Equals(targetItemScopeHint))
                && c.Status == 0
                select c;

            foreach (RTConflict update in chainedItemUpdatesInBacklog)
            {
                update.Status = 1;

                // unblock the chained item updates in backlog
                update.ConflictedChangeActionReference.Load();
                UnblockChangeGroupWithNoConflicts(context, update.ConflictedChangeAction, changeGroupIdsForReEvaluation);
            }
        }

        public void BacklogUnresolvedConflict(Guid sourceId, MigrationConflict conflict, bool raiseConflictUnresolvedEvent)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                RTConflictCollection conflictCollection = context.RTConflictCollectionSet.Where
                    (cc => cc.Id == this.m_internalConflictCollectionId).First<RTConflictCollection>();
                Debug.Assert(conflictCollection != null, "Conflict collection is not created for the session run");

                RTMigrationSource sourceSideMigrationSource = FindOrCreateSource(context, sourceId);

                RTConflictType conflictType = FindOrCreateConflictType(context, conflict.ConflictType);
                const int UnresolvedConflictStatus = 0;

                IEnumerable<RTConflict> rtConflicts = null;
                RTChangeAction conflictedAction = null;
                RTLinkChangeAction conflictedLinkAction = null;
                if (conflict.ConflictedChangeAction != null)
                {
                    Debug.Assert(conflict.ConflictedChangeAction.ActionId > 0,
                        "conflict.ConflictedChangeAction.ActionId has invalid value");

                    conflictedAction = context.RTChangeActionSet.Where
                        (ca => ca.ChangeActionId == conflict.ConflictedChangeAction.ActionId).First();

                    if (conflictedAction == null)
                    {
                        throw new MigrationException(MigrationToolkitResources.ErrorNonexistConflictChangeAction);
                    }

                    // look for an existing active conflict, so that we won't unnecessarily create a new one
                    rtConflicts =
                        (from c in context.RTConflictSet
                         where c.ConflictDetails.Equals(conflict.ConflictDetails)
                         && c.ConflictedChangeAction.ChangeActionId == conflictedAction.ChangeActionId
                         && c.ConflictType.Id == conflictType.Id
                         && c.ScopeHint == conflict.ScopeHint
                         && c.SourceSideMigrationSource.Id == sourceSideMigrationSource.Id
                         && c.Status == UnresolvedConflictStatus 
                         select c);

                    // update change action and (potentially) change group status
                    conflictedAction.Backlogged = true;
                    conflictedAction.ChangeGroupReference.Load();
                    conflictedAction.ChangeGroup.ContainsBackloggedAction = true;
                }
                else if (conflict.ConflictedLinkChangeAction != null)
                {
                    Debug.Assert(
                        conflict.ConflictedLinkChangeAction.InternalId != LinkChangeAction.INVALID_INTERNAL_ID,
                        "conflict.ConflictedLinkChangeAction.ActionId has invalid value");

                    conflictedLinkAction = context.RTLinkChangeActionSet.Where
                        (ca => ca.Id == conflict.ConflictedLinkChangeAction.InternalId).First();

                    if (conflictedLinkAction == null)
                    {
                        throw new MigrationException(MigrationToolkitResources.ErrorNonexistConflictChangeAction);
                    }

                    // look for an existing active conflict, so that we won't unnecessarily create a new one
                    rtConflicts =
                        (from c in context.RTConflictSet
                         where c.ConflictDetails.Equals(conflict.ConflictDetails)
                         && c.ConflictedLinkChangeAction.Id == conflictedLinkAction.Id
                         && c.ConflictType.Id == conflictType.Id
                         && c.ScopeHint == conflict.ScopeHint
                         && c.SourceSideMigrationSource.Id == sourceSideMigrationSource.Id
                         && c.Status == UnresolvedConflictStatus 
                         select c);

                    conflictedLinkAction.Conflicted = true;
                    conflictedLinkAction.LinkChangeGroupReference.Load();
                    conflictedLinkAction.LinkChangeGroup.ContainsConflictedAction = true;
                }
                else if (conflict.ConflictType.IsCountable)
                {
                    // conflict is countable and has no associated (link) change actions

                    // look for an existing active conflict, and increment its count
                    rtConflicts =
                        (from c in context.RTConflictSet
                         where c.ConflictDetails.Equals(conflict.ConflictDetails)
                         && c.ConflictType.Id == conflictType.Id
                         && c.ScopeHint == conflict.ScopeHint
                         && c.SourceSideMigrationSource.Id == sourceSideMigrationSource.Id
                         && c.Status == UnresolvedConflictStatus
                         select c);

                    if (rtConflicts.Count() > 0)
                    {
                        // increment the conflict count
                        if (rtConflicts.First().ConflictCount.HasValue)
                        {
                            rtConflicts.First().ConflictCount += 1;
                        }
                        else
                        {
                            rtConflicts.First().ConflictCount = 1;
                        }
                    }
                }

                RTConflict rtConflict;
                if (rtConflicts != null && rtConflicts.Count() > 0)
                {
                    rtConflict = rtConflicts.First();
                }
                else
                {
                    rtConflict = RTConflict.CreateRTConflict(0, 0, ScopeId);
                    rtConflict.ConflictDetails = conflict.ConflictDetails;
                    rtConflict.ConflictedChangeAction = conflictedAction;
                    rtConflict.ConflictedLinkChangeAction = conflictedLinkAction;
                    rtConflict.ConflictType = conflictType;
                    rtConflict.InCollection = conflictCollection;
                    rtConflict.ScopeHint = conflict.ScopeHint;
                    rtConflict.SourceSideMigrationSource = sourceSideMigrationSource;
                    rtConflict.CreationTime = DateTime.Now;
                    rtConflict.ConflictCount = 1;
                }
                context.TrySaveChanges();

                conflict.InternalId = rtConflict.Id;
            }

            if (raiseConflictUnresolvedEvent)
            {
                RaiseConflictUnresolvedEvent(sourceId, conflict, "Backlogged an unresolved conflict");
            }
        }

        private RTMigrationSource FindOrCreateSource(RuntimeEntityModel context, Guid sourceId)
        {
            var sources = context.RTMigrationSourceSet.Where
                    (ms => ms.UniqueId.Equals(sourceId));

            if (sources.Count<RTMigrationSource>() > 0)
            {
                return sources.First<RTMigrationSource>();
            }

            if (sourceId.Equals(Constants.FrameworkSourceId))
            {
                RTMigrationSource frameWorkSource = RTMigrationSource.CreateRTMigrationSource(
                    0, sourceId, Constants.FrameworkName, sourceId.ToString(), sourceId.ToString(), sourceId.ToString());
                RTProvider frameWorkProvider = RTProvider.CreateRTProvider(0, sourceId, Constants.FrameworkName);
                frameWorkSource.Provider = frameWorkProvider;
                context.AddToRTMigrationSourceSet(frameWorkSource);
                return frameWorkSource;
            }
                
            Debug.Assert(false, string.Format("Cannot find Migration Source ({0}) in DB.", sourceId));
            throw new MigrationException(string.Format(
                MigrationToolkitResources.UnknownSourceId, sourceId.ToString()));
        }

        private RTResolutionRule FindResolutionRule(RuntimeEntityModel context, ConflictResolutionRule resolutionRule)
        {
            var rules = context.RTResolutionRuleSet.Where
                (r => r.ReferenceName.Equals(resolutionRule.RuleRefNameGuid));

            if (rules.Count<RTResolutionRule>() == 0)
            {
                throw new ConflictManagementGeneralException(
                    MigrationToolkitResources.ErrorResolutionRuleNotFound, 
                    resolutionRule.RuleRefNameGuid.ToString());
            }

            return rules.First<RTResolutionRule>();
        }

        private static RTConflictType FindOrCreateConflictType(RuntimeEntityModel context, ConflictType conflictType)
        {
            var types = context.RTConflictTypeSet.Where(t => t.ReferenceName.Equals(conflictType.ReferenceName));
            if (types.Count<RTConflictType>() > 0)
            {
                if (!string.IsNullOrEmpty(conflictType.FriendlyName)
                    && (string.IsNullOrEmpty(types.First().FriendlyName)
                        || !types.First().FriendlyName.Equals(conflictType.FriendlyName)))
                {
                    types.First().FriendlyName = conflictType.FriendlyName;
                }
                return types.First<RTConflictType>();
            }

            RTConflictType type = RTConflictType.CreateRTConflictType(0, conflictType.ReferenceName, conflictType.FriendlyName);
            return type;
        }

        public ReadOnlyCollection<ConflictResolutionRule> GetPersistedRules(ConflictType conflictType)
        {
            List<ConflictResolutionRule> resultRules = new List<ConflictResolutionRule>();

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                // get all the rules in the scope for the conflict type
                var rtRules =
                    from r in context.RTResolutionRuleSet
                    where r.ConflictType.ReferenceName.Equals(conflictType.ReferenceName)
                    && r.Status == 0
                    && r.ScopeInfoUniqueId.Equals(this.ScopeId)
                    && r.SourceInfoUniqueId.Equals(this.SourceId)
                    select r;

                foreach (RTResolutionRule rtRule in rtRules)
                {
                    ConflictResolutionRule rule = RealizeRuleFromEDM(rtRule);
                    resultRules.Add(rule);
                }
            }

            if (resultRules.Count > 0)
            {
                resultRules.Sort(conflictType.ScopeInterpreter.RuleScopeComparer);
            }
            return resultRules.AsReadOnly();
        }

        private ConflictResolutionRule RealizeRuleFromEDM(RTResolutionRule rtRule)
        {
            rtRule.ConflictTypeReference.Load();
            if (!this.RegisteredConflictTypes.ContainsKey(rtRule.ConflictType.ReferenceName))
            {
                throw new MigrationException(string.Format(
                    MigrationToolkitResources.ErrorInvalidConflictResolutionRule,
                    rtRule.ConflictType.FriendlyName,
                    rtRule.ConflictType.ReferenceName,
                    rtRule.ReferenceName,
                    ScopeId.ToString(),
                    SourceId.ToString(),
                    SourceId.Equals(Constants.FrameworkSourceId) ? "(Framework)" : "(Migration Source)"));
            }

            ConflictType type = this.RegisteredConflictTypes[rtRule.ConflictType.ReferenceName];
            rtRule.ResolutionActionReference.Load();
            if (!type.SupportedResolutionActions.ContainsKey(rtRule.ResolutionAction.ReferenceName))
            {
                throw new MigrationException(string.Format(
                    MigrationToolkitResources.ErrorInvalidResolutionActionForConflictType,
                    rtRule.ResolutionAction.ReferenceName,
                    type.FriendlyName,
                    type.ReferenceName));
            }

            ConfliceResolutionRuleSerializer ruleSerializer = new ConfliceResolutionRuleSerializer();
            ConflictResolutionRule rule = ruleSerializer.Deserialize(rtRule.RuleData);
            rule.InternalId = rtRule.Id;
            return rule;
        }

        private void RaiseConflictUnresolvedEvent(Guid sourceId, MigrationConflict conflict, string message)
        {
            if (null != ConflictUnresolvedEvent)
            {
                ConflictUnresolvedEventArgs args = 
                    new ConflictUnresolvedEventArgs(conflict, message, SourceId, ScopeId, Thread.CurrentThread);

                if (this.RegisteredConflictTypeWithExplictSyncOrchOption.ContainsKey(conflict.ConflictType.ReferenceName))
                {
                    args.SyncOrchestrationOption = this.RegisteredConflictTypeWithExplictSyncOrchOption[conflict.ConflictType.ReferenceName];
                }

                ConflictUnresolvedEvent(this, args);
            }
        }

        public bool IsItemInBacklog(string sourceSideItemId)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                var conflictedItems =
                    from c in context.RTConflictSet
                    where c.ConflictedChangeAction != null
                       && c.ConflictedChangeAction.FromPath.Equals(sourceSideItemId)
                       && c.Status == 0
                       && c.SourceSideMigrationSource != null
                       && c.SourceSideMigrationSource.UniqueId.Equals(SourceId)
                    select c;
                stopwatch.Stop();
                if (stopwatch.ElapsedMilliseconds > 100)
                {
                    TraceManager.TraceInformation(String.Format("IsItemInBacklog1: Query completed in {0} ms", stopwatch.ElapsedMilliseconds));
                }
                if (conflictedItems.Count<RTConflict>() > 0)
                {
                    return true;
                }
            }

            return false;
        }

        internal void ResolveExistingConflictsAndSkipBackloggedItems(
            Guid sessionId,
            Guid sourceMigrationSourceId, 
            Guid targetMigrationSourceId, 
            string sourceSideItemId, 
            string targetSideItemId)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var conflictedItems =
                    from c in context.RTConflictSet
                    where c.ConflictedChangeAction != null
                       && c.Status != (int)MigrationConflict.Status.Resolved
                       && c.ConflictedChangeAction.ChangeGroup.SessionUniqueId.Equals(sessionId)
                       && c.SourceSideMigrationSource != null
                       && (c.SourceSideMigrationSource.UniqueId.Equals(sourceMigrationSourceId) || 
                          (c.SourceSideMigrationSource.UniqueId.Equals(targetMigrationSourceId) && c.ConflictedChangeAction.ChangeGroup.Status == (int)ChangeStatus.Pending))
                       && (c.ConflictedChangeAction.FromPath.Equals(sourceSideItemId) || c.ConflictedChangeAction.FromPath.Equals(targetSideItemId))
                    select c;

                foreach (RTConflict rtConflict in conflictedItems)
                {
                    rtConflict.Status = (int)MigrationConflict.Status.Resolved;
                    rtConflict.ConflictedChangeActionReference.Load();
                    rtConflict.ConflictedChangeAction.Backlogged = false;
                    rtConflict.ConflictedChangeAction.ChangeGroupReference.Load();
                    rtConflict.ConflictedChangeAction.ChangeGroup.Status = (int)ChangeStatus.Skipped;
                    rtConflict.ConflictedChangeAction.ChangeGroup.ContainsBackloggedAction = false;
                }

                // Find conflicts on link actions
                var conflictedLinkItems =
                    from c in context.RTConflictSet
                    where c.ConflictedLinkChangeAction != null
                       && c.Status != (int)MigrationConflict.Status.Resolved
                       && c.ConflictedLinkChangeAction.SessionUniqueId.Equals(sessionId)
                       && c.ConflictedLinkChangeAction.LinkChangeGroup.GroupName.Equals(sourceSideItemId)
                    select c;

                foreach (RTConflict rtConflict in conflictedLinkItems)
                {
                    rtConflict.Status = (int)MigrationConflict.Status.Resolved;
                    rtConflict.ConflictedLinkChangeActionReference.Load();
                    rtConflict.ConflictedLinkChangeAction.Conflicted = false;
                    rtConflict.ConflictedLinkChangeAction.LinkChangeGroupReference.Load();
                    rtConflict.ConflictedLinkChangeAction.LinkChangeGroup.Status = (int)LinkChangeGroup.LinkChangeGroupStatus.Completed;
                    rtConflict.ConflictedLinkChangeAction.LinkChangeGroup.ContainsConflictedAction = false;
                }

                int nonLinkConflictsResolved = conflictedItems.Count();
                int linkConflictsResolved = conflictedLinkItems.Count();

                TraceManager.TraceInformation(String.Format("Clearing a total of {0} conflicts ({1} for links) as a result of force syncing item {2}",
                    nonLinkConflictsResolved + linkConflictsResolved,
                    linkConflictsResolved,
                    sourceSideItemId));

                context.TrySaveChanges();
            }
        }

        public bool IsItemInBacklog(Guid sourceMigrationSourceId, Guid targetMigrationSourceId, string sourceSideItemId)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                var conflictedItems =
                    from c in context.RTConflictSet
                    where c.ConflictedChangeAction != null
                       && c.ConflictedChangeAction.FromPath.Equals(sourceSideItemId)
                       && c.Status == 0
                    select c;
                stopwatch.Stop();
                if (stopwatch.ElapsedMilliseconds > 100)
                {
                    TraceManager.TraceInformation(String.Format("IsItemInBacklog2: Query 1 completed in {0} ms", stopwatch.ElapsedMilliseconds));
                }

                if (conflictedItems.Count<RTConflict>() > 0)
                {
                    int srcSideStatusDelta = (int)ChangeStatus.Delta;
                    int srcSideStatusDeltaPending = (int)ChangeStatus.DeltaPending;
                    int srcSideStatusDeltaComplete = (int)ChangeStatus.DeltaComplete;
                    int srcSideStatusDeltaSynced = (int)ChangeStatus.DeltaSynced;

                    stopwatch = Stopwatch.StartNew();
                    var srcItemQuery = from c in conflictedItems
                                       where c.ConflictedChangeAction.ChangeGroup.SourceUniqueId.Equals(sourceMigrationSourceId)
                                          && (c.ConflictedChangeAction.ChangeGroup.Status == srcSideStatusDelta
                                          || c.ConflictedChangeAction.ChangeGroup.Status == srcSideStatusDeltaPending
                                          || c.ConflictedChangeAction.ChangeGroup.Status == srcSideStatusDeltaComplete
                                          || c.ConflictedChangeAction.ChangeGroup.Status == srcSideStatusDeltaSynced)
                                       select c;
                    stopwatch.Stop();
                    if (stopwatch.ElapsedMilliseconds > 100)
                    {
                        TraceManager.TraceInformation(String.Format("IsItemInBacklog2: Query 2 completed in {0} ms", stopwatch.ElapsedMilliseconds));
                    }
                    if (srcItemQuery.Count() > 0)
                    {
                        return true;
                    }

                    stopwatch = Stopwatch.StartNew();
                    srcItemQuery = from c in conflictedItems
                                   where c.ConflictedChangeAction.ChangeGroup.SourceUniqueId.Equals(targetMigrationSourceId)
                                      && ! (c.ConflictedChangeAction.ChangeGroup.Status == srcSideStatusDelta
                                      || c.ConflictedChangeAction.ChangeGroup.Status == srcSideStatusDeltaPending
                                      || c.ConflictedChangeAction.ChangeGroup.Status == srcSideStatusDeltaComplete
                                      || c.ConflictedChangeAction.ChangeGroup.Status == srcSideStatusDeltaSynced)
                                   select c;
                    stopwatch.Stop();
                    if (stopwatch.ElapsedMilliseconds > 100)
                    {
                        TraceManager.TraceInformation(String.Format("IsItemInBacklog2: Query 3 completed in {0} ms", stopwatch.ElapsedMilliseconds));
                    }
                    if (srcItemQuery.Count() > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
