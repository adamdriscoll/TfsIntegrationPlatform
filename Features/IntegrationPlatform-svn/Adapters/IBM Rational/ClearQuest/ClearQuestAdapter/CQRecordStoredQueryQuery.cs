// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClearQuestOleServer;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    internal class CQRecordStoredQueryQuery : CQRecordQueryBase
    {
        private const string QueryBase =
@"{0} and (T1.dbid in (SELECT DISTINCT entity_dbid FROM history WHERE action_timestamp > '{1}'))";

        private OAdQuerydef m_queryDef;

        public CQRecordStoredQueryQuery(
            Session userSession,
            CQRecordFilter recordFilter,
            string hwmStr, 
            IServiceProvider serviceProvider)
            : base(userSession, recordFilter, serviceProvider)
        {
            if (!(recordFilter is CQRecordStoredQueryFilter))
            {
                throw new ArgumentException("recordFilter is not CQRecordStoredQueryFilter");
            }

            m_queryDef = CQWrapper.GetQueryDef(
                    CQWrapper.GetWorkSpace(m_userSession), ((CQRecordStoredQueryFilter)m_recordFilter).StoredQueryName);

            string originalQueryString = NormalizeSqlQuery(m_queryDef.SQL);
            if (string.IsNullOrEmpty(hwmStr))
            {
                m_queryStr = originalQueryString;
            }
            else
            {
                m_queryStr = UtilityMethods.Format(QueryBase, originalQueryString, hwmStr);
            }

            Query();
        }

        private string NormalizeSqlQuery(string sqlQuery)
        {
            Debug.Assert(!string.IsNullOrEmpty(sqlQuery), "sqlQuery is NULL");

            int orderbyIndex = sqlQuery.IndexOf("order by", StringComparison.OrdinalIgnoreCase);
            if (orderbyIndex > 0)
            {
                sqlQuery = sqlQuery.Substring(0, orderbyIndex);
            }

            return sqlQuery.Trim();
        }

        private CQRecordStoredQueryQuery(CQRecordStoredQueryQuery src)
            : base(src.m_userSession, src.m_recordFilter, src.m_serviceProvider)
        {
            m_queryStr = src.m_queryStr;
            m_resultSetDbIds = src.m_resultSetDbIds;
            m_queryDef = src.m_queryDef;
        }

        protected override IEnumerator<ClearQuestOleServer.OAdEntity> CreateEnumerator()
        {
            return new CQRecordStoredQueryQuery(this);
        }
    }
}
