// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class VCFilePropertyCreationConflictSkipAction : ResolutionAction
    {
        static VCFilePropertyCreationConflictSkipAction()
        {
            m_referenceName = new Guid("99707036-A05E-49ce-9884-7BF1315E94AB");
            m_friendlyName = MigrationToolkitResources.Conflict_FilePropertyCreation_SkipAction;
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

    public class VCFilePropertyCreationConflictRetryAction : ResolutionAction
    {
        static VCFilePropertyCreationConflictRetryAction()
        {
            m_referenceName = new Guid("52DD0BC5-7FC0-4064-B6C0-31F632FB2AF7");
            m_friendlyName = MigrationToolkitResources.Conflict_FilePropertyCreation_RetryAction;
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
