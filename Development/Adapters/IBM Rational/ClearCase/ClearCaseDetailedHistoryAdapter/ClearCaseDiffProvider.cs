// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{
    public class ClearCaseDiffProvider : IVCDiffProvider
    {
        IServiceContainer m_serviceContainer;
        ClearCaseServer m_clearCaseServer;
        ConfigurationService m_configurationService;
        CCConfiguration m_ccConfiguration;

        /// <summary>
        /// Initialize ClearCaseDiffProvider 
        /// </summary>
        public void InitializeServices(IServiceContainer diffServiceContainer)
        {
            m_serviceContainer = diffServiceContainer;
            m_configurationService = (ConfigurationService)diffServiceContainer.GetService(typeof(ConfigurationService));
            Debug.Assert(m_configurationService != null, "Configuration service is not initialized");
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


        #region IVCServerDiffProvider implementation
        public IVCDiffItem InitializeForDiff(string treeFilterSpecifier, string version)
        {
            string localPath = m_clearCaseServer.GetViewLocalPathFromServerPath(treeFilterSpecifier);
            if (Directory.Exists(localPath))
            {
                return new ClearCaseDiffItem(treeFilterSpecifier, null, VCItemType.Folder);
            }
            else if (File.Exists(localPath))
            {
                return new ClearCaseDiffItem(treeFilterSpecifier, localPath, VCItemType.File);
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<IVCDiffItem> GetFolderSubDiffItems(IVCDiffItem folderDiffItem)
        {
            List<IVCDiffItem> diffItems = new List<IVCDiffItem>();

            try
            {
                string directoryPath = m_clearCaseServer.GetViewLocalPathFromServerPath(folderDiffItem.ServerPath);
                if (Directory.Exists(directoryPath))
                {
                    foreach (string file in Directory.GetFiles(directoryPath))
                    {
                        string serverPath = m_clearCaseServer.GetServerPathFromViewLocalPath(file);
                        diffItems.Add(new ClearCaseDiffItem(serverPath, file, VCItemType.File));
                    }

                    foreach (string subDirectory in Directory.GetDirectories(directoryPath))
                    {
                        string serverPath = m_clearCaseServer.GetServerPathFromViewLocalPath(subDirectory);
                        if (ClearCasePath.Equals(ClearCasePath.GetFileName(serverPath), ClearCasePath.LostFoundFolder))
                        {
                            continue;
                        }
                        diffItems.Add(new ClearCaseDiffItem(serverPath, null, VCItemType.Folder));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new VersionControlDiffException(string.Format(CultureInfo.InvariantCulture,
                    CCResources.GetDiffItemsException, folderDiffItem.ServerPath, ex.Message), ex);
            }

            return diffItems;
        }

        public void Cleanup(IVCDiffItem rootDiffItem)
        {
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
            return (IServiceProvider)this;
        }
        #endregion

        #region IDisposable members
        public void Dispose()
        {
        }
        #endregion
    }

}
