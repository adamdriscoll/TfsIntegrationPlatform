// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class AutomaticResolutionAction : ResolutionAction
    {
        static AutomaticResolutionAction()
        {
            s_autotResolutionActionRefName = new Guid("CE445810-3F3F-4b1c-97BB-CF230C16FD0E");
            s_autoResolutionActionDispName = "Auto-resolve conflict";
            
            s_supportedActionDataKeys = new List<string>(0);
        }

        public override Guid ReferenceName
        {
            get 
            { 
                return s_autotResolutionActionRefName;
            }
        }

        public override string FriendlyName
        {
            get 
            {
                return s_autoResolutionActionDispName;
            }
        }

        public override System.Collections.ObjectModel.ReadOnlyCollection<string> ActionDataKeys
        {
            get 
            { 
                return s_supportedActionDataKeys.AsReadOnly();    
            }
        }

        private static readonly List<string> s_supportedActionDataKeys;
        private static readonly string s_autoResolutionActionDispName;
        private static readonly Guid s_autotResolutionActionRefName;
    }
}
