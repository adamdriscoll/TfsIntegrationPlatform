// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Conflict Class
    /// </summary>
    public class VCNameSpaceContentConflict : MigrationConflict
    {
        public VCNameSpaceContentConflict(ConflictType conflictType, MigrationAction conflictAction)
            : base(conflictType,
            Status.Unresolved,
            string.Format("{0}", conflictAction.Path),
            string.Format("{0};{1}", conflictAction.Path, conflictAction.ChangeGroup.Name))
        {
            ConflictedChangeAction = conflictAction;
        }
    }
}