// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class WITEditEditConflictTakeSourceChangesAction : ResolutionAction
    {
        static WITEditEditConflictTakeSourceChangesAction()
        {
            s_WitEditEditConflictTakeSourceChangeActionRefName = new Guid("3170A3A5-FEF1-4748-BA87-B2D9A78BA62C");
            s_WitEditEditConflictTakeSourceChangeActionDispName = "Resolve WIT Edit/Edit Conflict by always taking source changes";
            s_supportedActionDataKeys = new List<string>(0);
        }

        public override Guid ReferenceName
        {
            get 
            {
                return s_WitEditEditConflictTakeSourceChangeActionRefName;
            }
        }

        public override string FriendlyName
        {
            get 
            {
                return s_WitEditEditConflictTakeSourceChangeActionDispName;
            }
        }

        public override ReadOnlyCollection<string> ActionDataKeys
        {
            get
            {
                return s_supportedActionDataKeys.AsReadOnly();
            }
        }

        private static readonly Guid s_WitEditEditConflictTakeSourceChangeActionRefName;
        private static readonly string s_WitEditEditConflictTakeSourceChangeActionDispName;
        private static readonly List<string> s_supportedActionDataKeys;
    }
}
