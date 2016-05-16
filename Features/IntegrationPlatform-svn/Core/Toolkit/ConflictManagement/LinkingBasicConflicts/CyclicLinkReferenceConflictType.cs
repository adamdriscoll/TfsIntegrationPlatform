// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Diagnostics;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement
{
    /// <summary>
    /// Cyclic link reference conflict of non-circular link types
    /// </summary>
    public class CyclicLinkReferenceConflictType : ConflictType
    {
        private static readonly Guid s_conflictTypeReferenceName = new Guid("{B1D21FE2-7FEA-4aec-B2C2-0ADC004C4426}");
        private static readonly string s_conflictTypeFriendlyName = "Cyclic link reference conflict type";

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

        public CyclicLinkReferenceConflictType()
            : base(new CyclicLinkReferenceConflictHandler())
        {
        }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            AddSupportedResolutionAction(new CyclicLinkReferenceConflictDropLinkFromSource());
            AddSupportedResolutionAction(new ManualConflictResolutionAction());
        }

        public override string TranslateConflictDetailsToReadableDescription(string dtls)
        {
            int linkActionInternalId = int.Parse(dtls);
            return string.Format(
                "Link change action (Internal Id: {0}) creates a cyclic reference among the artifact," +
                " which violates the topology of the link type.",
                linkActionInternalId);
        }

        public static string CreateConflictDetails(
            LinkChangeAction linkChangeAction)
        {
            Debug.Assert(linkChangeAction.InternalId != LinkChangeAction.INVALID_INTERNAL_ID);
            return linkChangeAction.InternalId.ToString();
        }

        /// <summary>
        /// Creates the scope hint of this type of conflict.
        /// /SessionGroupId/LinkTypeReferenceName/InternalLinkChangeActionId
        /// </summary>
        /// <param name="sessionGroupId"></param>
        /// <param name="linkChangeAction"></param>
        /// <returns></returns>
        public static string CreateScopeHint(
            Guid sessionGroupId,
            LinkChangeAction linkChangeAction)
        {
            Debug.Assert(linkChangeAction.InternalId != LinkChangeAction.INVALID_INTERNAL_ID);
            Debug.Assert(!sessionGroupId.Equals(Guid.Empty));

            return string.Format(
                BasicPathScopeInterpreter.PathSeparator + "{0}" +
                BasicPathScopeInterpreter.PathSeparator + "{1}" +
                BasicPathScopeInterpreter.PathSeparator + "{2}",
                sessionGroupId.ToString(),
                linkChangeAction.Link.LinkType.ReferenceName,
                linkChangeAction.InternalId);
        }
    }
}
