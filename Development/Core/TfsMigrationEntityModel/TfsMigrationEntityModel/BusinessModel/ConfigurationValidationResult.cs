// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Xml.Schema;
using System.Xml;
using System.IO;

namespace Microsoft.TeamFoundation.Migration.BusinessModel
{
    /// <summary>
    /// Encapsulates the configuration validation results
    /// </summary>
    public class ConfigurationValidationResult
    {
        ReadOnlyCollection<ValidationEventArgs> m_schemaValidationResults;
        XmlSchema m_schema;
        string m_configFileName;

        internal ConfigurationValidationResult(
            string configFileName,
            ReadOnlyCollection<ValidationEventArgs> schemaValidationResults,
            XmlSchema schema)
        {
            m_configFileName = configFileName;
            m_schemaValidationResults = schemaValidationResults;
            m_schema = schema;
        }

        /// <summary>
        /// Gets the schema validation results
        /// </summary>
        public ReadOnlyCollection<ValidationEventArgs> SchemaValidationResults
        {
            get
            {
                return m_schemaValidationResults;
            }
        }

        /// <summary>
        /// The schema used to validate the configuration
        /// </summary>
        public XmlSchema Schema
        {
            get
            {
                return m_schema ?? new XmlSchema();
            }
        }

        /// <summary>
        /// Gets whether the validated configuration is valid
        /// </summary>
        public bool IsValid
        {
            get
            {
                return (m_schemaValidationResults.Count == 0);
            }
        }

        /// <summary>
        /// Prints the validation results to a string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("---- Configuration Validation Result ---- ");

            if (!string.IsNullOrEmpty(m_configFileName))
            {
                try
                {
                    string fullPath = Path.GetFullPath(m_configFileName);
                    sb.AppendLine("Configuration File: " + fullPath);
                }
                catch (Exception)
                {
                    sb.AppendLine("Configuration File: " + m_configFileName);
                }
            }

            for (int i = 0; i < m_schemaValidationResults.Count; ++i)
            {
                ValidationEventArgs arg = m_schemaValidationResults.ElementAt(i);
                sb.AppendLine("Issue #: " + i.ToString());
                sb.AppendLine("Severity: " + arg.Severity.ToString());
                sb.AppendLine("Description: " + (arg.Message ?? string.Empty));
                sb.AppendLine("Line: " + arg.Exception.LineNumber + ", Column: " + arg.Exception.LinePosition);
                sb.AppendLine("Source: " + (arg.Exception.Source ?? string.Empty));
                if (null != arg.Exception.Data)
                {
                    foreach (var key in arg.Exception.Data.Keys)
                    {
                        sb.AppendLine("Key: " + key.ToString() + ", Value: " + arg.Exception.Data[key]);
                    }
                }
            }

            sb.AppendLine("---- Configuration Schema ----");
            PrintSchema(sb);

            return sb.ToString();
        }

        private void PrintSchema(StringBuilder sb)
        {
            XmlWriterSettings writerSettings = new XmlWriterSettings();
            writerSettings.CloseOutput = false;
            writerSettings.Indent = true;
            writerSettings.IndentChars = "  ";
            writerSettings.NewLineChars = Environment.NewLine;
            writerSettings.NewLineOnAttributes = false;
            writerSettings.OmitXmlDeclaration = false;
            writerSettings.NewLineHandling = NewLineHandling.Replace;
            writerSettings.NewLineOnAttributes = false;
            using (XmlWriter writer = XmlWriter.Create(sb, writerSettings))
            {
                Schema.Write(writer);
            }
        }
    }
}
