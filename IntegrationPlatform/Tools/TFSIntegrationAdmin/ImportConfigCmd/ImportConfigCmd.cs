// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Toolkit = Microsoft.TeamFoundation.Migration.Toolkit;

namespace TFSIntegrationAdmin.ImportConfigCmd
{
    internal class ImportConfigCmd : CommandBase
    {
        const string ConfigPackageSwitchL = "/Package:";
        const string ConfigPackageSwitchS = "/P:";
        const string ExcludeConflictResolutionRulesL = "/ExcludeRules";
        const string ExcludeConflictResolutionRulesS = "/E";
        const string ConfigFileSwitchL = "/File:";
        const string ConfigFileSwitchS = "/F:";

        bool m_importConfigPackage = false;
        bool m_excludeResolutionRules = false;
        bool m_importConfigFile = false;
        string m_pathToImport = string.Empty;

        public override bool TryParseArgs(string[] cmdSpecificArgs)
        {
            if (base.TryParseArgs(cmdSpecificArgs))
            {
                // base-class parses "Help" related args
                return true;
            }

            return TryParseAsImportPackage(cmdSpecificArgs);
        }

        private bool TryParseAsImportPackage(string[] cmdSpecificArgs)
        {
            if (cmdSpecificArgs.Length == 0 || cmdSpecificArgs.Length > 2)
            {
                return false;
            }

            foreach (string arg in cmdSpecificArgs)
            {
                if (arg.StartsWith(ConfigPackageSwitchL, StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryParseImportSwitch(arg, ConfigPackageSwitchL, ref m_importConfigPackage, ref m_pathToImport))
                    {
                        return false;
                    }
                }
                else if (arg.StartsWith(ConfigPackageSwitchS, StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryParseImportSwitch(arg, ConfigPackageSwitchS, ref m_importConfigPackage, ref m_pathToImport))
                    {
                        return false;
                    }
                }
                else if (arg.StartsWith(ExcludeConflictResolutionRulesL, StringComparison.OrdinalIgnoreCase))
                {
                    if (arg.Length == ExcludeConflictResolutionRulesL.Length)
                    {
                        m_excludeResolutionRules = true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (arg.StartsWith(ExcludeConflictResolutionRulesS, StringComparison.OrdinalIgnoreCase))
                {
                    if (arg.Length == ExcludeConflictResolutionRulesS.Length)
                    {
                        m_excludeResolutionRules = true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (arg.StartsWith(ConfigFileSwitchL, StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryParseImportSwitch(arg, ConfigFileSwitchL, ref m_importConfigFile, ref m_pathToImport))
                    {
                        return false;
                    }
                }
                else if (arg.StartsWith(ConfigFileSwitchS, StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryParseImportSwitch(arg, ConfigFileSwitchS, ref m_importConfigFile, ref m_pathToImport))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return m_importConfigPackage || (m_importConfigFile && !m_excludeResolutionRules);
        }

        /// <summary>
        /// Try parsing the import config file|package switch
        /// </summary>
        /// <param name="arg">argument to parse</param>
        /// <param name="argSwitch">the switch to try parsing</param>
        /// <param name="switchFlag">the flag corresponding to the switch</param>
        /// <param name="pathToImport">the parsed import file path</param>
        /// <returns>TRUE if succeeded, FALSE otherwise</returns>
        private bool TryParseImportSwitch(string arg, string argSwitch, ref bool switchFlag, ref string pathToImport)
        {
            if (arg.Length <= argSwitch.Length)
            {
                return false;
            }

            string filePath = arg.Substring(argSwitch.Length);
            if (!FileExists(filePath))
            {
                throw new TFSIntegrationAdmin.Exceptions.InvalidCommandSpecificArgException(
                    arg, this, string.Format("File '{0}' does not exist.", filePath));
            }
            else
            {
                pathToImport = filePath;
                switchFlag = true;
            }

            return true;
        }

        public override string CommandName
        {
            get { return "ImportConfig"; /* do not localize */ }
        }

        public override TFSIntegrationAdmin.Interfaces.ICommandResult Run()
        {
            if (m_importConfigPackage)
            {
                ConfigImporter importer = new ConfigImporter(m_pathToImport, m_excludeResolutionRules, this);
                return importer.Import();
            }
            else
            {
                Debug.Assert(m_importConfigFile, "m_importConfigFile is false");
                try
                {
                    Dictionary<string, string> oldToNewGuidMapping;
                    Configuration importedConfig = Toolkit.ConfigImporter.LoadReGuidAndSaveConfig(m_pathToImport, out oldToNewGuidMapping);
                    Debug.Assert(null != importedConfig, "importedConfig is NULL");

                    var retVal = new ImportConfigRslt(true, this);
                    retVal.ImportedConfig = importedConfig;
                    return retVal;
                }
                catch (Exception e)
                {
                    var retVal = new ImportConfigRslt(false, this);
                    retVal.Exception = e;
                    return retVal;
                }
            }
        }

        public override string GetHelpString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Create a new session group by importing a configuration package:");
            sb.AppendFormat("{0} {1} {2}{3} [{4}|{5}]",
                Program.ProgramName, CommandName, ConfigPackageSwitchL, "<Configuration Package (zip) file>", 
                ExcludeConflictResolutionRulesL, ExcludeConflictResolutionRulesS);
            sb.AppendLine();
            sb.AppendFormat("{0} {1} {2}{3} [{4}|{5}]",
                Program.ProgramName, CommandName, ConfigPackageSwitchS, "<Configuration Package (zip) file>",
                ExcludeConflictResolutionRulesL, ExcludeConflictResolutionRulesS);
            sb.AppendLine();
            sb.AppendFormat("{0} {1} {2}{3}",
                Program.ProgramName, CommandName, ConfigFileSwitchL, "<Configuration (XML) file>");
            sb.AppendLine();
            sb.AppendFormat("{0} {1} {2}{3}",
                Program.ProgramName, CommandName, ConfigFileSwitchS, "<Configuration (XML) file>");
            return sb.ToString();
        }
        
        private bool FileExists(string config)
        {
            return System.IO.File.Exists(config);
        }
    }
}
