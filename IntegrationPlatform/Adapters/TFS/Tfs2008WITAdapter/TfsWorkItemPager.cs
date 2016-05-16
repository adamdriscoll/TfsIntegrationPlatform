// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter
{
    /// <summary>
    /// Class used to read a large number of TFS work items by reading a page (or batch) at a time
    /// </summary>
    class TfsWorkItemPager
    {
        /// <summary>
        /// Return time of the first query.
        /// </summary>
        public DateTime AsOf { get { return m_asOf; } }

        /// <summary>
        /// Enumerable's constructor.
        /// </summary>
        /// <param name="projectName">The name of the TFS project from which to get work items</param>
        /// <param name="store">Source work item store</param>
        /// <param name="condition">Query's condition for obtaining work items</param>
        public TfsWorkItemPager(
            string projectName,
            WorkItemStore store,
            string condition)
        {
            m_store = store;
            m_firstItemIdInNextPage = 0;
            m_queryBase = string.Format(
                CultureInfo.InvariantCulture,
                "SELECT [System.Id], [System.Rev] FROM WorkItems WHERE ({0})",
                condition);
            m_context = new Dictionary<string, object>();
            m_context.Add("project", projectName);
            Query();
        }

        public WorkItemCollection GetNextPage()
        {
            return GetWorkItems(m_firstItemIdInNextPage, PageSize);          
        }

        /// <summary>
        /// Queries for work items, caches returned Work Item Ids and QueryResult ASOF time.
        /// </summary>
        private void Query()
        {
            StringBuilder q = new StringBuilder(m_queryBase);
            q.Append(" ORDER BY [System.Id]");

            string query = q.ToString();
            TraceManager.TraceInformation("TFS Query: {0}", query);
            Query qry = new Query(m_store, query, m_context, false);
            WorkItemCollection workItems = qry.EndQuery(qry.BeginQuery());
            TraceManager.TraceInformation("TFS Query: returned {0} item(s)", workItems.Count);

            m_asOf = qry.AsOfUTC;

            m_idsInQueryResult = new int[workItems.Count];
            for (int i = 0; i < workItems.Count; ++i)
            {
                m_idsInQueryResult[i] = workItems[i].Id;
            }
        }

        private WorkItemCollection GetWorkItems(int startIndex, int length)
        {
            Debug.Assert(startIndex >= 0, "startIndex < 0");
            Debug.Assert(length >= 0, "length < 0");
            Debug.Assert(m_idsInQueryResult != null, "m_idsInQueryResult is NULL");

            BatchReadParameterCollection readParams = new BatchReadParameterCollection();
            int index = startIndex;
            for (; length > 0 && index < m_idsInQueryResult.Length; ++index)
            {
                readParams.Add(new BatchReadParameter(m_idsInQueryResult[index]));
                --length;
            }

            if (readParams.Count == 0)
            {
                return null;
            }

            WorkItemCollection items = m_store.Query(readParams, BatchReadQuery);
            m_firstItemIdInNextPage = index;
            return items;
        }

        /// <summary>
        /// Gets the number of items in this collection
        /// </summary>
        public int Count
        {
            get
            {
                return m_idsInQueryResult.Length;
            }
        }

        private const int PageSize = 50;                   // Number of items to process from a single query
        private static readonly string BatchReadQuery = @"SELECT [System.ID] FROM WorkItems";

        private WorkItemStore m_store;                      // Source work item store
        private string m_queryBase;                         // Query base
        private Dictionary<string, object> m_context;       // Query context

        private int m_firstItemIdInNextPage;                // Collection Index of the first item in the next page
        private DateTime m_asOf;                            // As of date time
        private int[] m_idsInQueryResult;                   // Work Item Ids returned by the WIQL query
    }
}