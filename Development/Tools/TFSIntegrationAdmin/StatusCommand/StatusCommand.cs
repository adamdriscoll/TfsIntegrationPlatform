// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.WCFServices;

namespace TFSIntegrationAdmin.StatusCommand
{
    internal class StatusCommand : CommandBase
    {
        public override string CommandName
        {
            get { return "PrintStatus"; /* do not localize */ }
        }

        public override bool TryParseArgs(string[] cmdSpecificArgs)
        {
            if (cmdSpecificArgs.Length == 1
                && (cmdSpecificArgs[0].Equals(Constants.CmdHelpSwitch1, StringComparison.OrdinalIgnoreCase)
                    || cmdSpecificArgs[0].Equals(Constants.CmdHelpSwitch2, StringComparison.OrdinalIgnoreCase)
                    || cmdSpecificArgs[0].Equals(Constants.CmdHelpSwitch3, StringComparison.OrdinalIgnoreCase)))
            {
                PrintHelp = true;
                return true;
            }
            else if (cmdSpecificArgs.Length == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override Interfaces.ICommandResult Run()
        {
            try
            {
                MigrationServiceClient serviceClient = new MigrationServiceClient();
                List<Guid> runningSessionGroupIds = serviceClient.GetRunningSessionGroups();

                Dictionary<Guid, string> sessionGroupDescription = new Dictionary<Guid, string>();
                using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
                {
                    foreach (var sessionGroupId in runningSessionGroupIds)
                    {
                        var sessionGroupQuery = context.RTSessionGroupSet.Where(g => g.GroupUniqueId == sessionGroupId);
                        if (sessionGroupQuery.Count() > 0)
                        {
                            sessionGroupDescription[sessionGroupId] = sessionGroupQuery.First().FriendlyName ?? string.Empty;
                        }
                    }
                }

                return new StatusRslt(sessionGroupDescription, this);
            }
            catch (MigrationServiceEndpointNotFoundException)
            {
                return new StatusRslt(ResourceStrings.IntegrationServiceNotRunningInfo, this, true);
            }
            catch (Exception e)
            {
                return new StatusRslt(e.Message, this, false);
            }
        }

        public override string GetHelpString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Print the intergation service status:");
            sb.AppendFormat("  {0} {1}", Program.ProgramName, CommandName);
            return sb.ToString();
        }
    }
}
