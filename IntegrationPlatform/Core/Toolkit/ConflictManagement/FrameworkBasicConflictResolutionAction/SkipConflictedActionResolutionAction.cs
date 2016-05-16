// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement
{
    public class SkipConflictedActionResolutionAction : ResolutionAction
    {
        public override Guid ReferenceName
        {
            get { return new Guid("7EE80C20-B145-4a7e-8729-9ECF93D60B2F"); }
        }

        public override string FriendlyName
        {
            get { return "Skip the conflicted migration action"; }
        }

        public override System.Collections.ObjectModel.ReadOnlyCollection<string> ActionDataKeys
        {
            get { return new List<string>().AsReadOnly(); }
        }

        public static ConflictResolutionResult SkipConflict(MigrationConflict conflict, bool skipEntireChangeGroup)
        {
            Debug.Assert(conflict != null);
            Debug.Assert(conflict.ConflictedChangeAction != null);

            if (skipEntireChangeGroup)
            {
                Debug.Assert(conflict.ConflictedChangeAction.ChangeGroup != null);
                conflict.ConflictedChangeAction.ChangeGroup.Status = ChangeStatus.Skipped;
            }
            conflict.ConflictedChangeAction.State = ActionState.Skipped;
            return new ConflictResolutionResult(true, ConflictResolutionType.SkipConflictedChangeAction);
        }
    }
}
