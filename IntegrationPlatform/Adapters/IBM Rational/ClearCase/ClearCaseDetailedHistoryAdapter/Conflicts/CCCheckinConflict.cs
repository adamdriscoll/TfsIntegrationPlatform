// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{
    class CCCheckinConflict : MigrationConflict
    {
        public CCCheckinConflict(ConflictType conflictType, string message, string changeGroupId)
            : base(conflictType,
            Status.Unresolved,
            message,
            changeGroupId)
        {
        }
    }
}
