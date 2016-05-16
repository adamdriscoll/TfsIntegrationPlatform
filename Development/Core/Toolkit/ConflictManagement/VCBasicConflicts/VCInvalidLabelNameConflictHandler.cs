// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.ComponentModel.Design;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    class VCInvalidLabelNameConflictHandler : IConflictHandler
    {
        #region IConflictHandler Members

        public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            //if (rule.ActionRefNameGuid.Equals(new VCInvalidLabelNameAutomaticRenameAction().ReferenceName) ||
            //    rule.ActionRefNameGuid.Equals(new VCLabelConflictManualRenameAction().ReferenceName))
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
            /* Removing for now until the conflict handler can handle this for any adaptter via an interface and not just for TFS
            if (rule.ActionRefNameGuid.Equals(new VCInvalidLabelNameAutomaticRenameAction().ReferenceName))
            {
                conflict.ConflictedChangeAction.ChangeGroup.Name = FixupTfsLabelName(conflict.ConflictedChangeAction.ChangeGroup.Name);
                actions = new List<MigrationAction>();
                return new ConflictResolutionResult(true, ConflictResolutionType.UpdatedConflictedChangeAction);
            }
            else
             */
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
