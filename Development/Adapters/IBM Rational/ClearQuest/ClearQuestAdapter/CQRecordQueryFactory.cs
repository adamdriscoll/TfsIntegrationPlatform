// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClearQuestOleServer;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    internal static class CQRecordQueryFactory
    {
        public static CQRecordQueryBase CreatQuery(
            Session userSession,
            CQRecordFilter recordFilter,
            string hwmStr,
            IServiceProvider serviceProvider)
        {
            if (recordFilter is CQRecordStoredQueryFilter)
            {
                return new CQRecordStoredQueryQuery(
                    userSession, recordFilter, hwmStr, serviceProvider);
            }
            else
            {
                return new CQRecordSqlQuery(
                    userSession, recordFilter, hwmStr, serviceProvider);
            }
        }
    }
}
