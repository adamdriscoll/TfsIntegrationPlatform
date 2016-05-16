// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{
    public class CCAttrTypeNotFoundSkipAction : ResolutionAction
    {
        static CCAttrTypeNotFoundSkipAction()
        {
            m_referenceName = new Guid("694352AB-E843-4678-BBAF-DA4AC26EED96");
            m_friendlyName = CCResources.CCAttrTypeNotFoundSkipActionFriendlyName;
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

    public class CCAttrTypeNotFoundRetryAction : ResolutionAction
    {
        static CCAttrTypeNotFoundRetryAction()
        {
            m_referenceName = new Guid("E94C4D34-4D61-4a6f-90AF-77A3C4970ECE");
            m_friendlyName = CCResources.CCAttrTypeNotFoundRetryActionFriendlyName;
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
