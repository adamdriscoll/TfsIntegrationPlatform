// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal class VCContentConflictHandler : IConflictHandler
    {
        #region IConflictHandler Members

        public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            if (rule.ActionRefNameGuid.Equals(new VCContentConflictUserMergeChangeAction().ReferenceName))
            {
                if (!conflict.ScopeHint.Contains(';'))
                {
                    TraceManager.TraceWarning("Manual resolution rule of content conflict must be change group specific");
                    return false;
                }
            }
            return ConflictTypeHandled.ScopeInterpreter.IsInScope(conflict.ScopeHint, rule.ApplicabilityScope);
        }

        public ConflictResolutionResult Resolve(IServiceContainer serviceContainer, MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;
            long otherSideConflictActionId;

            if (rule.ActionRefNameGuid.Equals(new VCContentConflictTakeLocalChangeAction().ReferenceName))
            {
                // left-> right migration. User select take local (right). So we skip left side conflict action(the original conflict action).
                if (!markChangeActionAsSkipped(conflict.ConflictedChangeAction.ActionId))
                {
                    TraceManager.TraceError("Cannot mark conflict action {0} as skipped", conflict.ConflictedChangeAction.ActionId);
                    return new ConflictResolutionResult(false, ConflictResolutionType.SkipConflictedChangeAction);
                }
                return new ConflictResolutionResult(true, ConflictResolutionType.SkipConflictedChangeAction);
            }
            else if (rule.ActionRefNameGuid.Equals(new VCContentConflictTakeOtherChangesAction().ReferenceName))
            {
                // left-> right migration. User select take other (left). So we skip right side conflict action(the other side's conflict action).
                if (!long.TryParse(conflict.ConflictDetails, out otherSideConflictActionId))
                {
                    TraceManager.TraceError("Cannot find other side's conflict action {0}", conflict.ConflictDetails);
                    return new ConflictResolutionResult(false, ConflictResolutionType.SuppressOtherSideChangeAction);
                }
                if (!markChangeActionAsSkipped(otherSideConflictActionId))
                {
                    TraceManager.TraceError("Cannot mark conflict action {0} as skipped", conflict.ConflictDetails);
                    return new ConflictResolutionResult(false, ConflictResolutionType.SuppressOtherSideChangeAction);
                }
                return new ConflictResolutionResult(true, ConflictResolutionType.SuppressOtherSideChangeAction);
            }
            else if (rule.ActionRefNameGuid.Equals(new VCContentConflictUserMergeChangeAction().ReferenceName))
            {

                if (updateConversionHistory(conflict, rule, serviceContainer))
                {
                    return new ConflictResolutionResult(true, ConflictResolutionType.SuppressedConflictedChangeGroup);
                }
                else
                {
                    return new ConflictResolutionResult(false, ConflictResolutionType.Other);
                }
            }
            else
            {
                return new ConflictResolutionResult(false, ConflictResolutionType.UnknownResolutionAction);
            }
        }

        /// <summary>
        /// Mark the action state IsSubstituted (skipped) as true.
        /// </summary>
        /// <param name="actionId"></param>
        /// <returns></returns>
        private bool markChangeActionAsSkipped(long actionId)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var actionTableEntries =
                    from a in context.RTChangeActionSet
                    where a.ChangeActionId == actionId
                    select a;
                if (actionTableEntries.Count() > 0)
                {
                    actionTableEntries.First().IsSubstituted = true;
                    context.TrySaveChanges();
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private bool updateConversionHistory(MigrationConflict conflict, ConflictResolutionRule rule, IServiceContainer serviceContainer)
        {
            if (!rule.DataFieldDictionary.ContainsKey(VCContentConflictUserMergeChangeAction.MigrationInstructionChangeId) ||
                !rule.DataFieldDictionary.ContainsKey(VCContentConflictUserMergeChangeAction.DeltaTableChangeId))
            {
                return false;
            }
            
            ChangeGroupService changeGroupService = serviceContainer.GetService(typeof(ChangeGroupService)) as ChangeGroupService;
            string migrationInstructionName = rule.DataFieldDictionary[VCContentConflictUserMergeChangeAction.MigrationInstructionChangeId];
            string deltaTableName = rule.DataFieldDictionary[VCContentConflictUserMergeChangeAction.DeltaTableChangeId];
            string comment = rule.RuleDescription;
            return changeGroupService.UpdateConversionHistoryAndRemovePendingChangeGroups(migrationInstructionName, deltaTableName, comment);
        }

        private bool readyToManualResolve(MigrationConflict conflict)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var changeGroups =
                    from c in context.RTChangeGroupSet
                    where c.ExecutionOrder < conflict.ConflictedChangeAction.ChangeGroup.ExecutionOrder
                        && (c.Status == (int)ChangeStatus.Pending || c.Status == (int)ChangeStatus.InProgress)
                    select c;

                if (changeGroups.Count() > 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }            
        }

        public ConflictType ConflictTypeHandled
        {
            get;
            set;
        }

        #endregion
    }
}
