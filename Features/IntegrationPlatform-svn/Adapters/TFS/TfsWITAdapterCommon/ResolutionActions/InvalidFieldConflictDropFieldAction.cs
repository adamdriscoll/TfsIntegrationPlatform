// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common.ResolutionActions
{
    public class InvalidFieldConflictDropFieldAction : ResolutionAction
    {
        public static readonly string DATAKEY_INVALID_FIELD = "InvalidFieldReferenceName";

        static InvalidFieldConflictDropFieldAction()
        {
            s_actionRefName = new Guid("3C8FE19D-3D02-4a19-BC5A-77640B0F5904");
            s_ationDispName = "Resolve invalid field conflict by dropping the field";

            s_supportedActionDataKeys = new List<string>();
            s_supportedActionDataKeys.Add(DATAKEY_INVALID_FIELD);
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
