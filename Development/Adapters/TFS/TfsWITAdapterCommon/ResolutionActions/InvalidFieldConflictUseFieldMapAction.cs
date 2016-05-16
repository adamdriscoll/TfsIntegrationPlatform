// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common.ResolutionActions
{
    public class InvalidFieldConflictUseFieldMapAction : ResolutionAction
    {
        public static readonly string DATAKEY_MAP_FROM = "MapFrom";
        public static readonly string DATAKEY_MAP_TO = "MapTo";

        static InvalidFieldConflictUseFieldMapAction()
        {
            s_actionRefName = new Guid("FE028FAC-6DD8-400a-B8EE-26CF63F8AAEE");
            s_ationDispName = "Resolve invalid field conflict by using field mapping";

            s_supportedActionDataKeys = new List<string>();
            s_supportedActionDataKeys.Add(DATAKEY_MAP_FROM);
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
