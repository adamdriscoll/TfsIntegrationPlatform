// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    public partial class Tfs2010WitMigrationProvider
    {
        public Tfs2010WitMigrationProvider(
            string serverUrl,
            string teamProject,
            string targetWorkItemId)
            : base(serverUrl, teamProject, targetWorkItemId)
        {
        }
    }
}
