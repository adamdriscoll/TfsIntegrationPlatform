// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class ManualConflictResolutionAction : ResolutionAction
    {
        //public static readonly string DATAKEY_CREATE_OTHER = ConflictResolutionType.Other.ToString();
        //public static readonly string DATAKEY_CREATE_SUPPRESS_CONFLICTED_ACTION = ConflictResolutionType.SuppressedConflictedChangeAction.ToString();

        static ManualConflictResolutionAction()
        {
            s_manualConflictResolutionActionRefName = new Guid("CAB359A9-5095-4fdc-8D43-0B2245E51851");
            s_manualConflictResolutionActionDispName = "Retry";
            
            s_supportedActionDataKeys = new List<string>(0);
            //s_supportedActionDataKeys.Add(DATAKEY_CREATE_OTHER);
            //s_supportedActionDataKeys.Add(DATAKEY_CREATE_SUPPRESS_CONFLICTED_ACTION);
        }

        public override Guid ReferenceName
        {
            get 
            { 
                return s_manualConflictResolutionActionRefName;
            }
        }

        public override string FriendlyName
        {
            get 
            {
                return s_manualConflictResolutionActionDispName;
            }
        }

        public override System.Collections.ObjectModel.ReadOnlyCollection<string> ActionDataKeys
        {
            get 
            { 
                return s_supportedActionDataKeys.AsReadOnly();    
            }
        }

        private static readonly Guid s_manualConflictResolutionActionRefName;
        private static readonly string s_manualConflictResolutionActionDispName;
        private static readonly List<string> s_supportedActionDataKeys;
    }
}
