// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    internal class CQMigrationItem
    {
        public CQMigrationItem(string migrationItemId, string migrationItemVersion)
        {
            MigrationItemId = migrationItemId;
            MigrationItemVersion = migrationItemVersion;
        }

        public string MigrationItemId
        {
            get;
            set;
        }

        public string MigrationItemVersion
        {
            get;
            set;
        }
    }

    internal class CQHistoryMigrationItem : CQMigrationItem
    {
        public const string Delimiter = "::"; // version string -> HistoryField::HistoryIndex

        public static string CreateHistoryItemVersion(
            string historyFieldName,
            int historyIndex)
        {
            return historyFieldName + Delimiter + historyIndex.ToString();
        }

        public CQHistoryMigrationItem(
            string recordDisplayName,
            string historyFieldName,
            int historyIndex)
            : base(recordDisplayName, CreateHistoryItemVersion(historyFieldName, historyIndex))
        {
            Initialize(recordDisplayName, historyFieldName, historyIndex);
        }

        public CQHistoryMigrationItem(
            string itemId,
            string versionStr)
            : base(itemId, versionStr)
        {
            Initialize(itemId, versionStr);
        }

        public string RecordDisplayName
        {
            get;
            private set;
        }

        public string HistoryFieldName
        {
            get;
            private set;
        }

        public int HistoryIndex
        {
            get;
            internal set;
        }

        private void Initialize(string recordDisplayName, string historyFieldName, int historyIndex)
        {
            if (string.IsNullOrEmpty(recordDisplayName))
            {
                throw new ArgumentNullException("recordDisplayName");
            }

            if (string.IsNullOrEmpty(historyFieldName))
            {
                throw new ArgumentNullException("historyFieldName");
            }

            RecordDisplayName = recordDisplayName;
            HistoryFieldName = historyFieldName;
            HistoryIndex = historyIndex;
        }

        private void Initialize(string migrationItemId, string migrationItemVersion)
        {
            if (string.IsNullOrEmpty(migrationItemId))
            {
                throw new ArgumentNullException("itemIdFormattedStr");
            }

            if (string.IsNullOrEmpty(migrationItemVersion))
            {
                throw new ArgumentNullException("historyIndexStr");
            }

            string[] splits = migrationItemVersion.Trim().Split(new string[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries);
            if (splits.Length != 2)
            {
                throw new ArgumentException(migrationItemVersion, "migrationItemVersion");
            }

            RecordDisplayName = migrationItemId;
            HistoryFieldName = splits[0];
            HistoryIndex = int.Parse(splits[1]);
        }
    }
}
