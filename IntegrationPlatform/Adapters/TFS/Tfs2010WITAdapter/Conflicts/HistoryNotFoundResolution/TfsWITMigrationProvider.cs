// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Hist = Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts.HistoryNotFoundResolution;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    public partial class TfsWITMigrationProvider
    {
        internal TfsWITMigrationProvider(
            string serverUrl,
            string teamProject,
            string targetWorkItemId)
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

            if (!string.IsNullOrEmpty(targetWorkItemId))
            {
                int workItemId;
                if (int.TryParse(targetWorkItemId, out workItemId))
                {
                    m_migrationSource.WorkItemStore.TargetWorkItemId = workItemId;
                }
            }

            this.m_migrationSource.WorkItemStore.ByPassrules = true;
        }

        internal Hist.ConversionResult ProcessChangeGroup(Hist.MigrationAction[] changeGroup)
        {
            Hist.ConversionResult changeResult = new Hist.ConversionResult();
            changeResult.ChangeId = string.Empty;

            m_migrationSource.WorkItemStore.SubmitChanges(changeGroup, changeResult);
            if (!string.IsNullOrEmpty(changeResult.ChangeId))
            {
                TraceManager.TraceInformation(string.Format(
                                                  "Completed migration, result change: {0}", changeResult.ChangeId));
            }

            return changeResult;
        }

    }
}
