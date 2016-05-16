// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    internal class CQRecordFilter
    {
        public CQRecordFilter(string recordType)
        {
            Initialize(recordType, recordType, null);
        }

        public CQRecordFilter(string recordType, string recordSqlFilter)
        {
            Initialize(recordType, recordType, recordSqlFilter);
        }

        public CQRecordFilter(string recordType, string selectFromTable, string recordSqlFilter)
        {
            Initialize(recordType, selectFromTable, recordSqlFilter);
        }

        public string RecordType
        {
            get;
            protected set;
        }

        public string SelectFromTable
        {
            get;
            private set;
        }

        public string SqlFilter
        {
            get;
            private set;
        }

        private void Initialize(string recordType, string selectFromTable, string recordSqlFilter)
        {
            RecordType = recordType;
            SelectFromTable = selectFromTable;
            SqlFilter = recordSqlFilter;
        }
    }
}
