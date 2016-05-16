// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TFSIntegrationAdmin.HelpCmd
{
    class HelpCmd : CommandBase
    {
        public override bool PrintHelp
        {
            get
            {
                return true;
            }
            protected set
            {
                base.PrintHelp = value;
            }
        }

        public override bool CanRecognizeArgs(string[] originalCmdLineArgs)
        {
            if (originalCmdLineArgs.Length == 1 &&
                (originalCmdLineArgs[0].Equals(CommandName, StringComparison.OrdinalIgnoreCase)
                || originalCmdLineArgs[0].Equals(Constants.CmdHelpSwitch1, StringComparison.OrdinalIgnoreCase)
                || originalCmdLineArgs[0].Equals(Constants.CmdHelpSwitch2, StringComparison.OrdinalIgnoreCase)
                || originalCmdLineArgs[0].Equals(Constants.CmdHelpSwitch3, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool TryParseArgs(string[] cmdSpecificArgs)
        {
            return true;
        }

        public override string CommandName
        {
            get { return "Help"; /* do not localize */ }
        }

        public override TFSIntegrationAdmin.Interfaces.ICommandResult Run()
        {
            return null;
        }

        public override string GetHelpString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("TFS Integration Platform Administration Utility supported commands:");
            sb.AppendLine();

            /*
             * Add a line of brief description for each supported command
             */
            sb.AppendLine("  ExportConfig - export the configuration of a session group");
            sb.AppendLine("  ImportConfig - import a configuration package to create a new session group");
            sb.AppendLine("  DeleteSessionGroup - delete an existing session group");
            sb.AppendLine("  ConfigServiceAccount - add an account to TFS Integration service account");
            sb.AppendLine("  ConfigAccessControl - sets access control of the TFS Integration data folder");
            sb.AppendLine("  PrintStatus - print the status of the integration service");
            sb.AppendLine("  Compress - delete the processed data in the integration platformation Database");

            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("To print detailed help of individual command:");
            sb.AppendLine();
            sb.AppendFormat("  {0} <Command> /?", Program.ProgramName);
            return sb.ToString();
        }
    }
}
