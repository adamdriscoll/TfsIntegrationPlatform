// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    public partial class Tfs2010WitAnalysisProvider : TfsWITAnalysisProvider
    {
        public Tfs2010WitAnalysisProvider() 
        { }

        protected override TfsMigrationDataSource InitializeMigrationDataSource()
        {
            return new Tfs2010MigrationDataSource();
        }

        public override string GetNativeId(BusinessModel.MigrationSource migrationSourceConfig)
        {
            TfsTeamProjectCollection tfsServer = 
                TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(migrationSourceConfig.ServerUrl));
            return tfsServer.InstanceId.ToString();
        }
    }
}