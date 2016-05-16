// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{

    /// <summary>
    /// Encapsulates just the information need to perform a diff on a VC item
    /// </summary>
    [Serializable]
    public sealed class ClearCaseDiffItem : IVCDiffItem
    {
        private byte[] m_hashValue;

        /// <summary>
        /// Constructor that takes a CCElement object
        /// </summary>
        internal ClearCaseDiffItem(
            string serverPath,
            string localPath,
            VCItemType itemType)
        {
            this.ServerPath = serverPath;
            Debug.Assert((itemType != VCItemType.File) || !string.IsNullOrEmpty(localPath), "File item must have a local path");
            this.LocalPath = localPath;
            this.VCItemType = itemType;
        }

        #region IDiffItem implementation
        /// <summary>
        /// The path by which the version control item is known on it server
        /// </summary>
        public string ServerPath
        {
            get;
            set;
        }

        /// <summary>
        /// The path by which the version control item is known to local system.
        /// </summary>
        public string LocalPath
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
                if (m_hashValue == null)
                {
                    try
                    {
                        m_hashValue = CalculateMD5Hash(LocalPath);
                    }
                    catch (Exception ex)
                    {
                        throw new VersionControlDiffException(String.Format(CultureInfo.InvariantCulture,
                            CCResources.UnableToGetHashVaue, ServerPath, ex.Message), ex);
                    }
                }
                return m_hashValue;
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

        public bool IsSubItemOf(string folderPath)
        {
            return ServerPath == null ? false : ClearCasePath.IsSubItem(ServerPath, folderPath);
        }
        #endregion

        #region Private Methods
        private static byte[] CalculateMD5Hash(string fileName)
        {
            byte[] hash;

            using (FileStream fileStream = new FileStream(fileName, FileMode.Open,
                                                          FileAccess.Read, FileShare.Read))
            {
                hash = CalculateMD5(fileStream);
            }

            return hash;
        }

        private static byte[] CalculateMD5(Stream stream)
        {
            byte[] hash;

            using (MD5 md5Provider = new MD5CryptoServiceProvider())
            {
                hash = md5Provider.ComputeHash(stream);
            }

            return hash;
        }

        #endregion
    }
}
