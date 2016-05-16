// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// A conflict type that is caused by other in-progress changegroups. 
    /// Scope hint is string exact match
    /// </summary>
    public class VCChangeGroupInProgressConflictType : ConflictType
    {
        /// <summary>
        /// Gets the reference name of this conflict type.
        /// </summary>
        public override Guid ReferenceName
        {
            get
            {
                return m_conflictTypeReferenceName;
            }
        }

        /// <summary>
        /// Gets the friendly name of this conflict type.
        /// </summary>
        public override string FriendlyName
        {
            get
            {
                return m_conflictTypeFriendlyName;
            }
        }

        /// <summary>
        /// We don't create a new ChangeGroupInProgress conflict when there is an active one with the same scope hint.
        /// </summary>
        public override bool IsCountable
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public VCChangeGroupInProgressConflictType()
            : base(new VCChangeGroupInprogressConflictHandler())
        { }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            this.AddSupportedResolutionAction(new VCChangeGroupInProgressConflictSuppressAction());
            this.AddSupportedResolutionAction(new VCChangeGroupInProgressConflictWaitAction());
        }

        /// <summary>
        /// Gets the scope string interpreter of this conflict type.
        /// </summary>
        public override IApplicabilityScopeInterpreter ScopeInterpreter
        {
            get
            {
                // Exact case-insensitive string match.
                return new StringScopeInterpreter();
            }
        }

        public static MigrationConflict CreateConflict(string message, string scopeHint)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }
            if (scopeHint == null)
            {
                throw new ArgumentNullException("scopeHint");
            }
            VCChangeGroupInProgressConflictType conflictInstance = new VCChangeGroupInProgressConflictType();

            return new VCChangeGroupInProgressConflict(conflictInstance, message, scopeHint);
        }

        private static readonly Guid m_conflictTypeReferenceName = new Guid("7B0A85F4-1752-44AC-90EB-E0F5B0A7FA25");
        private static readonly string m_conflictTypeFriendlyName = "A conflict type that caused by other in-progress change groups in the migration pipeline";
    }
}

