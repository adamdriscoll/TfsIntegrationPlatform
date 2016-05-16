// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.ConflictManagement
{
    public class UseValueMapConflictResolutionAction : ResolutionAction
    {
        public const string ActionDataKey_MapFromValue = "MapFromFieldValue";
        public const string ActionDataKey_MapToValue = "MapToFieldValue";
        public const string ActionDataKey_TargetFieldName = "TargetFieldName";
        List<string> m_actionDataKeys = new List<string>();

        public UseValueMapConflictResolutionAction()
        {
            m_actionDataKeys.Add(ActionDataKey_MapFromValue);
            m_actionDataKeys.Add(ActionDataKey_MapToValue);
            m_actionDataKeys.Add(ActionDataKey_TargetFieldName);
            m_actionDataKeys.Add(Constants.DATAKEY_UPDATED_CONFIGURATION_ID);
        }

        public override System.Collections.ObjectModel.ReadOnlyCollection<string> ActionDataKeys
        {
            get { return m_actionDataKeys.AsReadOnly(); }
        }

        public override string FriendlyName
        {
            get { return ClearQuestResource.ClearQuest_Conflict_Action_UseValueMap; }
        }

        public override Guid ReferenceName
        {
            get { return new Guid("{56242292-2DDA-4136-A924-FAA8DBAAF3F3}"); }
        }
    }
}
