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
    /// <summary>
    /// CQ conflict resolution aciton: use value map
    /// </summary>
    public class ClearQuestConflictResolutionUseValueMap : ResolutionAction
    {
        public static readonly string DATAKEY_FIELD_NAME = "Field Name";
        public static readonly string DATAKEY_MAP_FROM = "Field Value (from)";
        public static readonly string DATAKEY_MAP_TO = "Field Value (to)";
        private static readonly List<string> s_supportedActionDataKeys;

        static ClearQuestConflictResolutionUseValueMap()
        {
            s_supportedActionDataKeys = new List<string>();
            s_supportedActionDataKeys.Add(DATAKEY_FIELD_NAME);
            s_supportedActionDataKeys.Add(DATAKEY_MAP_FROM);
            s_supportedActionDataKeys.Add(DATAKEY_MAP_TO);
        }

        public override Guid ReferenceName
        {
            get { return new Guid("{E21DC3F3-D609-4e96-95EE-6852F444A94C}"); }
        }

        public override string FriendlyName
        {
            get { return ClearQuestResource.ClearQuest_Conflict_Action_UseValueMap; }
        }

        public override System.Collections.ObjectModel.ReadOnlyCollection<string> ActionDataKeys
        {
            get { return s_supportedActionDataKeys.AsReadOnly(); }
        }
    }
}
