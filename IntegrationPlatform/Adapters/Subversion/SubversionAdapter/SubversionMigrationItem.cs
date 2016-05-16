// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.SubversionAdapter.Interop.Subversion.ObjectModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.SubversionAdapter.SubversionOM;
using Microsoft.TeamFoundation.Migration.SubversionAdapter.Interop.Subversion;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.SubversionAdapter
{
    #region IMigrationItem implementation

    [Serializable]
    public class SubversionMigrationItem : IMigrationItem
    {
        #region Private Fields

        private string m_repositoryUri;
        private string m_itemUri;

        private int m_itemRevision;
        private bool m_isItemDirectory;

        #endregion

        #region Constructor

        /// <summary>
        /// Default Constructor that is needed for the XML Serializer
        /// </summary>
        public SubversionMigrationItem()
        {
        }

        /// <summary>
        /// Create a new instance of the SubversionMigrationItem
        /// </summary>
        /// <param name="changeItem">The actual changed item</param>
        internal SubversionMigrationItem(Change changeItem)
        {
            if (null == changeItem)
                throw new ArgumentNullException("changeItem");

            m_repositoryUri = changeItem.Changeset.Repository;
            m_itemUri = changeItem.FullServerPath;

            m_itemRevision = changeItem.Changeset.Revision;
            m_isItemDirectory = changeItem.ItemType == WellKnownContentType.VersionControlledFolder;
        }

        /// <summary>
        /// Create a new instance of the SubversionMigrationItem
        /// </summary>
        /// <param name="changeItem">The actual changed item</param>
        internal SubversionMigrationItem(Item item)
        {
            if (null == item)
                throw new ArgumentNullException("item");

            m_repositoryUri = item.Repository;
            m_itemUri = item.FullServerPath;

            m_itemRevision = item.CreatedRev;
            m_isItemDirectory = item.ItemType == WellKnownContentType.VersionControlledFolder;
        }

        /// <summary>
        /// Create a new instance of the SubversionMigrationItem from an Subversion Item, but use a specified Revision instead of Item.CreatedRev.
        /// This is used in cases where a Branch action was changed as Add due to branch parent not found conflict resolution.
        /// </summary>
        /// <param name="changeItem">The actual item</param>
        /// <param name="changeItem">The specified change revision</param>
        internal SubversionMigrationItem(Item item, int changeRevision)
        {
            if (null == item)
                throw new ArgumentNullException("item");

            m_repositoryUri = item.Repository;
            m_itemUri = item.FullServerPath;

            m_itemRevision = changeRevision;
            m_isItemDirectory = item.ItemType == WellKnownContentType.VersionControlledFolder;
        }

        /// <summary>
        /// Creates a new instance of the SubversionMigrationItem
        /// </summary>
        /// <param name="repositoryUri">The Uri that is used to connect to the svn repository</param>
        /// <param name="itemUri">The absolute Uri to the item in the repository</param>
        /// <param name="revision">The revision of the item in the repository</param>
        /// <param name="contentType">The content type of this item</param>
        internal SubversionMigrationItem(Uri repositoryUri, Uri itemUri, int revision, ContentType contentType)
        {
            if (repositoryUri == null)
            {
                throw new ArgumentNullException("repositoryUri");
            }
            if (itemUri == null)
            {
                throw new ArgumentNullException("itemUri");
            }

            m_repositoryUri = repositoryUri.AbsoluteUri;
            m_itemUri = itemUri.AbsoluteUri;
            m_itemRevision = revision;
            m_isItemDirectory = contentType == WellKnownContentType.VersionControlledFolder;
        }

        #endregion

        #region Public Properties (Serialize support)

        /// <summary>
        /// Gets or sets whether the actual change is a directory or a file change
        /// </summary>
        public bool IsDirectory 
        {
            get
            {
                return m_isItemDirectory;
            }
            set
            {
                m_isItemDirectory = value;
            }
        }

        /// <summary>
        /// Gets or sets the revision of the changed item
        /// </summary>
        public int Revision
        {
            get
            {
                return m_itemRevision;
            }
            set
            {
                m_itemRevision = value;
            }
        }

        /// <summary>
        /// Gets or sets the absolute uri of the item in the svn repository
        /// </summary>
        public string ItemUri
        { 
            get
            {
                return m_itemUri;
            }
            set
            {
                m_itemUri = value;
            }
        }

        /// <summary>
        /// Gets or sets the uri of the svn repository
        /// </summary>
        public string RepositoryUri
        {
            get
            {
                return m_repositoryUri;
            }
            set
            {
                m_repositoryUri = value;
            }
        }

        #endregion

        #region Private Properties

        private Repository Repository
        {
            get
            {
                return SubversionAdapter.SubversionOM.Repository.GetRepository(new Uri(RepositoryUri));
            }
        }

        #endregion

        #region IMigrationItem implementation

        /// <summary>
        /// Downloads the item from the source system to the provided path.
        /// </summary>
        /// <param name="localPath">The path to download the item to.</param>
        public void Download(string localPath)
        {
            if (!IsDirectory)
            {
                Repository.DownloadFile(localPath, new Uri(m_itemUri), m_itemRevision);
            }
            else
            {
                if (!Directory.Exists(localPath))
                    Directory.CreateDirectory(localPath);
            }
        }

        /// <summary>
        /// A display name for the item.  This string is not gaurenteed to useful for parsing
        /// or to represent a meaningful path within the version control system or local file system.
        /// </summary>
        public string DisplayName
        {
            get 
            {
                return string.Format("{0}#{1}", m_itemUri, m_itemRevision);
            }
        }

        #endregion
    }

    #endregion

    #region IMigrationItemSerializer implementation

    /// <summary>
    /// An actual implementation of the <see cref="IMigrationItemSerializer"/> interface for <see cref="SubversionMigrationItem"/> items
    /// </summary>
    public class SubversionMigrationItemSerialzier : IMigrationItemSerializer
    {
        /// <summary>
        /// Deserializes the itemblob to a actual instance of an <see cref="IMigrationItem"/> object
        /// </summary>
        /// <param name="itemBlob">The string representation of the object that has to be deserialized</param>
        /// <param name="manager"></param>
        /// <returns>Returns a new instance of the deserialized object</returns>
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

            XmlSerializer serializer = new XmlSerializer(typeof(SubversionMigrationItem));

            using (StringReader strReader = new StringReader(itemBlob))
            using (XmlReader xmlReader = XmlReader.Create(strReader))
            {
                SubversionMigrationItem item = (SubversionMigrationItem)serializer.Deserialize(xmlReader);
                return item;
            }
        }

        /// <summary>
        /// Serializes the <see cref="IMigrationItem"/> to an XML stream
        /// </summary>
        /// <param name="item">The item that has to be serialized</param>
        /// <returns>An XML string that contains the serialized represenation of the <see cref="IMigrationItem"/></returns>
        public string SerializeItem(IMigrationItem item)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(SubversionMigrationItem));

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
    }
    
    #endregion
}
