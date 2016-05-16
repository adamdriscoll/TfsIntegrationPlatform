// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//
// 20091101 TFS Integration Platform Custom Adapter Proof-of-Concept
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Rangers.TFS.Migration.PocAdapter.VC
{
    [ProviderDescription(m_adapterGuid, m_adapterName,m_adapterVersion)]
    public class PocVCAdapter : IProvider
    {
        private const string m_adapterGuid    = "{4C12E989-D2EB-4c83-BC06-7DE07B109344}";
        private const string m_adapterName    = "Poc VC Provider";
        private const string m_adapterVersion = "1.0.0.0";

        IAnalysisProvider    m_analysisProvider;
        IMigrationProvider   m_migrationProvider;
        IServerPathTranslationService m_serverPathTranslationProvider;

        #region IServiceProvider Members

        object IServiceProvider.GetService(Type serviceType)
        {
            TraceManager.TraceInformation("POC:Adapter:GetService - {0}", serviceType.ToString());
            if (serviceType == typeof(IAnalysisProvider))
            {
                if (m_analysisProvider == null)
                {
                    m_analysisProvider = new PocVCAnalysisProvider();
                }
                return m_analysisProvider;
            }
            else if (serviceType == typeof(IMigrationProvider))
            {
                if (m_migrationProvider == null)
                {
                    m_migrationProvider = new PocVCMigrationProvider();
                }
                return m_migrationProvider;
            }
            else if (serviceType == typeof(IServerPathTranslationService))
            {
                if (m_serverPathTranslationProvider == null)
                {
                    m_serverPathTranslationProvider = new PoCVCAdapterTranslation();
                }
                return m_serverPathTranslationProvider;
            }
            return new ArgumentException("Invalid Type :" + serviceType.Name);
        }

        #endregion

    }
}
