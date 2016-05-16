// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;

using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{
    class Utils
    {
        /// <summary>
        /// If the parent directory to the specified file does not exist the parent directory is created.
        /// </summary>
        /// <param name="path">The file whose parent directory should be created.</param>
        internal static void EnsurePathToFileExists(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            string directoryName = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directoryName);
        }

        // there was a high incidence of IO exceptions from files being locked when clearing the
        // directory contents.  This retry logic should help resolve any timing issues around
        // virus scanning or someone accidently locking a directory by being in it in a cmd window
        public static void DeleteFiles(string directory)
        {
            const int maxAttempts = 20;
            int currentAttempt = 0;

            if (!Directory.Exists(directory))
            {
                return;
            }

            while (true)
            {
                try
                {
                    deleteFiles_inner(directory);
                    return;
                }
                catch (IOException ioe)
                {
                    currentAttempt++;

                    TraceManager.WriteLine(TraceManager.Engine, "Caught an IO exception cleaning the directory tree:");
                    TraceManager.WriteLine(TraceManager.Engine, ioe.Message);
                    TraceManager.WriteLine(TraceManager.Engine, "That was attempt {0} of {1}", currentAttempt, maxAttempts);

                    if (currentAttempt < maxAttempts)
                    {
                        TraceManager.WriteLine(TraceManager.Engine, "Sleeping for 30 seconds to let the IO issue resolve itself");
                        Thread.Sleep(30 * 1000);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private static void deleteFiles_inner(string directory)
        {
            foreach (string file in Directory.GetFiles(directory))
            {
                DeleteFile(file);
            }

            foreach (string subDirectory in Directory.GetDirectories(directory))
            {
                deleteFiles_inner(subDirectory);
                Directory.Delete(subDirectory);
            }
        }

        /// <summary>
        /// Deletes the specified file.  Removes the ReadOnly attribute if it is set on the
        /// file.
        /// </summary>
        /// <param name="localPath">The local file to delete</param>
        public static void DeleteFile(string localPath)
        {
            if (string.IsNullOrEmpty(localPath))
            {
                throw new ArgumentNullException("localPath");
            }

            if (File.Exists(localPath))
            {
                FileAttributes attr = File.GetAttributes(localPath);
                if ((attr & FileAttributes.ReadOnly) != 0)
                {
                    attr &= ~FileAttributes.ReadOnly;
                    File.SetAttributes(localPath, attr);
                }

                File.Delete(localPath);
            }
        }

        /// <summary>
        /// For a list of paths, check whether an element's ancestor is in the list.
        /// </summary>
        /// <param name="listToBeChecked"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        public static bool IsElementAncestorInList(HashSet<string>listToBeChecked, string element)
        {
            while (!ClearCasePath.IsVobRoot(element))
            {
                if (listToBeChecked.Contains(element))
                {
                    return true;
                }
                element = ClearCasePath.GetFolderName(element);
            }
            return false;
        }

        /// <summary>
        /// Verify the correctness of the storage location path. Create one if the storage location doesn't exist.
        /// </summary>
        /// <param name="path">Path to the storage location.</param>
        internal static bool VerifyStorageLocationSetting(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            if (Directory.Exists(path))
            {
                return true;
            }
            else
            {
                Directory.CreateDirectory(path);
                return true;
            }
        }

        internal static bool IsPathMapped(string item, ConfigurationService configurationService)
        {
            MappingEntry mappingEntry = FindMappedPath(item, configurationService);

            if (mappingEntry == null)
            {
                // Path is not mapped
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Detect changes checked in by CC adapter by comparing comment.
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        internal static bool IsOurChange(CCHistoryRecord historyRecord)
        {
            // Todo, change to a more robust self-change detection.
            if (historyRecord.Comment.Contains(Microsoft.TeamFoundation.Migration.Toolkit.Constants.PlatformCommentSuffixMarker))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Find the mapping entry for the given path. 
        /// </summary>
        /// <param name="serverPath"></param>
        /// <returns>Null if the path is not mapped or cloaked. Otherwise, return the mapping entry.</returns>
        internal static MappingEntry FindMappedPath(string serverPath, ConfigurationService configurationService)
        {
            MappingEntry mostSpecificMapping = null;
            foreach (MappingEntry current in configurationService.Filters)
            {
                if (ClearCasePath.IsSubItem(serverPath, ClearCasePath.GetFullPath(current.Path)))
                {
                    if (mostSpecificMapping == null ||
                        ClearCasePath.IsSubItem(ClearCasePath.GetFullPath(current.Path),
                        ClearCasePath.GetFullPath(mostSpecificMapping.Path)))
                    {
                        mostSpecificMapping = current;
                    }
                }
            }

            if ((mostSpecificMapping != null) && (!mostSpecificMapping.Cloak))
            {
                return mostSpecificMapping;
            }
            else
            {
                return null;
            }
        }
    }
}
