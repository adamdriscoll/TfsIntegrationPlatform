// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;

using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsFileSystemAdapter
{
    public class TfsFileSystemMigrationItem : IMigrationItem
    {
        bool m_isDirectory;
        string m_fileSystemPath;

        /// <summary>
        /// Whether this is a directory or not
        /// </summary>
        public bool IsDirectory
        {
            get
            {
                return m_isDirectory;
            }
            set
            {
                m_isDirectory = value;
            }
        }

        /// <summary>
        /// The path on the local file system. 
        /// </summary>
        public string FileSystemPath
        {
            get
            {
                return m_fileSystemPath;
            }
            set
            {
                m_fileSystemPath = value;
            }
        }

        /// <summary>
        /// The default constructor required by serialization
        /// </summary>
        public TfsFileSystemMigrationItem()
        {
        }

        /// <summary>
        /// Creates a migration item for the specified server, server path and session.  The default version of the 
        /// TFS item will be the latest item.
        /// </summary>
        /// <param name="server">The server the item exists on.</param>
        /// <param name="serverPath">The complete path to the TFS item</param>
        public TfsFileSystemMigrationItem(string fileSystemPath, bool isDirectory)
        {
            m_fileSystemPath = fileSystemPath;
            m_isDirectory = isDirectory;
        }

        #region IMigrationItem Members

        /// <summary>
        /// Copy the file system item to the specified path.  If the item is a directory, just create it.
        /// </summary>
        /// <param name="localPath">The local path to copy the file system item to.</param>
        public void Download(string localPath)
        {
            if (!IsDirectory)
            {
                Utils.EnsurePathToFileExists(localPath);

                if (File.Exists(localPath))
                {
                    Utils.DeleteFile(localPath);
                }

                // We need to have overwrite be true here for the branch|merge|edit case.
                File.Copy(m_fileSystemPath, localPath);
            }
            else
            {
                Directory.CreateDirectory(localPath);
            }
        }

        /// <summary>
        /// Returns the complete display name of the file system item path.
        /// </summary>
        public string DisplayName
        {
            get
            {
                return m_fileSystemPath;
            }
        }

        #endregion

        /// <summary>
        /// Returns the migration item display name.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return m_fileSystemPath;
        }
    }

    public class TfsFileSystemMigrationItemSerializer : IMigrationItemSerializer
    {
        #region IMigrationItemSerializer Members

        public IMigrationItem LoadItem(string itemBlob, ChangeGroupManager manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }

            if (string.IsNullOrEmpty(itemBlob))
            {
                throw new ArgumentNullException("itemBlob");
            }

            XmlSerializer serializer = new XmlSerializer(typeof(TfsFileSystemMigrationItem));

            using (StringReader strReader = new StringReader(itemBlob))
            using (XmlReader xmlReader = XmlReader.Create(strReader))
            {
                TfsFileSystemMigrationItem item = (TfsFileSystemMigrationItem)serializer.Deserialize(xmlReader);

                return item;
            }
        }

        public string SerializeItem(IMigrationItem item)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(TfsFileSystemMigrationItem));

            using (MemoryStream memStrm = new MemoryStream())
            {
                serializer.Serialize(memStrm, item);
                memStrm.Seek(0, SeekOrigin.Begin);
                using (StreamReader sw = new StreamReader(memStrm))
                {
                    return sw.ReadToEnd();
                }
            }
        }

        #endregion
    }
}
