// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClearQuestOleServer;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop
{
    public static class CQWorkSpace
    {
        public static IEnumerable<IOAdDatabaseDesc> GetAccessibleDatabases()
        {
            Session session = CQWrapper.CreateSession();
            return CQWrapper.GetAccessibleDatabases(session, CQConstants.MasterDBName, string.Empty, string.Empty);
        }

        public static IEnumerable<string> GetQueryList(Session session)
        {
            WORKSPACE ws = CQWrapper.GetWorkSpace(session);
            return CQWrapper.GetQueryList(ws, (short)OLEWKSPCQUERYTYPE.OLEWKSPCBOTHQUERIES);
        }

        public static Dictionary<string, bool> GetQueryListWithValidity(Session session)
        {
            Dictionary<string, bool> validQueries = new Dictionary<string, bool>();
            IEnumerable<string> queryList = GetQueryList(session);
            foreach (string query in queryList)
            {
                OAdQuerydef qryDef = CQWrapper.GetQueryDef(CQWrapper.GetWorkSpace(session), query);
                
                string str = CQWrapper.GetPrimaryEntityDefName(qryDef);
                validQueries[query] = !string.Equals(str, "All_UCM_Activities");
            }
            return validQueries;
        }
    }
}
