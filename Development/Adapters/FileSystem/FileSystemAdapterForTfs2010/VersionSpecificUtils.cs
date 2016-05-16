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
    /// <summary>
    /// This class exists to isolate provider version specific dependencies so that the same body of
    /// code can be compiled to different targets.  For the File System adapter, code on disk is compared
    /// against code in TFS using the TFS Version Control object model.  The TFS OM is highly compatible 
    /// and we take advantage of that by creating File System Import adapters that work on machines with 
    /// either TFS 2010 or TFS 2008 assemblies installed by simply compiling to different targets.
    /// </summary>
     
    class VersionSpecificUtils
    {
        internal const string AdapterGuid = "43B0D301-9B38-4caa-A754-61E854A71C78";
        internal const string AdapterName = "File System Provider for TFS 2010";
        internal const string AdapterVersion = "1.0.0.0";

        internal static VersionControlServer GetVersionControlServer(string peerServerUrl)
        {
            TfsTeamProjectCollection tfsProjectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(peerServerUrl));
            return (VersionControlServer)tfsProjectCollection.GetService(typeof(VersionControlServer));
        }
    }
}
