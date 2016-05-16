// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Collections.ObjectModel;

namespace Microsoft.TeamFoundation.Migration.BusinessModel
{
    /// <summary>
    /// Validator of the configuration xml document
    /// </summary>
    /// <remarks>Each validator instance can only be used to validate a configuration once.</remarks>
    internal class ConfigurationValidator
    {
        List<ValidationEventArgs> m_schemaValidationResults = new List<ValidationEventArgs>();
        XmlSchema m_schema = null;
        string m_configFileName = string.Empty;

        /// <summary>
        /// This method will validate the given xml fragment against the given xsd file (embedded in this assembly) 
        /// </summary>
        /// <param name="xmlFileName">XML file name from which the fragment has to be validated</param>
        /// <param name="node">Node with the XML fragment</param>
        /// <param name="xsdFile">XSD file name to validate</param>
        /// <returns>XmlDocument object with xml file contents</returns>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public XmlDocument ValidateXmlFragment(string xmlFileName, XmlNode node, string xsdFile)
        {
            if (m_schemaValidationResults.Count > 0)
            {
                throw new InvalidOperationException(Resource.ErrorUsingConfigValidatorMultiTimes);
            }

            m_configFileName = xmlFileName;

            //Assuming that the xsd file is embedded in the assembly the file is being opened.
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(xsdFile))
            {
                XmlParserContext context = new XmlParserContext(null, null, string.Empty, XmlSpace.None);
                m_schema = XmlSchema.Read(new XmlTextReader(stream), null);

                // Load XML document with validator
                XmlDocument xmldoc = new XmlDocument();
                XmlReaderSettings settings = new XmlReaderSettings();

                settings.CheckCharacters = true;
                settings.CloseInput = true;
                settings.ConformanceLevel = ConformanceLevel.Fragment;
                settings.IgnoreComments = false;
                settings.Schemas.Add(m_schema);
                settings.ValidationType = ValidationType.Schema;
                settings.ValidationEventHandler += new ValidationEventHandler(settings_ValidationEventHandler);

                //using (reader = XmlReader.Create(new XmlTextReader(node.OuterXml, XmlNodeType.Element, context), settings))
                using (XmlReader reader = XmlReader.Create(new StringReader(node.OuterXml), settings, context))
                {
                    xmldoc.Load(reader);
                    return xmldoc;
                }
            }
        }

        /// <summary>
        /// Gets the configuration validation result
        /// </summary>
        public ConfigurationValidationResult ValidationResult
        {
            get
            {
                return new ConfigurationValidationResult(
                    m_configFileName,
                    m_schemaValidationResults.AsReadOnly(), 
                    m_schema);
            }
        }

        private void settings_ValidationEventHandler(object sender, ValidationEventArgs e)
        {
            m_schemaValidationResults.Add(e);
        }       
    }
}
