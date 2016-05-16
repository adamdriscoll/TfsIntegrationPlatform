// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;
using System.Collections.ObjectModel;

namespace Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common
{
    public static class InvalidSubmissionConflictTypeConstants
    {
        private static List<ResolutionAction> m_supportedActions =
            new List<ResolutionAction>();

        static InvalidSubmissionConflictTypeConstants()
        {
            m_supportedActions.Add(new ManualConflictResolutionAction());
        }

        public static Guid ReferenceName
        {
            get
            {
                return new Guid("C9D80B52-BB8A-4f7b-A40C-F8F63D6FD374");
            }
        }

        public const string FriendlyName = "TFS WIT invalid submission conflict type";

        public static ReadOnlyCollection<ResolutionAction> SupportedActions
        {
            get
            {
                return m_supportedActions.AsReadOnly();
            }
        }
    }
}
