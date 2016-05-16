// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement
{
    public class CyclicLinkReferenceConflictDropLinkFromSource : ResolutionAction
    {
        private static readonly Guid s_actionRefName;
        private static readonly string s_actionDispName;
        private static readonly List<string> s_supportedActionDataKeys;

        static CyclicLinkReferenceConflictDropLinkFromSource()
        {
            s_actionRefName = new Guid("{5437B57A-0B9B-45bf-9CFE-858CA17018CF}");
            s_actionDispName = "Resolve cyclic link reference by droping source-side link change";
            s_supportedActionDataKeys = new List<string>(0);
        }

        public override Guid ReferenceName
        {
            get
            {
                return s_actionRefName;
            }
        }

        public override string FriendlyName
        {
            get
            {
                return s_actionDispName;
            }
        }

        public override ReadOnlyCollection<string> ActionDataKeys
        {
            get
            {
                return s_supportedActionDataKeys.AsReadOnly();
            }
        }
    }
}
