// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)


namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    class VCInvalidPathConflict : MigrationConflict
    {
        public VCInvalidPathConflict(ConflictType conflictType, MigrationAction conflictAction, string message, string path)
            : base(conflictType,
            Status.Unresolved,
            message,
            path)
        {
            ConflictedChangeAction = conflictAction;
        }
    }
}
