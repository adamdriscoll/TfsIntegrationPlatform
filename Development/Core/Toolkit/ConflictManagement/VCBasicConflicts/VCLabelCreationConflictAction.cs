// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class VCLabelCreationConflictSkipAction : ResolutionAction
    {
        static VCLabelCreationConflictSkipAction()
        {
            m_referenceName = new Guid("290CB851-1172-41e4-87EF-EAE9958BD3A7");
            m_friendlyName = MigrationToolkitResources.Conflict_LabelCreation_SkipAction;
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

    public class VCLabelCreationConflictRetryAction : ResolutionAction
    {
        static VCLabelCreationConflictRetryAction()
        {
            m_referenceName = new Guid("A3338C41-02FC-4b43-8C8D-BE85F68ED0CA");
            m_friendlyName = MigrationToolkitResources.Conflict_LabelCreation_RetryAction;
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
