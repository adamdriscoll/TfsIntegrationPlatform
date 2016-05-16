// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon
{
    class TfsCheckinFailureConflict : MigrationConflict
    {
        public TfsCheckinFailureConflict(ConflictType conflictType, string changeGroupName, string conflictDetails)
            : base(conflictType,
            Status.Unresolved,
            conflictDetails,
            changeGroupName)
        {
        }
    }
}
