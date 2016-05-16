// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TFSIntegrationAdmin.Interfaces;

namespace TFSIntegrationAdmin
{
    internal abstract class CommandResultBase : ICommandResult
    {
        protected ICommand m_command;

        public CommandResultBase(bool succeeded, ICommand command)
        {
            if (null == command)
            {
                throw new ArgumentNullException("command");
            }

            Succeeded = succeeded;
            m_command = command;
        }

        #region ICommandResult Members

        public virtual bool Succeeded
        {
            get;
            private set;
        }

        public virtual string Print()
        {
            if (Succeeded)
            {
                return string.Format(ResourceStrings.CommandExecutionSucceedFormat, NormalizedCommandName);
            }
            else
            {
                return string.Format(ResourceStrings.CommandExecutionFailFormat, NormalizedCommandName);
            }
        }

        private string NormalizedCommandName
        {
            get
            {
                return m_command.CommandName ?? ResourceStrings.UnknownCommandName;
            }
        }

        #endregion
    }
}
