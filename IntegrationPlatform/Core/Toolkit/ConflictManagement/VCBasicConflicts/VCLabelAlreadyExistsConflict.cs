// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    class VCLabelAlreadyExistsConflict : MigrationConflict
    {
        public VCLabelAlreadyExistsConflict(ConflictType conflictType, MigrationAction conflictAction, string message)
            : base(conflictType,
            Status.Unresolved,
            message,
            string.Empty)
        {
            ConflictedChangeAction = conflictAction;
        }
    }
}
