// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter
{
    [ProviderCapabilityAttribute(SessionTypeEnum.WorkItemTracking, "TFS")]
    [ProviderDescription(m_adapterGuid, m_adapterName, m_version)]
    public class TfsWITAdapter : IProvider
    {

        const string m_adapterGuid = "{663A8B36-7852-4750-87FC-D189B0640FC1}";
        const string m_adapterName = "TFS 2008 Migration WIT Provider";
        const string m_version = "1.0.0.0";

        protected IAnalysisProvider m_analysisProvider;
        protected IMigrationProvider m_migrationProvider;
        protected ILinkProvider m_linkProvider;
        protected IWITDiffProvider m_witDiffProvider;
        protected ISyncMonitorProvider m_syncMonitorProvider;

        /// <summary>
        /// Gets the analysis or migration provider supported by this Adapter.
        /// </summary>
        /// <param name="serviceType">IAnalysisProvider or IMigrationProvider</param>
        /// <returns></returns>
        public virtual object GetService(Type serviceType)
        {
            if (serviceType == typeof(IAnalysisProvider))
            {
                if (m_analysisProvider == null)
                {
                    m_analysisProvider = new TfsWITAnalysisProvider();
                }
                return m_analysisProvider;
            }
            
            if (serviceType == typeof(IMigrationProvider))
            {
                if (m_migrationProvider == null)
                {
                    m_migrationProvider = new TfsWITMigrationProvider();
                }
                return m_migrationProvider;
            }

            if (serviceType == typeof(ILinkProvider))
            {
                if (m_linkProvider == null)
                {
                    m_linkProvider = new Tfs2008LinkProvider();
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
