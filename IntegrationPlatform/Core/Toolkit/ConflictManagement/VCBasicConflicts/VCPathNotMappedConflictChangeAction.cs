// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class VCChangeToAddOnBranchSourceNotMappedAction : ResolutionAction
    {
        static VCChangeToAddOnBranchSourceNotMappedAction()
        {
            m_referenceName = new Guid("049599CC-C729-4679-B605-6DB600D2F691");
            m_friendlyName = "Resolve source not mapped conflict by changing to 'Add' for 'Branch', by skipping for 'Merge' and by changing to 'Add' for 'Rename'. ";
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

    public class VCAddPathToMappingAction : ResolutionAction
    {
        static VCAddPathToMappingAction()
        {
            m_referenceName = new Guid("62C343CD-598F-4e72-B249-489FD1A32A58");
            m_friendlyName = "Resolve path not mapped conflict add the path or its parent path to mapping.";
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
