// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using Microsoft.TeamFoundation.Migration.BusinessModel.BusinessRuleEvaluation;
using System.Text.RegularExpressions;

namespace Microsoft.TeamFoundation.Migration.BusinessModel
{
    /// <summary>
    /// The configuration class is used to manage the configuration of the toolkit.
    /// </summary>
    public partial class Configuration
    {
        private ConfigurationStatus m_status = ConfigurationStatus.Valid;
        private Dictionary<Guid, MigrationSource> m_searchResultCache = new Dictionary<Guid, MigrationSource>();

        /// <summary>
        /// ConfigurationStatus provides an enumeration list of valid configuration statuses.
        /// </summary>
        public enum ConfigurationStatus
        {
            Valid = 0,
            Proposed = 1,
            Obsolete = 2,
        }

        [XmlIgnore]
        public BusinessModelManager Manager
        {
            get;
            set;
        }
                
        /// <summary>
        /// Proposes the current configuration changes to the BusinessModelManager.  
        /// If a manager isn't currently associated with this configuration, a new one is created
        /// and a detached configuration is proposed to that manager.
        /// </summary>
        /// <seealso cref="Microsoft.TeamFoundation.Migration.BusinessModel.BusinessModelManager"/>
        public void Propose()
        {
            if (null != Manager)
            {
                Manager.ProposeChanges();
            }
            else
            {
                Manager = new BusinessModelManager();
                Manager.ProposeDetachedConfiguration(this);
            }
        }
        /// <summary>
        /// Gets or sets the current ConfigurationStatus for this Configuration
        /// </summary>
        /// <seealso cref="Microsoft.TeamFoundation.Migration.BusinessModel.Configuration.ConfigurationStatus"/>
        [XmlIgnore]
        public ConfigurationStatus Status
        {
            get { return m_status; }
            set { m_status = value; }
        }

        /// <summary>
        /// Loads the configuration for the specified configuration id.
        /// </summary>
        /// <param name="internalConfigurationId">Identifier for the configuration</param>
        /// <returns>A Configuration object populated with the desired configuration.</returns>
        public static Configuration LoadConfiguration(int internalConfigurationId)
        {
            BusinessModelManager manager  = new BusinessModelManager();
            return manager.LoadConfiguration(internalConfigurationId);
        }

        /// <summary>
        /// Loads the active configuration for the specified session.
        /// </summary>
        /// <param name="sessionGroupId">Identifier for the session group</param>
        /// <returns>A Configuration object populated with the desired configuration.</returns>
        public static Configuration LoadActiveConfiguration(int sessionGroupId)
        {
            BusinessModelManager manager = new BusinessModelManager();
            return manager.LoadActiveConfiguration(sessionGroupId);
        }

        /// <summary>
        /// Retrieves the configured migration source based on the source id.
        /// </summary>
        /// <param name="migrationSourceId">Identifier of the source to retrieve</param>
        /// <returns>The MigrationSource object associated with the specified id; returns null if the source id cannot be found.</returns>
        public MigrationSource GetMigrationSource(Guid migrationSourceId)
        {
            if (m_searchResultCache.ContainsKey(migrationSourceId))
            {
                return m_searchResultCache[migrationSourceId];
            }

            foreach (MigrationSource source in this.SessionGroup.MigrationSources.MigrationSource)
            {
                Guid id = new Guid(source.InternalUniqueId);
                if (id == migrationSourceId)
                {
                    m_searchResultCache.Add(migrationSourceId, source);
                    return source;
                }
            }

            return null;
        }
        
        [XmlIgnore]
        public Guid SessionGroupUniqueId 
        {
            get
            {
                return new Guid(this.SessionGroup.SessionGroupGUID);
            }
        }

        public static Configuration LoadFromFile(string configFile)
        {
            return LoadFromFile(configFile, true);
        }

        public static Configuration LoadFromFile(string configFile, bool validate)
        {
            if (string.IsNullOrEmpty(configFile))
            {
                throw new ArgumentNullException("configFile");
            }

            if (!File.Exists(configFile))
            {
                throw new InvalidConfigurationException(
                    string.Format(Resource.ErrorMissingConfigurationFile, configFile));
            }

            string configFileContent = File.ReadAllText(configFile);
            return LoadFromXml(configFileContent, validate);            
        }

        public static Configuration LoadFromXml(string configXmlString)
        {
            return LoadFromXml(configXmlString, true);
        }

