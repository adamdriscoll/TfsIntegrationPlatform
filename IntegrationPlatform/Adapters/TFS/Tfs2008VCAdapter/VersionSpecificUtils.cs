// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

using Microsoft.TeamFoundation.VersionControl.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2008VCAdapter
{
    /// <summary>
    /// This class exists to isolate provider version specific dependencies so that the same body of
    /// code can be compiled to different targets.  See the File System adapter for another example
    /// of this pattern.
    /// </summary>

    class VersionSpecificUtils
    {
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
                       ChangeType.Undelete;
            }
        }
    }
}
