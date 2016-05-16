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

namespace Microsoft.TeamFoundation.Migration.TfsFileSystemAdapter
{
    /// <summary>
    /// Tfs file system adapter
    /// </summary>
    [ProviderCapabilityAttribute(SessionTypeEnum.VersionControl, "FileSystem")]
    // Bind to the TFS 2010 shell adapter so that we don't need to create
    // a separate shell adapter given that it has no explicit TFS OM dependency.
    [ProviderDescription(VersionSpecificUtils.AdapterGuid, VersionSpecificUtils.AdapterName, VersionSpecificUtils.AdapterVersion, "43B0D301-9B38-4caa-A754-61E854A71C78")]
    public class TfsFileSystemAdapter : IProvider
    {
        IAnalysisProvider m_analysisProvider;
        IMigrationProvider m_migrationProvider;
        IVCDiffProvider m_diffProvider;
        ISyncMonitorProvider m_syncMonitorProvider;
        IServerPathTranslationService m_serverPathTranslationProvider;
        IMigrationItemSerializer m_migrationItemSerializer;

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
                    m_analysisProvider = new TfsFileSystemAnalysisProvider();
                }
                return m_analysisProvider;
            }
            else if (serviceType == typeof(IServerPathTranslationService))
            {
                if (m_serverPathTranslationProvider == null)
                {
                    m_serverPathTranslationProvider = new TfsFileSystemTranslationService();
                }
                return m_serverPathTranslationProvider;
            }
            else if (serviceType == typeof(IMigrationProvider))
            {
                if (m_migrationProvider == null)
                {
                    m_migrationProvider = new TfsFileSystemMigrationProvider();
                }
                return m_migrationProvider;
            }
            else if (serviceType == typeof(IVCDiffProvider))
            {
                if (m_diffProvider == null)
                {
                    m_diffProvider = new TfsFileSystemDiffProvider();
                }
                return m_diffProvider;
            }
            else if (serviceType == typeof(ISyncMonitorProvider))
            {
                if (m_syncMonitorProvider == null)
                {
                    m_syncMonitorProvider = new TfsFileSystemSyncMonitorProvider();
                }
                return m_syncMonitorProvider;
            }
            else if (serviceType == typeof(IMigrationItemSerializer))
            {
                if (m_migrationItemSerializer == null)
                {
                    m_migrationItemSerializer = new TfsFileSystemMigrationItemSerializer();
                }
                return m_migrationItemSerializer;
            }
            return null;
        }
    }
}
