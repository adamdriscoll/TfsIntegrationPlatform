// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts
{
    public class TFSMulitpleParentLinkConflictType : ConflictType
    {
        private static readonly Guid s_conflictTypeReferenceName = new Guid("ADCE870C-33C0-46bc-9350-31660A463F9A");
        private static readonly string s_conflictTypeFriendlyName = "Single linked parent violation conflict type";
        internal const string SingleParentViolationMessage = "AddLink: The specified link type requires that work items have a single parent.";

        public static MigrationConflict CreateConflict(
            LinkChangeAction conflictedAction,
            Exception linkSubmissionException)
        {
            string scopeHint = null;
            string conflictDetails = null;
            ParseExceptionMessage(linkSubmissionException, conflictedAction, out scopeHint, out conflictDetails);
            MigrationConflict conflict = new MigrationConflict(new TFSMulitpleParentLinkConflictType(), MigrationConflict.Status.Unresolved, conflictDetails, scopeHint);
            conflict.ConflictedLinkChangeAction = conflictedAction;

            return conflict;
        }

        private static void ParseExceptionMessage(Exception linkSubmissionException, LinkChangeAction action, out string scopeHint, out string conflictDetails)
        {
            /*
             * Example Exception:
             * System.Web.Services.Protocols.SoapException
             * 
             * Example Message
             * AddLink: The specified link type requires that work items have a single parent. 
             * The target work item already has a parent of that type: %SourceID="969";%, %TargetID="967";%, %LinkType="2";% 
             * ---> AddLink: The specified link type requires that work items have a single parent. The target work item already 
             * has a parent of that type: %SourceID="969";%, %TargetID="967";%, %LinkType="2";%
             */
            Debug.Assert(linkSubmissionException is System.Web.Services.Protocols.SoapException,
                "linkSubmissionException is not System.Web.Services.Protocols.SoapException");

            string sourceItem = action.Link.SourceArtifactId;
            string targetItem = TfsWorkItemHandler.IdFromUri(action.Link.TargetArtifact.Uri);
            string linkType = action.Link.LinkType.ReferenceName;

            scopeHint = string.Format("/{0}/{1}/{2}", linkType, sourceItem, targetItem);
            conflictDetails = InvalidWorkItemLinkDetails.CreateConflictDetails(sourceItem, targetItem, linkType);
        }

        public TFSMulitpleParentLinkConflictType()
            : base(new TFSMulitpleParentLinkConflictHandler())
        {
        }

        /// <summary>
        /// Gets the reference name of this conflict type.
        /// </summary>
        public override Guid ReferenceName
        {
            get
            {
                return s_conflictTypeReferenceName;
            }
        }

        /// <summary>
        /// Gets the friendly name of this conflict type.
        /// </summary>
        public override string FriendlyName
        {
            get
            {
                return s_conflictTypeFriendlyName;
            }
        }

        public override string HelpKeyword
        {
            get
            {
                return "TFSIT_TFSMulitpleParentLinkConflictType";
            }
        }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            AddSupportedResolutionAction(new ManualConflictResolutionAction()); // fix target side hierarchy and retry
            AddSupportedResolutionAction(new SkipConflictedActionResolutionAction()); // skip adding the new parent and keep the existing parent
        }

        public override string TranslateConflictDetailsToReadableDescription(string dtls)
        {
            return InvalidWorkItemLinkDetails.TranslateConflictDetailsToReadableDescription(dtls, SingleParentViolationMessage);
        }
    }
}
