// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;

namespace Microsoft.TeamFoundation.Migration.Tfs2008VCAdapter
{
    /// <summary>
    /// Provides version control path and mapping methods that are common throughout the migration toolkit.
    /// </summary>
    public static class TfsUtil
    {
        public static bool IsOurTfsChange(
            Changeset changeset,
            ITranslationService translationService,
            Guid sourceId)
        {
            string reflectedChangesetId = translationService.TryGetTargetItemId(changeset.ChangesetId.ToString(), sourceId);
            return !string.IsNullOrEmpty(reflectedChangesetId);
        }

        /// <summary>
        /// Performs an Undo on any pending changes and
        /// removes all items from the workspace.
        /// </summary>
        /// <param name="activeWorkspace">The workspace to clean</param>
        public static void CleanWorkspace(Workspace activeWorkspace)
        {
            if (activeWorkspace == null)
            {
                throw new ArgumentNullException("activeWorkspace");
            }

            CleanWorkspaceFiles(activeWorkspace);

            PendingChange[] changes = activeWorkspace.GetPendingChanges();
            if (changes != null && changes.Length > 0)
            {
                activeWorkspace.Undo(changes);
            }

            TraceManager.WriteLine(TraceManager.Engine, "Calling get;C1 to clear workspace");
            activeWorkspace.Get(new ChangesetVersionSpec(1), GetOptions.Overwrite);
        }

        public static void CleanWorkspaceFiles(Workspace activeWorkspace)
        {
            if (activeWorkspace == null)
            {
                throw new ArgumentNullException("activeWorkspace");
            }

            // Flush to changeset 1 to cleanup any files
            TraceManager.WriteLine(TraceManager.Engine, "Clearing TFS workspace files");

            foreach (WorkingFolder wf in activeWorkspace.Folders)
            {
                if (!wf.IsCloaked)
                {
                    DeleteFiles(wf.LocalItem);
                }
            }
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
                TfsUtil.DeleteFile(file);
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
        /// If the parent directory to the specified file does not exist the parent directory is created.
        /// </summary>
        /// <param name="path">The file whose parent directory should be created.</param>
        public static void EnsurePathToFileExists(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            string directoryName = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directoryName);
        }

        /// <summary>
        /// Logs the provided error and throws a MigrationException
        /// </summary>
        /// <param name="message">The message to log and throw</param>
        public static void FailWithError(string message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            TraceManager.TraceError(message);
            throw new MigrationException(message);
        }

