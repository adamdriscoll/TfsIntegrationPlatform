// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    public class WITDUpdateCommands
    {
        // SETTING SYNTAX
        // <CustomSetting SettingKey="ContextSyncOp" SettingValue="OPERATION_NAME::COMMAND" />, where COMMAND can be 
        //    1. DeleteNode,
        //    2. InsertNode,  
        //    3. ReplaceNode, or
        //    4. AddAttribute
        // <CustomSetting SettingKey="OPERATION_NAME::PARAMETER_NAME" SettingValue="PARAMETER_VALUE" />, where PARAMETER_NAME can be 
        //    1. SearchPath,
        //    2. NewNodeContent,
        //    3. DuplicateSearchPath
        //    3. NewAttribute
        //    3. AttributeValue
        //
        // SAMEPLE CONFIG 
        // <CustomSetting SettingKey="ContextSyncOp" SettingValue="Op1::InsertNode" />
        // <CustomSetting SettingKey="Op1::SearchPath" SettingValue="//FIELDS" />
        // <CustomSetting SettingKey="Op1::NewNodeContent" SettingValue="content_xml_goes_here" />
        // <CustomSetting SettingKey="Op1::DuplicateSearchPath" SettingValue="//FIELDS/FIELD[@refname='TfsMigrationTool.ReflectedWorkItemId']" />
        //
        // ADDITIONALLY
        // Check IWITDSyncer for the sematics of the params for each command

        public const string ContextSyncOp = "ContextSyncOp";

        public const string CmdDeleteNode = "DeleteNode";
        public const string CmdInsertNode = "InsertNode";
        public const string CmdReplaceNode = "ReplaceNode";
        public const string CmdAddAttribute = "AddAttribute";

        public const string ParamSearchPath = "SearchPath";
        public const string ParamNewNodeContent = "NewNodeContent";
        public const string ParamNewAttribute = "NewAttribute";
        public const string ParamAttributeValue = "AttributeValue";
        public const string ParamDuplicateSearchPath = "DuplicateSearchPath";
        public const string ParamNameDelimiter = "::";

        private Dictionary<string, SyncUpdateCmdBase> m_commands = new Dictionary<string,SyncUpdateCmdBase>();
        private List<CustomSetting> m_orphanedSettings = new List<CustomSetting>();

        public WITDUpdateCommands(ICollection<CustomSetting> settings)
        {
            foreach (var setting in settings)
            {
                ParseCommandSetting(setting, false);
            }

            foreach (var setting in m_orphanedSettings)
            {
                ParseCommandSetting(setting, true);
            }
        }

        public void Process(IWITDSyncer witSyncer)
        {
            foreach (var cmd in m_commands.Values)
            {
                cmd.Invoke(witSyncer);
            }
        }

        public void AddCommand(string cmdName, SyncUpdateCmdBase syncCmd)
        {
            if (null == syncCmd)
            {
                return;
            }

            if (!m_commands.ContainsKey(cmdName))
            {
                m_commands.Add(cmdName, syncCmd);
            }
        }

        private void ParseCommandSetting(
            CustomSetting setting,
            bool neglectOrphanedSetting)
        {
            string key = setting.SettingKey;
            string value = setting.SettingValue;
            if (key.Equals(ContextSyncOp))
            {
                // found new ContextSyncOp
                string cmdName = ExtractCommandName(value);
                string cmd = ExtractCmmand(value);

                if (string.IsNullOrEmpty(cmdName)
                    || string.IsNullOrEmpty(cmd))
                {
                    return;
                }

                SyncUpdateCmdBase syncCmd = CreateCommand(cmd);
                AddCommand(cmdName, syncCmd);
            }
            else if (key.EndsWith(ParamDuplicateSearchPath))
            {
                string cmdName = ExtractCommandName(key);
                if (string.IsNullOrEmpty(cmdName))
                {
                    return;
                }

                if (m_commands.ContainsKey(cmdName))
                {
                    m_commands[cmdName].AddParam(ParamDuplicateSearchPath, value);
                }
                else if (!neglectOrphanedSetting)
                {
                    m_orphanedSettings.Add(setting);
                }
            }
            else if (key.EndsWith(ParamSearchPath))
            {
                string cmdName = ExtractCommandName(key);
                if (string.IsNullOrEmpty(cmdName))
                {
                    return;
                }

                if (m_commands.ContainsKey(cmdName))
                {
                    m_commands[cmdName].AddParam(ParamSearchPath, value);
                }
                else if (!neglectOrphanedSetting)
                {
                    m_orphanedSettings.Add(setting);
                }
            }
            else if (key.EndsWith(ParamNewNodeContent))
            {
                string cmdName = ExtractCommandName(key);
                if (string.IsNullOrEmpty(cmdName))
                {
                    return;
                }

                if (m_commands.ContainsKey(cmdName))
                {
                    m_commands[cmdName].AddParam(ParamNewNodeContent, value);
                }
                else if (!neglectOrphanedSetting)
                {
                    m_orphanedSettings.Add(setting);
                }
            }
            else if (key.EndsWith(ParamNewAttribute))
            {
                string cmdName = ExtractCommandName(key);
                if (string.IsNullOrEmpty(cmdName))
                {
                    return;
                }

                if (m_commands.ContainsKey(cmdName))
                {
                    m_commands[cmdName].AddParam(ParamNewAttribute, value);
                }
                else if (!neglectOrphanedSetting)
                {
                    m_orphanedSettings.Add(setting);
                }
            }
            else if (key.EndsWith(ParamAttributeValue))
            {
                string cmdName = ExtractCommandName(key);
                if (string.IsNullOrEmpty(cmdName))
                {
                    return;
                }

                if (m_commands.ContainsKey(cmdName))
                {
                    m_commands[cmdName].AddParam(ParamAttributeValue, value);
                }
                else if (!neglectOrphanedSetting)
                {
                    m_orphanedSettings.Add(setting);
                }
            }
        }

        private SyncUpdateCmdBase CreateCommand(string cmd)
        {
            if (cmd.Equals(CmdDeleteNode))
            {
                return new DeleteNodeCmd();
            }
            else if (cmd.Equals(CmdInsertNode))
            {
                return new InsertNodeCmd();
            }
            else if (cmd.Equals(CmdReplaceNode))
            {
                return new ReplaceNodeCmd();
            }
            else if (cmd.Equals(CmdAddAttribute))
            {
                return new AddAttributeCmd();
            }

            return null;
        }

        private string ExtractCmmand(string value)
        {
            return value.Substring(value.IndexOf(ParamNameDelimiter, 0) + ParamNameDelimiter.Length);
        }

        private string ExtractCommandName(string key)
        {
            return key.Substring(0, key.IndexOf(ParamNameDelimiter, 0));
        }
    }

    public abstract class SyncUpdateCmdBase
    {
        protected Dictionary<string, string> m_paramas = new Dictionary<string,string>();

        public abstract bool IsValid();
        public abstract void Invoke(IWITDSyncer witSyncer);

        public virtual void AddParam(string paramName, string paramValue)
        {
            if (string.IsNullOrEmpty(paramName))
            {
                throw new ArgumentNullException("paramName");
            }

            if (null == paramValue)
            {
                throw new ArgumentNullException("paramValue");
            }

            if (m_paramas.ContainsKey(paramName))
            {
                throw new InvalidOperationException(
                    string.Format("Duplicate param is detected: {0}", paramName));
            }

            m_paramas.Add(paramName, paramValue);
        }
    }

    public class DeleteNodeCmd : SyncUpdateCmdBase
    {
        public override bool IsValid()
        {
            return m_paramas.ContainsKey(WITDUpdateCommands.ParamSearchPath);
        }

        public override void Invoke(IWITDSyncer witSyncer)
        {
            if (IsValid())
            {
                witSyncer.DeleteNode(m_paramas[WITDUpdateCommands.ParamSearchPath]);
            }
        }
    }

    public class InsertNodeCmd : SyncUpdateCmdBase
    {
        public override bool IsValid()
        {
            return m_paramas.ContainsKey(WITDUpdateCommands.ParamSearchPath)
                && m_paramas.ContainsKey(WITDUpdateCommands.ParamDuplicateSearchPath);
        }

        public override void Invoke(IWITDSyncer witSyncer)
        {
            if (IsValid())
            {
                if (m_paramas.ContainsKey(WITDUpdateCommands.ParamDuplicateSearchPath))
                {
                    witSyncer.InsertNode(
                        m_paramas[WITDUpdateCommands.ParamSearchPath],
                        m_paramas[WITDUpdateCommands.ParamNewNodeContent],
                        m_paramas[WITDUpdateCommands.ParamDuplicateSearchPath]);
                }
                else
                {
                    witSyncer.InsertNode(
                        m_paramas[WITDUpdateCommands.ParamSearchPath],
                        m_paramas[WITDUpdateCommands.ParamNewNodeContent]);
                }
            }
        }
    }

    public class ReplaceNodeCmd : SyncUpdateCmdBase
    {
        public override bool IsValid()
        {
            return m_paramas.ContainsKey(WITDUpdateCommands.ParamSearchPath);
        }

        public override void Invoke(IWITDSyncer witSyncer)
        {
            if (IsValid())
            {
                witSyncer.ReplaceNode(
                    m_paramas[WITDUpdateCommands.ParamSearchPath],
                    m_paramas[WITDUpdateCommands.ParamNewNodeContent]);
            }
        }
    }

    public class AddAttributeCmd : SyncUpdateCmdBase
    {
        public override bool IsValid()
        {
            return m_paramas.ContainsKey(WITDUpdateCommands.ParamSearchPath);
        }

        public override void Invoke(IWITDSyncer witSyncer)
        {
            if (IsValid())
            {
                witSyncer.AddAttribute(
                    m_paramas[WITDUpdateCommands.ParamSearchPath],
                    m_paramas[WITDUpdateCommands.ParamNewAttribute],
                    m_paramas[WITDUpdateCommands.ParamAttributeValue]);
            }
        }
    }
}
