// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.SubversionAdapter.Interop.Subversion.ObjectModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.Migration.SubversionAdapter.SubversionOM;
using Microsoft.TeamFoundation.Migration.SubversionAdapter.Interop.Subversion;

namespace Microsoft.TeamFoundation.Migration.SubversionAdapter
{
    /// <summary>
    /// Encapsulates just the information needed to perform a diff on a SubversionVC item
    /// </summary>
    [Serializable]
    class SubversionVCDiffItem: IVCDiffItem
    {
        private Repository m_repository;
        private byte[] m_hashValue;
        int m_revision;
        string m_serverUri;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="item">The Subversion item object that this DiffItem represents</param>
        /// <param name="revision">The revision to be diffed</param>
        public SubversionVCDiffItem(Item item, int revision)
        {
            m_repository = Repository.GetRepository(new Uri(item.Repository));
            m_revision = revision;
            m_serverUri = item.FullServerPath;
            this.ServerPath = item.Path.ToString().TrimEnd(PathUtils.Separator);

            if (item.ItemType == WellKnownContentType.VersionControlledFolder)
            {
                this.VCItemType = VCItemType.Folder;
            }
            else if (item.ItemType == WellKnownContentType.VersionControlledFile)
            {
                this.VCItemType = VCItemType.File;
            }
            else
            {
                throw new ArgumentException("Unexpected value for changeItem.ItemType");
            }
        }

        public string ServerPath
        {
            get;
            set;
        }

        /// <summary>
        /// An MD5 hash value for the file
        /// </summary>
        public byte[] HashValue
        {
            get
            {
                if (VCItemType == Toolkit.VCItemType.Folder)
                {
                    return null;
                }
                else
                {
                    // Todo, find a way to get MD5 hash value from subversion repository
                    string fileName = System.IO.Path.GetRandomFileName();
                    m_repository.DownloadFile(fileName, new Uri(m_serverUri), m_revision);
                    m_hashValue = Utility.CalculateMD5Hash(fileName);
                    File.Delete(fileName);
                    return m_hashValue;
                }
            }
            set
            {
                m_hashValue = value;
            }
        }

        /// <summary>
        /// Identifies the type of version control item (file or folder)
        /// </summary>
        public VCItemType VCItemType
        {
            get;
            set;
        }

        /// <summary>
        /// Check whether the current item is a sub item of the supplied serverFolderPath.
        /// </summary>
        /// <param name="serverFolderPath"></param>
        /// <returns></returns>
        public bool IsSubItemOf(string serverFolderPath)
        {
            // remove the beginning '\'
            if (!String.IsNullOrEmpty(serverFolderPath))
            {
                serverFolderPath = serverFolderPath.TrimStart(PathUtils.Separator);
            }

            Uri fullServerFolderUri;

            // Try to construct the full server Uri of the inpur string and then check for parent/child relationship.
            if (Uri.TryCreate(serverFolderPath, UriKind.Absolute, out fullServerFolderUri)
                || Uri.TryCreate(m_repository.RepositoryRoot, serverFolderPath, out fullServerFolderUri))
            {
                return PathUtils.IsChildItem(fullServerFolderUri, new Uri(m_serverUri));
            }

            TraceManager.TraceInformation("The input string {0} is neither an absolute nor a relative Uri path", serverFolderPath);
            return false;
        }
    }
}