        /// <summary>
        /// Logs the provided formatted error string and throws a MigrationException
        /// </summary>
        /// <param name="format">The error message format</param>
        /// <param name="args">The formatted error message arguments</param>
        public static void FailWithError(string format, params object[] args)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }

            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            FailWithError(
                string.Format(TfsVCAdapterResource.Culture, format, args));
        }

        /// <summary>
        /// Correct a shelveset name if it is longer than 64 character or contains invalid characters /\
        /// </summary>
        /// <param name="shelvesetName">The shelveset name to be corrected</param>
        /// <returns></returns>
        internal static string CorrectShelvesetname(string shelvesetName)
        {
            if (shelvesetName.Length >= 64)
            {
                shelvesetName = shelvesetName.Substring(0, 31) + Guid.NewGuid().ToString("N"); ;
            }
            return shelvesetName.Replace('/', '_').Replace(':', '_').Replace('<', '_').Replace('>', '_').Replace('\\', '_').Replace('|', '_').Replace('?', '_').Replace(';', '_').TrimEnd(' ');
        }

        /// <summary>
        /// Determines if the provided path is a valid TFS server path
        /// </summary>
        /// <param name="path">The path to test</param>
        /// <param name="throwOnError">If true a MigrationException is thrown if the path is not valid.  if false, false will be returned if the path is not valid.</param>
        /// <returns>True if the path is valid or false if it is not and if throwOnError is false.</returns>
        public static bool IsValidTfsServerPath(string path, bool throwOnError)
        {
            bool isValid = false;
            string errorMsg = null;

            if (!string.IsNullOrEmpty(path))
            {
                if (path.Length <= 260)
                {
                    // server paths always start with the root node
                    if (path.StartsWith("$/"))
                    {
                        // server paths use '/' not '\'
                        if (path.IndexOf('\\') == -1)
                        {
                            // path segment starts with '$'
                            if (!path.Contains("/$"))
                            {
                                isValid = true;
                            }
                            else
                            {
                                errorMsg = string.Format(
                                    TfsVCAdapterResource.Culture,
                                    TfsVCAdapterResource.InvalidServerPath_DollarSegment,
                                    path);
                            }
                        }
                        else
                        {
                            errorMsg = string.Format(
                                TfsVCAdapterResource.Culture,
                                TfsVCAdapterResource.InvalidServerPath_WrongSlashes,
                                path);
                        }
                    }
                    else
                    {
                        errorMsg = string.Format(
                            TfsVCAdapterResource.Culture,
                            TfsVCAdapterResource.InvalidServerPath_MustStartWithDollarSlash,
                            path);
                    }
                }
                else
                {
                    errorMsg = string.Format(
                        TfsVCAdapterResource.Culture,
                        TfsVCAdapterResource.InvalidServerPath_260Limit,
                        path);
                }
            }
            else
            {
                errorMsg = TfsVCAdapterResource.InvalidServerPath_NullOrEmpty;
            }

            if (!isValid)
            {
                Debug.Assert(!string.IsNullOrEmpty(errorMsg));

                TraceManager.TraceWarning(errorMsg);
                if (throwOnError)
                {
                    throw new MigrationException(errorMsg);
                }
            }

            return isValid;
        }

        /// <summary>
        /// Combines two paths and ensures that only one forward slash seperates the two paths. 
        /// </summary>
        /// <param name="part1">The first part of the path to combine</param>
        /// <param name="part2">The second part of the path to combine</param>
        /// <returns>The combines path with a single forward slash between the two parts.</returns>
        public static string ConcatWithoutDoubleSlashes(string part1, string part2)
        {
            if (part1 == null)
            {
                throw new ArgumentNullException("part1");
            }

            if (part2 == null)
            {
                throw new ArgumentNullException("part2");
            }

            if (part1.IndexOf('\\') != -1)
            {
                part1 = part1.Replace('\\', '/');
            }

            if (part2.IndexOf('\\') != -1)
            {
                part2 = part2.Replace('\\', '/');
            }

            if (part1.EndsWith("/"))
            {
                part1 = part1.TrimEnd(slashArray);
            }

            if (part2.StartsWith("/"))
            {
                part2 = part2.TrimStart(slashArray);
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}/{1}", part1, part2);
        }

        /// <summary>
        /// Compare 'Action', 'Source', and 'Target' of the current item with the provided item. 
        /// The current item is the child of the provided item if 
        /// 1. Actions are the same 
        /// 2. Parent items's Target is the sub-item of the current item's Target. 
        /// 3. For rename/branch/merge, Parent item's Source is the sub-item of the item's Source and postfix sould be the same. 
        /// </summary>
        /// <param name="parentItem">Item to be compared</param>
        /// <returns>True if the provided item is the parent of the current item.</returns>
        public static bool isChildItemOf(BatchedItem item, BatchedItem parentItem)
        {
            if ((parentItem == null) || (parentItem.Target == null) || (item == null) || (item.Target == null))
            {
                return false;
            }

            if ((item.Action == parentItem.Action) && (VersionControlPath.IsSubItem(item.Target, parentItem.Target)))
            {
                if ((item.Action == WellKnownChangeActionId.Rename) && (VersionControlPath.IsSubItem(item.Source, parentItem.Source)))
                {
                    //Construct a canonlized serverpath instead of a truncated path to avoid assertion failure in TFC debug build.
                    string sourcePostFix = item.Source.Substring(parentItem.Source.Length);
                    string constructedTarget = ConcatWithoutDoubleSlashes(parentItem.Target, sourcePostFix);
                    if (VersionControlPath.EqualsCaseSensitive(item.Target, constructedTarget))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        static char[] slashArray = new char[] { '/' };

    }
}
