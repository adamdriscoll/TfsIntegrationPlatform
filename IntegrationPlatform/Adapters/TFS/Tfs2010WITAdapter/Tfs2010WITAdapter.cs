// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    [ProviderCapabilityAttribute(SessionTypeEnum.WorkItemTracking, "TFS")]
    [ProviderDescription(VersionSpecificUtils.AdapterGuid, VersionSpecificUtils.AdapterName, VersionSpecificUtils.AdapterVersion)]
    public class Tfs2010WITAdapter : IProvider
    {
        protected IAnalysisProvider m_analysisProvider;
        protected IMigrationProvider m_migrationProvider;
        protected ILinkProvider m_linkProvider;
        protected IWITDiffProvider m_witDiffProvider;
        protected ISyncMonitorProvider m_syncMonitorProvider;

        public virtual object GetService(Type serviceType)
        {
            if (serviceType == typeof(IAnalysisProvider))
            {
                if (m_analysisProvider == null)
                {
                    m_analysisProvider = new Tfs2010WitAnalysisProvider();
                }
                return m_analysisProvider;
            }

            if (serviceType == typeof(IMigrationProvider))
            {
                if (m_migrationProvider == null)
                {
                    m_migrationProvider = new Tfs2010WitMigrationProvider();
                }
                return m_migrationProvider;
            }

            if (serviceType == typeof(ILinkProvider))
            {
                if (m_linkProvider == null)
                {
                    m_linkProvider = new Tfs2010LinkProvider();
                }
                return m_linkProvider;
            }

            if (serviceType == typeof(IWITDiffProvider))
            {
                if (m_witDiffProvider == null)
                {
                    m_witDiffProvider = new TfsWITDiffProvider();
                }
                return m_witDiffProvider;
            }

            if (serviceType == typeof(ISyncMonitorProvider))
            {
                if (m_syncMonitorProvider == null)
                {
                    m_syncMonitorProvider = new TfsWITSyncMonitorProvider();
                }
                return m_syncMonitorProvider;
            }

            return null;
        }
    }
}
