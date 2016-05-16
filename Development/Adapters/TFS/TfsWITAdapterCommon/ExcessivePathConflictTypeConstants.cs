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
    public static class ExcessivePathConflictTypeConstants
    {
        private static List<ResolutionAction> m_supportedActions =
            new List<ResolutionAction>();

        static ExcessivePathConflictTypeConstants()
        {
            m_supportedActions.Add(new ManualConflictResolutionAction());
        }

        public static Guid ReferenceName
        {
            get
            {
                return new Guid("1B5BB0E2-5514-4b46-85AD-06960635E04E");
            }
        }

        public const string FriendlyName = "Excessive Path Conflict Type";

        public static ReadOnlyCollection<ResolutionAction> SupportedActions
        {
            get
            {
                return m_supportedActions.AsReadOnly();
            }
        }
    }
}
