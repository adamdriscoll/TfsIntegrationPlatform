// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class WITUnmappedWITConflictUpdateWITMappingAction : ResolutionAction
    {
        public static readonly string DATAKEY_MAP_TO = "MapTo";

        static WITUnmappedWITConflictUpdateWITMappingAction()
        {
            s_actionRefName = new Guid("{699AACCD-F0C5-44bd-991D-0AF45E958E54}");
            s_ationDispName = "Resolve unmapped Work Item Type conflict by creating a mapping";

            s_supportedActionDataKeys = new List<string>();
            s_supportedActionDataKeys.Add(DATAKEY_MAP_TO);
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
