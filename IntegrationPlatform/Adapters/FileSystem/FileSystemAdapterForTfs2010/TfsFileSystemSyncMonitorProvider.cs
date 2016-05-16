// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsFileSystemAdapter
{
    public class TfsFileSystemSyncMonitorProvider : ISyncMonitorProvider
    {
        /// <summary>
        /// Initialize TfsFileSystemSyncMonitorProvider 
        /// </summary>
        public void InitializeServices(IServiceContainer syncMonitorServiceContainer)
        {
        }

        /// <summary>
        /// Perform the adapter-specific initialization
        /// </summary>
        public void InitializeClient(MigrationSource migrationSource)
        {
        }

        #region ISyncMonitorProvider implementation
        public ChangeSummary GetSummaryOfChangesSince(string lastProcessedChangeItemId, List<string> filterStrings)
        {
            ChangeSummary changeSummary = new ChangeSummary();
            changeSummary.ChangeCount = 0;
            changeSummary.FirstChangeModifiedTimeUtc = DateTime.MinValue;

            DateTime timeLastItemProcessedUtc = DateTime.MinValue;
            try
            {
                timeLastItemProcessedUtc = DateTime.Parse(lastProcessedChangeItemId);
                timeLastItemProcessedUtc = timeLastItemProcessedUtc.ToUniversalTime();
            }
            catch (FormatException)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture,
                    TfsFileSystemResources.InvalidChangeItemIdFormat, lastProcessedChangeItemId));
            }

            foreach (string filterPath in filterStrings)
            {
                if (Directory.Exists(filterPath))
                {
                    foreach (string path in Directory.GetDirectories(filterPath, "*", SearchOption.AllDirectories))
                    {
                        CheckFileSystemItemForChangeSinceTime(path, timeLastItemProcessedUtc, ref changeSummary);
                    }
                    CheckFileSystemItemForChangeSinceTime(filterPath, timeLastItemProcessedUtc, ref changeSummary);

                    foreach (string path in Directory.GetFiles(filterPath, "*", SearchOption.AllDirectories))
                    {
                        CheckFileSystemItemForChangeSinceTime(path, timeLastItemProcessedUtc, ref changeSummary);
                    }
                }
                else if (File.Exists(filterPath))
                {
                    CheckFileSystemItemForChangeSinceTime(filterPath, timeLastItemProcessedUtc, ref changeSummary);
                }
            }

            return changeSummary;
        }

        #endregion

        #region private methods

        private void CheckFileSystemItemForChangeSinceTime(string itemPath, DateTime comparisonTimeUtc, ref ChangeSummary changeSummary)
        {
            FileInfo fileInfo = new FileInfo(itemPath);
            DateTime fileModifiedTimeUtc = fileInfo.CreationTimeUtc > fileInfo.LastWriteTimeUtc ? fileInfo.CreationTimeUtc : fileInfo.LastWriteTimeUtc;
            if (fileModifiedTimeUtc > comparisonTimeUtc)
            {
                changeSummary.ChangeCount++;
                if (changeSummary.FirstChangeModifiedTimeUtc == DateTime.MinValue || fileModifiedTimeUtc < changeSummary.FirstChangeModifiedTimeUtc)
                {
                    changeSummary.FirstChangeModifiedTimeUtc = fileModifiedTimeUtc;
                }
            }
        }

        #endregion

        #region IServiceProvider implementation
        /// <summary>
        /// Gets the service object of the specified type. 
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public Object GetService(Type serviceType)
        {
            if (serviceType == typeof(ISyncMonitorProvider))
            {
                return this;
            }
            return null;
        }
        #endregion

        #region IDisposable members
        public void Dispose()
        {
        }
        #endregion
    }

}
