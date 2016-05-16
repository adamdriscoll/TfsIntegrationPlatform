// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.ComponentModel.Design;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal class ChainOnConflictConflictHandler : IConflictHandler
    {
        #region IConflictHandler Members

        public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            return true;
        }

        public ConflictResolutionResult Resolve(IServiceContainer serviceContainer, MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;
            return new ConflictResolutionResult(true, ConflictResolutionType.Other);
        }

        public ConflictType ConflictTypeHandled
        {
            get;
            set;
        }

        #endregion
    }
}
