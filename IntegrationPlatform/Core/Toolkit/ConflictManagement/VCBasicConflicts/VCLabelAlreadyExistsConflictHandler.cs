// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.ComponentModel.Design;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    class VCLabelAlreadyExistsConflictHandler : IConflictHandler
    {
        #region IConflictHandler Members

        public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            if (rule.ActionRefNameGuid.Equals(new VCLabelConflictManualRenameAction().ReferenceName))
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

            if (rule.ActionRefNameGuid.Equals(new VCLabelConflictManualRenameAction().ReferenceName))
            {
                conflict.ConflictedChangeAction.ChangeGroup.Name = rule.DataFieldDictionary[VCLabelConflictManualRenameAction.DATAKEY_RENAME_LABEL];
                return new ConflictResolutionResult(true, ConflictResolutionType.UpdatedConflictedChangeAction);
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
