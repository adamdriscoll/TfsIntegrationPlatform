// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ServerDiff
{
    internal class VCDiffComparer : IDiffComparer
    {
        private ServerDiffEngine m_serverDiffEngine;
        private VCTranslationService m_vcTranslationService;

        public VCDiffComparer(ServerDiffEngine serverDiffEngine)
        {
            m_serverDiffEngine = serverDiffEngine;
        }

        private VCTranslationService TranslationService
        {
            get
            {
                if (m_vcTranslationService == null)
                {
                    m_vcTranslationService = new VCTranslationService(m_serverDiffEngine.Session,
                                                                      m_serverDiffEngine.LeftDiffProviderGuid,
                                                                      m_serverDiffEngine.RightDiffProviderGuid,
                                                                      m_serverDiffEngine.LeftProvider,
                                                                      m_serverDiffEngine.RightProvider,
                                                                      new UserIdentityLookupService(m_serverDiffEngine.Config, m_serverDiffEngine.AddinManagementService));
                }
                return m_vcTranslationService;
            }
        }

        private IVCDiffProvider SourceVCDiffProvider
        {
            get { return m_serverDiffEngine.SourceDiffProvider as IVCDiffProvider; }
        }

        private IVCDiffProvider TargetVCDiffProvider
        {
            get { return m_serverDiffEngine.TargetDiffProvider as IVCDiffProvider; }
        }

        public bool VerifyContentsMatch(string sourceVersion, string targetVersion)
        {
            Stopwatch stopWatch = Stopwatch.StartNew();
            Trace.WriteLine(String.Format(CultureInfo.InvariantCulture,
                "Entering VCDiffComparer.VerifyContentsMatch: sourceVersion: {0}, targetVersion: {1}",
                sourceVersion == null ? "latest" : sourceVersion,
                targetVersion == null ? "latest" : targetVersion));

            List<IVCDiffItem> sourceRootDiffItems = new List<IVCDiffItem>();
            List<IVCDiffItem> targetRootDiffItems = new List<IVCDiffItem>();

            bool contentMatch = true;
            int foldersProcessed = 0;
            int filesProcessed = 0;
            try
            {
                Stack<IVCDiffItem> sourceFolders = new Stack<IVCDiffItem>();
                Queue<IVCDiffItem> sourceFiles = new Queue<IVCDiffItem>();

                Dictionary<string, IVCDiffItem> targetFolders = new Dictionary<string, IVCDiffItem>(StringComparer.InvariantCultureIgnoreCase);
                Dictionary<string, IVCDiffItem> targetFiles = new Dictionary<string, IVCDiffItem>(StringComparer.InvariantCultureIgnoreCase);

                List<string> sourceCloakList = new List<string>();
                List<string> targetCloakList = new List<string>();

                foreach (var filterPair in m_serverDiffEngine.Session.Filters.FilterPair)
                {
                    if (filterPair.Neglect)
                    {
                        // TODO: Need to deal with translating paths for cloaked filter pairs into the canonical relative form used by IVCDiffItem.Path !!!
                        sourceCloakList.Add(GetSourceFilterString(filterPair));
                        targetCloakList.Add(GetTargetFilterString(filterPair));
                    }
                }

                foreach (var filterPair in m_serverDiffEngine.Session.Filters.FilterPair)
                {
                    if (!filterPair.Neglect)
                    {
                        sourceFolders.Clear();
                        sourceFiles.Clear();
                        targetFolders.Clear();
                        targetFiles.Clear();

                        // Always go 1 level down to avoid too large query
                        string sourceFilterString = GetSourceFilterString(filterPair);
                        IVCDiffItem sourceDiffRootItem = SourceVCDiffProvider.InitializeForDiff(sourceFilterString, sourceVersion);
                        if (sourceDiffRootItem != null)
                        {
                            if (sourceDiffRootItem.VCItemType == VCItemType.Folder)
                            {
                                sourceFolders.Push(sourceDiffRootItem);
                            }
                            else
                            {
                                sourceFiles.Enqueue(sourceDiffRootItem);
                            }
                            sourceRootDiffItems.Add(sourceDiffRootItem);
                        }

                        string targetFilterString = GetTargetFilterString(filterPair);
                        IVCDiffItem targetDiffRootItem = TargetVCDiffProvider.InitializeForDiff(targetFilterString, targetVersion);
                        if (targetDiffRootItem != null)
                        {
                            if (targetDiffRootItem.VCItemType == VCItemType.Folder)
                            {
                                targetFolders.Add(targetDiffRootItem.ServerPath, targetDiffRootItem);
                            }
                            else
                            {
                                targetFiles.Add(targetDiffRootItem.ServerPath, targetDiffRootItem);
                            }
                            targetRootDiffItems.Add(targetDiffRootItem);
                        }

                        while (sourceFiles.Count > 0 | sourceFolders.Count > 0 | targetFiles.Count > 0 | targetFolders.Count > 0)
                        {
                            while (sourceFiles.Count > 0)
                            {
                                IVCDiffItem sourceItem = sourceFiles.Dequeue();
                                if (sourceItem.VCItemType != VCItemType.File)
                                {
                                    Debug.Fail("VerifyContentMatch: Found IVCDiffItem that is not type File in the sourceFiles queue");
                                    continue;
                                }
                                string targetPath = TranslationService.GetMappedPath(sourceItem.ServerPath, new Guid(m_serverDiffEngine.Session.LeftMigrationSourceUniqueId));

                                if (targetPath == null || !targetFiles.ContainsKey(targetPath))
                                {
                                    m_serverDiffEngine.LogError(string.Format(CultureInfo.InvariantCulture, ServerDiffResources.ItemOnlyFoundOnSource, sourceItem.ServerPath));
                                    contentMatch = false;
                                }
                                else
                                {
                                    bool fileContentsMatch = ContentsMatch(sourceItem.HashValue, targetFiles[targetPath].HashValue);
                                    if (fileContentsMatch)
                                    {
                                        m_serverDiffEngine.LogVerbose(String.Format(CultureInfo.InvariantCulture, ServerDiffResources.FilesMatch,
                                            sourceItem.ServerPath, targetPath));
                                    }
                                    else
                                    {
                                        contentMatch = false;
                                        m_serverDiffEngine.LogError(string.Format(CultureInfo.InvariantCulture, ServerDiffResources.ItemContentDoesNotMatch, sourceItem.ServerPath));
                                    }
                                    targetFiles.Remove(targetPath);
                                }
                                filesProcessed++;
                            }

                            foreach (KeyValuePair<string, IVCDiffItem> remainingFile in targetFiles)
                            {
                                m_serverDiffEngine.LogError(string.Format(CultureInfo.InvariantCulture, ServerDiffResources.ItemOnlyFoundOnTarget, remainingFile.Key));
                                contentMatch = false;
                            }

                            Debug.Assert(sourceFiles.Count == 0);
                            targetFiles.Clear();

                            if (sourceFolders.Count > 0)
                            {
                                IVCDiffItem sourceFolder = sourceFolders.Pop();
                                m_serverDiffEngine.LogVerbose(string.Format(CultureInfo.InvariantCulture, ServerDiffResources.ProcessingSourceFolder, sourceFolder.ServerPath));

                                string targetFolder = TranslationService.GetMappedPath(sourceFolder.ServerPath, new Guid(m_serverDiffEngine.Session.LeftMigrationSourceUniqueId));

                                if (targetFolder != null && targetFolders.ContainsKey(targetFolder))
                                {
                                    // Always go 1 level down to avoid too large query
                                    IEnumerable<IVCDiffItem> sourceDiffItems = SourceVCDiffProvider.GetFolderSubDiffItems(sourceFolder);
                                    foreach (IVCDiffItem diffItem in sourceDiffItems)
                                    {
                                        if (isCloaked(SourceVCDiffProvider, diffItem, sourceCloakList))
                                        {
                                            continue;
                                        }
                                        if (diffItem.VCItemType == VCItemType.File)
                                        {
                                            sourceFiles.Enqueue(diffItem);
                                        }
                                        else
                                        {
                                            sourceFolders.Push(diffItem);
                                        }
                                    }

                                    IEnumerable<IVCDiffItem> targetDiffItems = TargetVCDiffProvider.GetFolderSubDiffItems(targetFolders[targetFolder]);
                                    foreach (IVCDiffItem diffItem in targetDiffItems)
                                    {
                                        if (isCloaked(TargetVCDiffProvider, diffItem, targetCloakList))
                                        {
                                            continue;
                                        }
                                        if (diffItem.VCItemType == VCItemType.File)
                                        {
                                            if (!targetFiles.ContainsKey(diffItem.ServerPath))
                                            {
                                                targetFiles.Add(diffItem.ServerPath, diffItem);
                                            }
                                        }
                                        else
                                        {
                                            if (!targetFolders.ContainsKey(diffItem.ServerPath))
                                            {
                                                targetFolders.Add(diffItem.ServerPath, diffItem);
                                            }
                                        }
                                    }
                                    targetFolders.Remove(targetFolder);
                                    if (++foldersProcessed % 100 == 0)
                                    {
                                        m_serverDiffEngine.LogInfo(String.Format(CultureInfo.InvariantCulture, "Processed {0} source folders containing {1} files ...",
                                            foldersProcessed, filesProcessed));
                                    }                                    
                                }
                                else
                                {
                                    m_serverDiffEngine.LogError(string.Format(CultureInfo.InvariantCulture, ServerDiffResources.ItemOnlyFoundOnSource, sourceFolder.ServerPath));
                                    contentMatch = false;
                                }
                            }
                            else
                            {
                                foreach (KeyValuePair<string, IVCDiffItem> remainingFolder in targetFolders)
                                {
                                    m_serverDiffEngine.LogError(string.Format(CultureInfo.InvariantCulture, ServerDiffResources.ItemOnlyFoundOnTarget, remainingFolder.Key));
                                    contentMatch = false;
                                }
                                targetFolders.Clear();

                            }
                        }
                    }
                }

            }
            catch (Exception e)
            {
                Trace.WriteLine("Exception occurred while verifying contents match: " + e.ToString());
                throw;
            }
            finally
            {
                if (SourceVCDiffProvider != null)
                {
                    foreach (IVCDiffItem sourceRootDiffItem in sourceRootDiffItems)
                    {
                        SourceVCDiffProvider.Cleanup(sourceRootDiffItem);
                    }
                }
                if (TargetVCDiffProvider != null)
                {
                    foreach (IVCDiffItem targetRootDiffItem in targetRootDiffItems)
                    {
                        TargetVCDiffProvider.Cleanup(targetRootDiffItem);
                    }
                }
                stopWatch.Stop();
                m_serverDiffEngine.LogInfo(String.Format(CultureInfo.InvariantCulture, ServerDiffResources.VCServerDiffTimeToRun,
                    stopWatch.Elapsed.TotalSeconds));
                m_serverDiffEngine.LogInfo(String.Format(CultureInfo.InvariantCulture, "Processed a total of {0} source folders containing {1} files",
                    foldersProcessed, filesProcessed));
            }

            Trace.WriteLine("VCDiffComparer.VerifyContentsMatch result: " + contentMatch);

            return contentMatch;
        }

        /// <summary>
        /// Check to see if an item is a sub item in cloak list.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cloakList"></param>
        /// <returns></returns>
        private bool isCloaked(IVCDiffProvider diffProvider, IVCDiffItem item, List<string> cloakList)
        {
            foreach (string cloakedPath in cloakList)
            {
                if (item.IsSubItemOf(cloakedPath))
                {
                    return true;
                }
            }
            return false;
        }

        #region private methods
        /// <summary>
        ///  Compare the hash values for two files in the tfs servers
        /// </summary>
        /// <param name="targetMd5Sum">One files hashvalue</param>
        /// <param name="sourceMd5Sum">The other files hash value</param>
        /// <returns>true if the hash values are the same for both files</returns>
        private static bool ContentsMatch(byte[] targetMd5Sum, byte[] sourceMd5Sum)
        {
            if (targetMd5Sum.Length != sourceMd5Sum.Length)
            {
                return false;
            }

            for (int i = 0; i < targetMd5Sum.Length; i++)
            {
                if (targetMd5Sum[i] != sourceMd5Sum[i])
                {
                    return false;
                }
            }
            return true;
        }

        private string GetSourceFilterString(FilterPair filterPair)
        {
            if (Guid.Equals(new Guid(m_serverDiffEngine.Session.LeftMigrationSourceUniqueId), new Guid(filterPair.FilterItem[0].MigrationSourceUniqueId)))
            {
                return VCTranslationService.TrimTrailingPathSeparator(filterPair.FilterItem[0].FilterString);
            }
            else
            {
                return VCTranslationService.TrimTrailingPathSeparator(filterPair.FilterItem[1].FilterString);
            }
        }

        private string GetTargetFilterString(FilterPair filterPair)
        {
            if (Guid.Equals(new Guid(m_serverDiffEngine.Session.LeftMigrationSourceUniqueId), new Guid(filterPair.FilterItem[0].MigrationSourceUniqueId)))
            {
                return VCTranslationService.TrimTrailingPathSeparator(filterPair.FilterItem[1].FilterString);
            }
            else
            {
                return VCTranslationService.TrimTrailingPathSeparator(filterPair.FilterItem[0].FilterString);
            }
        }

        #endregion
    }
}