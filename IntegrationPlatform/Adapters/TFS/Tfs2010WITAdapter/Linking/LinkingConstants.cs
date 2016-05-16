// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Linking
{
    /// <summary>
    /// Linking constants.
    /// </summary>
    internal static class LinkingConstants
    {
        public const string HyperlinkPrefix = "hyperlink:";             // Do not localize!
        public const string ExternalArtifactPrefix = "external:";       // Do not localize!
        public const string WorkItemPrefix = "vstfs:///WorkItemTracking/WorkItem/";     // Do not localize!
        public const string VcChangelistPrefix = "vstfs:///VersionControl/Changeset/";  // Do not localize!
        public const string VcLatestFilePrefix = "vstfs:///VersionControl/LatestItemVersion/";  // Do not localize!
        public const string VcRevisionFilePrefix = "vstfs:///VersionControl/VersionedItem/";    // Do not localize!

        public const string VcChangelistLinkType = "Fixed in Changeset";        // Do not localize!
        public const string WitRelatedWorkItemLinkType = "Related Workitem";    // Do not localize!
        public const string VcFileLinkType = "Source Code File";                // Do not localize!
        public const string WitTestResultLinkType = "Test Result";              // Do not localize!
        public const string WitHyperLinkType = "Workitem Hyperlink";            // Do not localize!
    }
}