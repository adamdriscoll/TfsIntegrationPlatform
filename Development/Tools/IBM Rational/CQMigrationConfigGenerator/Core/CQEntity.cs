// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// Description: Cache for all the bugs migrated in this run.

#region Using directives
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.TeamFoundation.Converters.Utility;
using ClearQuestOleServer;
#endregion

namespace Microsoft.TeamFoundation.Converters.WorkItemTracking.CQ
{
    /// <summary>
    /// </summary>
    internal class CQEntity
    {
        #region Private Members
        private string m_entityName;
        private OAdEntity m_cqEntity;
        private List<CQEntityRec> m_records;
        #endregion

        #region Constructors
        public CQEntity(string entityName)
        {
            m_entityName = entityName;
            m_records = new List<CQEntityRec>();
        }
        #endregion Constructors

        /// <summary>
        /// Add the record in cache
        /// </summary>
        /// <param name="rec">Record Handle</param>
        public void AddRecord(CQEntityRec rec)
        {
            Logger.Write(LogSource.CQ, TraceLevel.Verbose, "Adding work item with entity {0}, dbid {1} in the cache", rec.EntityName, rec.DBID);
            m_records.Add(rec);
        }

        /// <summary>
        /// Look up for the record in the cacje
        /// </summary>
        /// <param name="entityToFind">Entity Name</param>
        /// <param name="dbid">DBID of the entity to look up</param>
        /// <returns>Handle to record if found, null if not found</returns>
        public CQEntityRec FindEntityRec(string entityToFind, int dbid)
        {
            foreach (CQEntityRec tmpEntityRec in m_records)
            {
                if (tmpEntityRec.DBID == dbid &&
                    TFStringComparer.WorkItemType.Equals(tmpEntityRec.EntityName, entityToFind)
                    && tmpEntityRec.SourceId != null) // bug is in processing
                {
                    return tmpEntityRec;
                }
            }
            return null;
        }

        public OAdEntity Entity
        {
            get { return m_cqEntity; }
            set { m_cqEntity = value; }
        }

        public string EntityName
        {
            get { return m_entityName; }
        }
    }
}