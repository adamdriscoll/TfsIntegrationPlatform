// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;
using System.Xml.Serialization;
using System.IO;
using System.Globalization;
using System.Xml;

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{
    /// <summary>
    /// A single item in a IMigrationAction (which is part of a ChangeGroup).  The migration item
    /// represents a single versioned item in ClearCase
    /// </summary>
    [Serializable]
    public sealed class ClearCaseMigrationItem : IMigrationItem
    {
        string m_versionExtendedPath;
        bool m_isDirectory;
        string m_viewName;

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
        /// VersionExtendedPath
        /// </summary>
        public string VersionExtendedPath
        {
            get
            {
                return m_versionExtendedPath;
            }
            set
            {
                m_versionExtendedPath = value;
            }
        
        }

        /// <summary>
        /// View associated with this migration item.
        /// </summary>
        public string ViewName
        {
            get
            {
                return m_viewName;
            }
            set
            {
                m_viewName = value;
            }
        }

        /// <summary>
        /// The default constructor required by serialization
        /// </summary>
        public ClearCaseMigrationItem()
        {
        }

        /// <summary>
        /// Creates a migration item for the specified server, server path and session.  The default version of the 
        /// TFS item will be the latest item.
        /// </summary>
        /// <param name="server">The server the item exists on.</param>
        /// <param name="serverPath">The complete path to the TFS item</param>
        public ClearCaseMigrationItem(string viewName, string versionExtendedPath, bool isDirectory)
        {
            m_viewName = viewName;
            m_versionExtendedPath = versionExtendedPath;
            m_isDirectory = isDirectory;
        }

        #region IMigrationItem Members

        /// <summary>
        /// Downloads the TFS server item to the specified path.  If the item is a directory, just create it.
        /// </summary>
        /// <param name="localPath">The local path to download the TFS server item to.</param>
        public void Download(string localPath)
        {
            if (!IsDirectory)
            {
                Utils.EnsurePathToFileExists(localPath);
                ClearCaseServer clearCaseServer = ClearCaseServer.GetInstance(m_viewName);
                string getCmd = string.Format("get -to \"{0}\" \"{1}\"", localPath, ClearCasePath.MakeRelative(m_versionExtendedPath));
                try
                {
                    string cmdOutput = clearCaseServer.ExecuteClearToolCommand(getCmd);
                }
                catch (Exception)
                {
                    throw ;
                }
            }
            else
            {
                Directory.CreateDirectory(localPath);
            }
        }

        /// <summary>
        /// Returns the complete display name of the TFS server path.  This will include any version information or the deletion ID.
        /// </summary>
        public string DisplayName
        {
            get
            {
                return m_versionExtendedPath;
            }
        }

        #endregion

        /// <summary>
        /// Returns the migration item display name.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return m_versionExtendedPath;
        }
    }

    public class ClearCaseV6MigrationItemSerialzier : IMigrationItemSerializer
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

            XmlSerializer serializer = new XmlSerializer(typeof(ClearCaseMigrationItem));

            using (StringReader strReader = new StringReader(itemBlob))
            using (XmlReader xmlReader = XmlReader.Create(strReader))
            {
                ClearCaseMigrationItem item = (ClearCaseMigrationItem)serializer.Deserialize(xmlReader);

                return item;
            }
        }

        public string SerializeItem(IMigrationItem item)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ClearCaseMigrationItem));

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
