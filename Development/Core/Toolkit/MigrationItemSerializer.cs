// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class MigrationItemSerializer <T> : IMigrationItemSerializer
        where T : IMigrationItem
    {
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

            XmlSerializer serializer = new XmlSerializer(typeof(T));

            using (StringReader strReader = new StringReader(itemBlob))
            using (XmlReader xmlReader = XmlReader.Create(strReader))
            {
                T item = (T)serializer.Deserialize(xmlReader);

                return item;
            }
        }

        public string SerializeItem(IMigrationItem item)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

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
}
