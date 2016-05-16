// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.ComponentModel.Design;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    class VCFilePropertyCreationConflictHandler : IConflictHandler
    {
        #region IConflictHandler Members

        public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            if (rule.ActionRefNameGuid.Equals(new VCFilePropertyCreationConflictSkipAction().ReferenceName) ||
                rule.ActionRefNameGuid.Equals(new VCFilePropertyCreationConflictRetryAction().ReferenceName))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public ConflictResolutionResult Resolve(IServiceContainer serviceContainer, MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;

            if (rule.ActionRefNameGuid.Equals(new VCFilePropertyCreationConflictSkipAction().ReferenceName))
            {
                return new ConflictResolutionResult(true, ConflictResolutionType.SkipConflictedChangeAction);
            }
            else if (rule.ActionRefNameGuid.Equals(new VCFilePropertyCreationConflictRetryAction().ReferenceName))
            {
                return new ConflictResolutionResult(true, ConflictResolutionType.Retry);
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

        #endregion

    }
}
