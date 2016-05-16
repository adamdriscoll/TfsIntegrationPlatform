// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    class VCBranchParentNotFoundConflict : MigrationConflict
    {
        public VCBranchParentNotFoundConflict(ConflictType conflictType, string path)
            : base(conflictType,
            Status.Unresolved,
            string.Format("The branch parent or merge contributor of '{0}' cannot be found", path),
            path)
        {
        }
    }
}
