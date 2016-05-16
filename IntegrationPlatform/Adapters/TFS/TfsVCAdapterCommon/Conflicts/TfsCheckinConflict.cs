// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon
{
    class TfsCheckinConflict : MigrationConflict
    {
        public TfsCheckinConflict(ConflictType conflictType, string changeGroupName)
            : base(conflictType,
            Status.Unresolved,
            string.Format("Error occurred during the code review of change group '{0}'. Please check {0}.txt for detail.", changeGroupName),
            changeGroupName)
        {
        }
    }
}
