// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TFSIntegrationAdmin.Interfaces;
using System.Diagnostics;

namespace TFSIntegrationAdmin
{
    internal abstract class CommandBase : ICommand
    {
        public CommandBase()
        {
            PrintHelp = false;
        }

        #region ICommand Members

        public virtual bool CanRecognizeArgs(string[] originalCmdLineArgs)
        {
            if (originalCmdLineArgs == null
                || originalCmdLineArgs.Length == 0)
            {
                return false;
            }

            Debug.Assert(!string.IsNullOrEmpty(CommandName), "CommandName is NULL or Empty");
            if (CommandName.Equals(originalCmdLineArgs[0], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public virtual bool TryParseArgs(string[] cmdSpecificArgs)
        {
            if (cmdSpecificArgs.Length == 0
                || cmdSpecificArgs[0].Equals(Constants.CmdHelpSwitch1, StringComparison.OrdinalIgnoreCase)
                || cmdSpecificArgs[0].Equals(Constants.CmdHelpSwitch2, StringComparison.OrdinalIgnoreCase)
                || cmdSpecificArgs[0].Equals(Constants.CmdHelpSwitch3, StringComparison.OrdinalIgnoreCase))
            {
                PrintHelp = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        public virtual bool PrintHelp
        {
            get;
            protected set;
        }

        public abstract string CommandName
        {
            get;
        }

        public abstract ICommandResult Run();

        public abstract string GetHelpString();
        
        #endregion

        protected bool TryParseArg(
            string arg,
            string argSwitch,
            out string optionValue)
        {
            if (arg.Length <= argSwitch.Length)
            {
                optionValue = string.Empty;
                return false;
            }

            optionValue = arg.Substring(argSwitch.Length);
            return true;
        }
    }
}
