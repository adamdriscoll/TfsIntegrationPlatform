// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.Toolkit;
using System.Xml.Serialization;
using System.IO;
using System.Xml;

namespace Microsoft.TeamFoundation.Migration.SubversionAdapter
{
    #region IMigrationItem implementation

    //TODO Implement Me
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class SubversionMigrationItem : IMigrationItem
    {
        /// <summary>
        /// Downloads the item from the source system to the provided path.
        /// </summary>
        /// <param name="localPath">The path to download the item to.</param>
        public void Download(string localPath)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// A display name for the item.  This string is not gaurenteed to useful for parsing
        /// or to represent a meaningful path within the version control system or local file system.
        /// </summary>
        public string DisplayName
        {
            get { throw new NotImplementedException(); }
        }
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
