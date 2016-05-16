// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Microsoft.TeamFoundation.Migration.TfsFileSystemAdapter
{
    public class TfsFileSystemRelatedBranchView
    {
        public TfsFileSystemRelatedBranchView(String localRootPath, String relatedBranchLocalRootPath)
        {
            m_localRootPath = localRootPath.TrimEnd(Path.DirectorySeparatorChar);
            m_relatedBranchLocalRootPath = (relatedBranchLocalRootPath == null) ? null : relatedBranchLocalRootPath.TrimEnd(Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Returns true if the file or folder exists in the related branch.
        /// </summary>
        /// <param name="localPath"></param>
        /// <param name="isDirectory"></param>
        /// <param name="relatedBranchPath"></param>
        /// <returns></returns>
        public Boolean TryGetRelatedBranchItem(String localPath, Boolean isDirectory, out String relatedBranchPath)
        {
            if (m_relatedBranchLocalRootPath == null)
            {
                relatedBranchPath = null;
                return false;
            }

            // Error checking: Make sure the local path starts with the m_localRootPath
            if (localPath == null || !localPath.StartsWith(m_localRootPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("localPath");
            }

            // Map the path to the related directory.
            String relativePath = localPath.Substring(m_localRootPath.Length);
            relatedBranchPath = String.Concat(m_relatedBranchLocalRootPath, relativePath);

            if (isDirectory)
            {
                return Directory.Exists(relatedBranchPath);
            }
            else
            {
                return File.Exists(relatedBranchPath);
            }
        }

        private String m_localRootPath;
        private String m_relatedBranchLocalRootPath;
    }
}
