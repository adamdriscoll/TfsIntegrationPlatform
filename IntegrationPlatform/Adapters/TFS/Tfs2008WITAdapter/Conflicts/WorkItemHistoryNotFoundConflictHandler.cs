// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common.ResolutionActions;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Toolkit.WCFServices;
using Microsoft.TeamFoundation.Migration.Toolkit.WIT;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using BM = Microsoft.TeamFoundation.Migration.BusinessModel;
using Hist = Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter.Conflicts.HistoryNotFoundResolution;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter.Conflicts
{
    /// <summary>
    /// This handler resolves the WorkItemHistoryNotFound conflicts
    /// </summary>
    public class WorkItemHistoryNotFoundConflictHandler : IConflictHandler
    {
        const string HistoryNotFoundResolutionChangeId = "WitHistoryNotFoundResolution";

        #region IConflictHandler Members

        /// <summary>
        /// Determines whether a rule can handles the subject conflict.
        /// </summary>
        /// <param name="conflict">The conflict to be resolved</param>
        /// <param name="rule">The candidate rule</param>
        /// <returns></returns>
        public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            if (conflict.ConflictedChangeAction == null
                || !ConflictTypeHandled.ScopeInterpreter.IsInScope(conflict.ScopeHint, rule.ApplicabilityScope))
            {
                return false;
            }

            if (rule.ActionRefNameGuid.Equals(new HistoryNotFoundSubmitMissingChangesAction().ReferenceName))
            {
                if (SessionGroupIsRunning(conflict))
                {
                    // auto submitting missiong change action requires the session to be interrupted
                    return false;
                }
            }

            if (rule.ActionRefNameGuid.Equals(new HistoryNotFoundUpdateConversionHistoryAction().ReferenceName))
            {
                // todo: check if session is running

                string srcItemId = rule.DataFieldDictionary[HistoryNotFoundUpdateConversionHistoryAction.DATAKEY_SOURCE_ITEM_ID];
                string tgtItemId = rule.DataFieldDictionary[HistoryNotFoundUpdateConversionHistoryAction.DATAKEY_TARGET_ITEM_ID];
                string srcRevRanges = rule.DataFieldDictionary[HistoryNotFoundUpdateConversionHistoryAction.DATAKEY_SOURCE_REVISION_RANGES];
                string tgtRevRanges = rule.DataFieldDictionary[HistoryNotFoundUpdateConversionHistoryAction.DATAKEY_TARGET_REVISION_RANGES];

                if (string.IsNullOrEmpty(srcItemId) || string.IsNullOrEmpty(tgtItemId))
                {
                    return false;
                }

                int[] srcRevs;
                int[] tgtRevs;
                if (!IntegerRange.TryParseRangeString(srcRevRanges, out srcRevs)
                    || !IntegerRange.TryParseRangeString(srcRevRanges, out tgtRevs))
                {
                    return false;
                }

                if (srcRevs == null || srcRevs.Length == 0 || tgtRevs == null || tgtRevs.Length == 0 || srcRevs.Length != tgtRevs.Length)
                {
                    return false;
                }
            }

            return true;
        }

        private RTSession FindSessionForConflictedAction(
            MigrationConflict conflict,
            RuntimeEntityModel context)
        {
            // figure out which session the conflicted change action belongs to
            Guid sessionId = Guid.Empty;
            if (conflict.ConflictedChangeAction.ChangeGroup == null)
            {
                RTChangeGroup rtChangeGroup = FindChangeGroupForConflictedAction(conflict, context);
                if (null != rtChangeGroup)
                {
                    sessionId = rtChangeGroup.SessionUniqueId;
                }
            }
            else
            {
                sessionId = conflict.ConflictedChangeAction.ChangeGroup.SessionId;
            }

            if (sessionId.Equals(Guid.Empty))
            {
                Debug.Assert(false, "cannot find session for the conflicted action");
                return null;
            }

            var sessionQuery = context.RTSessionSet.Where(s => s.SessionUniqueId.Equals(sessionId));
            if (sessionQuery.Count() > 0)
            {
                return sessionQuery.First();
            }
            else
            {
                return null;
            }
        }

        private RTChangeGroup FindChangeGroupForConflictedAction(
            MigrationConflict conflict,
            RuntimeEntityModel context)
        {
            var changeGroupQuery =
                    (from a in context.RTChangeActionSet
                     where a.ChangeActionId == conflict.ConflictedChangeAction.ActionId
                     select a.ChangeGroup);

            if (changeGroupQuery.Count() != 0)
            {
                return changeGroupQuery.First();
            }

            return null;
        }

        private RTSessionGroup FindSessionGroupForConflictedAction(
            MigrationConflict conflict,
            RuntimeEntityModel context)
        {
            RTSession rtSession = FindSessionForConflictedAction(conflict, context);

            // find the change group that contains the session
            var sessionGroupQuery =
                from s in context.RTSessionSet
                where s.SessionUniqueId.Equals(rtSession.SessionUniqueId)
                select s.SessionGroup;
            Debug.Assert(sessionGroupQuery.Count() != 0, "session has no parent session group");
            Debug.Assert(sessionGroupQuery.Count() == 1, "session has multiple parent session group");
            return sessionGroupQuery.First();
        }

        private RTSessionGroup FindSessionGroupForConflictedAction(
            RuntimeEntityModel context,
            RTSession rtSession)
        {
            // find the change group that contains the session
            var sessionGroupQuery =
                from s in context.RTSessionSet
                where s.SessionUniqueId.Equals(rtSession.SessionUniqueId)
                select s.SessionGroup;
            Debug.Assert(sessionGroupQuery.Count() != 0, "session has no parent session group");
            Debug.Assert(sessionGroupQuery.Count() == 1, "session has multiple parent session group");
            return sessionGroupQuery.First();
        }

        private bool SessionGroupIsRunning(MigrationConflict conflict)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                RTSessionGroup rtSessionGroup = FindSessionGroupForConflictedAction(conflict, context);

                if (rtSessionGroup == null)
                {
                    return false;
                }
                Guid sessionGroupId = rtSessionGroup.GroupUniqueId;

                // first, ask the wcf service if it is managing an active session group of this
                MigrationServiceClient client = new MigrationServiceClient();
                var runningGroupIds = client.GetRunningSessionGroups();
                if (!runningGroupIds.Contains(sessionGroupId))
                {
                    return false;
                }

                // next, check if the group is not in the "running" sync orchestration state
                switch (rtSessionGroup.OrchestrationStatus)
                {
                    case (int)PipelineState.Running:
                    case (int)PipelineState.Starting:
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Resolves the subject conflict with the passed-in rule.
        /// </summary>
        /// <param name="conflict">The conflict to be resolved</param>
        /// <param name="rule">The candidate rule</param>
        /// <param name="actions">The migration actions that are generated as part of the resolution plan.</param>
        /// <returns>The conflict resolution result</returns>
        public ConflictResolutionResult Resolve(IServiceContainer serviceContainer, MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;
            if (rule.ActionRefNameGuid.Equals(new HistoryNotFoundSubmitMissingChangesAction().ReferenceName))
            {
                return ResolveBySubmitMissingChanges(serviceContainer, conflict, rule, out actions);
            }
            else if (rule.ActionRefNameGuid.Equals(new HistoryNotFoundUpdateConversionHistoryAction().ReferenceName))
            {
                return ResolveByUpdateConversionHisotry(conflict, rule, out actions);
            }
            else if (rule.ActionRefNameGuid.Equals(new SkipConflictedActionResolutionAction().ReferenceName))
            {
                return SkipConflictedActionResolutionAction.SkipConflict(conflict, true);
            }

            return new ConflictResolutionResult(false, ConflictResolutionType.Other);
        }

        private ConflictResolutionResult ResolveByUpdateConversionHisotry(MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;
            var unresolvedRslt = new ConflictResolutionResult(false, ConflictResolutionType.Other);

            string tgtItemId = rule.DataFieldDictionary[HistoryNotFoundUpdateConversionHistoryAction.DATAKEY_TARGET_ITEM_ID];
            string srcRevRanges = rule.DataFieldDictionary[HistoryNotFoundUpdateConversionHistoryAction.DATAKEY_SOURCE_REVISION_RANGES];
            string tgtRevRanges = rule.DataFieldDictionary[HistoryNotFoundUpdateConversionHistoryAction.DATAKEY_TARGET_REVISION_RANGES];

            int[] srcRevs;
            int[] tgtRevs;
            if (string.IsNullOrEmpty(tgtItemId)
                || !IntegerRange.TryParseRangeString(srcRevRanges, out srcRevs)
                || !IntegerRange.TryParseRangeString(srcRevRanges, out tgtRevs)
                || srcRevs == null || srcRevs.Length == 0 || tgtRevs == null || tgtRevs.Length == 0 || srcRevs.Length != tgtRevs.Length)
            {
                return unresolvedRslt;
            }

            WorkItemHistoryNotFoundConflictType conflictType = conflict.ConflictType as WorkItemHistoryNotFoundConflictType;
            Debug.Assert(null != conflictType, "conflictType is null");

            WorkItemHistoryNotFoundConflictTypeDetails dtls = conflictType.GetConflictDetails(conflict);

            ConversionResult convRslt = new ConversionResult(dtls.SourceMigrationSourceId, dtls.TargetMigrationSourceId);
            convRslt.ChangeId = HistoryNotFoundResolutionChangeId;
            convRslt.ContinueProcessing = true;

            for (int i = 0; i < srcRevs.Length; ++i)
            {
                convRslt.ItemConversionHistory.Add(new ItemConversionHistory(dtls.SourceWorkItemID, srcRevs[i].ToString(), tgtItemId, tgtRevs[i].ToString()));
            }

            int sessionRunId = 0;
            Guid sourceMigrationSourceId = Guid.Empty;
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                long actionInternalId = conflict.ConflictedChangeAction.ActionId;
                var actionQuery = context.RTChangeActionSet.Where(a => a.ChangeActionId == actionInternalId);
                if (actionQuery.Count() == 0)
                {
                    return unresolvedRslt;
                }

                RTChangeAction action = actionQuery.First();
                action.ChangeGroupReference.Load();
                action.ChangeGroup.SessionRunReference.Load();
                sessionRunId = action.ChangeGroup.SessionRun.Id;

                sourceMigrationSourceId = action.ChangeGroup.SourceUniqueId;
            }

            convRslt.Save(sessionRunId, sourceMigrationSourceId);

            return new ConflictResolutionResult(true, ConflictResolutionType.Other);
        }

        private ConflictResolutionResult ResolveBySubmitMissingChanges(
            IServiceContainer serviceContainer,
            MigrationConflict conflict,
            ConflictResolutionRule rule,
            out List<MigrationAction> actions)
        {
            actions = null;
            var retVal = new ConflictResolutionResult(false, ConflictResolutionType.Other);

            WITTranslationService translationService = serviceContainer.GetService(typeof(ITranslationService)) as WITTranslationService;
            Debug.Assert(null != translationService, "translationService is not initialized or not a wit translation service");

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                RTSessionGroup rtSessionGroup = FindSessionGroupForConflictedAction(conflict, context);
                if (null == rtSessionGroup)
                {
                    return retVal;
                }

                BM.BusinessModelManager bmm = new BM.BusinessModelManager();
                BM.Configuration sessionGroupConfig = bmm.LoadConfiguration(rtSessionGroup.GroupUniqueId);

                // find target-side migration source config
                var parentChangeGroup = FindChangeGroupForConflictedAction(conflict, context);
                Guid targetMigrationSourceId = parentChangeGroup.SourceUniqueId;
                BM.MigrationSource targetMigrationSourceConfig = sessionGroupConfig.SessionGroup.MigrationSources[targetMigrationSourceId];
                if (null == targetMigrationSourceConfig)
                {
                    return retVal;
                }

                // find source-side migration source config
                RTSession rtSession = FindSessionForConflictedAction(conflict, context);
                BM.Session parentSession = null;
                foreach (BM.Session s in sessionGroupConfig.SessionGroup.Sessions.Session)
                {
                    if (new Guid(s.SessionUniqueId).Equals(rtSession.SessionUniqueId))
                    {
                        parentSession = s;
                        break;
                    }
                }
                if (parentSession == null)
                {
                    return retVal;
                }
                Guid sourceMigrationSourceId = ((new Guid(parentSession.LeftMigrationSourceUniqueId)).Equals(targetMigrationSourceId))
                    ? new Guid(parentSession.RightMigrationSourceUniqueId) : new Guid(parentSession.LeftMigrationSourceUniqueId);
                BM.MigrationSource sourceMigrationSourceConfig = sessionGroupConfig.SessionGroup.MigrationSources[sourceMigrationSourceId];
                if (null == sourceMigrationSourceConfig)
                {
                    return retVal;
                }

                string sourceServerUrl = sourceMigrationSourceConfig.ServerUrl;
                string sourceTeamProject = sourceMigrationSourceConfig.SourceIdentifier;
                string targetServerUrl = targetMigrationSourceConfig.ServerUrl;
                string targetTeamProject = targetMigrationSourceConfig.SourceIdentifier;

                string srcWorkItemIdStr = TfsMigrationWorkItemStore.GetSourceWorkItemId(conflict.ConflictedChangeAction);
                Debug.Assert(!string.IsNullOrEmpty(srcWorkItemIdStr), "srcWorkItemId is null or empty");
                int srcWorkItemId;
                if (!int.TryParse(srcWorkItemIdStr, out srcWorkItemId))
                {
                    return retVal;
                }

                string srcRevRanges = rule.DataFieldDictionary[HistoryNotFoundSubmitMissingChangesAction.DATAKEY_REVISION_RANGE];
                int[] sourceRevToSync = new int[0];
                if (string.IsNullOrEmpty(srcRevRanges))
                {
                    sourceRevToSync = ExtractMissingRevs(conflict.ConflictedChangeAction);
                }
                else
                {
                    if (!IntegerRange.TryParseRangeString(srcRevRanges, out sourceRevToSync))
                    {
                        return retVal;
                    }
                }
                if (sourceRevToSync.Length == 0)
                {
                    return retVal;
                }

                try
                {
                    // compute delta from source side
                    TfsWITAnalysisProvider analysisProvider = new TfsWITAnalysisProvider(sourceServerUrl, sourceTeamProject);
                    WorkItem sourceWorkItem = analysisProvider.GetWorkItem(srcWorkItemId);

                    Hist.MigrationAction[] sourceRevDetails = new Hist.MigrationAction[sourceRevToSync.Length];
                    for (int revIndex = 0; revIndex < sourceRevToSync.Length; ++revIndex)
                    {
                        var details = new TfsWITRecordDetails(sourceWorkItem, sourceRevToSync[revIndex]);
                        SanitizeDetails(details);
                        translationService.MapWorkItemTypeFieldValues(
                            sourceWorkItem.Id.ToString(), details.DetailsDocument, sourceMigrationSourceId);

                        TfsConstants.ChangeActionId actionId = (sourceRevToSync[revIndex] == 1 ? TfsConstants.ChangeActionId.Add : TfsConstants.ChangeActionId.Edit);
                        sourceRevDetails[revIndex] = new Hist.MigrationAction(sourceWorkItem.Id.ToString(), details, actionId);
                    }

                    // migrate to target side
                    TfsWITMigrationProvider migrationProvider = new TfsWITMigrationProvider(targetServerUrl, targetTeamProject, string.Empty);
                    Hist.ConversionResult conversionResult = migrationProvider.ProcessChangeGroup(sourceRevDetails);

                    // update conversion history
                    ConversionResult convRslt = new ConversionResult(sourceMigrationSourceId, targetMigrationSourceId);
                    convRslt.ChangeId = HistoryNotFoundResolutionChangeId;
                    convRslt.ContinueProcessing = true;

                    foreach (var itemConvHist in conversionResult.ItemConversionHistory)
                    {
                        convRslt.ItemConversionHistory.Add(new ItemConversionHistory(
                            itemConvHist.SourceItemId, itemConvHist.SourceItemVersion, itemConvHist.TargetItemId, itemConvHist.TargetItemVersion));
                    }

                    parentChangeGroup.SessionRunReference.Load();
                    int sessionRunId = parentChangeGroup.SessionRun.Id;
                    convRslt.Save(sessionRunId, sourceMigrationSourceId);
                }
                catch (Exception ex)
                {
                    TraceManager.TraceException(ex);
                    retVal.Comment = ex.ToString();
                    return retVal;
                }
            }

            retVal.Resolved = true;
            return retVal;
        }

        private void SanitizeDetails(TfsWITRecordDetails details)
        {
            var nodeToDelete = details.DetailsDocument.DocumentElement.SelectSingleNode(string.Format(
                @"/WorkItemChanges/Columns/Column[@ReferenceName=""{0}""]", CoreFieldReferenceNames.AreaId));
            if (nodeToDelete != null) nodeToDelete.ParentNode.RemoveChild(nodeToDelete);
            nodeToDelete = details.DetailsDocument.DocumentElement.SelectSingleNode(string.Format(
                @"/WorkItemChanges/Columns/Column[@ReferenceName=""{0}""]", CoreFieldReferenceNames.IterationId));
            if (nodeToDelete != null) nodeToDelete.ParentNode.RemoveChild(nodeToDelete);
            nodeToDelete = details.DetailsDocument.DocumentElement.SelectSingleNode(string.Format(
                @"/WorkItemChanges/Columns/Column[@ReferenceName=""{0}""]", CoreFieldReferenceNames.WorkItemType));
            if (nodeToDelete != null) nodeToDelete.ParentNode.RemoveChild(nodeToDelete);
        }

        private int[] ExtractMissingRevs(IMigrationAction iMigrationAction)
        {
            string currRevStr = TfsMigrationWorkItemStore.GetSourceWorkItemRevision(iMigrationAction);
            int currRev;
            if (int.TryParse(currRevStr, out currRev))
            {
                Debug.Assert(currRev > 0, "current revision <= 0");
                int[] retVal = new int[currRev - 1];
                for (int i = 1; i < currRev; ++i)
                {
                    retVal[i - 1] = i;
                }
                return retVal;
            }
            else
            {
                return new int[0];
            }
        }

        /// <summary>
        /// Gets the conflict type that's handled by this handler.
        /// </summary>
        public ConflictType ConflictTypeHandled
        {
            get;
            set;
        }

        #endregion
    }
}
