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
    public class DropFieldConflictResolutionAction : ResolutionAction
    {
        public const string ActionDataKey_FieldName = "FieldToDrop";
        List<string> m_actionDataKeys = new List<string>();

        public DropFieldConflictResolutionAction()
        {
            m_actionDataKeys.Add(ActionDataKey_FieldName);
            m_actionDataKeys.Add(Constants.DATAKEY_UPDATED_CONFIGURATION_ID);
        }

        public override System.Collections.ObjectModel.ReadOnlyCollection<string> ActionDataKeys
        {
            get { return m_actionDataKeys.AsReadOnly(); }
        }

        public override string FriendlyName
        {
            get { return ClearQuestResource.ClearQuest_Conflict_Action_DropField; }
        }

        public override Guid ReferenceName
        {
            get { return new Guid("{835455F2-E63F-4EA8-8437-E3513A54B873}"); }
        }
    }
}
