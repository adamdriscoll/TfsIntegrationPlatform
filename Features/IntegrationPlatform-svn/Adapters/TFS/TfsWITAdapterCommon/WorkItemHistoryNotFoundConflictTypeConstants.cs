// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common.ResolutionActions;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common
{
    public static class WorkItemHistoryNotFoundConflictTypeConstants
    {
        private static List<ResolutionAction> m_supportedActions =
            new List<ResolutionAction>();

        static WorkItemHistoryNotFoundConflictTypeConstants()
        {
            m_supportedActions.Add(new HistoryNotFoundSubmitMissingChangesAction());
            m_supportedActions.Add(new HistoryNotFoundUpdateConversionHistoryAction());
        }

        public static Guid ReferenceName
        {
            get
            {
                return new Guid("1722DF87-AB61-4ad0-8B41-531D3D804089");
            }
        }

        public const string FriendlyName = "TFS WIT history not found conflict type";

        public static ReadOnlyCollection<ResolutionAction> SupportedActions
        {
            get
            {
                return m_supportedActions.AsReadOnly();
            }
        }
    }
}
