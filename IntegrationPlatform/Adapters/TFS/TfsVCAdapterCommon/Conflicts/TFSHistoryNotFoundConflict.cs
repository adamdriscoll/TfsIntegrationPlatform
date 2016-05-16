// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon
{
    class TFSHistoryNotFoundConflict : MigrationConflict
    {
        public TFSHistoryNotFoundConflict(ConflictType conflictType, string changesetId, IMigrationAction conflictAction)
            : base(conflictType,
            Status.Unresolved,
            string.Format("Migration history of changeset {0} cannot be found", changesetId),
            changesetId)
        {
            if (conflictAction != null)
            {
                ConflictedChangeAction = conflictAction;
            }
        }
    }
}
