// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Conflict Class
    /// </summary>
    public class VCContentConflict : MigrationConflict
    {
        public VCContentConflict(ConflictType conflictType, MigrationAction conflictAction, MigrationAction otherSideConflictAction)
            : base(conflictType, 
            Status.Unresolved,
            otherSideConflictAction.ActionId.ToString(), 
            string.Format("{0};{1}",conflictAction.Path, conflictAction.ChangeGroup.Name))
        {
            ConflictedChangeAction = conflictAction;
        }

        public VCContentConflict(ConflictType conflictType, ChangeGroup changeGroup, string conflictDetails, string actionPath)
            : base(conflictType,
            Status.Unresolved,
            conflictDetails,
            string.Format("{0};{1}", actionPath, changeGroup.Name))
        {
        }
    }
}