// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TFSIntegrationAdmin.Exceptions;
using TFSIntegrationAdmin.Interfaces;

namespace TFSIntegrationAdmin
{
    class CommandFactory
    {
        private List<ICommand> m_supportedCommands = new List<ICommand>();

        public CommandFactory()
        {
            RegisterCommands();
        }

        private void RegisterCommands()
        {
            /*
             * NOTE: register each supported command here 
             */
            m_supportedCommands.Add(new HelpCmd.HelpCmd());
            m_supportedCommands.Add(new ExportConfigCmd.ExportConfigCmd());
            m_supportedCommands.Add(new ImportConfigCmd.ImportConfigCmd());
            m_supportedCommands.Add(new DeleteSessionGroupCmd.DeleteSessionGroupCmd());
            m_supportedCommands.Add(new ServiceAccountCmd.ServiceAccountCommand());
            m_supportedCommands.Add(new StatusCommand.StatusCommand());
            m_supportedCommands.Add(new CompressCmd.CompressCmd());
            m_supportedCommands.Add(new GrantAccessControlCmd.GrantAccessControlCmd());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args">command-line argument list</param>
        /// <param name="newCommand">parsed and recognized command to execute</param>
        /// <exception cref="TFSIntegrationAdmin.Exceptions.MissingCommandInArgException" />
        /// <exception cref="TFSIntegrationAdmin.Exceptions.InvalidCommandSpecificArgException" />
        /// <exception cref="TFSIntegrationAdmin.Exceptions.InvalidCommandLineArgException" />
        public void TryCreateCommand(string[] args, out ICommand newCommand)
        {
            newCommand = null;

            if (args == null || args.Length == 0)
            {
                throw new MissingCommandInArgException();
            }

            ICommand recognizedCommand = null; 
            foreach (ICommand cmd in m_supportedCommands)
            {
                Debug.Assert(!string.IsNullOrEmpty(cmd.CommandName), "cmd.CommandName is Null or Empty");

                if (cmd.CanRecognizeArgs(args))
                {
                    recognizedCommand = cmd;
                    string[] cmdSpecificArgs = ExtractCommandSpecificArgs(args);

                    if (cmd.TryParseArgs(cmdSpecificArgs))
                    {
                        newCommand = cmd;
                        return;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (null == recognizedCommand)
            {
                throw new MissingCommandInArgException();
            }
            else
            {
                throw new InvalidCommandSpecificArgException(recognizedCommand);
            }
        }

        private string[] ExtractCommandSpecificArgs(string[] args)
        {
            Debug.Assert(args.Length > 0, string.Format("args.Length = {0} is invalid", args.Length.ToString()));

            var retVal = new string[args.Length - 1];

            if (args.Length > 1)
            {
                for (int i = 0; i < retVal.Length; ++i)
                {
                    retVal[i] = args[i + 1];
                }
            }

            return retVal;
        }
    }
}
