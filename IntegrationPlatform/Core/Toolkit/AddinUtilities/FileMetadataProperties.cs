// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Xml;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// This class can be used by an implementation of the IFilePropertiesAddin to provide the file property data.
    /// It implements the IDictionary&lt;string,string&gt; interface to allow name/value pairs to be easily added and manipulated
    /// and methods to convert the data to and from an XmlDocument which is need when assigning creating a MigrationAction
    /// of type AddFileProperties where the detailed data is the XmlDocument returned by the ToXmlDocument method of this class
    /// </summary>
    public class FileMetadataProperties : IDictionary<string,string>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public FileMetadataProperties()
        {
        }
 
        #region public Methods
        public XmlDocument ToXmlDocument()
        {
            XmlDocument doc = new XmlDocument();
            int propertyNumber = 0;
            XmlElement rootElement = doc.CreateElement("FileMetadataProperties");
            doc.AppendChild(rootElement);
            foreach (KeyValuePair<string, string> nameValuePair in m_properties)
            {
                XmlAttribute attribute = doc.CreateAttribute(nameValuePair.Key);
                attribute.Value = nameValuePair.Value;
                XmlElement element = doc.CreateElement("Property" + (propertyNumber++).ToString());
                element.Attributes.Append(attribute);
                rootElement.AppendChild(element);
            }
            return doc;
        }

        public static FileMetadataProperties CreateFromXmlDocument(XmlDocument doc)
        {
            FileMetadataProperties fileProperties = new FileMetadataProperties();
            XmlNode rootNode = doc.ChildNodes[0];

            foreach (XmlElement element in rootNode.ChildNodes)
            {
                if (!fileProperties.ContainsKey(element.Attributes[0].Name))
                {
                    fileProperties.Add(element.Attributes[0].Name, element.Attributes[0].Value);
                }
            }

            return fileProperties;
        }

        #endregion


        #region IDictionary implementation

        /// <summary>
        /// Adds an element with the provided key and value to the FileMetadataProperties
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(string key, string value)
        {
            if (m_properties.ContainsKey(key))
            {
                m_properties[key] = value;
            }
            else
            {
                m_properties.Add(key, value);
            }
        }

        /// <summary>
        /// Removes the element with the specified key from the FileMetadataProperties
        /// </summary>
        /// <param name="key"></param>
        public bool Remove(string key)
        {
            return m_properties.Remove(key);
        }

        /// <summary>
        /// Determines whether the FileMetadataProperties contains an element with the specified key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string key)
        {
            return m_properties.ContainsKey(key);
        }

        /// <summary>
        /// Returns the value associated with the specified key from the FileMetadataProperties
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string this[string key]
        {
            get
            {
                return m_properties[key];
            }

            set
            {
                m_properties[key] = value;
            }
        }

        /// <summary>
        /// Returns a collection of all of the keys in the FileMetadataProperties
        /// </summary>
        public ICollection<string> Keys
        {
            get { return m_properties.Keys; }
        }

        /// <summary>
        /// Returns a collection of all of the values in the FileMetadataProperties
        /// </summary>
        public ICollection<string> Values
        {
            get { return m_properties.Values; }
        }

        /// <summary>
        /// Gets the value associated with the specified key
        /// </summary>
        /// <param name="key">The key to find</param>
        /// <param name="value">The value associated with the specified key is return, or null if the key is not found</param>
        /// <returns>A bool indicating whether or not the key was found</returns>
        public bool TryGetValue(string key, out string value)
        {
            return m_properties.TryGetValue(key, out value);
        }

        /// <summary>
        /// Removes all of the elements from the FileMetadataProperties
        /// </summary>
        public void Clear()
        {
            m_properties.Clear();
        }

        /// <summary>
        /// Adds a key/value pair to the FileMetadataProperties
        /// </summary>
        /// <param name="keyValuePair">The KeyValuePair string to add</param>
        public void Add(KeyValuePair<string, string> keyValuePair)
        {
            m_properties.Add(keyValuePair.Key, keyValuePair.Value);
        }

        /// <summary>
        /// Removes a key/value pair from the FileMetadataProperties
        /// </summary>
        /// <param name="keyValuePair"></param>
        /// <returns></returns>
        public bool Remove(KeyValuePair<string, string> keyValuePair)
        {
            return m_properties.Remove(keyValuePair.Key);
        }

        /// <summary>
        /// Determines whether the FileMetadataProperties contains the specified key/value pair
        /// </summary>
        /// <param name="keyValuePair"></param>
        /// <returns></returns>
        public bool Contains(KeyValuePair<string,string> keyValuePair)
        {
            return ((ICollection<KeyValuePair<string, string>>)m_properties).Contains(keyValuePair);
        }

        /// <summary>
        /// Copies the FileMetadataProperties elements to an array of KeyValuePair strings
        /// </summary>
        /// <param name="keyValuePairArray"></param>
        /// <param name="index"></param>
        public void CopyTo(KeyValuePair<string,string>[] keyValuePairArray, int index)
        {
            ((ICollection<KeyValuePair<string, string>>)m_properties).CopyTo(keyValuePairArray, index);
        }

        /// <summary>
        /// The number of key/value pair elements in the FileMetadataProperties
        /// </summary>
        public int Count
        {
            get { return m_properties.Count; }
        }

        /// <summary>
        /// Returns an Enumerator of the FileMetadataProperties elements
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_properties.GetEnumerator();
        }
        
        /// <summary>
        /// Returns an Enumerator of the FileMetadataProperties elements
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return m_properties.GetEnumerator();
        }

        /// <summary>
        /// Whether or not the FileMetadataProperties is read-only.   This is included because 
        /// FileMetadataProperties implements IDictionary, but is always false.
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }
        #endregion

        // ****************************************************************************
        // Private Members
        // ****************************************************************************
       
        // The set of name/value pair properties
        private Dictionary<string, string> m_properties = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
    }
}
