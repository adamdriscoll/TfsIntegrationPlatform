// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    class VCInvalidLabelNameConflict : MigrationConflict
    {
        public VCInvalidLabelNameConflict(ConflictType conflictType, MigrationAction conflictAction, string message)
            : base(conflictType,
            Status.Unresolved,
            message,
            string.Empty)
        {
            ConflictedChangeAction = conflictAction;
        }
    }
}
