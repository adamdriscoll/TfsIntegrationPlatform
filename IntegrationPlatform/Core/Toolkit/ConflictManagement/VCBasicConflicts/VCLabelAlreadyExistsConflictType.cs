// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Invalid label name conflict in TFS
    /// </summary>
    public class VCLabelAlreadyExistsConflictType : ConflictType
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

        public override string HelpKeyword
        {
            get
            {
                return "TFSIT_VCLabelAlreadyExistsConflictType";
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public VCLabelAlreadyExistsConflictType()
            : base(new VCLabelAlreadyExistsConflictHandler())
        { }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            AddSupportedResolutionAction(new VCLabelConflictManualRenameAction());
        }

        /// <summary>
        /// Gets the scope string interpreter of this conflict type.
        /// </summary>
        public override IApplicabilityScopeInterpreter ScopeInterpreter
        {
            get
            {
                // Scope doesn't apply to label names, so we use the GlobalScopeInterpreter which always returns true for IsInScope()
                return new LabelScopeInterpreter();
            }
        }

        public static MigrationConflict CreateConflict(
            MigrationAction conflictAction,
            string message)
        {
            VCLabelAlreadyExistsConflictType conflictInstance = new VCLabelAlreadyExistsConflictType();

            return new VCLabelAlreadyExistsConflict(conflictInstance, conflictAction, message);
        }

        private static readonly Guid m_conflictTypeReferenceName = new Guid("F2106F48-F713-453b-9D2C-E698A5CFBC21");
        private static readonly string m_conflictTypeFriendlyName = "Label already exists conflict type";
    }
}

