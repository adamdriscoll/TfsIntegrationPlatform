// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Tfs2010VCAdapter
{
    [ProviderCapabilityAttribute(SessionTypeEnum.VersionControl, "TFS")]
    [ProviderDescription(m_adapterGuid, m_adapterName, m_version)]
    public class TfsVCAdapter : IProvider    
    {
        private const string m_adapterGuid = "FEBC091F-82A2-449e-AED8-133E5896C47A";
        private const string m_adapterName = "TFS 2010 Migration VC Provider";
        private const string m_version = "1.0.0.0";

        IAnalysisProvider m_analysisProvider;
        IMigrationProvider m_migrationProvider;
        IVCDiffProvider m_diffProvider;
        IServerPathTranslationService m_serverPathTranslationProvider;
        ISyncMonitorProvider m_syncMonitorProvider;

        public object GetService(Type serviceType)
        {
            if ( serviceType == typeof(IAnalysisProvider))
            {
                if (m_analysisProvider == null)
                {
                    m_analysisProvider = new TfsVCAnalysisProvider();
                }
                return m_analysisProvider;
            }
            else if (serviceType == typeof(IMigrationProvider))
            {
                if (m_migrationProvider == null)
                {
                    m_migrationProvider = new TfsVCMigrationProvider();
                }
                return m_migrationProvider;
            }
            else if (serviceType == typeof(IVCDiffProvider))
            {
                if (m_diffProvider == null)
                {
                    m_diffProvider = new TfsVCDiffProvider();
                }
                return m_diffProvider;
            }
            else if (serviceType == typeof(IServerPathTranslationService))
            {
                if (m_serverPathTranslationProvider == null)
                {
                    m_serverPathTranslationProvider = new TFSVCServerPathTranslationService();
                }
                return m_serverPathTranslationProvider;
            }
            else if (serviceType == typeof(ISyncMonitorProvider))
            {
                if (m_syncMonitorProvider == null)
                {
                    m_syncMonitorProvider = new TfsVCSyncMonitorProvider();
                }
                return m_syncMonitorProvider;
            }

            return null;
        }
    }
}
