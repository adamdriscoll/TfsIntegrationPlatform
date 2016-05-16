// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.Exceptions;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    /// <summary>
    /// class CQRecordFilters
    /// </summary>
    /// <remarks>
    /// The filter strings for CQ adapters can be in the forms of:
    ///  1. RecordType::SelectFromCQTableName::SqlFilter
    ///  2. RecordType::SqlFilter
    ///  3. RecordType
    /// </remarks>
    internal class CQRecordFilters : List<CQRecordFilter>
    {
        private const string FilterDelimitor = "::";
        private const string StoredQueryPrefix = "@StoredQuery@::";

        public CQRecordFilters(
            ReadOnlyCollection<MappingEntry> rawMappingFilters,
            ClearQuestOleServer.Session userSession)
        {
            Initialize(rawMappingFilters, userSession);
        }

        protected void Initialize(
            ReadOnlyCollection<MappingEntry> rawMappingFilters,
            ClearQuestOleServer.Session userSession)
        {
            foreach (MappingEntry filter in rawMappingFilters)
            {
                CQRecordFilter recordFilter = ParseFilter(filter, userSession);
                if (null != recordFilter)
                {
                    this.Add(recordFilter);
                }
            }
        }

        private CQRecordFilter ParseFilter(MappingEntry filter, ClearQuestOleServer.Session userSession)
        {
            return ParseFilterPath(filter.Path, userSession);
        }

        internal static CQRecordFilter ParseFilterPath(string filterPath, ClearQuestOleServer.Session userSession)
        {
            if (string.IsNullOrEmpty(filterPath))
            {
                return null;
            }

            filterPath = filterPath.Trim();
            if (filterPath.StartsWith(FilterDelimitor))
            {
                return null;
            }

            if (filterPath.StartsWith(StoredQueryPrefix, StringComparison.OrdinalIgnoreCase))
            {
                if (filterPath.Length == StoredQueryPrefix.Length)
                {
                    // todo: raise error
                    return null;
                }

                string storedQuery = filterPath.Substring(StoredQueryPrefix.Length);
                return new CQRecordStoredQueryFilter(storedQuery, userSession);
            }
            else
            {
                // advanced setting: DefectType::[TableName::]SQL Query Condition
                string[] splits = filterPath.Split(new string[] { FilterDelimitor }, StringSplitOptions.RemoveEmptyEntries);

                switch (splits.Length)
                {
                    case 1:
                        return new CQRecordFilter(splits[0].Trim()); // RecordType
                    case 2:
                        return new CQRecordFilter(splits[0].Trim(), splits[1].Trim()); // RecordType::SqlCondition
                    case 3:
                        return new CQRecordFilter(splits[0].Trim(), splits[1].Trim(), splits[2].Trim()); // RecordType::SelectFromTable::SqlCondition
                    default:
                        TraceManager.TraceError("CQ adapter FilterString '{0}' is invalid.", filterPath);
                        return null;
                }
            }
        }
    }
}
