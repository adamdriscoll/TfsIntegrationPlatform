// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2008VCAdapter
{
    public class TfsVCDiffProvider : IVCDiffProvider
    {
        // TODO: Make configurable as undocumented setting
        private const int c_treeDepthToStartFullRecursion = 2;

        private IServiceContainer m_serviceContainer;
        private ConfigurationService m_configurationService;
        private VersionControlServer m_tfsClient;
        private VersionSpec m_versionSpec;

        // The string key is the folder's ServerPath
        private Dictionary<string, TfsVCDiffItem> m_cachedFolderItems;

        /// <summary>
        /// Obtain references to services needed by this class
        /// </summary>
        public void InitializeServices(IServiceContainer diffServiceContainer)
        {
            m_serviceContainer = diffServiceContainer;
            m_configurationService = (ConfigurationService)m_serviceContainer.GetService(typeof(ConfigurationService));
            Debug.Assert(m_configurationService != null, "Configuration service is not initialized");           
        }

        /// <summary>
        /// Perform the adapter-specific initialization
        /// </summary>
        public virtual void InitializeClient(MigrationSource migrationSource)
        {
            initializeTfsClient();
            m_cachedFolderItems = new Dictionary<string, TfsVCDiffItem>(StringComparer.InvariantCultureIgnoreCase);
        }

        private void initializeTfsClient()
        {
            TeamFoundationServer tfsServer = TeamFoundationServerFactory.GetServer(
                m_configurationService.ServerUrl);
            m_tfsClient = (VersionControlServer)tfsServer.GetService(typeof(VersionControlServer));
            m_tfsClient.NonFatalError += new ExceptionEventHandler(NonFatalError);
        }

        private void NonFatalError(object sender, ExceptionEventArgs e)
        {
            if (e.Exception != null)
            {
                processNonFatalErrorException(e.Exception);
            }

            if (e.Failure != null)
            {
                Trace.TraceWarning(e.Failure.Message);
            }
        }

        /// <summary>
        /// Process the exceptions in returned from nonfatalerror
        /// </summary>
        /// <param name="exception"></param>
        private void processNonFatalErrorException(Exception exception)
        {
            // For now we always throw the exception, but leaving this method here in case that changes
            throw exception;
        }

        #region IVCDiffProvider implementation
        public IVCDiffItem InitializeForDiff(string treeFilterSpecifier, string version)
        {
            string tfsTreeRoot = VersionControlPath.GetFullPath(treeFilterSpecifier);

            if (string.IsNullOrEmpty(version))
            {
                m_versionSpec = VersionSpec.Latest;
            }
            else
            {
                m_versionSpec = VersionSpec.ParseSingleSpec(version, null);
            }

            TfsVCDiffItem rootDiffItem = null;

            List<IVCDiffItem> subDiffItems = new List<IVCDiffItem>();

            ItemSet tfsItemSet = m_tfsClient.GetItems(tfsTreeRoot, m_versionSpec, RecursionType.OneLevel);
            foreach (Item item in tfsItemSet.Items)
            {
                if (string.Equals(item.ServerItem, tfsTreeRoot, StringComparison.OrdinalIgnoreCase))
                {
                    rootDiffItem = new TfsVCDiffItem(item, 0);
                }
                else
                {
                    TfsVCDiffItem diffItem = new TfsVCDiffItem(item, 1);
                    subDiffItems.Add(diffItem);
                }
            }

            if (rootDiffItem == null)
            {
                return null;
            }

            rootDiffItem.SubItems.AddRange(subDiffItems);
            m_cachedFolderItems.Add(rootDiffItem.ServerPath, rootDiffItem);

            return rootDiffItem;
        }

        public IEnumerable<IVCDiffItem> GetFolderSubDiffItems(
            IVCDiffItem folderDiffItem)
        {
            TfsVCDiffItem tfsFolderDiffItem = folderDiffItem as TfsVCDiffItem;
            if (tfsFolderDiffItem == null)
            {
                throw new ArgumentException("folderDiffItem");
            }
            if (!(folderDiffItem.VCItemType == VCItemType.Folder))
            {
                throw new ArgumentException("folderDiffItem.VCItemType != VCItemType.Folder");
            }

            TfsVCDiffItem cachedDiffItem;
            if (m_cachedFolderItems.TryGetValue(folderDiffItem.ServerPath, out cachedDiffItem))
            {
                List<IVCDiffItem> subItems = cachedDiffItem.SubItems;
                cachedDiffItem.SubItems = null;
                m_cachedFolderItems.Remove(folderDiffItem.ServerPath);
                return subItems;
            }
            else
            {
                // All of the TfsDiffItem objects are placed in separate list on the first pass so that all of the folders
                // are in m_cachedFolderItems before the loop below where ProcessSubItem is called for each TfsDiffItem
                // This means that we make no assumptions about the TFS GetItems() method returning the folders and files in
                // any particular order.

                List<TfsVCDiffItem> tfsDiffItems = new List<TfsVCDiffItem>();
                RecursionType recursionType = tfsFolderDiffItem.TreeLevel < c_treeDepthToStartFullRecursion ? RecursionType.OneLevel : RecursionType.Full;
                // Ignore deleted items in the diff operation
                ItemSet tfsItemSet = m_tfsClient.GetItems(tfsFolderDiffItem.ServerPath, m_versionSpec, recursionType, DeletedState.NonDeleted, ItemType.Any);
                foreach (Item item in tfsItemSet.Items)
                {
                    TfsVCDiffItem diffItem = new TfsVCDiffItem(item, tfsFolderDiffItem.TreeLevel+1);

                    if (recursionType == RecursionType.Full && item.ItemType == ItemType.Folder)
                    {
                        // Item is a folder; add it to the cache of folder diff items, keyed by its ServerPath
                        if (!m_cachedFolderItems.ContainsKey(diffItem.ServerPath))
                        {
                            m_cachedFolderItems.Add(diffItem.ServerPath, diffItem);
                        }
                    }

                    // Don't return the item passed in
                    if (!string.Equals(item.ServerItem, tfsFolderDiffItem.ServerPath, StringComparison.OrdinalIgnoreCase))
                    {
                        tfsDiffItems.Add(diffItem);
                    }
                }
                tfsItemSet = null;

                List<IVCDiffItem> directSubItems = new List<IVCDiffItem>();

                // Attach the diff items to the appropriate parent folder, and include them in the directSubItems list if appropriate
                foreach (TfsVCDiffItem tfsDiffItem in tfsDiffItems)
                {
                    ProcessSubItem(tfsDiffItem, tfsFolderDiffItem, directSubItems, recursionType == RecursionType.Full);
                }

                return directSubItems;
            }
        }

        // This common method used for both folders and files places the item in the SubItems list
        // of the appropriate parent folder, and also adds the item to the directSubItems list to be
        // returned on this call if and only if item is a direct child of the folderDiffItem argument
        // of GetFolderSubDiffItems().
        private void ProcessSubItem(
            TfsVCDiffItem tfsDiffItem, 
            TfsVCDiffItem topFolderDiffItem, 
            List<IVCDiffItem> directSubItems,
            bool addSubItemToContainingFolderItem)
        {
            string containingFolderPath = VersionControlPath.GetFolderName(tfsDiffItem.ServerPath);

            if (addSubItemToContainingFolderItem)
            {
                TfsVCDiffItem containingFolderItem;
                if (!m_cachedFolderItems.TryGetValue(containingFolderPath, out containingFolderItem))
                {
                    TraceManager.TraceWarning(String.Format(CultureInfo.InvariantCulture, TfsVCAdapterResource.ContainingFolderNotFound,
                        tfsDiffItem.ServerPath));
                    return;
                }

                containingFolderItem.SubItems.Add(tfsDiffItem);
            }

            if (string.Equals(topFolderDiffItem.ServerPath, containingFolderPath, StringComparison.InvariantCultureIgnoreCase))
            {
                directSubItems.Add(tfsDiffItem);
            }
        }

        public void Cleanup(IVCDiffItem rootDiffItem)
        {
            m_cachedFolderItems.Clear();
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
