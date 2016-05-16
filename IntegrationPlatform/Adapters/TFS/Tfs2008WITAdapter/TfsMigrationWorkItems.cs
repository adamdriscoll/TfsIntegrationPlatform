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
    /// Collection of TFS work items.
    /// </summary>
    class TfsMigrationWorkItems : IEnumerable<TfsMigrationWorkItem>, IEnumerator<TfsMigrationWorkItem>
    {
        /// <summary>
        /// Return time of the first query.
        /// </summary>
        public DateTime AsOf { get { return m_asOf; } }

        /// <summary>
        /// Enumerable's constructor.
        /// </summary>
        /// <param name="core">TFS core object</param>
        /// <param name="store">Source work item store</param>
        /// <param name="condition">Query's condition for obtaining work items</param>
        public TfsMigrationWorkItems(
            TfsCore core,
            WorkItemStore store,
            string condition)
        {
            m_core = core;
            m_store = store;
            m_firstItemIdInNextPage = 0;
            m_queryBase = string.Format(
                CultureInfo.InvariantCulture,
                "SELECT [System.Id], [System.Rev] FROM WorkItems WHERE ({0})",
                condition);
            m_context = new Dictionary<string, object>();
            m_context.Add("project", m_core.Config.Project);
            Query();
            ShallowReset();
        }

        /// <summary>
        /// Enumerator's constructor.
        /// </summary>
        /// <param name="src">Source enumerator</param>
        private TfsMigrationWorkItems(
            TfsMigrationWorkItems src)
        {
            m_core = src.m_core;
            m_store = src.m_store;
            m_queryBase = src.m_queryBase;
            m_context = src.m_context;
            m_firstItemIdInNextPage = src.m_firstItemIdInNextPage;
            m_items = src.m_items;
            m_idsInQueryResult = src.m_idsInQueryResult;
            m_indexInCurrPage = src.m_indexInCurrPage;
        }

        #region IDisposable Members

        public void Dispose()
        {
            m_items = null;
        }

        #endregion

        #region IEnumerable<TfsMigrationWorkItem> methods

        /// <summary>
        /// Creates an enumerator for the collection.
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<TfsMigrationWorkItem> IEnumerable<TfsMigrationWorkItem>.GetEnumerator()
        {
            return new TfsMigrationWorkItems(this);
        }

        /// <summary>
        /// Creates an enumerator for the collection.
        /// </summary>
        /// <returns>Enumerator</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new TfsMigrationWorkItems(this);
        }

        #endregion

        #region IEnumerator<TfsMigrationWorkItem> Members

        public TfsMigrationWorkItem Current { get { return m_item; } }

        #endregion

        #region IEnumerator Members

        object System.Collections.IEnumerator.Current { get { return m_item; } }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public bool MoveNext()
        {
            if (null == m_items)
            {
                GetWorkItems(m_firstItemIdInNextPage, PageSize);
                if (null == m_items)
                {
                    return false;
                }
            }

            for (; ; )
            {
                while (m_indexInCurrPage < PageSize && m_indexInCurrPage < m_items.Count)
                {
                    var itemBkup = m_item;
                    try
                    {
                        WorkItem wi =  m_items[m_indexInCurrPage];
                        wi.Open();
                        m_item = new TfsMigrationWorkItem(m_core, wi);
                        m_indexInCurrPage++;
                        return true;
                    }
                    catch (Exception)
                    {
                        m_item = itemBkup;
                        throw;
                    }
                }

                // open next batch of Work Items
                m_items = null;
                GetWorkItems(m_firstItemIdInNextPage, PageSize);

                if (m_items == null || m_items.Count == 0)
                {
                    return false;
                }

                continue;
            }
        }

        public void Reset()
        {
            ShallowReset();
        }

        #endregion

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
            m_items = qry.EndQuery(qry.BeginQuery());
            TraceManager.TraceInformation("TFS Query: returned {0} item(s)", m_items.Count);

            m_asOf = qry.AsOfUTC;            

            m_idsInQueryResult = new int[m_items.Count];
            for (int i = 0; i < m_items.Count; ++i)
            {
                m_idsInQueryResult[i] = m_items[i].Id;
            }
        }

        private void ShallowReset()
        {
            m_indexInCurrPage = 0;
            m_firstItemIdInNextPage = 0;
            m_items = null;
            m_item = null;
        }

        private void GetWorkItems(int startIndex, int length)
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
                m_items = null;
                m_indexInCurrPage = 0;
                return;
            }

            WorkItemCollection itemsBkup = m_items;
            int pageIndexBkup = m_indexInCurrPage;
            int collectionIndexBkup = m_firstItemIdInNextPage;
            try
            {
                m_items = m_store.Query(readParams, BatchReadQuery);
                m_indexInCurrPage = 0;
                m_firstItemIdInNextPage = index;
            }
            catch (Exception)
            {
                m_items = itemsBkup;
                m_indexInCurrPage = pageIndexBkup;
                m_firstItemIdInNextPage = collectionIndexBkup;
                throw;
            }
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

        private TfsCore m_core;                             // TFS core
        private WorkItemStore m_store;                      // Source work item store
        private string m_queryBase;                         // Query base
        private Dictionary<string, object> m_context;       // Query context
        private WorkItemCollection m_items;                 // Collection of work items

        private TfsMigrationWorkItem m_item;                // Current item
        private int m_indexInCurrPage;                      // Page Index of the current item
        private int m_firstItemIdInNextPage;                // Collection Index of the first item in the next page
        private DateTime m_asOf;                            // As of date time
        private int[] m_idsInQueryResult;                   // Work Item Ids returned by the WIQL query
    }
}