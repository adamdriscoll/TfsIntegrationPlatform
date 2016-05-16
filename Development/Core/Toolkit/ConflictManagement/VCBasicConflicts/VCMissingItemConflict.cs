// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    class VCMissingItemConflict : MigrationConflict
    {
        public VCMissingItemConflict(ConflictType conflictType, string path)
            : base(conflictType,
            Status.Unresolved,
            string.Format("The item '{0}' does not exist on the system.", path),
            path)
        {
        }
    }
}