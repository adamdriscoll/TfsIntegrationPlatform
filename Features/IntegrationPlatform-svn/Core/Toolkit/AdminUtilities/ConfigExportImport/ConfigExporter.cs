// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.BusinessModel.ConflictResolutionRules;
using Microsoft.TeamFoundation.Migration.Toolkit;
using System.IO;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// This utility class exports the active configuration of a session group and optionally
    /// all the existing conflict resolution rules.
    /// </summary>
    public class ConfigExporter
    {
        public const string ConfigFileName = "_Configuration.xml"; // do not localize
        public const string ResolutionRuleFileName = "_ResolutionRules.xml"; // do not localize

        private bool m_exportConfigFileOnly = false;
        private string m_exportFilePath = null;

        public ConfigExporter(bool exportConfigFileOnly, string exportConfigPath)
        {
            Initialize(exportConfigFileOnly, exportConfigPath);
        }

        public string Export(Guid sessionGroupUniqueId)
        {
            string preExportDir = Environment.CurrentDirectory;

            try
            {
                Environment.CurrentDirectory = System.IO.Path.GetTempPath();

                if (m_exportConfigFileOnly)
                {
                    if (string.IsNullOrEmpty(m_exportFilePath))
                    {
                        m_exportFilePath = sessionGroupUniqueId.ToString() + ConfigFileName;
                    }

                    ExportConfiguration(sessionGroupUniqueId, m_exportFilePath);
                    return Path.GetFullPath(m_exportFilePath);
                }
                else
                {
                    string exportConfigFileName = ConfigFileName;
                    ExportConfiguration(sessionGroupUniqueId, exportConfigFileName);

                    string exportResolutionRulesFileName = ResolutionRuleFileName;
                    ExportConflictResolutionRules(sessionGroupUniqueId, exportResolutionRulesFileName);

                    string exportZipFileName = (string.IsNullOrEmpty(m_exportFilePath) ?
                        GetZipPackageFileName(sessionGroupUniqueId) : m_exportFilePath);
                    ZipUtility.Zip(exportZipFileName, new string[2] { exportConfigFileName, exportResolutionRulesFileName });

                    File.Delete(exportConfigFileName);
                    File.Delete(exportResolutionRulesFileName);

                    return Path.GetFullPath(exportZipFileName);
                }
            }
            finally
            {
                Environment.CurrentDirectory = preExportDir;
            }
        }

        private void Initialize(bool exportConfigFileOnly, string exportConfigPath)
        {
            m_exportConfigFileOnly = exportConfigFileOnly;
            m_exportFilePath = exportConfigPath;
        }

        private void ExportConfiguration(Guid sessionGroupUniqueId, string exportConfigFileName)
        {
            BusinessModelManager bmMgr = new BusinessModelManager();
            Configuration config = bmMgr.LoadConfiguration(sessionGroupUniqueId);
            if (null == config)
            {
                throw new NonExistingSessionGroupUniqueIdException(sessionGroupUniqueId);
            }

            XmlSerializer configSerializer = new XmlSerializer(typeof(Configuration));
            using (FileStream fs = new FileStream(exportConfigFileName, FileMode.Create))
            {
                configSerializer.Serialize(fs, config);
                fs.Dispose();
            }
        }

        private void ExportConflictResolutionRules(Guid sessionGroupUniqueId, string exportResolutionRulesFileName)
        {
            using (TfsMigrationConsolidatedDBEntities context = TfsMigrationConsolidatedDBEntities.CreateInstance())
            {
                SerializableConflictResolutionRuleCollection ruleCollection =
                    new SerializableConflictResolutionRuleCollection();

                #region session group scope rules
                var sessionGroupScopeRules =
                            from r in context.ConfigConflictResolutionRuleSet
                            where r.ScopeInfoUniqueId.Equals(sessionGroupUniqueId)
                            select r;
                AddRules(sessionGroupScopeRules, ruleCollection);
                #endregion

                #region session scope rules
                int activeStatus = (int)Microsoft.TeamFoundation.Migration.BusinessModel.Configuration.ConfigurationStatus.Valid;
                var activeSessionConfigs =
                    from sc in context.SessionConfigSet
                    where sc.SessionGroupConfig.Status == activeStatus
                    && sc.SessionGroupConfig.SessionGroup.GroupUniqueId.Equals(sessionGroupUniqueId)
                    select sc;

                foreach (var sessionConfig in activeSessionConfigs)
                {
                    Guid sessionUniqueId = sessionConfig.SessionUniqueId;
                    var sessionScopeRules =
                        from r in context.ConfigConflictResolutionRuleSet
                        where r.ScopeInfoUniqueId.Equals(sessionUniqueId)
                        select r;
                    AddRules(sessionScopeRules, ruleCollection);
                }
                #endregion

                using (FileStream fs = new FileStream(exportResolutionRulesFileName, FileMode.Create))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(SerializableConflictResolutionRuleCollection));
                    serializer.Serialize(fs, ruleCollection);
                }
            }
        }

        private byte[] StringToByteArray(string str)
        {
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            return encoding.GetBytes(str);
        }

        private string GetZipPackageFileName(Guid sessionGroupUniqueId)
        {
            return sessionGroupUniqueId.ToString() + ".zip";
        }

        private static void AddRules(
            IQueryable<ConfigConflictResolutionRule> sessionGroupScopeRules,
            SerializableConflictResolutionRuleCollection ruleCollection)
        {
            foreach (var rule in sessionGroupScopeRules)
            {
                ruleCollection.AddRule(new SerializableConflictResolutionRule(rule));
            }
        }
    }
}
