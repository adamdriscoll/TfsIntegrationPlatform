// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2010VCAdapter
{
    class UnhandledChangeTypeConflict : MigrationConflict
    {
        public UnhandledChangeTypeConflict(ConflictType conflictType, string changeType)
            : base(conflictType,
            Status.Unresolved,
            string.Format("ChangeType '{0}' is unrecognized.", changeType),
            changeType)
        {
        }
    }
}
