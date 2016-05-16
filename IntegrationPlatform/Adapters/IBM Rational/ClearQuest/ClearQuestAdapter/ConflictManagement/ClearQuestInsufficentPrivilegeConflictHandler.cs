// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.ConflictManagement
{
    public class ClearQuestInsufficentPrivilegeConflictHandler : ClearQuestConflictHandlerBase
    {
        public override ConflictResolutionResult Resolve(
            IServiceContainer serviceContainer, 
            MigrationConflict conflict, 
            ConflictResolutionRule rule, 
            out List<MigrationAction> actions)
        {
            actions = null;

            if (rule.ActionRefNameGuid.Equals(new MultipleRetryResolutionAction().ReferenceName))
            {
                return MultipleRetryResolutionAction.TryResolve(rule, conflict);
            }
            else
            {
                return base.Resolve(serviceContainer, conflict, rule, out actions);
            }
        }
    }
}
