//------------------------------------------------------------------------------
// <copyright file="SharePointVCMigrationItemSerializer.cs" company="Microsoft Corporation">
//      Copyright © Microsoft Corporation.  All Rights Reserved.
// </copyright>
//
//  This code released under the terms of the 
//  Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//------------------------------------------------------------------------------

namespace Microsoft.TeamFoundation.Integration.SharePointVCAdapter
{
    using System;
    using System.IO;
    using System.Xml;
    using System.Xml.Serialization;
    using Microsoft.TeamFoundation.Migration.Toolkit;

    class SharePointVCMigrationItemSerializer : IMigrationItemSerializer
    {
        #region IMigrationItemSerializer Members

        /// <summary>
        /// Loads the item.
        /// </summary>
        /// <param name="itemBlob">The item BLOB.</param>
        /// <param name="manager">The manager.</param>
        /// <returns></returns>
        IMigrationItem IMigrationItemSerializer.LoadItem(string itemBlob, ChangeGroupManager manager)
        {
            TraceManager.TraceInformation("WSSVC:Serializer:LoadItem");
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }

            if (string.IsNullOrEmpty(itemBlob))
            {
                throw new ArgumentNullException("itemBlob");
            }

            XmlSerializer serializer = new XmlSerializer(typeof(SharePointItem));

            using (StringReader strReader = new StringReader(itemBlob))
            {
                using (XmlReader xmlReader = XmlReader.Create(strReader))
                {
                    SharePointItem item = (SharePointItem)serializer.Deserialize(xmlReader);
                    TraceManager.TraceInformation("WSSVC:Serializer:Item  - {0}", item.AbsoluteURL);
                    return item;
                }
            }
        }

        /// <summary>
        /// Serializes the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        string IMigrationItemSerializer.SerializeItem(IMigrationItem item)
        {
            TraceManager.TraceInformation("WSSVC:Ser:SerializeItem - {0}", item.DisplayName);
            XmlSerializer serializer = new XmlSerializer(typeof(SharePointItem));

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
