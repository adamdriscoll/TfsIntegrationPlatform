// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class WITEditEditConflictIgnoreByFieldChangeAction : ResolutionAction
    {
        static WITEditEditConflictIgnoreByFieldChangeAction()
        {
            s_WitEditEditConflictTakeSourceChangeActionRefName = new Guid("C8B8AEAC-DCA1-49e9-992D-BD99092B015A");
            s_WitEditEditConflictTakeSourceChangeActionDispName = 
                "Resolve WIT Edit/Edit Conflict by ignoring the conflict if the conflicted revisions edit different fields.";
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
