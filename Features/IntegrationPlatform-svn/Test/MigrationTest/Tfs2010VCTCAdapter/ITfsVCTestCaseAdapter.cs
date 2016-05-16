// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.VersionControl.Client;
using MigrationTestLibrary;

namespace Tfs2008VCTCAdapter
{
    public interface ITfsVCTestCaseAdapter : IVCTestCaseAdapter
    {
        Workspace Workspace
        {
            get;
        }

        VersionControlServer TfsClient
        {
            get;
        }
    }
}
