// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.UserIdLookup;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    [ProviderCapabilityAttribute(SessionTypeEnum.WorkItemTracking, "ClearQuest")]
    [ProviderDescription(m_adapterGuid, m_adapterName, m_version)]
    public class ClearQuestAdapter : IProvider
    {
        const string m_adapterGuid = "{D9637401-7385-4643-9C64-31585D77ED16}";
        const string m_adapterName = "ClearQuest Adapter";
        const string m_version = "1.0.0.0";

        protected IAnalysisProvider m_analysisProvider;
        protected IMigrationProvider m_migrationProvider;
        protected ILinkProvider m_linkProvider;
        protected IWITDiffProvider m_witDiffProvider;
        protected ISyncMonitorProvider m_syncMonitorProvider;
        protected IAddin m_userIdLookupAddin;

        #region IServiceProvider Members

        /// <summary>
        /// Gets the providers supported by this Adapter.
        /// </summary>
        /// <param name="serviceType">IAnalysisProvider, IMigrationProvider, or ILinkProvider</param>
        /// <returns></returns>
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IAnalysisProvider))
            {
                if (m_analysisProvider == null)
                {
                    m_analysisProvider = new ClearQuestAnalysisProvider();
                }
                return m_analysisProvider;
            }
            else if (serviceType == typeof(IMigrationProvider))
            {
                if (m_migrationProvider == null)
                {
                    m_migrationProvider = new ClearQuestMigrationProvider();
                }
                return m_migrationProvider;
            }
            else if (serviceType == typeof(ILinkProvider))
            {
                if (m_linkProvider == null)
                {
                    m_linkProvider = new ClearQuestLinkProvider();
                }
                return m_linkProvider;
            }
            else if (serviceType == typeof(IWITDiffProvider))
            {
                if (m_witDiffProvider == null)
                {
                    m_witDiffProvider = new ClearQuestDiffProvider();
                }
                return m_witDiffProvider;
            }
            else if (serviceType == typeof(ISyncMonitorProvider))
            {
                if (m_syncMonitorProvider == null)
                {
                    m_syncMonitorProvider = new ClearQuestSyncMonitorProvider();
                }
                return m_syncMonitorProvider;
            }
            else if (serviceType == typeof(IAddin))
            {
                if (m_userIdLookupAddin == null)
                {
                    m_userIdLookupAddin = new ClearQuestUserIdLookupAddin();
                }
                return m_userIdLookupAddin;
            }
            else
            {
                return null;
            }
        }

        #endregion
    }
}
