// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop;
using ClearQuestOleServer;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    internal class CQRecordStoredQueryFilter : CQRecordFilter
    {
        public CQRecordStoredQueryFilter(string storedQueryName, ClearQuestOleServer.Session userSession)
            : base(string.Empty)
        {
            StoredQueryName = storedQueryName;

            if (null != userSession)
            {
                OAdQuerydef qryDef = CQWrapper.GetQueryDef(CQWrapper.GetWorkSpace(userSession), StoredQueryName);
                base.RecordType = CQWrapper.GetPrimaryEntityDefName(qryDef);
            }            
        }
        
        public string StoredQueryName
        {
            get;
            private set;
        }
    }
}
