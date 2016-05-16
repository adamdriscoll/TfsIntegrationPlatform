//------------------------------------------------------------------------------
// <copyright file="SimpleDictionary.cs" company="Microsoft Corporation">
//      Copyright © Microsoft Corporation.  All Rights Reserved.
// </copyright>
//
//  This code released under the terms of the 
//  Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//------------------------------------------------------------------------------

namespace Microsoft.TeamFoundation.Integration.SharePointWITAdapter
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// An simple implementation of a dictionary.
    /// </summary>
    /// <typeparam name="Key">The type of the key.</typeparam>
    /// <typeparam name="Value">The type of the value.</typeparam>
    /// <remarks>This is a simple generic dictionary for use in the SharePoint items. It differs from the standard Dictionary in that it can be serialised and it stores keys and values seperately.</remarks>
    [XmlRoot("simpleDictionary")]
    public class SimpleDictionary<Key, Value> : IEnumerable, IXmlSerializable
    {
        private List<Key> keys = new List<Key>();
        private List<Value> values = new List<Value>();

        /// <summary>
        /// Gets the keys.
        /// </summary>
        /// <value>The keys.</value>
        public List<Key> Keys
        {
            get
            {
                return keys;
            }
        }

        /// <summary>
        /// Gets the values.
        /// </summary>
        /// <value>The values.</value>
        public List<Value> Values
        {
            get
            {
                return values;
            }
        }

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator GetEnumerator()
        {
            return (IEnumerator)new SimpleDictionaryEnumerator(this);
        }

        /// <summary>
        /// Adds the specified key &amp; value to the dictionary.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Add(Key key, Value value)
        {
            keys.Add(key);
            values.Add(value);
        }

        /// <summary>
        /// Adds the specified object to the dictionary.
        /// </summary>
        /// <param name="o">The o.</param>
        public void Add(object o)
        {
            KeyValuePair<Key, Value>? keyValuePair = o as KeyValuePair<Key, Value>?;
            if (keyValuePair != null)
            {
                this.Add(keyValuePair.Value.Key, keyValuePair.Value.Value);
            }
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            keys.Clear();
            values.Clear();
        }

        #endregion

        private class SimpleDictionaryEnumerator : IEnumerator
        {
            private SimpleDictionary<Key, Value> simpleDictionary;
            private int index = -1;

            /// <summary>
            /// Initializes a new instance of the <see cref="SimpleDictionary&lt;Key, Value&gt;.SimpleDictionaryEnumerator"/> class.
            /// </summary>
            /// <param name="simpleDictionary">The simple dictionary.</param>
            public SimpleDictionaryEnumerator(SimpleDictionary<Key, Value> simpleDictionary)
            {
                this.simpleDictionary = simpleDictionary;
            }

            #region IEnumerator Members

            /// <summary>
            /// Gets the current element in the collection.
            /// </summary>
            /// <value></value>
            /// <returns>
            /// The current element in the collection.
            /// </returns>
            /// <exception cref="T:System.InvalidOperationException">
            /// The enumerator is positioned before the first element of the collection or after the last element.
            /// </exception>
            public object Current
            {
                get
                {
                    return new KeyValuePair<Key, Value>(simpleDictionary.keys[index], simpleDictionary.values[index]);
                }
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
            /// </returns>
            /// <exception cref="T:System.InvalidOperationException">
            /// The collection was modified after the enumerator was created.
            /// </exception>
            public bool MoveNext()
            {
                index++;
                return !(index >= simpleDictionary.keys.Count);

            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="T:System.InvalidOperationException">
            /// The collection was modified after the enumerator was created.
            /// </exception>
            public void Reset()
            {
                index = -1;
            }

            #endregion
        }

        #region IXmlSerializable Members

        /// <summary>
        /// This method is reserved and should not be used. When implementing the IXmlSerializable interface, you should return null (Nothing in Visual Basic) from this method, and instead, if specifying a custom schema is required, apply the <see cref="T:System.Xml.Serialization.XmlSchemaProviderAttribute"/> to the class.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Xml.Schema.XmlSchema"/> that describes the XML representation of the object that is produced by the <see cref="M:System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter)"/> method and consumed by the <see cref="M:System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader)"/> method.
        /// </returns>
        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        /// Generates an object from its XML representation.
        /// </summary>
        /// <param name="reader">The <see cref="T:System.Xml.XmlReader"/> stream from which the object is deserialized.</param>
        public void ReadXml(XmlReader reader)
        {
            if (reader.IsEmptyElement)
            {
                return;
            }

            XmlSerializer keySerialiser = new XmlSerializer(typeof(Key));
            XmlSerializer valueSerialiser = new XmlSerializer(typeof(Value));

            reader.Read();
            while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
            {
                reader.ReadStartElement("keyValuePair");

                reader.ReadStartElement("key");
                Key key = (Key)keySerialiser.Deserialize(reader);
                reader.ReadEndElement();

                reader.ReadStartElement("value");
                Value value = (Value)valueSerialiser.Deserialize(reader);
                reader.ReadEndElement();

                this.Add(key, value);

                reader.ReadEndElement(); // for keyvaluepair
                reader.MoveToContent();
            }

            reader.ReadEndElement(); // for root
        }

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Xml.XmlWriter"/> stream to which the object is serialized.</param>
        public void WriteXml(XmlWriter writer)
        {
            XmlSerializer keySerialiser = new XmlSerializer(typeof(Key));
            XmlSerializer valueSerialiser = new XmlSerializer(typeof(Value));

            for (int counter = 0; counter < this.keys.Count; counter++)
            {
                writer.WriteStartElement("keyValuePair");

                writer.WriteStartElement("key");
                keySerialiser.Serialize(writer, this.keys[counter]);
                writer.WriteEndElement();

                writer.WriteStartElement("value");
                valueSerialiser.Serialize(writer, this.values[counter]);
                writer.WriteEndElement();

                writer.WriteEndElement();

            }
        }

        #endregion
    }
}
