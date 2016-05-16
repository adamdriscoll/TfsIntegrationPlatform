// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2010VCAdapter
{

    /// <summary>
    /// A single item in a IMigrationAction (which is part of a ChangeGroup).  The migration item
    /// represents a single versioned item in the TFS server
    /// </summary>
    [Serializable]
    public sealed class TfsMigrationItem : IMigrationItem
    {
        [NonSerialized]
        VersionControlServer m_Server;

        string m_serverPath;
        int m_changeSet;
        int m_deletionId;
        string m_serverUrl;
        string m_displayNameCache;

        [NonSerialized]
        Item m_cachedItem;

        /// <summary>
        /// The default constructor required by serialization
        /// </summary>
        public TfsMigrationItem()
        {
        }

        /// <summary>
        /// Creates a migration item for the specified server, server path and session.  The default version of the 
        /// TFS item will be the latest item.
        /// </summary>
        /// <param name="server">The server the item exists on.</param>
        /// <param name="serverPath">The complete path to the TFS item</param>
        public TfsMigrationItem(VersionControlServer server,
            string serverPath)
            : this(server, serverPath, Latest, 0)
        {
        }

        /// <summary>
        /// Creates a migration item for the specified server, server path, server path changeset ID (version) and session.
        /// </summary>
        /// <param name="server">The server the item exists on.</param>
        /// <param name="serverPath">The complete path to the TFS item</param>
        /// <param name="session">The migration session.</param>
        /// <param name="changeSet">The changeset of the server item.</param>
        public TfsMigrationItem(VersionControlServer server,
            string serverPath, 
            int changeSet)
            : this(server, serverPath, changeSet, 0)
        {
        }

        /// <summary>
        /// Creates a migration item for the specified server, server path, server path changeset ID (version), session
        /// and deletion id.  A deletion id of 0 (default) is a non-deleted item.
        /// </summary>
        /// <param name="server">The server the item exists on.</param>
        /// <param name="serverPath">The complete path to the TFS item</param>
        /// <param name="changeSet">The changeset of the server item.</param>
        /// <param name="deletionId">The deletion ID of the server path.</param>
        public TfsMigrationItem(VersionControlServer server,
            string serverPath,
            int changeSet,
            int deletionId)
        {
            m_Server = server;
            ServerPath = serverPath;
            Changeset = changeSet;
            m_deletionId = deletionId;

            if (m_Server != null)
            {
                m_serverUrl = m_Server.TeamProjectCollection.Uri.AbsoluteUri;
            }
        }

        /// <summary>
        /// Creates a migration item using the provided TFS Target instance and version control session.
        /// </summary>
        /// <param name="changeItem">The TFS item this migration item represents</param>
        public TfsMigrationItem(Item changeItem)
        {
            if (changeItem == null)
            {
                throw new ArgumentNullException("changeItem");
            }

            m_Server = changeItem.VersionControlServer;
            ServerPath = changeItem.ServerItem;
            Changeset = changeItem.ChangesetId;
            m_deletionId = changeItem.DeletionId;

            if (m_Server != null)
            {
                m_serverUrl = m_Server.TeamProjectCollection.Uri.AbsoluteUri;
            }
        }

        /// <summary>
        /// True if the migration item represents a directory, false otherwise.
        /// </summary>
        public bool IsDirectory
        {
            get
            {
                return CachedItem.ItemType == ItemType.Folder;
            }
        }

        internal Item CachedItem
        {
            get
            {
                if (m_cachedItem == null)
                {
                    loadCachedItem();
                }

                return m_cachedItem;
            }
        }

        private void loadCachedItem()
        {
            if (m_cachedItem == null)
            {
                if (Server == null)
                {
                    throw new MigrationException("Unable to determine TFS server to download item");
                }

                VersionSpec spec;

                if (m_changeSet == Latest)
                {
                    spec = VersionSpec.Latest;
                }
                else
                {
                    spec = new ChangesetVersionSpec(m_changeSet);
                }

                m_cachedItem = Server.GetItem(
                    m_serverPath,
                    spec,
                    m_deletionId,
                    true);
            }
        }

        /// <summary>
        /// The TFS server path to the migration item.
        /// </summary>
        public string ServerPath
        {
            get
            {
                return m_serverPath;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }
                m_serverPath = value;
            }
        }

        // this item is serialized to XML and therefore should be a string and not a System.Uri
        /// <summary>
        /// The complete url to the TFS server the item exsits on.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
        public string TfsServerUrl
        {
            get
            {
                return m_serverUrl;
            }
            set
            {
                m_serverUrl = value;
            }
        }

        /// <summary>
        /// The TFS changeset id of the server item or Latest.
        /// </summary>
        public int Changeset
        {
            get
            {
                return m_changeSet;
            }
            set
            {
                if ((value < 0)&&(value != Latest))
                {
                    throw new ArgumentOutOfRangeException("Changeset", value, MigrationToolkitResources.ChangesetIdNonNegative);
                }
                m_changeSet = value;
            }
        }

        /// <summary>
        /// The deletion ID of the server item (or 0 for a non-deleted item)
        /// </summary>
        public int DeletionId
        {
            get
            {
                return m_deletionId;
            }
            set
            {
                m_deletionId = value;
            }
        }

        /// <summary>
        /// The version control server the item exists on.
        /// </summary>
        [XmlIgnore]
        public VersionControlServer Server
        {
            get
            {
                if (m_Server == null && !string.IsNullOrEmpty(m_serverUrl))
                {
                    TfsTeamProjectCollection tfServer = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(m_serverUrl));
                    m_Server = (VersionControlServer)tfServer.GetService(typeof(VersionControlServer));
                }

                Debug.Assert(m_Server != null);

                return m_Server;
            }
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
                CachedItem.DownloadFile(localPath);
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
                if (m_displayNameCache == null)
                {
                    if (m_deletionId > 0)
                    {
                        m_displayNameCache = VersionSpec.AddDeletionModifierIfNecessary(m_serverPath, m_deletionId);
                    }
                    else
                    {
                        if (m_changeSet != Latest)
                        {
                            m_displayNameCache = string.Format(
                                CultureInfo.InvariantCulture,
                                "{0}{1}{2}{3}",
                                m_serverPath,
                                VersionSpec.Separator,
                                ChangesetVersionSpec.Identifier,
                                m_changeSet);
                        }
                        else
                        {
                            m_displayNameCache = string.Format(
                                CultureInfo.InvariantCulture,
                                "{0}{1}{2}",
                                m_serverPath,
                                VersionSpec.Separator,
                                VersionSpec.Latest.DisplayString);
                        }
                    }
                }

                return m_displayNameCache;
            }
        }

        #endregion

        /// <summary>
        /// Returns the migration item display name.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return DisplayName;
        }

        public const int Latest = int.MinValue;
    }

    public class TfsMigrationItemSerialzier : IMigrationItemSerializer
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

            XmlSerializer serializer = new XmlSerializer(typeof(TfsMigrationItem));

            using (StringReader strReader = new StringReader(itemBlob))
            using (XmlReader xmlReader = XmlReader.Create(strReader))
            {
                TfsMigrationItem item = (TfsMigrationItem)serializer.Deserialize(xmlReader);

                return item;
            }
        }

        public string SerializeItem(IMigrationItem item)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(TfsMigrationItem));

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
