// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class VCChangeToAddOnBranchParentNotFoundAction : ResolutionAction
    {
        static VCChangeToAddOnBranchParentNotFoundAction()
        {
            m_referenceName = new Guid("456BC775-755E-45da-B9F5-AE346666458C");
            m_friendlyName = "Resolve branch parent not found conflict by changing to 'Add' for 'Branch', by skipping for 'Merge' and by changing to 'Add' for 'Rename'. ";
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

    public class VCRetryOnBranchParentNotFoundAction : ResolutionAction
    {
        static VCRetryOnBranchParentNotFoundAction()
        {
            m_referenceName = new Guid("CF76EFA5-4226-437d-94D8-12589738D887");
            m_friendlyName = "Resolve branch parent not found conflict by retry.";
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
