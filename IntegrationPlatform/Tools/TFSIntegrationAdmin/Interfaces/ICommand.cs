// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TFSIntegrationAdmin.Interfaces
{
    public interface ICommand
    {
        bool CanRecognizeArgs(string[] originalCmdLineArgs);
        bool TryParseArgs(string[] cmdSpecificArgs);
        bool PrintHelp { get; }
        string CommandName { get; }
        ICommandResult Run();
        string GetHelpString();
    }
}
