// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using ClearQuestOleServer;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    internal class CQRecordSqlQuery : CQRecordQueryBase
    {
        private const string QueryBase =
@"SELECT dbid 
  FROM {0} 
  WHERE (dbid in (SELECT DISTINCT entity_dbid FROM history WHERE action_timestamp > '{1}'))";

        private const string QueryBaseWithoutTimestamp =
@"SELECT dbid 
  FROM {0} 
  WHERE (dbid in (SELECT DISTINCT entity_dbid FROM history))";

        private const string QueryFilter = @" AND ({0})";

        public CQRecordSqlQuery(
            Session userSession,
            CQRecordFilter recordFilter,
            string hwmStr,
            IServiceProvider serviceProvider)
            : base(userSession, recordFilter, serviceProvider)
        {
            List<CQRecordFilter> recordFilters = new List<CQRecordFilter>();
            recordFilters.Add(recordFilter);
            BuildQueryString(recordFilters, hwmStr);
            Query();
        }

        public CQRecordSqlQuery(
            Session userSession,
            List<CQRecordFilter> recordFilters,
            string hwmStr,
            IServiceProvider serviceProvider)
            : base(userSession, recordFilters[0], serviceProvider)
        {
            if (recordFilters == null || recordFilters.Count == 0)
            {
                throw new ArgumentException("recordFilters");
            }
            
            BuildQueryString(recordFilters, hwmStr);
            Query();
        }

        /// <summary>
        /// Enumerator's constructor
        /// </summary>
        /// <param name="src"></param>
        private CQRecordSqlQuery(CQRecordSqlQuery src)
            : base(src.m_userSession, src.m_recordFilter, src.m_serviceProvider)
        {
            m_queryStr = src.m_queryStr;
            m_resultSetDbIds = src.m_resultSetDbIds;
        }

        protected override IEnumerator<OAdEntity> CreateEnumerator()
        {
            return new CQRecordSqlQuery(this);
        }

        private void BuildQueryString(
            List<CQRecordFilter> recordFilters, 
            string hwmStr)
        {
            Debug.Assert(recordFilters.Count > 0);
            if (string.IsNullOrEmpty(hwmStr))
            {
                m_queryStr = UtilityMethods.Format(QueryBaseWithoutTimestamp, recordFilters[0].SelectFromTable);
            }
            else
            {
                m_queryStr = UtilityMethods.Format(QueryBase, recordFilters[0].SelectFromTable, hwmStr);
            }

            foreach (CQRecordFilter recordFilter in recordFilters)
            {
                if (!string.IsNullOrEmpty(recordFilter.SqlFilter))
                {
                    m_queryStr += UtilityMethods.Format(QueryFilter, recordFilter.SqlFilter);
                }
            }
        }
    }
}
