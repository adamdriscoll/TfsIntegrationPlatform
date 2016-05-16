// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement.WITBasicConflicts
{
    public class WITUnmappedWITConflictExcludeWITInSessionFilter : ResolutionAction
    {
        static WITUnmappedWITConflictExcludeWITInSessionFilter()
        {
            s_actionRefName = new Guid("{21F5A959-CBBE-40a2-84FE-3DCF58917392}");
            s_ationDispName = "Resolve unmapped Work Item Type conflict by excluding work items of the unmapped type in the session filter";

            s_supportedActionDataKeys = new List<string>();
            s_supportedActionDataKeys.Add(Constants.DATAKEY_UPDATED_CONFIGURATION_ID);
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
                return s_ationDispName;
            }
        }

        public override System.Collections.ObjectModel.ReadOnlyCollection<string> ActionDataKeys
        {
            get 
            {
                return s_supportedActionDataKeys.AsReadOnly();
            }
        }
        
        private static readonly Guid s_actionRefName;
        private static readonly string s_ationDispName;
        private static readonly List<string> s_supportedActionDataKeys;
    }
}
