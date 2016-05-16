// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;
using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement;

namespace Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common
{
    public static class WorkItemTypeNotExistConflictTypeConstants
    {
        private static List<ResolutionAction> m_supportedActions =
            new List<ResolutionAction>();
        private static List<string> m_supportedConflictDetailsPropertyKeys =
           new List<string>();

        static WorkItemTypeNotExistConflictTypeConstants()
        {
            m_supportedActions.Add(new ManualConflictResolutionAction());
            m_supportedActions.Add(new SkipConflictedActionResolutionAction());

            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_TeamFoundationServer);
            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_TeamProject);
            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_MissingWorkItemType);
        }

        public static Guid ReferenceName
        {
            get
            {
                return new Guid("87F1BAF4-E9DC-4c5b-8E08-68C70AEFB6FB");
            }
        }

        public const string FriendlyName = "TFS Work Item Type not exist conflict type";

        public static ReadOnlyCollection<ResolutionAction> SupportedActions
        {
            get
            {
                return m_supportedActions.AsReadOnly();
            }
        }

        public const string ConflictDetailsKey_TeamFoundationServer = "TeamFoundationServer";
        public const string ConflictDetailsKey_TeamProject = "TeamProject";
        public const string ConflictDetailsKey_MissingWorkItemType = "MissingWorkItemType";

        public static ReadOnlyCollection<string> SupportedConflictDetailsPropertyKeys
        {
            get
            {
                return m_supportedConflictDetailsPropertyKeys.AsReadOnly();
            }
        }
    }
}
