// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

using ClearCase;

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{
    public class ClearCaseSyncMonitorProvider : ISyncMonitorProvider
    {
        private ClearCaseServer m_clearCaseServer;
        private ConfigurationService m_configurationService;
        private CCConfiguration m_ccConfiguration;
        private HighWaterMark<DateTime> m_hwmDelta;
 
        /// <summary>
        /// Obtain references to services needed by this class
        /// </summary>
        public void InitializeServices(IServiceContainer syncMonitorServiceContainer)
        {
            m_configurationService = (ConfigurationService)syncMonitorServiceContainer.GetService(typeof(ConfigurationService));
            Debug.Assert(m_configurationService != null, "Configuration service is not initialized");

            m_hwmDelta = new HighWaterMark<DateTime>(Constants.HwmDelta);
            m_configurationService.RegisterHighWaterMarkWithSession(m_hwmDelta);
        }

        /// <summary>
        /// Perform the adapter-specific initialization
        /// </summary>
        public void InitializeClient(MigrationSource migrationSource)
        {
            initializeConfiguration();
            initializeClearCaseServer();
        }

        private void initializeConfiguration()
        {
            m_ccConfiguration = CCConfiguration.GetInstance(m_configurationService.MigrationSource);
        }

        private void initializeClearCaseServer()
        {
            m_clearCaseServer = ClearCaseServer.GetInstance(m_ccConfiguration, m_ccConfiguration.GetViewName("Analysis1"));
            m_clearCaseServer.Initialize();
        }

        #region ISyncMonitorProvider implementation
        public ChangeSummary GetSummaryOfChangesSince(string lastProcessedChangeItemId, List<string> filterStrings)
        {
            ChangeSummary changeSummary = new ChangeSummary();
            changeSummary.ChangeCount = 0;
            changeSummary.FirstChangeModifiedTimeUtc = DateTime.MinValue;

            // Now find all history records for changes since the High Water Mark
            m_hwmDelta.Reload();
            DateTime since = m_hwmDelta.Value;
            List<CCHistoryRecord> historyRecordList = m_clearCaseServer.GetHistoryRecords(m_configurationService.Filters, since, false);

            long lastProcessedEventId;
            try
            {
                lastProcessedEventId = long.Parse(lastProcessedChangeItemId);
            }
            catch(FormatException)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture,
                    CCResources.InvalidChangeItemIdFormat, lastProcessedChangeItemId));
            }

            CCHistoryRecord nextHistoryRecordToBeMigrated = null;

            // Of all of the history records since the HWM, find the oldest one (based on EventId which increases over time)
            foreach (CCHistoryRecord historyRecord in historyRecordList)
            {
                // We only want to count history records with OperationTypes of Checkin and Rmname for the backlog, 
                // not individual element operation or other operations such as mklabel that are not processed by the ClearCaseAnalysisProvider
                // *** NOTE: If the ClearCaseAnalysisProvider is changed to process other OperationTypes, this code needs to change as well ***
                // TODO: Should we create a common static list of processed OperationTypes so that there is just one place to update?
                if (historyRecord.EventId > lastProcessedEventId && 
                    (historyRecord.OperationType == OperationType.Checkin || historyRecord.OperationType == OperationType.Rmname))
                {
                    // Don't count DirectoryVersion check-in records in the backlog as these may produce false positives in the backlog
                    if (!string.Equals(historyRecord.OperationDescription, OperationDescription.DirectoryVersion, StringComparison.Ordinal))
                    {
                        changeSummary.ChangeCount++;
                        if (nextHistoryRecordToBeMigrated == null || historyRecord.EventId < nextHistoryRecordToBeMigrated.EventId)
                        {
                            nextHistoryRecordToBeMigrated = historyRecord;
                        }
                        /* Uncomment for debugging
                        Console.WriteLine(String.Format(CultureInfo.InvariantCulture,
                            "CCSyncMonitorProvider including backlog item for history record: EventId: {0}, OperationType: {1}, AbsoluteVobPath: {2}, VersionTime: {3}",
                            historyRecord.EventId, historyRecord.OperationType, historyRecord.AbsoluteVobPath, historyRecord.VersionTime));
                         */
                    }
                }
            }

            if (nextHistoryRecordToBeMigrated != null)
            {
                changeSummary.FirstChangeModifiedTimeUtc = nextHistoryRecordToBeMigrated.VersionTime.ToUniversalTime();
            }

            return changeSummary;
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
            return this as IServiceProvider;
        }
        #endregion

        #region IDisposable members
        public void Dispose()
        {
        }
        #endregion
    }

}
