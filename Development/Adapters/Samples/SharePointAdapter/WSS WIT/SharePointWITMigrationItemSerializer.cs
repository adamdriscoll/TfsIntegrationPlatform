//------------------------------------------------------------------------------
// <copyright file="SharePointWITMigrationItemSerializer.cs" company="Microsoft Corporation">
//      Copyright © Microsoft Corporation.  All Rights Reserved.
// </copyright>
//
//  This code released under the terms of the 
//  Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//------------------------------------------------------------------------------

namespace Microsoft.TeamFoundation.Integration.SharePointWITAdapter
{
    using System;
    using System.IO;
    using System.Xml;
    using System.Xml.Serialization;
    using Microsoft.TeamFoundation.Migration.Toolkit;

    /// <summary>
    /// Provides the support for serialisation of the WIT
    /// </summary>
    public class SharePointWITMigrationItemSerializer : IMigrationItemSerializer
    {
        #region IMigrationItemSerializer Members

        /// <summary>
        /// Loads the item.
        /// </summary>
        /// <param name="itemBlob">The item BLOB.</param>
        /// <param name="manager">The manager.</param>
        /// <returns></returns>
        public IMigrationItem LoadItem(string itemBlob, ChangeGroupManager manager)
        {
            TraceManager.TraceInformation("WSSWIT:S:LoadItem");
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }

            if (string.IsNullOrEmpty(itemBlob))
            {
                throw new ArgumentNullException("itemBlob");
            }

            XmlSerializer serializer = new XmlSerializer(typeof(SharePointListItem));

            using (StringReader itemBlobStringReader = new StringReader(itemBlob))
            {
                using (XmlReader itemBlobXmlReader = XmlReader.Create(itemBlobStringReader))
                {
                    return (SharePointListItem)serializer.Deserialize(itemBlobXmlReader);
                }
            }
        }

        /// <summary>
        /// Serializes the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public string SerializeItem(IMigrationItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            TraceManager.TraceInformation("WSSWIT:S:SerializeItem - {0}", item.DisplayName);

            XmlSerializer sharePointTaskSerializer = new XmlSerializer(item.GetType());

            using (MemoryStream memoryStream = new MemoryStream())
            {
                sharePointTaskSerializer.Serialize(memoryStream, item);
                memoryStream.Seek(0, SeekOrigin.Begin);
                using (StreamReader streamReader = new StreamReader(memoryStream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }

        #endregion
    }
}
