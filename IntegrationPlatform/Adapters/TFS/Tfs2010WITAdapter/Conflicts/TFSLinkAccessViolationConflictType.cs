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
    public class TFSLinkAccessViolationConflictType : ConflictType
    {
        public static MigrationConflict CreateConflict(
            LinkChangeAction conflictedAction,
            Exception linkSubmissionException)
        {
            string scopeHint = null;
            string conflictDetails = null;
            ParseExceptionMessage(linkSubmissionException, conflictedAction, out scopeHint, out conflictDetails);
            MigrationConflict conflict = new MigrationConflict(new TFSLinkAccessViolationConflictType(),
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
            Debug.Assert(linkSubmissionException is System.Web.Services.Protocols.SoapException,
                "linkSubmissionException is not System.Web.Services.Protocols.SoapException");

            string sourceItem = action.Link.SourceArtifactId;
            string targetItem = TfsWorkItemHandler.IdFromUri(action.Link.TargetArtifact.Uri);
            string linkType = action.Link.LinkType.ReferenceName;

            scopeHint = string.Format("/{0}/{1}/{2}", linkType, sourceItem, targetItem);
            conflictDetails = InvalidWorkItemLinkDetails.CreateConflictDetails(sourceItem, targetItem, linkType);
        }

        public TFSLinkAccessViolationConflictType()
            : base(new TFSLinkAccessViolationConflictHandler())
        { }

        public override Guid ReferenceName
        {
            get { return new Guid("C2C3832B-414D-4ebe-844B-3A7C316E2592"); }
        }

        public override string FriendlyName
        {
            get
            {
                return "Link modification conflict - link does not exist or access is denied.";
            }
        }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            AddSupportedResolutionAction(new SkipConflictedActionResolutionAction());
            AddSupportedResolutionAction(new ManualConflictResolutionAction());
        }

        public override string TranslateConflictDetailsToReadableDescription(string dtls)
        {
            return InvalidWorkItemLinkDetails.TranslateConflictDetailsToReadableDescription(dtls, FriendlyName);
        }
    }
}
