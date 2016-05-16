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
    public class VCChangeGroupInProgressConflictWaitAction : ResolutionAction
    {
        static VCChangeGroupInProgressConflictWaitAction()
        {
            m_referenceName = new Guid("D874D18D-FDAB-4D7E-938A-3715E83A6023");
            m_friendlyName = "Resolve the conflict by waiting for the in-progress change group to finish.";
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

    public class VCChangeGroupInProgressConflictSuppressAction : ResolutionAction
    {
        static VCChangeGroupInProgressConflictSuppressAction()
        {
            m_referenceName = new Guid("B8BDF2D9-0DBA-40ED-82DE-3CC45DB25217");
            m_friendlyName = "Resolve the conflict by removing the in-progress change groups.";
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

