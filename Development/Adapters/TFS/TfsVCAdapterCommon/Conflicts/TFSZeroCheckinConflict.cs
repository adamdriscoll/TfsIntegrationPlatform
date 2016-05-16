// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon
{
    class TFSZeroCheckinConflict : MigrationConflict
    {
        public TFSZeroCheckinConflict(ConflictType conflictType, string changeGroupName)
            : base(conflictType,
            Status.Unresolved,
            string.Format("All changes from change group '{0}' were ignored by the server", changeGroupName),
            changeGroupName)
        {
        }
    }
}
