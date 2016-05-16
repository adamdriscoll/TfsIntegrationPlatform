// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.ComponentModel.Design;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    class VCPathNotMappedConflictHandler : IConflictHandler
    {
        #region IConflictHandler Members

        public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            return ConflictTypeHandled.ScopeInterpreter.IsInScope(conflict.ScopeHint, rule.ApplicabilityScope);
        }

        public ConflictResolutionResult Resolve(IServiceContainer serviceContainer, MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;
            if (rule.ActionRefNameGuid.Equals(new VCChangeToAddOnBranchSourceNotMappedAction().ReferenceName))
            {
                return new ConflictResolutionResult(true, ConflictResolutionType.UpdatedConflictedChangeAction);
            }
            else if (rule.ActionRefNameGuid.Equals(new VCAddPathToMappingAction().ReferenceName))
            {
                //Todo, in case of namespace conflict, undo namespace changes. 
                return new ConflictResolutionResult(true, ConflictResolutionType.ChangeMappingInConfiguration);
            }
            else
            {
                //Todo, show UI, allow user to create resolution rules. 
                return new ConflictResolutionResult(false, ConflictResolutionType.UnknownResolutionAction);
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
