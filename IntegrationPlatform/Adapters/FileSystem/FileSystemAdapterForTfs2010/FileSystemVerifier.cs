// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsFileSystemAdapter
{
    /// <summary>
    /// File system is not version controlled. 
    /// When Migrating file system to TFS, we need a way to lock the file system during the migration. 
    /// FileSystemVerifier provides a way to verify that the file system doesn't change during the analysis. 
    /// In the case of ClearCase dynamic view, Winsock error may cause missing file problems when reading from mapped view.
    /// FilesystemVerifier can be used to verify the integrity of dynamic view. 
    /// </summary>
    public class FileSystemVerifier
    {
        private Dictionary<string, int> m_pathFileCount;

        public FileSystemVerifier()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize the verifier
        /// </summary>
        public void Initialize()
        {
            if (m_pathFileCount == null)
            {
                m_pathFileCount = new Dictionary<string, int>();
            }
            else
            {
                m_pathFileCount.Clear();
            }
        }

        /// <summary>
        /// Add a path for verification.
        /// </summary>
        /// <param name="path"></param>
        public void AddPathForVerification(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }
            // If the path has already been added, just return. 
            if (m_pathFileCount.ContainsKey(path))
            {
                return;
            }
            if (Directory.Exists(path))
            {
                m_pathFileCount.Add(path, Directory.GetFiles(path, "*", SearchOption.AllDirectories).Length);
            }
            else
            {
                m_pathFileCount.Add(path, 0);
            }
            return;
        }

        /// <summary>
        /// Verify a list of paths cached.
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public bool Verify()
        {
            bool allPathsVerified = true;

            foreach (string path in m_pathFileCount.Keys)
            {
                if (!Verify(path))
                {
                    // Don't return here so all paths can be verified. 
                    allPathsVerified = false;
                }
            }

            return allPathsVerified;
        }

        public bool Verify(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }
            // If the path has already been added, just return. 
            if (!m_pathFileCount.ContainsKey(path))
            {
                TraceManager.TraceWarning("The path {0} to be verified does not exist.", path);
                return false;
            }
            int fileCount;
            if (Directory.Exists(path))
            {
                fileCount = Directory.GetFiles(path, "*", SearchOption.AllDirectories).Length;
            }
            else
            {
                fileCount = 0;
            }

            if (m_pathFileCount[path] == fileCount)
            {
                return true;
            }
            else
            {
                TraceManager.TraceWarning("The path {0} has been changed. Previous file count {1}, current file count {2}.",
                    path, m_pathFileCount[path], fileCount);
                return false;
            }
        }
    }
}
