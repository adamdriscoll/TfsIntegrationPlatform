// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class WITEditEditConflictTakeTargetChangesAction : ResolutionAction
    {
        static WITEditEditConflictTakeTargetChangesAction()
        {
            s_WITEditEditConflictTakeTargetChangesActionRefName = new Guid("A82D5B2E-8D52-4f83-A195-BE6BD27F660C");
            s_WITEditEditConflictTakeTargetChangesActionDispName = "Resolve WIT Edit/Edit Conflict by always taking target changes";
            s_supportedActionDataKeys = new List<string>(0);
        }

        public override Guid ReferenceName
        {
            get 
            {
                return s_WITEditEditConflictTakeTargetChangesActionRefName;
            }
        }

        public override string FriendlyName
        {
            get 
            {
                return s_WITEditEditConflictTakeTargetChangesActionDispName;
            }
        }

        public override ReadOnlyCollection<string> ActionDataKeys
        {
            get
            {
                return s_supportedActionDataKeys.AsReadOnly();
            }
        }

        private static readonly Guid s_WITEditEditConflictTakeTargetChangesActionRefName;
        private static readonly string s_WITEditEditConflictTakeTargetChangesActionDispName;
        private static readonly List<string> s_supportedActionDataKeys;
    }
}
