// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon
{
    public class TFSHistoryNotFoundSuppressAction : ResolutionAction
    {
        public static readonly string SupressChangeSetId = "SupressChangeSetId";
        static TFSHistoryNotFoundSuppressAction()
        {
            m_referenceName = new Guid("A0285CEB-BEA1-426d-BFB1-A3814A64B1C6");
            m_friendlyName = "Suppress the history with the given changeset Id.";
            m_supportedActionDataKeys = new List<string>();
            m_supportedActionDataKeys.Add(SupressChangeSetId);
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

    public class TFSHistoryNotFoundSkipAction : ResolutionAction
    {
        static TFSHistoryNotFoundSkipAction()
        {
            m_referenceName = new Guid("2B510A2A-BA08-4bcd-891D-07186E352C0D");
            m_friendlyName = "Skip the history query. So the 'Branch' will be checked in as 'Add'.";
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
