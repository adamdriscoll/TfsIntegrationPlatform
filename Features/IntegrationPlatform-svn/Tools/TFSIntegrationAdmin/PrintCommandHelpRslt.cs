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
    /// <summary>
    /// This result is returned when users choose to print Help of a command
    /// </summary>
    internal class PrintCommandHelpRslt : CommandResultBase
    {
        public PrintCommandHelpRslt(ICommand command)
            : base(true, command)
        {
        }
    }
}
