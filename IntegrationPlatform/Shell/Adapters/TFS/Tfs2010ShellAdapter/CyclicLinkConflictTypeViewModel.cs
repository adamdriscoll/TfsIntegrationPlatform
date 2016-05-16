// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.Tfs2010ShellAdapter
{
    public class CyclicLinkConflictTypeViewModel : ConflictTypeViewModelBase
    {
        public CyclicLinkConflictTypeViewModel()
        {
            ConflictTypeDescription = "AddLink: The specified link type enforces noncircularity in its hierarchy.";

            ResolutionActionViewModel takeTargetAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = "Take target",
                //ResolutionActionReferenceName = new ManualConflictResolutionAction().ReferenceName
            };
            ResolutionActionViewModel takeSourceAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = "Take source",
                //ResolutionActionReferenceName = new ManualConflictResolutionAction().ReferenceName
            };
            ResolutionActionViewModel retryAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = "Retry",
                ResolutionActionReferenceName = new ManualConflictResolutionAction().ReferenceName,
                IsSelected = true
            };

            //RegisterResolutionAction(takeTargetAction);
            //RegisterResolutionAction(takeSourceAction);
            RegisterResolutionAction(retryAction);
        }
    }
}
