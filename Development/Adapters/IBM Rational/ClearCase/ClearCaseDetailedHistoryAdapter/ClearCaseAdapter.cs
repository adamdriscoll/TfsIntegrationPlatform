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

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{
    /// <summary>
    /// ClearCase 2003 adapter
    /// </summary>
    [ProviderCapabilityAttribute(SessionTypeEnum.VersionControl, "ClearCase")]
    [ProviderDescription(m_adapterGuid, m_adapterName, m_version)]
    public class ClearCaseAdapter : IProvider
    {
        private const string m_adapterGuid = "F2A6BA65-8ACB-4cd0-BE8F-B25887F94392";
        private const string m_adapterName = "ClearCase Detailed History Adapter";
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
                    m_analysisProvider = new ClearCaseAnalysisProvider();
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
                    m_migrationProvider = new ClearCaseMigrationProvider();
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
                    m_syncMonitorProvider = new ClearCaseSyncMonitorProvider();
                }
                return m_syncMonitorProvider;
            }
            return null;
        }
    }
}
