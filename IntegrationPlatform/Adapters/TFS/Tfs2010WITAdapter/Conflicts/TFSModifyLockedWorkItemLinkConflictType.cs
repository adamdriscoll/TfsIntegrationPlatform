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
    public class TFSModifyLockedWorkItemLinkConflictType : ConflictType
    {
        internal const string ModifyLockedWorkItemLinkViolationMessage = 
            "Failed to modify links. The following linked work items have been locked by an administrator";

        public static MigrationConflict CreateConflict(
            LinkChangeAction conflictedAction,
            Exception linkSubmissionException)
        {
            string scopeHint = null;
            string conflictDetails = null;
            ParseExceptionMessage(linkSubmissionException, conflictedAction, out scopeHint, out conflictDetails);
            MigrationConflict conflict = new MigrationConflict(new TFSModifyLockedWorkItemLinkConflictType(), 
                MigrationConflict.Status.Unresolved, conflictDetails, scopeHint);
            conflict.ConflictedLinkChangeAction = conflictedAction;

            return conflict;
        }

        private static void ParseExceptionMessage(
            Exception linkSubmissionException, 
            LinkChangeAction action, 
            out string scopeHint, 
            out string conflictDetails)
        {
            /*
             * Example Exception:
             * System.Web.Services.Protocols.SoapException
             * 
             * Example Message
             * Failed to modify links. The following linked work items have been locked by an administrator: 
             * %LinkFailures="1954;1955;2;2,";% ---> Failed to modify links. The following linked work items 
             * have been locked by an administrator: %LinkFailures="1954;1955;2;2,";%
             */
            Debug.Assert(linkSubmissionException is System.Web.Services.Protocols.SoapException,
                "linkSubmissionException is not System.Web.Services.Protocols.SoapException");

            string sourceItem = action.Link.SourceArtifactId;
            string targetItem = TfsWorkItemHandler.IdFromUri(action.Link.TargetArtifact.Uri);
            string linkType = action.Link.LinkType.ReferenceName;

            scopeHint = string.Format("/{0}/{1}/{2}", linkType, sourceItem, targetItem);
            conflictDetails = InvalidWorkItemLinkDetails.CreateConflictDetails(sourceItem, targetItem, linkType);
        }

        public TFSModifyLockedWorkItemLinkConflictType()
            : base(new TFSModifyLockedWorkItemLinkConflictHandler())
        {
        }

        public override Guid ReferenceName
        {
            get { return new Guid("62A55241-8853-4402-A1B7-18F3A76332A3"); }
        }

        public override string FriendlyName
        {
            get
            {
                return "Invalid modification of locked Work Item Link";
            }
        }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            AddSupportedResolutionAction(new TFSModifyLockedWorkItemLinkConflict_ResolveByForceDelete());
            AddSupportedResolutionAction(new SkipConflictedActionResolutionAction());
            AddSupportedResolutionAction(new ManualConflictResolutionAction());
        }

        public override string TranslateConflictDetailsToReadableDescription(string dtls)
        {
            return InvalidWorkItemLinkDetails.TranslateConflictDetailsToReadableDescription(dtls, ModifyLockedWorkItemLinkViolationMessage);
        }
    }
}
