// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.TeamFoundation.Migration.BusinessModel
{
    /// <summary>
    /// Session class
    /// </summary>
    public partial class Session
    {
        private VC.VCSessionCustomSetting m_vcCustomSetting = null;
        private WIT.WITSessionCustomSetting m_witCustomSetting = null;
        private bool m_customSettingEventHandlerRegistered = false;

        private void TryRegisterEventHandler()
        {
            if (!m_customSettingEventHandlerRegistered)
            {
                PropertyChanged += new UndoablePropertyChangedEventHandler(Session_PropertyChanged);
                m_customSettingEventHandlerRegistered = true;
            }
        }

        void Session_PropertyChanged(ModelObject sender, UndoablePropertyChangedEventArgs eventArgs)
        {
            if (sender == this && eventArgs.PropertyName.ToString().Equals("CustomSettings"))
            {
                m_vcCustomSetting = null;
                m_witCustomSetting = null;
            }
        }

        [XmlIgnore]
        public Guid SessionUniqueIdGuid
        {
            get
            {
                return new Guid(this.SessionUniqueId);
            }
        }

        /// <summary>
        /// Gets the custom settings for Version Control session 
        /// </summary>
        [XmlIgnore]
        public VC.VCSessionCustomSetting VCCustomSetting
        {
            get
            {
                TryRegisterEventHandler();

                if (m_vcCustomSetting == null)
                {
                    if (this.SessionType == SessionTypeEnum.WorkItemTracking)
                    {
                        var config = new VC.VCSessionCustomSetting();
                        config.SessionConfig = this;
                        m_vcCustomSetting = config;
                    }
                    else
                    {
                        try
                        {
                            string settingXml = BusinessModelManager.GenericSettingXmlToString(this.CustomSettings.SettingXml);

                            if (string.IsNullOrEmpty(settingXml))
                            {
                                return new VC.VCSessionCustomSetting();
                            }

                            XmlDocument settingDoc = new XmlDocument();
                            settingDoc.LoadXml(settingXml);
                            XmlReader reader = new XmlNodeReader(settingDoc.DocumentElement);

                            XmlSerializer serializer = new XmlSerializer(typeof(VC.VCSessionCustomSetting));
                            VC.VCSessionCustomSetting config =
                                serializer.Deserialize(reader) as VC.VCSessionCustomSetting;

                            config.SessionConfig = this;

                            m_vcCustomSetting = config;
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidConfigurationException(Resource.ErrorMissingSessionConfig, ex);
                        }
                    }
                }

                return m_vcCustomSetting;
            }
        }

        /// <summary>
        /// Gets the custom settings for Work Item Tracking session 
        /// </summary>
        [XmlIgnore]
        public WIT.WITSessionCustomSetting WITCustomSetting
        {
            get
            {
                TryRegisterEventHandler();

                if (m_witCustomSetting == null)
                {
                    if (this.SessionType == SessionTypeEnum.VersionControl)
                    {
                        var config = new WIT.WITSessionCustomSetting();
                        config.SessionConfig = this;
                        m_witCustomSetting = config;
                    }
                    else
                    {
                        try
                        {
                            string settingXml = BusinessModelManager.GenericSettingXmlToString(this.CustomSettings.SettingXml);

                            if (string.IsNullOrEmpty(settingXml))
                            {
                                return new WIT.WITSessionCustomSetting();
                            }

                            XmlDocument settingDoc = new XmlDocument();
                            settingDoc.LoadXml(settingXml);
                            XmlReader reader = new XmlNodeReader(settingDoc.DocumentElement);

                            XmlSerializer serializer = new XmlSerializer(typeof(WIT.WITSessionCustomSetting));
                            WIT.WITSessionCustomSetting config =
                                serializer.Deserialize(reader) as WIT.WITSessionCustomSetting;

                            config.SessionConfig = this;

                            m_witCustomSetting = config;
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidConfigurationException(Resource.ErrorMissingSessionConfig, ex);
                        }
                    }
                }

                return m_witCustomSetting;
            }
        }

        /// <summary>
        /// Gets the migration sources defined in this session.
        /// </summary>
        Dictionary<Guid, MigrationSource> m_migrationSources = new Dictionary<Guid, MigrationSource>(2);
        [XmlIgnore]
        public Dictionary<Guid, MigrationSource> MigrationSources
        {
            get
            {
                return m_migrationSources;
            }
        }

        public void UpdateCustomSetting(object sessionTypeSpecificCustomSetting)
        {
            TryRegisterEventHandler();

            XmlSerializer serializer = null;
            if (sessionTypeSpecificCustomSetting is WIT.WITSessionCustomSetting)
            {
                serializer = new XmlSerializer(typeof(WIT.WITSessionCustomSetting));
            }
            else if (sessionTypeSpecificCustomSetting is VC.VCSessionCustomSetting)
            {
                serializer = new XmlSerializer(typeof(VC.VCSessionCustomSetting));
            }

            if (null != serializer)
            {
                using (var stream = new MemoryStream())
                {
                    serializer.Serialize(stream, sessionTypeSpecificCustomSetting);
                    stream.Seek(0, SeekOrigin.Begin);
                    string xmlContent;
                    using (StreamReader sw = new StreamReader(stream))
                    {
                        xmlContent = sw.ReadToEnd();
                    }

                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml(xmlContent);

                    GenericSettingsElement updatedCustomSettings = new GenericSettingsElement();
                    updatedCustomSettings.SettingXml.Any = new XmlElement[] { xml.DocumentElement };
                    this.CustomSettings = updatedCustomSettings;
                }
            }
        }
    }
}
