// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.TeamFoundation.VersionControl.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2010VCAdapter
{
    /// <summary>
    /// This class exists to isolate provider version specific dependencies so that the same body of
    /// code can be compiled to different targets.  See the File System adapter for another example
    /// of this pattern.
    /// </summary>
     
    class VersionSpecificUtils
    {
        internal const string AdapterGuid = "FEBC091F-82A2-449e-AED8-133E5896C47A";
        internal const string AdapterName = "TFS 2010 Migration VC Provider";
        internal const string AdapterVersion = "1.0.0.0";

        public static ChangeType ChangeTypesToIgnore
        {
            get
            {
                return ChangeType.None; // This indicates there are none to ignore
            }
        }

        public static ChangeType SupportedChangeTypes
        {
            get
            {
                return ChangeType.Add |
                       ChangeType.Branch |
                       ChangeType.Delete |
                       ChangeType.Edit |
                       ChangeType.Encoding |
                       ChangeType.Lock |
                       ChangeType.Merge |
                       ChangeType.None |
                       ChangeType.Rename |
                       ChangeType.Rollback |
                       ChangeType.SourceRename |
                       ChangeType.Undelete;
            }
        }

        public static Workspace CreateWorkspace(VersionControlServer versionControlServer, string name, string owner, string comment)
        {
            return versionControlServer.CreateWorkspace(name, owner, comment);
        }
    }
}
