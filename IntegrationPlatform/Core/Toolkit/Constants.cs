// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public static partial class Constants
    {
        #region Session configuration settings
        public const string HwmDelta = "HWMDelta";
        public const string HwmMigrated = "HWMMigrated";
        public const string SkipComment = "SkipChangeComment";
        public const string CommentModifier = "CommentModifier";
        public const string HwmDeltaWit = "HWMDeltaWit";
        public const string HwmDeltaLink = "HWMDeltaLink";

        public const string DisableLinking = "DisableLinking";  // in Linking custom setting, using this string
                                                                // as key, and "true" as value can
                                                                // disable linking in a session group
        #endregion

        public const string SqlConnectionStringVariableName = "SQLConnectionString";

        // PluginsFolderName is static, not const, so the tests can change it
        public static string PluginsFolderName = "Plugins"; // do not localize

        public const string WitLastRevOfThisSyncCycleAttributeName = "LastRevOfThisSyncCycle";

        public const string PlatformCommentSuffixMarker = "(TFS Integration";
        public const string PlatformCommentSuffixMarkerEnd = ")";

        public const string ChangeGroupGenericVersionNumber = "Not specified";

        // The following Extended Property will be added to the Tfs Migration DB during installation
        // The deployment script locates at: 
        // ReleaseCode/Core/TfsMigrationDBConsolidation/Tfs_Integration/Scripts/Post-Deployment/Script.PostDeployment.sql
        // Please:
        //   1. update DBExtProp_ReferenceNameGuidStr whenever DB schema change is made in a release
        //   2. make the same change in the sql script
        internal const string DBExtProp_ReferenceName = "ReferenceName";
        internal const string DBExtProp_ReferenceNameGuidStr = "75DC3A21-AF0B-4161-A813-C2BF8E3AAA35";

        public const string TfsIntegrationExecWorkProcessGroupName = "TFSIPEXEC_WPG";
        public const string TfsIntegrationExecWorkProcessGroupComment = "TFS Integration Platform worker process group";

        public const string TfsVCLatestVersionSpec = "T";

        public const string WITAuthorUserIdPropertyName = "AuthorUserIdProperty";

        // Windows Service configuration
        public const string TfsIntegrationServiceName = "TFS Integration Service";
        public const string TfsIntegrationJobServiceName = "TFS Integration Job Service";
        public const string TfsServiceEventLogName = "Application";

        #region Conflicts

        public const string ConflictDetailsKey_MigrationSourceId = "MigrationSourceUniqueId";

        #endregion
    }
}
