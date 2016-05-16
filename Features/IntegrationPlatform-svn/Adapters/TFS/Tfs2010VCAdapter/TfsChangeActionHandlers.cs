// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.VersionControl.Common;
using Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon;

namespace Microsoft.TeamFoundation.Migration.Tfs2010VCAdapter
{
    internal class TfsChangeActionHandlers : ChangeActionHandlers
    {
        internal TfsChangeActionHandlers(IAnalysisProvider analysisProvider)
            :base(analysisProvider)
        {
        }

        public override void BasicActionHandler(MigrationAction action, ChangeGroup group)
        {
            try
            {
                VersionControlPath.GetFullPath(action.Path, true);
                if (!string.IsNullOrEmpty(action.FromPath))
                {
                    VersionControlPath.GetFullPath(action.FromPath, true);
                }
            }
            catch (Exception e)
            {
                MigrationConflict pathTooLongConflict = VCInvalidPathConflictType.CreateConflict(action, e.Message, action.Path);

                List<MigrationAction> returnActions;
                ConflictResolutionResult resolutionResult = ((TfsVCAnalysisProvider)AnalysisProvider).ConflictManager.TryResolveNewConflict(
                    group.SourceId, pathTooLongConflict, out returnActions);
                if (resolutionResult.Resolved)
                {
                    switch (resolutionResult.ResolutionType)
                    {
                        case ConflictResolutionType.SkipConflictedChangeAction:
                            return;
                        default:
                            Debug.Fail("Unknown resolution result");
                            return;
                    }
                }
                return;
            }
            base.BasicActionHandler(action, group);
        }

    }
}
