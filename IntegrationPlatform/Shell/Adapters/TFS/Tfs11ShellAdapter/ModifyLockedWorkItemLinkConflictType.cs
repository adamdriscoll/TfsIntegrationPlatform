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
using Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts;

namespace Microsoft.TeamFoundation.Migration.Shell.Tfs11ShellAdapter
{
    public class ModifyLockedWorkItemLinkConflictTypeViewModel : ConflictTypeViewModelBase
    {
        public ModifyLockedWorkItemLinkConflictTypeViewModel()
        {
            ConflictTypeDescription = "Failed to modify links.  The following linked work items have been locked by an administrator.";

            ResolutionActionViewModel forceDeleteAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = "Force to delete the locked link",
                ResolutionActionReferenceName = new TFSModifyLockedWorkItemLinkConflict_ResolveByForceDelete().ReferenceName,
                IsSelected = true
            };
            ResolutionActionViewModel skipAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = "Skip",
                ResolutionActionReferenceName = new SkipConflictedActionResolutionAction().ReferenceName
            };
            ResolutionActionViewModel retryAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = "Retry",
                ResolutionActionReferenceName = new ManualConflictResolutionAction().ReferenceName
            };

            RegisterResolutionAction(forceDeleteAction);
            RegisterResolutionAction(skipAction);
            RegisterResolutionAction(retryAction);
        }
    }
}
