// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Tfs2008WITTCAdapter
{
    public class WITUtil
    {
        private WorkItemStore m_store;                      // Source work item store
        private string m_queryBase;                         // Query base
        private Dictionary<string, object> m_context;       // Query context
        private WorkItemCollection m_items;                 // Collection of work items

        private DateTime m_asOf;                            // As of date time
        private string m_queryAsOfTime;                     // Query condition: AsOf
        private DateTime m_queryAsOfDateTime = default(DateTime);  // Query condition: AsOf DateTime

        public WorkItemCollection WorkItems
        {
            get
            {
                return m_items;
            }
        }

        public WITUtil(
            WorkItemStore store,
            string project,
            string condition,
            string asOfTime)
        {
            m_store = store;
            m_queryBase = string.Format(
                CultureInfo.InvariantCulture,
                "SELECT [System.Id], [System.Rev] FROM WorkItems WHERE ({0})",
                condition);
            m_context = new Dictionary<string, object>();
            m_context.Add("project", project);
            m_queryAsOfTime = asOfTime;
            Query();
        }

        private DateTime QueryAsOfDateTime
        {
            get
            {
                if (string.IsNullOrEmpty(m_queryAsOfTime))
                {
                    m_queryAsOfDateTime = default(DateTime);
                }
                else if (m_queryAsOfDateTime.Equals(default(DateTime)))
                {
                    m_queryAsOfDateTime = Convert.ToDateTime(m_queryAsOfTime, CultureInfo.InvariantCulture);
                }
                return m_queryAsOfDateTime;
            }
        }

        private void Query()
        {
            StringBuilder q = new StringBuilder(m_queryBase);
            if (!string.IsNullOrEmpty(m_queryAsOfTime))
            {
                q.AppendFormat(CultureInfo.InvariantCulture, " ASOF '{0:u}'", QueryAsOfDateTime);
            }
            q.Append(" ORDER BY [System.Id]");

            string query = q.ToString();
            Trace.TraceInformation("TFS Query: {0} : {1} ", m_store.TeamFoundationServer.Name, m_context["project"]);
            Trace.TraceInformation("TFS Query: {0} ", query);
            Query qry = new Query(m_store, query, m_context, false);
            m_items = qry.EndQuery(qry.BeginQuery());
            Trace.TraceInformation("TFS Query: returned {0} item(s)", m_items.Count);

            m_asOf = qry.AsOfUTC;
        }
    }
}
