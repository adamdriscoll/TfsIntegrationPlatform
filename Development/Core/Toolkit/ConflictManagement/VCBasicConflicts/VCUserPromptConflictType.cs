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
    /// A conflict type that requires user's prompt. 
    /// For example, delete the whole source control tree. 
    /// Scope hint is string exact match
    /// </summary>
    public class VCUserPromptConflictType : ConflictType
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
        /// Constructor.
        /// </summary>
        public VCUserPromptConflictType()
            : base(new VCUserPromptConflictHandler())
        { }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            this.AddSupportedResolutionAction(new VCUserPromptConflictSkipAction());
        }

        /// <summary>
        /// Gets the scope string interpreter of this conflict type.
        /// </summary>
        public override IApplicabilityScopeInterpreter ScopeInterpreter
        {
            get
            {
                // Exact case-insensitive string match.
                return new ChangeGroupScopeInterpreter();
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
            VCUserPromptConflictType conflictInstance = new VCUserPromptConflictType();

            return new VCUserPromptConflict(conflictInstance, message, scopeHint);
        }

        private static readonly Guid m_conflictTypeReferenceName = new Guid("2C32918F-2CA0-40EC-82EC-585CAA465876");
        private static readonly string m_conflictTypeFriendlyName = "A conflict type that requires user's intervention.";
    }
}

