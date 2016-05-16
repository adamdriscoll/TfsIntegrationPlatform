// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter
{
    public static partial class TfsConstants
    {
        public static readonly string DisableAreaPathAutoCreation = "DisableAreaPathAutoCreation";
        public static readonly string DisableIterationPathAutoCreation = "DisableIterationPathAutoCreation";
        public static readonly string EnableBypassRuleDataSubmission = "EnableBypassRuleDataSubmission";
        public static readonly string ReflectedWorkItemIdFieldReferenceName = "ReflectedWorkItemIdFieldReferenceName";
        public static readonly string EnableInsertReflectedWorkItemId = "EnableInsertReflectedWorkItemId";

        public static readonly string MigrationTracingFieldRefName = "TfsMigrationTool.ReflectedWorkItemId";
        public static readonly string MigrationTracingFieldDispName = "Mirrored TFS ID";
        public const FieldType MigrationTracingFieldType = FieldType.String;
        
        public static readonly string TfsAreaPathsContentTypeRefName = "Microsoft.TeamFoundation.Migration.TfsWitAdapter.AreaPaths";
        public static readonly string TfsAreaPathsContentTypeDispName = "Team Foundation Server Area Paths";

        public static readonly string TfsIterationPathsContentTypeRefName = "Microsoft.TeamFoundation.Migration.TfsWitAdapter.IterationPaths";
        public static readonly string TfsIterationPathsContentTypeDispName = "Team Foundation Server Iteration Paths";

        public static readonly string TfsCSSNodeChangesContentTypeRefName = "Microsoft.TeamFoundation.Migration.TfsWitAdapter.CSSNodeChanges";
        public static readonly string TfsCSSNodeChangesContentTypeDispName = "Team Foundation Server Common Structure node changes";
    }
}
