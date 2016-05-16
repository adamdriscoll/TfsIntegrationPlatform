// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ClearQuestOleServer;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    internal abstract class CQRecordQueryBase : IEnumerable<OAdEntity>, IEnumerator<OAdEntity>
    {
        protected CQRecordFilter m_recordFilter;
        protected Session m_userSession;
        protected IServiceProvider m_serviceProvider;
        protected int[] m_resultSetDbIds = new int[0];
        protected int m_currResultSetDbIdIndex = -1;
        protected string m_queryStr;

        public CQRecordQueryBase(
            Session userSession,
            CQRecordFilter recordFilter,
            IServiceProvider serviceProvider)
        {
            m_userSession = userSession;
            m_recordFilter = recordFilter;
            m_serviceProvider = serviceProvider;
        }

        #region IEnumerator<OAdEntity> Members

        public virtual OAdEntity Current
        {
            get
            {
                return GetCurrentRecord();
            }
        }

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
        }

        #endregion

        #region IEnumerator Members

        object IEnumerator.Current
        {
            get
            {
                return GetCurrentRecord();
            }
        }

        public virtual bool MoveNext()
        {
            return (++m_currResultSetDbIdIndex) < m_resultSetDbIds.Length;
        }

        public virtual void Reset()
        {
            m_currResultSetDbIdIndex = -1;
        }
        
        #endregion

        #region IEnumerable<OAdEntity> Members

        public IEnumerator<OAdEntity> GetEnumerator()
        {
            return CreateEnumerator();
        }
        
        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return CreateEnumerator();
        }
        
        #endregion

        protected OAdEntity GetCurrentRecord()
        {
            OAdEntity entity = null;

            do
            {
                try
                {
                    TraceManager.TraceInformation(
                        "DEBUG: GetCurrentRecord dbid : {0}", 
                        m_resultSetDbIds[m_currResultSetDbIdIndex]);
                    entity = CQWrapper.GetEntityByDbId(
                        m_userSession, m_recordFilter.RecordType, m_resultSetDbIds[m_currResultSetDbIdIndex]);
                }
                catch (Exception ex)
                {
                    if (m_currResultSetDbIdIndex < m_resultSetDbIds.Length)
                    {
                        TraceManager.TraceInformation(
                            "Failed to get the current record {0}, {1}", 
                            m_resultSetDbIds[m_currResultSetDbIdIndex], ex.Message);
                        MoveNext();
                    }
                    else
                    {
                        break;
                    }
                }
            } while (entity == null);

            return entity;
        }

        protected virtual void Query()
        {
            TraceManager.TraceInformation("CQ Query: {0}", m_queryStr);

            // prepare result set
            OAdResultset result = CQWrapper.BuildResultSet(m_userSession, m_queryStr);

            // enable record count before execute so that no of records can be fetched
            CQWrapper.EnableRecordCount(result);

            // execute the query
            CQWrapper.ExecuteResultSet(result);

            // lookup for dbid column
            bool dbidExist = false;
            int dbidColumnIndex = 0;
            int columnCount = CQWrapper.GetResultSetColumnCount(result);
            for (int colIter = 1; colIter <= columnCount; colIter++)
            {
                if (string.Equals(CQWrapper.GetColumnLabel(result, colIter), "dbid", StringComparison.OrdinalIgnoreCase))
                {
                    dbidExist = true;
                    dbidColumnIndex = colIter;
                    break;
                }
            }
            Debug.Assert(dbidExist, "dbid does not exist in resultset");

            int recordCount = CQWrapper.GetRecordCount(result);
            TraceManager.TraceInformation("CQ Query: returned {0} record(s)", recordCount);

            // cache dbids
            m_resultSetDbIds = new int[recordCount];
            int index = 0;
            while (CQWrapper.ResultSetMoveNext(result) == CQConstants.SUCCESS)
            {
                string dbid = (string)CQWrapper.GetColumnValue(result, dbidColumnIndex);
                m_resultSetDbIds[index++] = int.Parse(dbid);

                Trace.TraceInformation("DEBUG: dbid : {0}", int.Parse(dbid));
            }

            MakeSureDBIDsAreUnique();

            Trace.TraceInformation("Total number of records after removing the duplicate dbids: {0}", m_resultSetDbIds.Length);
        }

        protected virtual void MakeSureDBIDsAreUnique()
        {
            m_resultSetDbIds = m_resultSetDbIds.Distinct().ToArray();
        }

        protected abstract IEnumerator<OAdEntity> CreateEnumerator();
    }
}
