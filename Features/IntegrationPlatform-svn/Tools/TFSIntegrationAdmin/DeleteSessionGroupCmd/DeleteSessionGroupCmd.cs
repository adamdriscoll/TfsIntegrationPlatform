// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace TFSIntegrationAdmin.DeleteSessionGroupCmd
{
    internal class DeleteSessionGroupCmd : CommandBase
    {
        enum Mode
        {
            DeleteGroup,
            ListGroup,
        }

        const string GroupIdSwitchL = "/SessionGroupUniqeId:";
        const string GroupIdSwitchS = "/G:";
        const string ListGroupSwitchL = "/ListSessionGroup";
        const string ListGroupSwitchS = "/L";

        Mode m_mode;
        Guid m_sessionGroupUniqueId;

        public override bool TryParseArgs(string[] cmdSpecificArgs)
        {
            if (base.TryParseArgs(cmdSpecificArgs))
            {
                // base-class parses "Help" related args
                return true;
            }

            return TryParseAsDeleteSessionGroup(cmdSpecificArgs);
        }

        private bool TryParseAsDeleteSessionGroup(string[] cmdSpecificArgs)
        {
            if (cmdSpecificArgs.Length != 1)
            {
                return false;
            }

            string sessionGroupIdStr = null;
            string cmdOption = cmdSpecificArgs[0];
            if (cmdOption.StartsWith(GroupIdSwitchL, StringComparison.OrdinalIgnoreCase))
            {
                if (cmdOption.Length <= GroupIdSwitchL.Length)
                {
                    return false;
                }

                sessionGroupIdStr = cmdOption.Substring(GroupIdSwitchL.Length);
            }
            else if (cmdOption.StartsWith(GroupIdSwitchS, StringComparison.OrdinalIgnoreCase))
            {
                if (cmdOption.Length <= GroupIdSwitchS.Length)
                {
                    return false;
                }

                sessionGroupIdStr = cmdOption.Substring(GroupIdSwitchS.Length);
            }

            if (!string.IsNullOrEmpty(sessionGroupIdStr))
            {
                try
                {
                    m_sessionGroupUniqueId = new Guid(sessionGroupIdStr);
                    m_mode = Mode.DeleteGroup;
                    return true;
                }
                catch (Exception e)
                {
                    Microsoft.TeamFoundation.Migration.Toolkit.TraceManager.TraceException(e);
                    return false;
                }
            }
            else
            {
                if (cmdOption.Equals(ListGroupSwitchL, StringComparison.OrdinalIgnoreCase)
                    || cmdOption.Equals(ListGroupSwitchS, StringComparison.OrdinalIgnoreCase))
                {
                    m_sessionGroupUniqueId = Guid.Empty;
                    m_mode = Mode.ListGroup;
                    return true;
                }
            }

            return false;
        }

        public override string CommandName
        {
            get { return "DeleteSessionGroup"; /* do not localize */ }
        }

        public override Interfaces.ICommandResult Run()
        {
            switch (m_mode)
            {
                case Mode.DeleteGroup:
                    return DeleteGroup();
                case Mode.ListGroup:
                    return ListGroup();
                default:
                    throw new InvalidOperationException();
            }
        }

        private Interfaces.ICommandResult ListGroup()
        {
            Dictionary<Guid, string> nonActiveGroups = SessionGroupDeletionTask.GetDeletableSessionGroupUniqueIds();
            return new DeleteSessionGroupRslt(this, nonActiveGroups);
        }

        private Interfaces.ICommandResult DeleteGroup()
        {
            SessionGroupDeletionTask task = new SessionGroupDeletionTask(m_sessionGroupUniqueId);
            try
            {
                task.DeleteSessionGroup();
                return new DeleteSessionGroupRslt(this, m_sessionGroupUniqueId);
            }
            catch (Exception e)
            {
                return new DeleteSessionGroupRslt(e, this);
            }
        }

        public override string GetHelpString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Delete an existing session group:");
            sb.AppendFormat("{0} {1} {2}{3}",
                Program.ProgramName, CommandName, GroupIdSwitchL, "<Session Unique Id>");
            sb.AppendLine();
            sb.AppendFormat("{0} {1} {2}{3}",
                Program.ProgramName, CommandName, GroupIdSwitchS, "<Session Unique Id>");
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("List deletable session groups:");
            sb.AppendFormat("{0} {1} {2}|{3}",
                Program.ProgramName, CommandName, ListGroupSwitchL, ListGroupSwitchS);
            return sb.ToString();
        }
    }
}
