// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.SubversionAdapter
{
    /// <summary>
    /// The <see cref="IProvider"/> implementation for the subversion version control adapter
    /// </summary>
    [ProviderCapabilityAttribute(SessionTypeEnum.VersionControl, "Subversion")]
    [ProviderDescription(m_adapterGuid, m_adapterName, m_version)]
    public class SubversionVCAdapter : IProvider
    {
        #region Private Const Members

        private const string m_adapterGuid = "BCC31CA2-534D-4054-9013-C1FEF67D5273";
        private const string m_adapterName = "SVN Migration VC Provider";
        private const string m_version = "1.0.0.0";

        IVCDiffProvider m_vcDiffProvider;


        #endregion

        #region Members

        private IAnalysisProvider m_analysisProvider;
        private IMigrationProvider m_migrationProvider;

        #endregion

        #region IProvider Implementation

        /// <summary>
        /// Returns the actual implementation for requested service interfaces
        /// </summary>
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IAnalysisProvider))
            {
                if (m_analysisProvider == null)
                {
                    m_analysisProvider = new SubversionVCAnalysisProvider();
                }
                return m_analysisProvider;
            }
            else if (serviceType == typeof(IVCDiffProvider))
            {
                if (m_vcDiffProvider == null)
                {
                    m_vcDiffProvider = new SubversionVCDiffProvider();
                }
                return m_vcDiffProvider;
            }
            if (serviceType == typeof(IMigrationProvider))
            {
                if (m_migrationProvider == null)
                {
                    m_migrationProvider = new SubversionVCMigrationProvider();
                }
                return m_migrationProvider;
            }


            return null;
        }

        #endregion
    }
}
