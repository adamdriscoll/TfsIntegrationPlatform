// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts
{
    /// <summary>
    /// ExcessivePathConflictHandler class.
    /// </summary>
    public class ExcessivePathConflictHandler : IConflictHandler
    {
        #region IConflictHandler Members

        /// <summary>
        /// Determines if a conflict can be resolved by a resolution rule.
        /// </summary>
        /// <param name="conflict"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            return ConflictTypeHandled.ScopeInterpreter.IsInScope(conflict.ScopeHint, rule.ApplicabilityScope);
        }

        /// <summary>
        /// Resolve a conflict.
        /// </summary>
        /// <param name="conflict"></param>
        /// <param name="rule"></param>
        /// <param name="actions"></param>
        /// <returns></returns>
        public ConflictResolutionResult Resolve(IServiceContainer serviceContainer, MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;

            if (rule.ActionRefNameGuid.Equals(new ManualConflictResolutionAction().ReferenceName))
            {
                return ManualResolve(conflict, rule, out actions);
            }

            return new ConflictResolutionResult(false, ConflictResolutionType.Other);
        }

        /// <summary>
        /// Gets the conflict type handled by this handler.
        /// </summary>
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
    }
}
