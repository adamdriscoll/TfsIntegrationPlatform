// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter;
using System.IO;
using System.Diagnostics;
using Microsoft.TeamFoundation.Migration.TfsFileSystemAdapter;

namespace Microsoft.TeamFoundation.Migration.ClearCaseSelectedHistoryAdapter
{
    /// <summary>
    /// ClearCase selected history adapter between ClearCase and TFS
    /// </summary>
    [ProviderCapabilityAttribute(SessionTypeEnum.VersionControl, "ClearCase")]
    [ProviderDescription(m_adapterGuid, m_adapterName, m_version, "f2a6ba65-8acb-4cd0-be8f-b25887f94392")]
    public class ClearCaseSelectedHistoryAdapter : IProvider
    {
        private const string m_adapterGuid = "F65A4623-3856-4507-B5E9-AD28811FD37E";
        private const string m_adapterName = "ClearCase Selected History Adapter for Tfs 2008";
        private const string m_version = "1.0.0.0";

        IAnalysisProvider m_analysisProvider;
        IMigrationProvider m_migrationProvider;
        IVCDiffProvider m_diffProvider;
        ISyncMonitorProvider m_syncMonitorProvider;
        IServerPathTranslationService m_serverPathTranslationProvider;

        /// <summary>
        /// Return a service based on serviceType. 
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IAnalysisProvider))
            {
                if (m_analysisProvider == null)
                {
                    m_analysisProvider = new ClearCaseSelectedHistoryAnalysisProvider(true);
                }
                return m_analysisProvider;
            }
            else if (serviceType == typeof(IServerPathTranslationService))
            {
                if (m_serverPathTranslationProvider == null)
                {
                    m_serverPathTranslationProvider = new CCTranslationService();
                }
                return m_serverPathTranslationProvider;
            }
            else if (serviceType == typeof(IMigrationProvider))
            {
                if (m_migrationProvider == null)
                {
                    m_migrationProvider = new ClearCaseMigrationProvider(new TfsFileSystemMigrationItemSerializer(), "HWMLastSyncedTfsChangeset");
                }
                return m_migrationProvider;
            }
            else if (serviceType == typeof(IVCDiffProvider))
            {
                if (m_diffProvider == null)
                {
                    m_diffProvider = new ClearCaseDiffProvider();
                }
                return m_diffProvider;
            }
            else if (serviceType == typeof(ISyncMonitorProvider))
            {
                if (m_syncMonitorProvider == null)
                {
                    m_syncMonitorProvider = (ISyncMonitorProvider)m_analysisProvider.GetService(typeof(ISyncMonitorProvider));
                }
                return m_syncMonitorProvider;
            }
            return null;
        }
    }
}
