// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.SubversionAdapter.Interop.Subversion.ObjectModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.SubversionAdapter.SubversionOM;
using Microsoft.TeamFoundation.Migration.SubversionAdapter.Interop.Subversion;

namespace Microsoft.TeamFoundation.Migration.SubversionAdapter
{
    class SubversionVCDiffProvider : IVCDiffProvider
    {
        IServiceContainer m_serviceContainer;
        ConfigurationService m_configurationService;
        ConfigurationManager m_configurationManager;
        Repository m_repository;
        int m_revision;

        /// <summary>
        /// Initialize ClearCaseDiffProvider 
        /// </summary>
        public void InitializeServices(IServiceContainer diffServiceContainer)
        {
            m_serviceContainer = diffServiceContainer;
            m_configurationService = (ConfigurationService)diffServiceContainer.GetService(typeof(ConfigurationService));
            if (m_configurationService == null)
            {
                throw new MigrationException("Configuration service is not initialized");
            }
            m_configurationManager = new ConfigurationManager(m_configurationService);
        }

        /// <summary>
        /// Perform the adapter-specific initialization
        /// </summary>
        public void InitializeClient(MigrationSource migrationSource)
        {
            initializeSubversionClient();
        }

        /// <summary>
        /// Establishes a connection to the configured subversion server
        /// </summary>
        private void initializeSubversionClient()
        {
            m_repository = Repository.GetRepository(m_configurationManager.RepositoryUri, m_configurationManager.Username, m_configurationManager.Password);
            m_repository.EnsureAuthenticated();
        }


        #region IVCServerDiffProvider implementation
        public IVCDiffItem InitializeForDiff(string treeFilterSpecifier, string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                m_revision = m_repository.GetLatestRevisionNumber();
            }

            if (m_revision != 0 || int.TryParse(version, out m_revision))
            {
                foreach (Item item in m_repository.GetItems(PathUtils.Combine(m_configurationManager.RepositoryUri,treeFilterSpecifier), m_revision, Depth.Empty))
                {
                    return new SubversionVCDiffItem(item, m_revision);
                }
            }
            else
            {
                throw new MigrationException(string.Format("{0} is not a valid revision number.", version));
            }
            return null;
        }

        public IEnumerable<IVCDiffItem> GetFolderSubDiffItems(IVCDiffItem folderDiffItem)
        {
            foreach (Item subItem in m_repository.GetItems(PathUtils.Combine(m_configurationManager.RepositoryUri, folderDiffItem.ServerPath), m_revision, Depth.Immediates))
            {
                SubversionVCDiffItem diffItem = new SubversionVCDiffItem(subItem, m_revision);

                // Don't return the item itself
                if (string.Equals(diffItem.ServerPath, folderDiffItem.ServerPath))
                {
                    continue;
                }
                yield return diffItem;
            }

            yield break;
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

