// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Microsoft.TeamFoundation.Migration.TfsFileSystemAdapter
{
    // See the description in the 2010 tree

    class VersionSpecificUtils
    {
        internal const string AdapterGuid = "3A27F4DE-8637-483C-945D-D2B20541DF7C";
        internal const string AdapterName = "File System Provider for TFS 2008";
        internal const string AdapterVersion = "1.0.0.0";

        internal static VersionControlServer GetVersionControlServer(string peerServerUrl)
        {
            TeamFoundationServer tfsServer = TeamFoundationServerFactory.GetServer(peerServerUrl);
            return (VersionControlServer)tfsServer.GetService(typeof(VersionControlServer));
        }
    }
}
