// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TFSIntegrationAdmin.Interfaces;

namespace TFSIntegrationAdmin.ExportConfigCmd
{
    internal class ExportConfigCmd : CommandBase
    {
        const string SessionGroupIdSwitchL = "/SessionGroupUniqueId:";
        const string SessionGroupIdSwitchS = "/G:";
        const string OutputConfigFileSwitchL = "/Config:";
        const string OutputConfigFileSwitchS = "/C:";
        const string OutputPackageSwitchL = "/Package:";
        const string OutputPackageSwitchS = "/P:";

        bool m_exportConfigFileOnly = false;
        string m_outputFilePath = null;
        bool m_exportByGroupUniqueId = false;
        Guid m_sessionGroupUniqueId = Guid.Empty;

        //#endregion
        public override bool TryParseArgs(string[] cmdSpecificArgs)
        {
            if (base.TryParseArgs(cmdSpecificArgs))
            {
                // base-class parses "Help" related args
                return true;
            }

            bool groupUniqueIdOptionFound = false;
            foreach (var arg in cmdSpecificArgs)
            {
                if (arg.StartsWith(SessionGroupIdSwitchL, StringComparison.OrdinalIgnoreCase))
                {
                    if (groupUniqueIdOptionFound)
                    {
                        // cannot have the same switch twice or with session group guid specified
                        return false;
                    }
                    else
                    {
                        if (arg.Length <= SessionGroupIdSwitchL.Length)
                        {
                            throw new TFSIntegrationAdmin.Exceptions.InvalidCommandSpecificArgException(
                                arg, this);
                        }

                        groupUniqueIdOptionFound = TryParseSessionGroupUniqueId(arg.Substring(SessionGroupIdSwitchL.Length));
                        if (!groupUniqueIdOptionFound)
                        {
                            throw new TFSIntegrationAdmin.Exceptions.InvalidCommandSpecificArgException(arg, this, 
                                string.Format(ResourceStrings.InvalidSessionGroupUnqueIdInfoFormat, 
                                    arg.Substring(SessionGroupIdSwitchL.Length)));
                        }
                    }
                }
                else if (arg.StartsWith(SessionGroupIdSwitchS, StringComparison.OrdinalIgnoreCase))
                {
                    if (groupUniqueIdOptionFound)
                    {
                        // cannot have the same switch twice or with session group guid specified
                        return false;
                    }
                    else
                    {
                        if (arg.Length <= SessionGroupIdSwitchS.Length)
                        {
                            throw new TFSIntegrationAdmin.Exceptions.InvalidCommandSpecificArgException(
                                arg, this);
                        }

                        groupUniqueIdOptionFound = TryParseSessionGroupUniqueId(arg.Substring(SessionGroupIdSwitchS.Length));
                        if (!groupUniqueIdOptionFound)
                        {
                            throw new TFSIntegrationAdmin.Exceptions.InvalidCommandSpecificArgException(arg, this,
                                string.Format(ResourceStrings.InvalidSessionGroupUnqueIdInfoFormat, 
                                    arg.Substring(SessionGroupIdSwitchS.Length)));
                        }
                    }
                }
                else if (arg.StartsWith(OutputConfigFileSwitchL, StringComparison.OrdinalIgnoreCase))
                {
                    m_exportConfigFileOnly = true;
                    if (arg.Length > OutputConfigFileSwitchL.Length)
                    {
                        m_outputFilePath = arg.Substring(OutputConfigFileSwitchL.Length);
                    }
                }
                else if (arg.StartsWith(OutputConfigFileSwitchS, StringComparison.OrdinalIgnoreCase))
                {
                    m_exportConfigFileOnly = true;
                    if (arg.Length > OutputConfigFileSwitchS.Length)
                    {
                        m_outputFilePath = arg.Substring(OutputConfigFileSwitchS.Length);
                    }
                }
                else if (arg.StartsWith(OutputPackageSwitchL, StringComparison.OrdinalIgnoreCase))
                {
                    m_exportConfigFileOnly = false;
                    if (arg.Length > OutputPackageSwitchL.Length)
                    {
                        m_outputFilePath = arg.Substring(OutputPackageSwitchL.Length);
                    }
                }
                else if (arg.StartsWith(OutputPackageSwitchS, StringComparison.OrdinalIgnoreCase))
                {
                    m_exportConfigFileOnly = false;
                    if (arg.Length > OutputPackageSwitchS.Length)
                    {
                        m_outputFilePath = arg.Substring(OutputConfigFileSwitchS.Length);
                    }
                }
            }

            return groupUniqueIdOptionFound;
        }

        private bool TryParseSessionGroupUniqueId(string sessionGroupUniqueIdStr)
        {
            try
            {
                m_sessionGroupUniqueId = new Guid(sessionGroupUniqueIdStr);
                m_exportByGroupUniqueId = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override string CommandName
        {
            get { return "ExportConfig"; /* do not localize */ }
        }

        public override ICommandResult Run()
        {
            try
            {
                if (m_exportByGroupUniqueId && !m_sessionGroupUniqueId.Equals(Guid.Empty))
                {
                    ConfigExporter exporter = new ConfigExporter(this, m_exportConfigFileOnly, m_outputFilePath);
                    return exporter.Export(m_sessionGroupUniqueId);
                }

                return new ExportConfigRslt(false, this);
            }
            catch (Exception e)
            {
                var retVal = new ExportConfigRslt(this, e);
                return retVal;
            }
        }

        public override string GetHelpString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Export the configuration of a session group:");
            sb.AppendFormat("{0} {1} {2}{3} [{4}[{5}]]",
                Program.ProgramName, CommandName,
                SessionGroupIdSwitchL, "<Session Group Unique Id>",
                OutputPackageSwitchL, "<Output Config File Name>");
            sb.AppendLine();
            sb.AppendFormat("{0} {1} {2}{3} [{4}[{5}]]",
                Program.ProgramName, CommandName,
                SessionGroupIdSwitchS, "<Session Group Unique Id>",
                OutputPackageSwitchS, "<Output Config File Name>");
            sb.AppendLine();
            sb.AppendFormat("{0} {1} {2}{3} [{4}[{5}]]",
                Program.ProgramName, CommandName, 
                SessionGroupIdSwitchL, "<Session Group Unique Id>", 
                OutputConfigFileSwitchL, "<Output Config File Name>");
            sb.AppendLine();
            sb.AppendFormat("{0} {1} {2}{3} [{4}[{5}]]",
                Program.ProgramName, CommandName, 
                SessionGroupIdSwitchS, "<Session Group Unique Id>",
                OutputConfigFileSwitchS, "<Output Config File Name>");
            return sb.ToString();
        }
    }
}
