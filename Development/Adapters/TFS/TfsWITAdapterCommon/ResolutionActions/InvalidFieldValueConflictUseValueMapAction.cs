// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common.ResolutionActions
{
    public class InvalidFieldValueConflictUseValueMapAction : ResolutionAction
    {
        public static readonly string DATAKEY_MAP_FROM = "MapFrom";
        public static readonly string DATAKEY_MAP_TO = "MapTo";

        static InvalidFieldValueConflictUseValueMapAction()
        {
            s_InvalidFieldValueConflictUseValueMapActionRefName = new Guid("F3AFE975-4111-43dd-A7FC-B6FC0E0E738B");
            s_InvalidFieldValueConflictUseValueMapActionDispName = "Resolve invalid field value conflict by updating the value mapping in the configuration";
            
            s_supportedActionDataKeys = new List<string>();
            s_supportedActionDataKeys.Add(DATAKEY_MAP_FROM);
            s_supportedActionDataKeys.Add(DATAKEY_MAP_TO);
            s_supportedActionDataKeys.Add(Constants.DATAKEY_UPDATED_CONFIGURATION_ID);
        }

        public InvalidFieldValueConflictUseValueMapAction()
        {
        }

        public override Guid ReferenceName
        {
            get 
            {
                return s_InvalidFieldValueConflictUseValueMapActionRefName;
            }
        }

        public override string FriendlyName
        {
            get 
            {
                return s_InvalidFieldValueConflictUseValueMapActionDispName;
            }
        }

        public override ReadOnlyCollection<string> ActionDataKeys
        {
            get
            {
                return s_supportedActionDataKeys.AsReadOnly();
            }
        }

        private static readonly Guid s_InvalidFieldValueConflictUseValueMapActionRefName;
        private static readonly string s_InvalidFieldValueConflictUseValueMapActionDispName;
        private static readonly List<string> s_supportedActionDataKeys;
    }
}
