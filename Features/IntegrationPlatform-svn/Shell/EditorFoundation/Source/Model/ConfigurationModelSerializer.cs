// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    /// <summary>
    /// Model serializer that understands ConfigurationModel is a ModelRoot
    /// wrapper on the raw Configuration object that is the real root of a config.
    /// Implementation originally based on EditorFoundation XmlModelSerializer.
    /// </summary>
    public class ConfigurationModelSerializer : IModelSerializer
    {
        #region Public Methods
        /// <summary>
        /// Override model serialization to strip off our ModelRoot wrapper on the 
        /// raw BusinessModel Configuration class.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="model"></param>
        public virtual void Serialize(Stream stream, ModelObject model)
        {
            ConfigurationModel configurationModel = model as ConfigurationModel;

            using (XmlTextWriter xmlTextWriter = new XmlTextWriter(stream, Encoding.Unicode))
            {
                // Suppress default xmlns attributes at root
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add(string.Empty, string.Empty);

                xmlTextWriter.Formatting = Formatting.Indented;
                XmlSerializer xmlSerializer = new XmlSerializer(configurationModel.Configuration.GetType());
                xmlSerializer.Serialize(xmlTextWriter, configurationModel.Configuration, ns);
            }
        }

        /// <summary>
        /// Deserializes a Model.
        /// </summary>
        /// <param name="stream">The stream from which to deserialize the Model.</param>
        /// <returns>The deserialized Model.</returns>
        public virtual ModelObject Deserialize(Stream stream)
        {
            // read to memory
            StreamReader reader = new StreamReader(stream, true);
            string content = reader.ReadToEnd();

            // load configuration with validation
            Configuration configuration = TryDeserialize(content, true, true);

            // re-guid in memory
            content = Configuration.ReGuidConfigXml(configuration, content);

            ConfigurationModel configurationModel = new ConfigurationModel();
            configurationModel.Configuration = TryDeserialize(content, false, false);
            return configurationModel;
        }

        public Configuration TryDeserialize(string content, bool validate, bool prompt)
        {
            try
            {
                return Configuration.LoadFromXml(content, validate);
            }
            catch (Exception e)
            {
                if (prompt)
                {
                    StringBuilder stringBuilder = new StringBuilder();

                    stringBuilder.AppendLine("The configuration file contains the following XML validation errors:\n");
                    stringBuilder.AppendLine(e.Message);
                    stringBuilder.AppendLine("Attempt auto-repair?");
                    System.Windows.Window mainWindow = null;
                    DialogResult result = DialogResult.No;
                    System.Windows.Application.Current.Dispatcher.Invoke((System.Action)(() => 
                        {
                            mainWindow = System.Windows.Application.Current.MainWindow;
                            result = MessageBox.Show(new WindowWrapper(mainWindow), stringBuilder.ToString(), "Invalid XML",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                        }));
                    if (result == DialogResult.Yes)
                    {
                        return Configuration.LoadFromXml(content, false);
                    }
                    else
                    {
                        throw;
                    }
                }
                else
                {
                    throw;
                }
            }
        }
        #endregion
    }
}
