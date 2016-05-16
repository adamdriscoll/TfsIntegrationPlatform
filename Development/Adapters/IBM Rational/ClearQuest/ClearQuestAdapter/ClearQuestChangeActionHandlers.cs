// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    internal class ClearQuestChangeActionHandlers : ChangeActionHandlers
    {
        internal ClearQuestChangeActionHandlers(IAnalysisProvider analysisProvider)
            : base(analysisProvider)
        {
        }

        public override void BasicActionHandler(
            MigrationAction deltaTableAction,
            ChangeGroup migrationInstructionChangeGroup)
        {
            migrationInstructionChangeGroup.CreateAction(
                    deltaTableAction.Action,
                    deltaTableAction.SourceItem,
                    deltaTableAction.FromPath,
                    deltaTableAction.Path,
                    deltaTableAction.Version,
                    deltaTableAction.MergeVersionTo,
                    deltaTableAction.ItemTypeReferenceName,
                    deltaTableAction.MigrationActionDescription);
        }
    }
}
