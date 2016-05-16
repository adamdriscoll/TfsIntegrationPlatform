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
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.WCFServices;
using BM = Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon
{
    class TfsCheckinFailureConflictHandler : IConflictHandler
    {
        #region IConflictHandler Members

        public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            return ConflictTypeHandled.ScopeInterpreter.IsInScope(conflict.ScopeHint, rule.ApplicabilityScope);
        }

        public ConflictResolutionResult Resolve(IServiceContainer serviceContainer, MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;
            if (rule.ActionRefNameGuid.Equals(new TfsCheckinFailureRetryAction().ReferenceName))
            {
                ConflictManager conflictManager = (ConflictManager)serviceContainer.GetService(typeof(ConflictManager));
                RemoveInProgressChangeGroupsInSession(conflictManager.ScopeId);
                return new ConflictResolutionResult(true, ConflictResolutionType.Retry);
            }
            else if (rule.ActionRefNameGuid.Equals(new TfsCheckinFailureManualResolveAction().ReferenceName))
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

        public ConflictType ConflictTypeHandled
        {
            get;
            set;
        }

        /// <summary>
        /// Remove all change groups in current session which are in progress. 
        /// For delta table, this includes status of Delta, DeltaPending.
        /// For migration instruction, this includes Pending or Inprogress.
        /// </summary>
        private void RemoveInProgressChangeGroupsInSession(Guid sessionId)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                TraceManager.TraceInformation(
                                "Deleting cached data for session '{0}'", sessionId);
                context.DeleteSessionCachedData(sessionId);
            }
        }

        private bool updateConversionHistory(MigrationConflict conflict, ConflictResolutionRule rule, IServiceContainer serviceContainer)
        {
            if (!rule.DataFieldDictionary.ContainsKey(TfsCheckinFailureManualResolveAction.MigrationInstructionChangeId) ||
                !rule.DataFieldDictionary.ContainsKey(TfsCheckinFailureManualResolveAction.DeltaTableChangeId))
            {
                return false;
            }

            ChangeGroupService changeGroupService = serviceContainer.GetService(typeof(ChangeGroupService)) as ChangeGroupService;
            string migrationInstructionName = rule.DataFieldDictionary[TfsCheckinFailureManualResolveAction.MigrationInstructionChangeId];
            string deltaTableName = rule.DataFieldDictionary[TfsCheckinFailureManualResolveAction.DeltaTableChangeId];
            string comment = rule.RuleDescription;
            bool result = changeGroupService.UpdateConversionHistoryAndRemovePendingChangeGroups(migrationInstructionName, deltaTableName, comment);
            TraceManager.TraceInformation(string.Format("Conflict of type '{0}' resolved by updating history with new change versions: Source HighWaterMark: {1}; Target HighWaterMark: {2}",
                conflict.ConflictType.FriendlyName, deltaTableName, migrationInstructionName));
            return result;
        }

        #endregion
    }
}
