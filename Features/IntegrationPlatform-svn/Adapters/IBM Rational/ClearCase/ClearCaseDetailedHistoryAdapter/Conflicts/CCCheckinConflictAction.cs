// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{
    public class CCCheckinConflictSkipAction : ResolutionAction
    {
        static CCCheckinConflictSkipAction()
        {
            m_referenceName = new Guid("A079C61A-C3FA-4f4e-AE3C-31193D0CCE4A");
            m_friendlyName = CCResources.CCCheckinConflictSkipActionFriendlyName;
            m_supportedActionDataKeys = new List<string>();
        }

        public override Guid ReferenceName
        {
            get
            {
                return m_referenceName;
            }
        }

        public override string FriendlyName
        {
            get
            {
                return m_friendlyName;
            }
        }

        public override ReadOnlyCollection<string> ActionDataKeys
        {
            get
            {
                return m_supportedActionDataKeys.AsReadOnly();
            }
        }

        private static readonly Guid m_referenceName;
        private static readonly string m_friendlyName;
        private static readonly List<string> m_supportedActionDataKeys;
    }
}
