// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement;

namespace Microsoft.TeamFoundation.Migration.Shell.Tfs11ShellAdapter
{
    public class LinkAccessViolationConflictTypeViewModel : ConflictTypeViewModelBase
    {
        public LinkAccessViolationConflictTypeViewModel()
        {
            ConflictTypeDescription = "Link does not exist or access is denied.";

            ResolutionActionViewModel skipAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = "Skip",
                ResolutionActionReferenceName = new SkipConflictedActionResolutionAction().ReferenceName,
                IsSelected = true
            };
            ResolutionActionViewModel retryAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = "Retry",
                ResolutionActionReferenceName = new ManualConflictResolutionAction().ReferenceName
            };

            RegisterResolutionAction(skipAction);
            RegisterResolutionAction(retryAction);
        }
    }
}
