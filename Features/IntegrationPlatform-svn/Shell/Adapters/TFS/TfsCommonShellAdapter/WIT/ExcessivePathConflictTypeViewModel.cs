// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter.WIT
{
    public class ExcessivePathConflictTypeViewModel : ConflictTypeViewModelBase
    {
        public ExcessivePathConflictTypeViewModel()
        {
            ConflictTypeDescription = "This conflict is detected when an Area or Iteration Path exists on the server but should not.";

            ResolutionActionViewModel ignoreAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = "Ignore",
                ResolutionActionReferenceName = new ManualConflictResolutionAction().ReferenceName,
                IsSelected = true
            };

            RegisterResolutionAction(ignoreAction);
        }
    }
}
