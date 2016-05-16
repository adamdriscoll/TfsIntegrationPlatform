// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace MigrationTestLibrary
{
    public class ConfigurationCreator
    {
        private const string ConfigurationTemplateDirectory = "ConfigurationTemplate";

        public static Configuration CreateConfiguration(string configTemplateFileName)
        {
            string fileName = Path.GetFullPath(configTemplateFileName);

            if (File.Exists(fileName))
            {
                Trace.TraceInformation("Found Configuration file at {0}", fileName);
            }
            else
            {
                fileName = Path.Combine(ConfigurationTemplateDirectory, configTemplateFileName);
                fileName = Path.GetFullPath(fileName);
                if (File.Exists(fileName))
                {
                    Trace.TraceInformation("Found Configuration file at {0}", fileName);
                }
            }

            Configuration config = null;
            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
                    config = serializer.Deserialize(fs) as Configuration;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Loading the configuration file {0} failed", configTemplateFileName);
                Console.WriteLine(ex);
                return null;
            }

            return config;
        }

        public static Configuration CreateConfiguration(string defaultConfigFile, MigrationTestEnvironment env)
        {
            string configFile = env.ConfigurationFile;

            if (String.IsNullOrEmpty(configFile))
            {
                configFile = defaultConfigFile;
            }

            Configuration config = CreateConfiguration(configFile);
            if (config == null)
            {
                return null;
            }

            // Configuration Attributes
            config.UniqueId = Guid.NewGuid().ToString();
            if (env.TestName.Length > 35)
            {
                // note: in DB, SessionGroupConfig.FriendlyName is only 40-char long
                config.FriendlyName = env.TestName.Substring(0, 35);
            }
            else
            {
                config.FriendlyName = env.TestName; 
            }
            config.SessionGroup.WorkFlowType = env.WorkFlowType;

            // TODO: 
            // for now handle first two migration sources for now.
            // for now handle first two sessions and filter items
            Debug.Assert(config.SessionGroup.Sessions.Session.Count == 1);

            // MigrationSources
            var sources = config.SessionGroup.MigrationSources.MigrationSource;
            sources[0].ServerUrl = env.SourceServerUrl;
            sources[0].FriendlyName = env.SourceMigrationSource.ServerIdentifier;
            sources[0].ServerIdentifier = env.SourceMigrationSource.ServerIdentifier + Guid.NewGuid().ToString();
            sources[0].SourceIdentifier = env.SourceTeamProject;
            sources[0].ProviderReferenceName = env.SourceProviderRefName;
            sources[0].InternalUniqueId = Guid.NewGuid().ToString();

            foreach (Setting s in env.SourceMigrationSource.CustomSettingList.Setting)
            {
                CustomSetting customSetting = new CustomSetting();
                customSetting.SettingKey = s.SettingKey;
                customSetting.SettingValue = s.SettingValue;

                sources[0].CustomSettings.CustomSetting.Add(customSetting);
            }

            sources[1].ServerUrl = env.TargetServerUrl;
            sources[1].FriendlyName = env.TargetMigrationSource.ServerIdentifier;
            sources[1].ServerIdentifier = env.TargetMigrationSource.ServerIdentifier + Guid.NewGuid().ToString();
            sources[1].SourceIdentifier = env.TargetTeamProject;
            sources[1].ProviderReferenceName = env.TargetProviderRefName;
            sources[1].InternalUniqueId = Guid.NewGuid().ToString();

            foreach (Setting s in env.TargetMigrationSource.CustomSettingList.Setting)
            {
                CustomSetting customSetting = new CustomSetting();
                customSetting.SettingKey = s.SettingKey;
                customSetting.SettingValue = s.SettingValue;

                sources[1].CustomSettings.CustomSetting.Add(customSetting);
            }

            // Generate unique Guids for sessions
            config.SessionGroup.SessionGroupGUID = Guid.NewGuid().ToString();
            SessionsElement sessions = config.SessionGroup.Sessions;
            foreach (var session in sessions.Session)
            {
                session.SessionUniqueId = Guid.NewGuid().ToString();
                session.LeftMigrationSourceUniqueId = sources[0].InternalUniqueId;
                session.RightMigrationSourceUniqueId = sources[1].InternalUniqueId;
            }

            // Build mappings at run time based on env.Mappings
            Debug.Assert(sessions.Session[0].Filters.FilterPair.Count == 0, 
                "MigrationTestEnvironment template should not contain filter items. These will be ignored");

            // Ignore any existing mappings in the template file
            sessions.Session[0].Filters.FilterPair.Clear();
            foreach (MappingPair pair in env.Mappings)
            {
                FilterItem source = new FilterItem();
                FilterItem target = new FilterItem();
                source.FilterString = pair.SourcePath;
                target.FilterString = pair.TargetPath;
                if (!string.IsNullOrEmpty(pair.SourceSnapshotStartPoint))
                {
                    source.SnapshotStartPoint = pair.SourceSnapshotStartPoint;
                }
                if (!string.IsNullOrEmpty(pair.TargetSnapshotStartPoint))
                {
                    target.SnapshotStartPoint = pair.TargetSnapshotStartPoint;
                }
                if (!string.IsNullOrEmpty(pair.SourcePeerSnapshotStartPoint))
                {
                    source.PeerSnapshotStartPoint = pair.SourcePeerSnapshotStartPoint;
                }
                if (!string.IsNullOrEmpty(pair.TargetPeerSnapshotStartPoint))
                {
                    target.PeerSnapshotStartPoint = pair.TargetPeerSnapshotStartPoint;
                }
                if (!string.IsNullOrEmpty(pair.SourceMergeScope))
                {
                    source.MergeScope = pair.SourceMergeScope;
                }
                if (!string.IsNullOrEmpty(pair.TargetMergeScope))
                {
                    target.MergeScope = pair.TargetMergeScope;
                }
                source.MigrationSourceUniqueId = sources[0].InternalUniqueId;
                target.MigrationSourceUniqueId = sources[1].InternalUniqueId;

                FilterPair mapping = new FilterPair();
                mapping.Neglect = pair.Cloak;
                mapping.FilterItem.Add(source);
                mapping.FilterItem.Add(target);
                
                sessions.Session[0].Filters.FilterPair.Add(mapping);
            }

            #region Snapshot configuration
            foreach (KeyValuePair<string, string> snapshotStartPoint in env.SnapshotStartPoints)
            {
                XmlDocument ownerDocument = sessions.Session[0].CustomSettings.SettingXml.Any[0].OwnerDocument;

                // Set the batch size
                if (env.SnapshotBatchSize != 0)
                {
                    XmlNode snapshotBatchSizeNode = ownerDocument.CreateNode(XmlNodeType.Element, "Setting", "");
                    XmlAttribute snapshotBatchSizeName = ownerDocument.CreateAttribute("SettingKey");
                    snapshotBatchSizeName.Value = "SnapshotBatchSize";
                    XmlAttribute snapshotBatchSizeValue = ownerDocument.CreateAttribute("SettingValue");
                    snapshotBatchSizeValue.Value = env.SnapshotBatchSize.ToString();
                    snapshotBatchSizeNode.Attributes.Append(snapshotBatchSizeName);
                    snapshotBatchSizeNode.Attributes.Append(snapshotBatchSizeValue);
                    sessions.Session[0].CustomSettings.SettingXml.Any[0].ChildNodes[0].AppendChild(snapshotBatchSizeNode);
                }

                XmlNode snapshotSettingNode = ownerDocument.CreateNode(XmlNodeType.Element, "Setting", "");
                XmlAttribute snapshotName = ownerDocument.CreateAttribute("SettingKey");
                snapshotName.Value = "SnapshotStartPoint";
                XmlAttribute snapshotValue = ownerDocument.CreateAttribute("SettingValue");

                if (string.Equals(snapshotStartPoint.Key, sources[0].SourceIdentifier))
                {
                    snapshotValue.Value = sources[0].InternalUniqueId.ToString() + ';' + snapshotStartPoint.Value;
                    snapshotSettingNode.Attributes.Append(snapshotName);
                    snapshotSettingNode.Attributes.Append(snapshotValue);
                    sessions.Session[0].CustomSettings.SettingXml.Any[0].ChildNodes[0].AppendChild(snapshotSettingNode);
                }
                else if (string.Equals(snapshotStartPoint.Key, sources[1].SourceIdentifier))
                {
                    snapshotValue.Value = sources[1].InternalUniqueId.ToString() + ';' + snapshotStartPoint.Value;
                    snapshotSettingNode.Attributes.Append(snapshotName);
                    snapshotSettingNode.Attributes.Append(snapshotValue);
                    sessions.Session[0].CustomSettings.SettingXml.Any[0].ChildNodes[0].AppendChild(snapshotSettingNode);
                }
            }
            #endregion


            // additional configuration customization
            env.CustomizeConfiguration(config);

            // Change migration direction if needed
            if (!env.IsLeftToRightWorkflow)
            {
                SwapMigrationDirection(config);
            }

            return config;
        }

        public static void CreateConfigurationFile(Configuration config, string fileName)
        {
            using (XmlTextWriter writer = new XmlTextWriter(fileName, null))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
                serializer.Serialize(writer, config);
            }

            try
            {
                Trace.WriteLine(String.Format("---- Begin dump of Configuration File '{0}' ----", fileName));
                // Dump the config file to trace
                foreach (string line in File.ReadAllLines(fileName))
                {
                    Trace.WriteLine(line);
                }
            }
            catch
            {
            }
            Trace.WriteLine(String.Format("---- End dump of Configuration File '{0}' ----", fileName));
        }

        #region helper functions
        private static void SwapMigrationDirection(Configuration config)
        {
            SessionsElement sessions = config.SessionGroup.Sessions;
            foreach (Session session in sessions.Session)
            {
                string temp = session.LeftMigrationSourceUniqueId;
                session.LeftMigrationSourceUniqueId = session.RightMigrationSourceUniqueId;
                session.RightMigrationSourceUniqueId = temp;
            }
        }
        #endregion
    }
}