        public static Configuration LoadFromXml(string configXmlString, bool validate)
        {
            if (string.IsNullOrEmpty(configXmlString))
            {
                throw new ArgumentNullException("configXmlString");
            }

            configXmlString = GuidStrToLower(configXmlString);

            if (validate)
            {
                // session group config validation
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(configXmlString);
                ConfigurationValidator validator = new ConfigurationValidator();
                validator.ValidateXmlFragment(string.Empty, doc.DocumentElement, Constants.ConfigXsdResourcePath);
                var result = validator.ValidationResult;
                if (!result.IsValid)
                {
                    throw new ConfigurationSchemaViolationException(result);
                }
            }

            Configuration config = BusinessModelManager.Deserialize(configXmlString, typeof(Configuration)) as Configuration;

            if (validate)
            {
                // session-type-sepecific custom setting validation
                foreach (Session session in config.SessionGroup.Sessions.Session)
                {
                    ValidateCustomSettings(session.CustomSettings, session.SessionType);
                }

                BusinessRuleEvaluationEngine evaluationEngine = new BusinessRuleEvaluationEngine();
                var evaluationResult = evaluationEngine.Evaluate(config);

                if (!evaluationResult.Passed)
                {
                    throw new ConfigurationBusinessRuleViolationException(evaluationResult);
                }
            }

            return config;
        }

        public static void ValidateCustomSettings(GenericSettingsElement customSettings, SessionTypeEnum sessionType)
        {
            string settingXml = BusinessModelManager.GenericSettingXmlToString(customSettings.SettingXml);
            if (!string.IsNullOrEmpty(settingXml))
            {
                XmlDocument settingDoc = new XmlDocument();
                settingDoc.LoadXml(settingXml);

                string pathToXsd = string.Empty;
                switch (sessionType)
                {
                    case SessionTypeEnum.WorkItemTracking:
                        pathToXsd = Constants.WITConfigXsdResourcePath;
                        break;
                    case SessionTypeEnum.VersionControl:
                        pathToXsd = Constants.VCConfigXsdResourcePath;
                        break;
                    default:
                        break;
                }

                if (!string.IsNullOrEmpty(pathToXsd))
                {
                    ConfigurationValidator configValidator = new ConfigurationValidator();
                    configValidator.ValidateXmlFragment(string.Empty, settingDoc.DocumentElement, pathToXsd);

                    var sessionConfigValidateResult = configValidator.ValidationResult;
                    if (!sessionConfigValidateResult.IsValid)
                    {
                        throw new ConfigurationSchemaViolationException(sessionConfigValidateResult);
                    }
                }
            }
        }

        public static string ReGuidConfigXml(Configuration config, string configXml)
        {
            Dictionary<string, string> guidMappings;
            return ReGuidConfigXml(config, configXml, out guidMappings);            
        }

        public static string ReGuidConfigXml(
            Configuration config, 
            string configXml, 
            out Dictionary<string, string> oldToNewGuidMappings)
        {
            configXml = GuidStrToLower(configXml);

            // find all guids
            Dictionary<string, string> guidMappings = Utility.CreateGuidStringMappings(configXml, true);

            // remove provider guids
            foreach (var provider in config.Providers.Provider)
            {
                if (provider.ReferenceName != null)
                {
                    guidMappings.Remove(provider.ReferenceName);
                }
            }

            // remove addin guids
            foreach (var addin in config.Addins.Addin)
            {
                if (addin.ReferenceName != null)
                {
                    guidMappings.Remove(addin.ReferenceName);
                }
            }

            foreach (MigrationSource migrationSource in config.SessionGroup.MigrationSources.MigrationSource)
            {
                foreach (AddinElement addinElement in migrationSource.Settings.Addins.Addin)
                {
                    if (addinElement.ReferenceName != null)
                    {
                        guidMappings.Remove(addinElement.ReferenceName);
                    }
                }
            }
            oldToNewGuidMappings = guidMappings;

            // re-guid in memory
            return Utility.ReplaceGuids(configXml, guidMappings);
        }

        private static string GuidStrToLower(string configXml)
        {
            // find all guids
            Dictionary<string, string> guidMappings = Utility.CreateGuidStringMappings(configXml, false);

            // Guid strings ToLower in memory
            return Utility.ReplaceGuids(configXml, guidMappings);
        }
    }
}
