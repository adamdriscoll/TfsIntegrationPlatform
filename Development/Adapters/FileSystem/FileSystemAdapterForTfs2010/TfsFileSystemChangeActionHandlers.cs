// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsFileSystemAdapter
{
    /// <summary>
    /// Change action handler for Tfs file system adapter. 
    /// This just call the basic action handler to translate action paths.
    /// </summary>
    public class TfsFileSystemChangeActionHandlers : ChangeActionHandlers
    {
        public TfsFileSystemChangeActionHandlers(IAnalysisProvider analysisProvider)
            : base(analysisProvider)
        {
        }

        public override void BasicActionHandler(MigrationAction action, ChangeGroup group)
        {
            base.BasicActionHandler(action, group);
        }

    }
}
