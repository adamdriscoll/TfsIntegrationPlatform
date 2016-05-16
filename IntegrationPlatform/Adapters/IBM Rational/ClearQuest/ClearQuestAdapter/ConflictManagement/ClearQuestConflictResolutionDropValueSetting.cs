// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.ConflictManagement
{
    public class ClearQuestConflictResolutionDropValueSetting : ResolutionAction
    {
        public static readonly string DATAKEY_DROP_FIELD = "Field Name";

        private static readonly List<string> s_supportedActionDataKeys;

        static ClearQuestConflictResolutionDropValueSetting()
        {
            s_supportedActionDataKeys = new List<string>();
            s_supportedActionDataKeys.Add(DATAKEY_DROP_FIELD);
        }

        public override Guid ReferenceName
        {
            get { return new Guid("{A6E02000-2432-41d9-A9FE-A4CE23D4E8B8}"); }
        }

        public override string FriendlyName
        {
            get { return ClearQuestResource.ClearQuest_Conflict_Action_DropValueSetting; }
        }

        public override System.Collections.ObjectModel.ReadOnlyCollection<string> ActionDataKeys
        {
            get { return s_supportedActionDataKeys.AsReadOnly(); }
        }
    }
}
