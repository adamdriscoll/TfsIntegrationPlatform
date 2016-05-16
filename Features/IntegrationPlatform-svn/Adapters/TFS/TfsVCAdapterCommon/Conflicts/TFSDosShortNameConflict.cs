// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon
{
    class TFSDosShortNameConflict : MigrationConflict
    {
        public TFSDosShortNameConflict(ConflictType conflictType, string message, string changeGroupName)
            : base(conflictType,
            Status.Unresolved,
            message,
            changeGroupName)
        {
        }
    }
}
