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
    public class ClearQuestConflictResolutionUseRegexValueReplacement : ResolutionAction
    {
        public static readonly string DATAKEY_FIELD_NAME = "Applicable Field Name";
        public static readonly string DATAKEY_REGEX_PATTERN = "Regex Match Pattern";
        public static readonly string DATAKEY_REPLACEMENT = "Value Replacement";
        private static readonly List<string> s_supportedActionDataKeys;

        static ClearQuestConflictResolutionUseRegexValueReplacement()
        {
            s_supportedActionDataKeys = new List<string>();
            s_supportedActionDataKeys.Add(DATAKEY_FIELD_NAME);
            s_supportedActionDataKeys.Add(DATAKEY_REGEX_PATTERN);
            s_supportedActionDataKeys.Add(DATAKEY_REPLACEMENT);
        }

        public override Guid ReferenceName
        {
            get { return new Guid("{9ADE41CD-CA43-4176-B5E5-F8A82A174E91}"); }
        }

        public override string FriendlyName
        {
            get { return ClearQuestResource.ClearQuest_Conflict_Action_RegexValueReplacement; }
        }

        public override System.Collections.ObjectModel.ReadOnlyCollection<string> ActionDataKeys
        {
            get { return s_supportedActionDataKeys.AsReadOnly(); }
        }
    }
}
