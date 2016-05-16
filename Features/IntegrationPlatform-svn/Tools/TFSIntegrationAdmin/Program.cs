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
    public class Program
    {
        #region statics
        static HelpCmd.HelpCmd s_helpCommand = new TFSIntegrationAdmin.HelpCmd.HelpCmd();

        internal static string ProgramName
        {
            get
            {
                return "TFSIntegrationAdmin";
            }
        }

        static void Main(string[] args)
        {
            Program prog = new Program(args);

            try
            {
                var result = prog.Execute();
                if (!(result is PrintCommandHelpRslt))
                {
                    prog.PrintResult(result);
                }
            }
            catch (MissingCommandInArgException)
            {
                Console.WriteLine(ResourceStrings.MissingCommandInArgInfo);
                Console.WriteLine();
                Console.WriteLine(s_helpCommand.GetHelpString());
            }
            catch (InvalidCommandSpecificArgException e)
            {
                Console.Write(ResourceStrings.InvalidCommandSpecificArgInfo);
                Debug.Assert(null != e.Command, "e.Command is NULL");
                if (string.IsNullOrEmpty(e.InvalidArgumentValue))
                {
                    Console.Write(string.Format(ResourceStrings.UnknownInvalidCommandSpecificArgFormat, 
                        e.Command.CommandName));
                }
                else
                {
                    Console.Write(string.Format(ResourceStrings.KnownInvalidCommandSpecificArgFormat, 
                        e.InvalidArgumentValue, e.Command.CommandName));
                }
                if (!string.IsNullOrEmpty(e.Message))
                {
                    Console.Write(e.Message);
                }

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine(s_helpCommand.GetHelpString());
            }
            catch (InvalidCommandLineArgException)
            {
                Console.WriteLine(ResourceStrings.InvalidCommandLineArgInfo);
                Console.WriteLine();
                Console.WriteLine(s_helpCommand.GetHelpString());
            }
            catch (Exception e)
            {
                Console.WriteLine(ResourceStrings.UnhandledExceptionInfo);
                Console.WriteLine();
                Console.WriteLine(e.ToString());
            }
        }

        #endregion

        ICommand m_command = null;
        string[] m_args = null;

        public Program(string[] args)
        {
            m_args = args;
        }

        /// <summary>
        /// Execute the command specified in the command-line argument
        /// </summary>
        /// <returns>The command execution result</returns>
        /// <exception cref="TFSIntegrationAdmin.Exceptions.MissingCommandInArgException">
        /// Thrown when no valid Command is specified in the argument list
        /// </exception>
        /// <exception cref="TFSIntegrationAdmin.Exceptions.InvalidCommandSpecificArgException">
        /// Thrown when argument particular to a recognized Command is invalid
        /// </exception>
        /// <exception cref="TFSIntegrationAdmin.Exceptions.InvalidCommandLineArgException">
        /// Thrown when there is invalid argument in the argument list
        /// </exception>
        /// <exception cref="System.Exception">
        /// Thrown when there is an unhandled exception.
        /// </exception>
        public ICommandResult Execute()
        {
            TryParseArgs();

            Debug.Assert(null != m_command, "m_command is NULL");

            if (m_command.PrintHelp)
            {
                return PrintCommandHelp();
            }
            else
            {
                return m_command.Run();
            }
        }

        private void TryParseArgs()
        {
            CommandFactory cmdFactory = new CommandFactory();
            cmdFactory.TryCreateCommand(m_args, out m_command);
        }

        private ICommandResult PrintCommandHelp()
        {
            Console.WriteLine(m_command.GetHelpString());
            return new PrintCommandHelpRslt(m_command);
        }

        private void PrintResult(ICommandResult result)
        {
            Debug.Assert(null != result, "result is NULL");
            Console.WriteLine(result.Print());
        }
    }
}
