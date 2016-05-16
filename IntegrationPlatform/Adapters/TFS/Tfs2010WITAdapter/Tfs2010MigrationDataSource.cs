// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    public class Tfs2010MigrationDataSource : TfsMigrationDataSource
    {
        public override TfsMigrationWorkItemStore CreateWorkItemStore()
        {
            TfsCore core = new TfsCore(this);
            return new Tfs2010MigrationWorkItemStore(core);
        }
    }
}