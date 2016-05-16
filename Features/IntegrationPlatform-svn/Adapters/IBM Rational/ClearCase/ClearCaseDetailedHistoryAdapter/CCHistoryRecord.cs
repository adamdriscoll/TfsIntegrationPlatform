// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{
    /// <summary>
    /// CCHistoryRecord holds the information returned by lshistory 
    /// </summary>
    public class CCHistoryRecord : IComparable
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public CCHistoryRecord(string historyRow)
        {
            if (String.IsNullOrEmpty(historyRow))
                throw new ArgumentNullException("historyRow");

            string[] historyColumns =
                historyRow.Split(new string[] { ClearCaseCommandSpec.HISTORYRECORD_COLUMNDELIMINATOR }, StringSplitOptions.None);
            string[] strEventId =
                historyColumns[0].Split(new string[] { 
                    "event", ":" },
                    StringSplitOptions.RemoveEmptyEntries);
            EventId = long.Parse(strEventId[0]);
            VersionExtendedPath = historyColumns[1];
            OperationType = Operation.GetOperationType(historyColumns[2]);
            OperationDescription = historyColumns[3];
            VersionTime = DateTime.Parse(historyColumns[5]);
            Comment = historyColumns[6];
            UserComment = historyColumns[7].Trim(new char[] { '(', ')' });
            AbsoluteVobPath = ClearCasePath.GetAbsoluteVobPathFromVersionExtendedPath(VersionExtendedPath);
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
