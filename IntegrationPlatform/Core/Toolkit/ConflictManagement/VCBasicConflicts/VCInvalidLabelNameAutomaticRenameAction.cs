// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /* Removing for now until the conflict handler can handle this for any adaptter via an interface and not just for TFS
    public class VCInvalidLabelNameAutomaticRenameAction : ResolutionAction
    {
        static VCInvalidLabelNameAutomaticRenameAction()
        {
            m_referenceName = new Guid("3DDDDC98-EB45-4c47-822B-A70657806DE8");
            m_friendlyName = "Resolve invalid label name conflict by automatically renaming the label name";
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
     */
}
