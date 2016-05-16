// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Hist = Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts.HistoryNotFoundResolution;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    public partial class TfsWITAnalysisProvider
    {
        internal TfsWITAnalysisProvider(
            string serverUrl,
            string teamProject)
        {
            TfsMigrationDataSource dataSourceConfig = InitializeMigrationDataSource();
            // Allow multiple filter strings from other adapters
            // Debug.Assert(filters.Count == 1, "filters.Count != 1 for WIT migration source");
            dataSourceConfig.Filter = string.Empty;
            dataSourceConfig.ServerId = serverUrl;
            dataSourceConfig.ServerName = serverUrl;
            dataSourceConfig.Project = teamProject;

            this.m_migrationSource = new TfsWITMigrationSource(
                serverUrl,
                dataSourceConfig.CreateWorkItemStore());
        }

        internal WorkItem GetWorkItem(int workItemId)
        {
            return m_migrationSource.WorkItemStore.WorkItemStore.GetWorkItem(workItemId);                 
        }
    }
}
