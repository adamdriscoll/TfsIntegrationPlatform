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

namespace Microsoft.TeamFoundation.Migration.TfsFileSystemAdapter
{
    public class TfsFileSystemDiffProvider : IVCDiffProvider
    {
        IServiceContainer m_serviceContainer;
        ConfigurationService m_configurationService;

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
        }

        #region IVCDiffProvider implementation
        public IVCDiffItem InitializeForDiff(string treeFilterSpecifier, string version)
        {
            if (Directory.Exists(treeFilterSpecifier))
            {
                return new TfsFileSystemDiffItem(treeFilterSpecifier, VCItemType.Folder);
            }
            else if (File.Exists(treeFilterSpecifier))
            {
                return new TfsFileSystemDiffItem(treeFilterSpecifier, VCItemType.File);
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
                foreach (string file in Directory.GetFiles(folderDiffItem.ServerPath))
                {
                    diffItems.Add(new TfsFileSystemDiffItem(file, VCItemType.File));
                }

                foreach (string directory in Directory.GetDirectories(folderDiffItem.ServerPath))
                {
                    diffItems.Add(new TfsFileSystemDiffItem(directory, VCItemType.Folder));
                }
            }
            catch (Exception ex)
            {
                throw new VersionControlDiffException(string.Format(CultureInfo.InvariantCulture,
                    TfsFileSystemResources.GetDiffItemsException, folderDiffItem.ServerPath, ex.Message), ex);
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
