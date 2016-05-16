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
        internal const string AdapterGuid = "596DC7D4-0AC1-4BEC-861E-0D3F7C2901D9";
        internal const string AdapterName = "File System Provider for TFS 11";
        internal const string AdapterVersion = "1.0.0.0";

        internal static VersionControlServer GetVersionControlServer(string peerServerUrl)
        {
            TfsTeamProjectCollection tfsProjectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(peerServerUrl));
            return (VersionControlServer)tfsProjectCollection.GetService(typeof(VersionControlServer));
        }
    }
}
