// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement;

namespace Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common
{
    public static class WitGeneralConflictTypeConstants
    {
        private static List<ResolutionAction> m_supportedActions =
            new List<ResolutionAction>();

        static WitGeneralConflictTypeConstants()
        {
            m_supportedActions.Add(new ManualConflictResolutionAction());
            m_supportedActions.Add(new SkipConflictedActionResolutionAction());
        }

        public static Guid ReferenceName
        {
            get
            {
                return new Guid("470F9617-FC96-4166-96EB-44CC2CF73A97");
            }
        }

        public const string FriendlyName = "TFS WIT general conflict type";

        public static ReadOnlyCollection<ResolutionAction> SupportedActions
        {
            get
            {
                return m_supportedActions.AsReadOnly();
            }
        }
    }
}
