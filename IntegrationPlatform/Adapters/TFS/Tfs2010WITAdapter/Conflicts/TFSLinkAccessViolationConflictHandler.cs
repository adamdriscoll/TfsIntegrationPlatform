// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts
{
    public class TFSLinkAccessViolationConflictHandler : IConflictHandler
    {
        #region IConflictHandler Members

        public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            return ConflictTypeHandled.ScopeInterpreter.IsInScope(conflict.ScopeHint, rule.ApplicabilityScope);
        }

        public ConflictResolutionResult Resolve(System.ComponentModel.Design.IServiceContainer serviceContainer, MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;

            if (rule.ActionRefNameGuid.Equals(new ManualConflictResolutionAction().ReferenceName))
            {
                return ManualResolve(conflict, rule, out actions);
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

        private ConflictResolutionResult ManualResolve(
            MigrationConflict conflict,
            ConflictResolutionRule rule,
            out List<MigrationAction> actions)
        {
            actions = null;
            return new ConflictResolutionResult(true, ConflictResolutionType.Other);
        }

        private ConflictResolutionResult ForceDelete(
            MigrationConflict conflict,
            ConflictResolutionRule rule,
            out List<MigrationAction> actions)
        {
            actions = null;
            conflict.ConflictedLinkChangeAction.Link.IsLocked = false;
            conflict.ConflictedLinkChangeAction.Group.AddChangeAction(
                new Microsoft.TeamFoundation.Migration.Toolkit.Linking.LinkChangeAction(
                    WellKnownChangeActionId.Edit,
                    conflict.ConflictedLinkChangeAction.Link,
                    Microsoft.TeamFoundation.Migration.Toolkit.Linking.LinkChangeAction.LinkChangeActionStatus.ReadyForMigration, false));
            return new ConflictResolutionResult(true, ConflictResolutionType.CreatedUnlockLinkChangeActions);
        }

        private ConflictResolutionResult SkipConflictedAction(
            MigrationConflict conflict,
            ConflictResolutionRule rule,
            out List<MigrationAction> actions)
        {
            actions = null;
            conflict.ConflictedLinkChangeAction.Status = LinkChangeAction.LinkChangeActionStatus.Skipped;
            return new ConflictResolutionResult(true, ConflictResolutionType.UpdatedConflictedLinkChangeAction);
        }
    }
}
