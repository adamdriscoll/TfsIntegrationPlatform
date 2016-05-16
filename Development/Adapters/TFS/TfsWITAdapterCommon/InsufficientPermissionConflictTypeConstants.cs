// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common
{
    public static class InsufficientPermissionConflictTypeConstants
    {
        private static List<ResolutionAction> m_supportedActions =
            new List<ResolutionAction>();
        private static List<string> m_supportedConflictDetailsPropertyKeys =
            new List<string>(4);

        static InsufficientPermissionConflictTypeConstants()
        {
            m_supportedActions.Add(new MultipleRetryResolutionAction());
            m_supportedActions.Add(new ManualConflictResolutionAction());

            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_UserAlias);
            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_UserDomain);
            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_PermissionGroupName);
            m_supportedConflictDetailsPropertyKeys.Add(Constants.ConflictDetailsKey_MigrationSourceId);
        }

        public static Guid ReferenceName
        {
            get
            {
                return new Guid("CBF0B9F3-9A1B-4deb-ADD3-7FEC98604118");
            }
        }

        public const string FriendlyName = "TFS WIT insufficient permission conflict type";

        public static ReadOnlyCollection<ResolutionAction> SupportedActions
        {
            get
            {
                return m_supportedActions.AsReadOnly();
            }
        }

        public const string ConflictDetailsKey_UserAlias = "UserAlias";
        public const string ConflictDetailsKey_UserDomain = "UserDomain";
        public const string ConflictDetailsKey_PermissionGroupName = "PermissionGroupName";

        public static ReadOnlyCollection<string> SupportedConflictDetailsPropertyKeys
        {
            get
            {
                return m_supportedConflictDetailsPropertyKeys.AsReadOnly();
            }
        }
    }
}
