// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TFSIntegrationAdmin.Interfaces;

namespace TFSIntegrationAdmin.Exceptions
{
    public class InvalidCommandLineArgException : Exception
    {
        public InvalidCommandLineArgException()
        { }

        public InvalidCommandLineArgException(string message)
            : base(message)
        { }
    }

    /// <summary>
    /// This exception is thrown when the utility is launched without any 
    /// recognizable command provided in the command-line arg list
    /// </summary>
    public class MissingCommandInArgException : InvalidCommandLineArgException
    {
    }

    public class InvalidCommandSpecificArgException : InvalidCommandLineArgException
    {
        public string InvalidArgumentValue
        {
            get;
            private set;
        }

        public ICommand Command
        {
            get;
            private set;
        }

        public InvalidCommandSpecificArgException(ICommand command)
        {
            Initialize(string.Empty, command);
        }

        public InvalidCommandSpecificArgException(string invalidArgValue, ICommand command)
        {
            Initialize(invalidArgValue, command);
        }

        public InvalidCommandSpecificArgException(string invalidArgValue, ICommand command, string message)
            : base(message)
        {
            Initialize(invalidArgValue, command);
        }

        private void Initialize(string invalidArgValue, ICommand command)
        {
            InvalidArgumentValue = invalidArgValue;
            Command = command;
        }
    }
}
