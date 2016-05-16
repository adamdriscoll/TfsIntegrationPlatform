// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{
    /// <summary>
    /// CCHistoryRecord holds the information returned by lshistory 
    /// </summary>
    public class CCHistoryRecord : IComparable
    {
        /// <summary>
        /// Generate a CCHistoryRecord instance from a historyRow string.
        /// </summary>
        /// <param name="historyRow">A history row string returned from query history command</param>
        /// <returns>The CCHistoryRecord created or null if the creation failed.</returns>
        public static CCHistoryRecord CreateInstance(string historyRow)
        {
            if (string.IsNullOrEmpty(historyRow))
            {
                return null;
            }

            if (historyRow.Contains("ClearCase object not found."))
            {
                TraceManager.TraceInformation("Invalid history record - {0}", historyRow);
                return null;
            }

            string[] historyColumns =
                historyRow.Split(new string[] { ClearCaseCommandSpec.HISTORYRECORD_COLUMNDELIMINATOR }, StringSplitOptions.None);
            if (historyColumns.Length < 7)
            {
                TraceManager.TraceInformation("Wrong numbers of columns in history record - {0}", historyRow);
                return null;
            }
            string[] strEventId =
                historyColumns[0].Split(new string[] { 
                    "event", ":" },
                    StringSplitOptions.RemoveEmptyEntries);
            long eventId;
            if (!long.TryParse(strEventId[0], out eventId))
            {
                TraceManager.TraceInformation("Invalid event id in history record - {0}", historyRow);
                return null;
            }

            DateTime versionTime;
            if (!DateTime.TryParse(historyColumns[5], out versionTime))
            {
                TraceManager.TraceInformation("Invalid version time in history record - {0}", historyRow);
                return null;
            }
            CCHistoryRecord instance = new CCHistoryRecord
            {
                EventId = eventId,
                OperationType = Operation.GetOperationType(historyColumns[2]),
                VersionExtendedPath = historyColumns[1],
                OperationDescription = historyColumns[3],
                VersionTime = versionTime,
                Comment = historyColumns[6],
                UserComment = historyColumns[7].Trim(new char[] { '(', ')' }),
                AbsoluteVobPath = ClearCasePath.GetAbsoluteVobPathFromVersionExtendedPath(historyColumns[1])
            };

            return instance;
        }

        /// <summary>
        /// Compare CCHistoryRecord based on eventid.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            if (obj is CCHistoryRecord)
            {
                CCHistoryRecord otherHistoryRecord = (CCHistoryRecord)obj;
                return this.EventId.CompareTo(otherHistoryRecord.EventId);
            }
            else
            {
                throw new ArgumentException("Object is not a CCHistoryRecord");
            }
        }



        public long EventId { get; private set; }
        public string VersionExtendedPath { get; private set; }
        public OperationType OperationType { get; private set; }
        public string OperationDescription { get; private set; }
        public DateTime VersionTime { get; private set; }
        public string Comment { get; private set; }
        public string UserComment { get; private set; }
        public bool IsDirectory { get; set; }
        public string AbsoluteVobPath { get; set; }
        public string AbsoluteVobPathFrom { get; set; }
        public Guid ChangeAction { get; set; }
    }
}
