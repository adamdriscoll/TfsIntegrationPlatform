// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;

using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    class VCChangeGroupInprogressConflictHandler : IConflictHandler
    {
        #region IConflictHandler Members

        public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            return ConflictTypeHandled.ScopeInterpreter.IsInScope(conflict.ScopeHint, rule.ApplicabilityScope);
        }

        public ConflictResolutionResult Resolve(IServiceContainer serviceContainer, MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;
            if (rule.ActionRefNameGuid.Equals(new VCChangeGroupInProgressConflictWaitAction().ReferenceName))
            {
                return new ConflictResolutionResult(true, ConflictResolutionType.Retry);
            }
            if (rule.ActionRefNameGuid.Equals(new VCChangeGroupInProgressConflictSuppressAction().ReferenceName))
            {
                ConflictManager conflictManager = (ConflictManager) serviceContainer.GetService( typeof(ConflictManager));
                RemoveInProgressChangeGroupsInSession(conflictManager.ScopeId);
                return new ConflictResolutionResult(true, ConflictResolutionType.SuppressedConflictedChangeGroup);
            }
            else
            {
                return new ConflictResolutionResult(false, ConflictResolutionType.UnknownResolutionAction);
            }
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
                                "Deleting cached data for session '{0}'", sessionId );
                context.DeleteSessionCachedData(sessionId);
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

