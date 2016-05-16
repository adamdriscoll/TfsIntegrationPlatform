// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2008VCAdapter
{
    class UnhandledChangeTypeSkipAction : ResolutionAction
    {
        static UnhandledChangeTypeSkipAction()
        {
            m_referenceName = new Guid("F4932306-558E-4a09-9363-3B95A81D63E1");
            m_friendlyName = "Skip the conflict.";
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

    class UnhandledChangeTypeMapAction : ResolutionAction
    {
        static UnhandledChangeTypeMapAction()
        {
            m_referenceName = new Guid("B09B566A-CE78-4256-B3C5-5FE9BE4576EF");
            m_friendlyName = "Map to an existing ChangeType.";
            m_supportedActionDataKeys = new List<string>();
            m_supportedActionDataKeys.Add("MapTo");
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

