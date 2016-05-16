// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// This class provides a property bag to store all the details of a particular conflict.
    /// </summary>
    /// <remarks>
    /// This class cannot be serialized with XmlSerializer - use the static serialization methods instead.
    /// </remarks>
    [Serializable]
    public class ConflictDetailsProperties
    {
        [XmlIgnore]
        public const string DefaultConflictDetailsKey = "Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement.DefaultConflictDetails"; // do not localize

        [XmlIgnore]
        private Dictionary<string, string> m_propertyBag = new Dictionary<string, string>();

        /// <summary>
        /// Gets all the property key-value pairs
        /// </summary>
        public Dictionary<string, string> Properties
        {
            get
            {
                return m_propertyBag;
            }
            set
            {
                m_propertyBag = value;
            }
        }

        /// <summary>
        /// Gets the value of a particular key in this property bag
        /// </summary>
        /// <param name="key">The key to search in this property bag</param>
        /// <param name="value">The output value</param>
        /// <returns>TRUE if the key is in this property bag; FALSE otherwise</returns>
        public bool TryGetValue(string key, out string value)
        {
            bool retVal = false;
            value = null;
            if (!string.IsNullOrEmpty(key) && m_propertyBag.ContainsKey(key))
            {
                value = m_propertyBag[key];
                retVal = true;
            }

            return retVal;
        }

        /// <summary>
        /// Gets the value of a particular key in this property bag
        /// </summary>
        /// <param name="key">The key to search in this property bag</param>
        /// <returns>The output value</returns>
        /// <remarks>Empty string is returned if the key is not found in this property bag</remarks>
        [XmlIgnore]
        public string this[string key]
        {
            get
            {
                string retVal;
                if (!TryGetValue(key, out retVal))
                {
                    retVal = string.Empty;
                }

                return retVal;
            }
        }

        /// <summary>
        /// Overwrites the default ToString to output the serialized xml document content.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Serialize(this);
        }

        /// <summary>
        /// Serialize a ConflictDetailsProperties instance
        /// </summary>
        /// <param name="objectToSerialize"></param>
        /// <returns></returns>
        public static string Serialize(
            ConflictDetailsProperties objectToSerialize)
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(ConflictDetailsProperties));

            using (MemoryStream memStrm = new MemoryStream())
            {
                serializer.WriteObject(memStrm, objectToSerialize);
                memStrm.Seek(0, SeekOrigin.Begin);
                using (StreamReader sw = new StreamReader(memStrm))
                {
                    return sw.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Deserialize ConflictDetailsProperties
        /// </summary>
        /// <param name="objectBlob"></param>
        /// <returns></returns>
        public static ConflictDetailsProperties Deserialize(string objectBlob)
        {
            if (string.IsNullOrEmpty(objectBlob))
            {
                throw new ArgumentNullException("objectBlob");
            }

            DataContractSerializer serializer = new DataContractSerializer(typeof(ConflictDetailsProperties));

            using (StringReader strReader = new StringReader(objectBlob))
            using (XmlReader xmlReader = XmlReader.Create(strReader))
            {
                return (ConflictDetailsProperties)serializer.ReadObject(xmlReader);
            }
        }
    }
}
