// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// This resolution action resolves a conflict by updating the session group configuration.
    /// </summary>
    public class UpdatedConfigurationResolutionAction : ResolutionAction
    {
        static UpdatedConfigurationResolutionAction()
        {
            s_actionRefName = new Guid("F8B917C3-FE9A-4e60-88BE-9AB2E9F700D0");
            s_dispName = "Resolve the conflict by updating the configuration";
            
            s_supportedActionDataKeys = new List<string>();
            s_supportedActionDataKeys.Add(Constants.DATAKEY_UPDATED_CONFIGURATION_ID);
        }

        public UpdatedConfigurationResolutionAction()
        {
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
                return s_dispName;
            }
        }

        public override ReadOnlyCollection<string> ActionDataKeys
        {
            get
            {
                return s_supportedActionDataKeys.AsReadOnly();
            }
        }

        private static readonly Guid s_actionRefName;
        private static readonly string s_dispName;
        private static readonly List<string> s_supportedActionDataKeys;
    }
}
