// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.Toolkit;
using System.IO;
using System.Xml;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.MigrationItem
{
    internal class GenericSerializer<T>
    {
        XmlSerializer m_serializer;

        public GenericSerializer()
        {
            m_serializer = new XmlSerializer(typeof(T));
        }

        public object DeserializeItem(string itemBlob)
        {
            using (StringReader strReader = new StringReader(itemBlob))
            using (XmlReader xmlReader = XmlReader.Create(strReader))
            {
                return m_serializer.Deserialize(xmlReader);
            }
        }

        public string SerializeItem(object item)
        {
            using (MemoryStream memStrm = new MemoryStream())
            {
                m_serializer.Serialize(memStrm, item);
                memStrm.Seek(0, SeekOrigin.Begin);
                using (StreamReader sw = new StreamReader(memStrm))
                {
                    return sw.ReadToEnd();
                }
            }
        }
    }
}
