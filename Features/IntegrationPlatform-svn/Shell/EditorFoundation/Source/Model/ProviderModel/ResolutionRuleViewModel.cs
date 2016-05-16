// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    class ResolutionRuleViewModel : ModelObject
    {
        public string ApplicableScope { get; set; }
        public string Description { get; set; }
        //public bool CanSave { get; }
        //public virtual ConflictResolutionRule NewRule(string applicableScope, string description, Dictionary<string, string> actionData);
    }
}
