// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon
{
    public class TfsCheckinFailureRetryAction : ResolutionAction
    {
        static TfsCheckinFailureRetryAction()
        {
            m_referenceName = new Guid("2A526676-33B7-46D6-B032-4436C9F7F364");
            m_friendlyName = "Retry the checkin.";
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

    public class TfsCheckinFailureManualResolveAction : ResolutionAction
    {
        public static readonly string MigrationInstructionChangeId = "Synced change group name at migration side";
        public static readonly string DeltaTableChangeId = "Synced change group name at delta table side";

        static TfsCheckinFailureManualResolveAction()
        {
            m_referenceName = new Guid("3AF7FF4A-5BEA-4E21-8F99-08A750A9E9FA");
            m_friendlyName = "Resolve the check-in failure conflicts by manually checking in the changes.";
            m_supportedActionDataKeys = new List<string>(); 
            m_supportedActionDataKeys.Add(MigrationInstructionChangeId);
            m_supportedActionDataKeys.Add(DeltaTableChangeId);

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

