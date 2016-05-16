// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.BusinessModel.ConflictResolutionRules;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// This utility class imports the configuration zip archive that is produced by the peer
    /// ConfigExporter class.
    /// </summary>
    public class ConfigImporter
    {
        string m_configPackage;
        bool m_excludeRules;

        public ConfigImporter(string configPackage, bool excludeRules)
        {
            m_configPackage = configPackage;
            m_excludeRules = excludeRules;
        }

        public Configuration Import()
        {
            string tempFolder = System.IO.Path.GetTempPath();
            string destFolder = Path.Combine(tempFolder, "TFSIntegrationImportConfig");
            ZipUtility.Unzip(m_configPackage, destFolder, true);

            string configFilePath = Path.Combine(destFolder, ConfigExporter.ConfigFileName);
            if (!File.Exists(configFilePath))
            {
                throw new ConfigNotExistInPackageException(m_configPackage);
            }

            try
            {
                Dictionary<string, string> oldToNewGuidMaps;
                Configuration importedConfig = LoadReGuidAndSaveConfig(configFilePath, out oldToNewGuidMaps);

                Debug.Assert(null != oldToNewGuidMaps, "oldToNewGuidMaps is NULL");
                string rulesFilePath = Path.Combine(destFolder, ConfigExporter.ResolutionRuleFileName);
                SerializableConflictResolutionRuleCollection ruleCollection = LoadResolutionRulesAndReGuid(rulesFilePath, oldToNewGuidMaps);

                if (null != ruleCollection)
                {
                    ResolutionRuleImporter ruleImporter = new ResolutionRuleImporter();
                    ruleImporter.Import(ruleCollection);
                }

                return importedConfig;
            }
            finally
            {
                // clean-up the extracted temporary files
                DirectoryInfo directoryInfo = new DirectoryInfo(destFolder);
                if (directoryInfo.Exists)
                {
                    directoryInfo.Delete(true);
                }
            }
        }

        public static Configuration LoadReGuidAndSaveConfig(string configFilePath, out Dictionary<string, string> oldToNewGuidMaps)
        {
            Configuration config = Configuration.LoadFromFile(configFilePath);
            string configFileContent = File.ReadAllText(configFilePath);
            configFileContent = Configuration.ReGuidConfigXml(config, configFileContent, out oldToNewGuidMaps);
            config = Configuration.LoadFromXml(configFileContent);
            var configSaver = new SessionGroupConfigurationManager(config);
            configSaver.TrySave(false);

            return config;
        }

        private SerializableConflictResolutionRuleCollection LoadResolutionRulesAndReGuid(string rulesFilePath, Dictionary<string, string> oldToNewGuidMaps)
        {
            if (!m_excludeRules && File.Exists(rulesFilePath))
            {
                string ruleFileContent = File.ReadAllText(rulesFilePath);
                ruleFileContent = ReGuidResolutionRulesXml(ruleFileContent, oldToNewGuidMaps);

                SerializableConflictResolutionRuleCollection ruleCollection;
                XmlSerializer serializer = new XmlSerializer(typeof(SerializableConflictResolutionRuleCollection));
                using (var strReader = new StringReader(ruleFileContent))
                using (var xmlReader = XmlReader.Create(strReader))
                {
                    ruleCollection = serializer.Deserialize(xmlReader) as SerializableConflictResolutionRuleCollection;
                }
                return ruleCollection;
            }
            else
            {
                return null;
            }
        }

        private string ReGuidResolutionRulesXml(string ruleFileContent, Dictionary<string, string> oldToNewGuidMaps)
        {
            return Microsoft.TeamFoundation.Migration.Utility.ReplaceGuids(ruleFileContent, oldToNewGuidMaps);
        }
    }
}
