// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter
{
    public class TfsWITSyncMonitorProvider : ISyncMonitorProvider
    {
        private ITranslationService m_translationService;
        private MigrationSource m_migrationSource;
        private WorkItemStore m_workItemStore;
        private string m_projectName;

        /// <summary>
        /// Obtain references to services needed by this class
        /// </summary>
        public void InitializeServices(IServiceContainer syncMonitorServiceContainer)
        {
            m_translationService = syncMonitorServiceContainer.GetService(typeof(ITranslationService)) as ITranslationService;
            Debug.Assert(m_translationService != null);
        }

        /// <summary>
        /// Perform the adapter-specific initialization
        /// </summary>
        /// <param name="migrationSource">The MigrationSource associated with this adapter instance</param>
        public void InitializeClient(MigrationSource migrationSource)
        {
            m_migrationSource = migrationSource;
            TeamFoundationServer tfs = TeamFoundationServerFactory.GetServer(migrationSource.ServerUrl);
            tfs.Authenticate();

            m_workItemStore = (WorkItemStore)tfs.GetService(typeof(WorkItemStore));

            m_projectName = migrationSource.SourceIdentifier;
        }

        #region ISyncMonitorProvider implementation
        public ChangeSummary GetSummaryOfChangesSince(string lastProcessedChangeItemId, List<string> filterStrings)
        {
            if (string.IsNullOrEmpty(lastProcessedChangeItemId))
            {
                throw new ArgumentException("lastProcessedChangeItemId");
            }

            ChangeSummary changeSummary = new ChangeSummary();
            changeSummary.ChangeCount = 0;
            changeSummary.FirstChangeModifiedTimeUtc = DateTime.MinValue;

            string[] changeItemIdParts = lastProcessedChangeItemId.Split(new char[] { ':' });
            if (changeItemIdParts.Length != 2)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture,
                    TfsWITAdapterResources.InvalidChangeItemIdFormat, lastProcessedChangeItemId));
            }

            int lastProcessedWorkItemId = 0;
            WorkItem lastProcessedWorkItem = null;
            try
            {
                lastProcessedWorkItemId = int.Parse(changeItemIdParts[0]);
                lastProcessedWorkItem = m_workItemStore.GetWorkItem(lastProcessedWorkItemId);
                int revNumber = -1;
                try
                {
                    revNumber = int.Parse(changeItemIdParts[1]);
                }
                catch (FormatException)
                {
                }
                DateTime changedDate;
                if (revNumber > 0)
                {
                    Revision rev = lastProcessedWorkItem.Revisions[revNumber - 1];
                    changedDate = (DateTime)rev.Fields[CoreField.ChangedDate].Value;
                }
                else
                {
                    changedDate = lastProcessedWorkItem.ChangedDate;
                }
                changeSummary.FirstChangeModifiedTimeUtc = changedDate.ToUniversalTime();
            }
            catch(Exception ex)
            {
                Console.WriteLine(String.Format(CultureInfo.InvariantCulture,
                    "Tfs2008WitAdapter.TfsWITSyncMonitorProvider: Exception finding last processed work item (Id {0}): {1}",
                    lastProcessedWorkItemId, ex.ToString()));
            }

            WorkItem nextWorkItemToBeMigrated = null;

            foreach (string filterString in filterStrings)
            {
                Dictionary<string, object> context = new Dictionary<string, object>();
                string columnList = "[System.Id], [System.ChangedDate]";
                StringBuilder conditionBuilder = new StringBuilder(filterString);
                if (!string.IsNullOrEmpty(m_projectName))
                {
                    context.Add("project", m_projectName);
                    if (conditionBuilder.Length > 0)
                    {
                        conditionBuilder.Append(" AND ");
                    }
                    conditionBuilder.Append("[System.TeamProject] = @project");
                }
                if (!changeSummary.FirstChangeModifiedTimeUtc.Equals(default(DateTime)))
                {
                    if (conditionBuilder.Length > 0)
                    {
                        conditionBuilder.Append(" AND ");
                    }
                    conditionBuilder.AppendFormat(CultureInfo.InvariantCulture, "[System.ChangedDate] > '{0:u}'", changeSummary.FirstChangeModifiedTimeUtc);
                }
                string orderBy = "[System.Id]";
                string wiql = TfsWITQueryBuilder.BuildWiqlQuery(columnList, conditionBuilder.ToString(), orderBy);
     
                // Run query with date precision off
                Query wiq = new Query(m_workItemStore, wiql, context, false);

                // Retrieve all results
                ICancelableAsyncResult car = wiq.BeginQuery();
                WorkItemCollection workItems = wiq.EndQuery(car);

                foreach (WorkItem wi in workItems)
                {
                    if (!m_translationService.IsSyncGeneratedItemVersion(
                            wi.Id.ToString(),
                            wi.Rev.ToString(),
                            new Guid(m_migrationSource.InternalUniqueId)))
                    {
                        changeSummary.ChangeCount++;
                        if (nextWorkItemToBeMigrated == null || wi.ChangedDate < nextWorkItemToBeMigrated.ChangedDate)
                        {
                            nextWorkItemToBeMigrated = wi;
                        }
                    }
                }
            }

            if (nextWorkItemToBeMigrated != null)
            {
                changeSummary.FirstChangeModifiedTimeUtc = nextWorkItemToBeMigrated.ChangedDate.ToUniversalTime();
            }

            return changeSummary;
        }

        #endregion

        #region IServiceProvider implementation
        /// <summary>
        /// Gets the service object of the specified type. 
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public Object GetService(Type serviceType)
        {
            return this as IServiceProvider;
        }
        #endregion

        #region IDisposable members
        public void Dispose()
        {
        }
        #endregion
    }

}
