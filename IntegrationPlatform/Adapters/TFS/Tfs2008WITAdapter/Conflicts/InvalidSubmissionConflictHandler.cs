// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter.Conflicts
{
    /// <summary>
    /// This handler resolves the TFS WIT invalid revision submission conflicts
    /// </summary>
    public class InvalidSubmissionConflictHandler : IConflictHandler
    {
        #region IConflictHandler Members

        public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            return ConflictTypeHandled.ScopeInterpreter.IsInScope(conflict.ScopeHint, rule.ApplicabilityScope);
        }

        public ConflictResolutionResult Resolve(IServiceContainer serviceContainer, MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;

            if (rule.ActionRefNameGuid.Equals(new ManualConflictResolutionAction().ReferenceName))
            {
                return new ConflictResolutionResult(true, ConflictResolutionType.Other);
            }
            else if (rule.ActionRefNameGuid.Equals(new SkipConflictedActionResolutionAction().ReferenceName))
            {
                return SkipConflictedAction(conflict, rule, out actions);
            }

            return new ConflictResolutionResult(false, ConflictResolutionType.Other);
        }

        public ConflictType ConflictTypeHandled
        {
            get;
            set;
        }

        #endregion

        #region private methods
        private ConflictResolutionResult SkipConflictedAction(
            MigrationConflict conflict,
            ConflictResolutionRule rule,
            out List<MigrationAction> actions)
        {
            actions = null;

            return SkipConflictedActionResolutionAction.SkipConflict(conflict, true);
        }
        #endregion
    }
}
