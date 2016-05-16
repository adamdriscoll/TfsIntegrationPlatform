// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class GenericSerializer<T>
    {
        public T Deserialize(string serializedObjectBlob)
        {
            if (string.IsNullOrEmpty(serializedObjectBlob))
            {
                throw new ArgumentNullException("serializedObjectBlob");
            }

            XmlSerializer serializer = new XmlSerializer(typeof(T));

            using (StringReader strReader = new StringReader(serializedObjectBlob))
            using (XmlReader xmlReader = XmlReader.Create(strReader))
            {
                return (T)serializer.Deserialize(xmlReader);
            }
        }

        public string Serialize(T objectToSerialize)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            using (MemoryStream memStrm = new MemoryStream())
            {
                serializer.Serialize(memStrm, objectToSerialize);
                memStrm.Seek(0, SeekOrigin.Begin);
                using (StreamReader sw = new StreamReader(memStrm))
                {
                    return sw.ReadToEnd();
                }
            }
        }
    }
}
