// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    /// <summary>
    /// A serializer that uses standard .NET Xml Serialization to serialize and deserialize a Model.
    /// </summary>
    /// <typeparam name="T">The Model type.</typeparam>
    public class XmlModelSerializer<T> 
        : IModelSerializer 
        where T : ModelObject
    {
        #region Fields
        private readonly XmlSchema schema;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the XmlModelSerializer class.
        /// </summary>
        /// <param name="schema">The Xml Schema for the Model.</param>
        public XmlModelSerializer (XmlSchema schema)
        {
            this.schema = schema;
        }

        /// <summary>
        /// Initializes a new instance of the XmlModelSerializer class.
        /// </summary>
        /// <remarks>
        /// If this constructor is used, no Xml Schema validation is performed.
        /// </remarks>
        public XmlModelSerializer ()
            : this (null)
        {
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Serializes a Model.
        /// </summary>
        /// <param name="stream">The stream to which to serialize the Model.</param>
        /// <param name="model">The Model to serialize.</param>
        public virtual void Serialize(Stream stream, ModelObject model)
        {
            XmlTextWriter xmlTextWriter = new XmlTextWriter(stream, Encoding.Unicode);

            xmlTextWriter.Formatting = Formatting.Indented;
            XmlSerializer xmlSerializer = new XmlSerializer(model.GetType());
            xmlSerializer.Serialize(xmlTextWriter, model);
        }

        /// <summary>
        /// Deserializes a Model.
        /// </summary>
        /// <param name="stream">The stream from which to deserialize the Model.</param>
        /// <returns>The deserialized Model.</returns>
        public virtual ModelObject Deserialize (Stream stream)
        {
            Type type = typeof (T);

            XmlReader xmlReader = null;
            string schemaValidationError = null;

            // Check if Xml Schema information is available for this type
            if (this.schema != null)
            {
                ValidationEventHandler onValidationEvent = delegate (object sender, ValidationEventArgs args)
                {
                    if (args.Severity == XmlSeverityType.Error)
                    {
                        // Note that a schema validation exception happened
                        schemaValidationError = args.Message;
                    }
                };

                // Setup an XmlReader with schema validation
                XmlReaderSettings xmlReaderSettings = new XmlReaderSettings ();
                xmlReaderSettings.ValidationType = ValidationType.Schema;
                xmlReaderSettings.Schemas.Add (this.schema);
                xmlReaderSettings.ValidationEventHandler += onValidationEvent;
                xmlReader = XmlReader.Create (stream, xmlReaderSettings);
            }
            else
            {
                // Setup an XmlReader with no schema validation
                xmlReader = XmlReader.Create (stream);
            }

            T model = null;
            try
            {
                // Try to deserialize the model
                XmlSerializer xmlSerializer = new XmlSerializer (type);
                xmlSerializer.UnknownNode += new XmlNodeEventHandler(xmlSerializer_UnknownNode);
                model = (T)xmlSerializer.Deserialize (xmlReader);
            }
            catch (InvalidOperationException exception)
            {
                // If a schema validation error occurred, throw the associated exception
                if (schemaValidationError != null)
                {
                    string message = exception.Message + Environment.NewLine + schemaValidationError;
                    throw new Exception (message, exception);
                }

                // Otherwise throw the generic exception
                throw exception;
            }
            finally
            {
                xmlReader.Close ();
            }

            return model;

        }

        void xmlSerializer_UnknownNode(object sender, XmlNodeEventArgs e)
        {
            throw new InvalidOperationException(string.Format("Invalid {0} {1} at Line {2}, position {3}", e.NodeType, e.Name, e.LineNumber, e.LinePosition));
        }
        #endregion
    }
}
