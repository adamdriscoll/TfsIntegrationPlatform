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

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    public class TfsWITDiffProvider : IWITDiffProvider
    {
        private static string[] s_ignoredFieldNames = new string[] 
        { 
            "WorkItemID", 
            "Revision",
            "ChangeDate", 
            "System.CreatedDate", 
            "System.History", 
            "System.IterationID", 
            "System.AreaID", 
            "Microsoft.VSTS.Common.StateChangeDate", 
            "Microsoft.VSTS.Common.ActivatedDate", 
            "TfsMigrationTool.ReflectedWorkItemId"
        };

        private WorkItemStore m_workItemStore;
        private string m_projectName;
        private string m_filterString;
        private bool m_provideForContentComparison;
        private HashSet<string> m_ignoredFieldNames;
        private ILinkProvider m_linkProvider;

        /// <summary>
        /// Obtain references to services needed by this class
        /// </summary>
        public void InitializeServices(IServiceContainer witDiffServiceContainer)
        {
            m_linkProvider = witDiffServiceContainer.GetService(typeof(ILinkProvider)) as ILinkProvider;
        }

        /// <summary>
        /// Perform the adapter-specific initialization
        /// </summary>
        public virtual void InitializeClient(MigrationSource migrationSource)
        {
            TfsTeamProjectCollection tfs = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(migrationSource.ServerUrl));
            tfs.Authenticate();

            m_workItemStore = (WorkItemStore)tfs.GetService(typeof(WorkItemStore));

            m_projectName = migrationSource.SourceIdentifier;
        }

        #region IWITDiffProvider implementation

        /// <summary>
        /// The implementation can perform any one-time initialization here.
        /// Some adapter implementations may not need to perform any such initialization.
        /// It takes as optional arguments a filterString and a version that would be applied during subsequent queries.
        /// </summary>
        /// <param name="filterString">A string that specifies some filtering condition; if null or empty no additional filtering is applied</param>
        /// <param name="version">The version of the item; if null or empty, the tip version is accessed</param>
        /// <param name="provideForContentComparison">If true, any IDiffItem returned by any method should include the contents of the work item for comparison purposed.
        /// If false, detailed content data can be left out.
        /// </param>
        public void InitializeForDiff(string filterString, bool provideForContentComparison)
        {
            m_filterString = filterString;
            m_provideForContentComparison = provideForContentComparison;
        }

        /// <summary>
        /// Enumerate the diff items found based on the query passed in as well as the filterString and version passed
        /// to InitializeForDiff.  The return type is IEnumerable<> so that adapter implementations do not need download and keep 
        /// all of the IWITDiffItems in memory at once.
        /// </summary>
        /// <param name="queryCondition">A string that specifies a query used to select a subset of the work items defined by 
        /// the set that the filter string identified.</param>
        /// <returns>An enumeration of IWITDiffItems each representing a work item to be compared by the WIT Diff operation</returns>
        public IEnumerable<IWITDiffItem> GetWITDiffItems(string queryCondition)
        {
            string columnList = "[System.Id], [System.Rev]";

            StringBuilder conditionBuilder = new StringBuilder(m_filterString);

            if (!string.IsNullOrEmpty(queryCondition))
            {
                if (conditionBuilder.Length > 0)
                {
                    conditionBuilder.Append(" AND ");
                }
                conditionBuilder.Append(queryCondition);
            }

            string orderBy = "[System.Id]";
            string wiql = TfsWITQueryBuilder.BuildWiqlQuery(columnList, conditionBuilder.ToString(), orderBy);

            // Run query with date precision off
            Dictionary<string, object> context = new Dictionary<string, object>();
            context.Add("project", m_projectName);
            Query wiq = new Query(m_workItemStore, wiql, context, false);

            // Retrieve all results
            ICancelableAsyncResult car = wiq.BeginQuery();
            WorkItemCollection workItems = wiq.EndQuery(car);

            foreach (WorkItem workItem in workItems)
            {
                yield return (IWITDiffItem)new TfsWITDiffItem(this, workItem);
            }
        }

        /// <summary>
        /// Return a IWITDiffItem representing a single work item as identified by the adapter specific workItemId string
        /// </summary>
        /// <param name="workItemId"></param>
        /// <returns></returns>
        public IWITDiffItem GetWITDiffItem(string workItemId)
        {
            string queryCondition = String.Format(CultureInfo.InvariantCulture, "[System.Id] = {0}", workItemId);
            foreach (IWITDiffItem witDiffItem in GetWITDiffItems(queryCondition))
            {
                return witDiffItem;
            }
            return null;
        }

        /// <summary>
        /// IgnoreFieldInComparison is called to allow an adapter to specify that a named field should not be compared
        /// in the diff operation
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public bool IgnoreFieldInComparison(string fieldName)
        {
            return IgnoredFieldNames.Contains(fieldName);
        }

        /// <summary>
        /// Give the IWITDiffProvider a chance to cleanup any reources allocated during InitializeForDiff()
        /// </summary>
        public void Cleanup()
        {
            m_filterString = null;
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

        #region internal properties
        internal ILinkProvider LinkProvider
        {
            get { return m_linkProvider; }
        }
        #endregion

        #region private properties
        private HashSet<string> IgnoredFieldNames
        {
            get
            {
                if (m_ignoredFieldNames == null)
                {
                    m_ignoredFieldNames = new HashSet<string>(TFStringComparer.WorkItemFieldReferenceName);
                    foreach (string fieldName in s_ignoredFieldNames)
                    {
                        m_ignoredFieldNames.Add(fieldName);
                    }
                }
                return m_ignoredFieldNames;
            }
        }
        #endregion
    }

}
