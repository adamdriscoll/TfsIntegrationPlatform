// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public static partial class Constants
    {
        public static Guid FrameworkSourceId = new Guid("DDBD5E6F-12C3-4a0a-A381-725C0622697E");
        public static readonly string FrameworkName = "Team Foundation Server Migration and Synchronization Framework";
        public static readonly string FrameWorkVersion = "1.0.0.0";

        public static string MigrationResultSkipChangeGroup = "96C62F8C-79D7-468f-B05F-D40D3FB8A600";

        #region Conflict Management
        public static string DATAKEY_UPDATED_CONFIGURATION_ID = "UpdatedConfigurationId";
        public static string UNCHANGED_CONFIGURATION_ID = "Unchanged";
        #endregion
    }
}
