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
    public class InsufficientPermissionConflictHandler : IConflictHandler
    {
        #region IConflictHandler Members

        public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            return ConflictTypeHandled.ScopeInterpreter.IsInScope(conflict.ScopeHint, rule.ApplicabilityScope);
        }

        public ConflictType ConflictTypeHandled
        {
            get;
            set;
        }

        public ConflictResolutionResult Resolve(IServiceContainer serviceContainer, MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;

            if (rule.ActionRefNameGuid.Equals(new ManualConflictResolutionAction().ReferenceName))
            {
                return ManualResolve(conflict, rule, out actions);
            }
            else if (rule.ActionRefNameGuid.Equals(new MultipleRetryResolutionAction().ReferenceName))
            {
                return MultipleRetryResolutionAction.TryResolve(rule, conflict);
            }
            else if (rule.ActionRefNameGuid.Equals(new SkipConflictedActionResolutionAction().ReferenceName))
            {
                return SkipConflictedActionResolutionAction.SkipConflict(conflict, true);
            }

            return new ConflictResolutionResult(false, ConflictResolutionType.Other);
        }

        #endregion

        private ConflictResolutionResult ManualResolve(
            MigrationConflict conflict,
            ConflictResolutionRule rule,
            out List<MigrationAction> actions)
        {
            actions = null;
            return new ConflictResolutionResult(true, ConflictResolutionType.Other);
        }
    }
}
