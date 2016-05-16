// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    class VCPathNotMappedConflict : MigrationConflict
    {
        public VCPathNotMappedConflict(ConflictType conflictType, string path)
            : base(conflictType,
            Status.Unresolved,
            string.Format("'{0}' to be migrated is not mapped", path),
            path)
        {
        }
    }
}
