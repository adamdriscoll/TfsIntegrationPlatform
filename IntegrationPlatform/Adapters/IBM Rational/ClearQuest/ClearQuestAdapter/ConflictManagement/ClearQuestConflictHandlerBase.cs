// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.ConflictManagement
{
    public class ClearQuestConflictHandlerBase : IConflictHandler
    {
        #region IConflictHandler Members

        public virtual bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            return ConflictTypeHandled.ScopeInterpreter.IsInScope(conflict.ScopeHint, rule.ApplicabilityScope);
        }

        public virtual ConflictResolutionResult Resolve(IServiceContainer serviceContainer, MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;

            if (rule.ActionRefNameGuid.Equals(new ManualConflictResolutionAction().ReferenceName))
            {
                return ManualResolve(conflict, rule, out actions);
            }

            return new ConflictResolutionResult(false, ConflictResolutionType.Other);
        }

        public virtual ConflictType ConflictTypeHandled
        {
            get;
            set;
        }

        #endregion

        protected ConflictResolutionResult ManualResolve(
            MigrationConflict conflict,
            ConflictResolutionRule rule,
            out List<MigrationAction> actions)
        {
            actions = null;
            return new ConflictResolutionResult(true, ConflictResolutionType.Other);
        }
    }
}
