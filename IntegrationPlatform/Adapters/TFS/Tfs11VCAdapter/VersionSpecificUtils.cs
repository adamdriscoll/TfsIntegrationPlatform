// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;

namespace Microsoft.TeamFoundation.Migration.Tfs2010VCAdapter
{
    /// <summary>
    /// This class exists to isolate provider version specific dependencies so that the same body of
    /// code can be compiled to different targets.  See the File System adapter for another example
    /// of this pattern.
    /// </summary>
     
    class VersionSpecificUtils
    {
        internal const string AdapterGuid = "4CC33B2B-4B76-451F-8C2C-D86A3846D6D2";
        internal const string AdapterName = "TFS 11 Migration VC Provider";
        internal const string AdapterVersion = "1.0.0.0";

        public static ChangeType ChangeTypesToIgnore
        {
            get
            {
                try
                {
                    return ChangeType.Property;
                }
                catch
                {
                    // An Exception might be thrown if run with a Dev11 client OM before ChangeType.Property was added
                    // In this case, we don't need to ignore it
                    return ChangeType.None;
                }
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
            CreateWorkspaceParameters createWorkspaceParameters = new CreateWorkspaceParameters(name);
            createWorkspaceParameters.Location = WorkspaceLocation.Server;
            createWorkspaceParameters.OwnerName = owner;
            createWorkspaceParameters.Comment = comment;
            return versionControlServer.CreateWorkspace(createWorkspaceParameters);  
        }
    }
}
