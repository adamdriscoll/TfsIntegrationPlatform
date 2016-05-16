// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Invalid label name conflict in TFS
    /// </summary>
    public class VCInvalidLabelNameConflictType : ConflictType
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
                return "TFSIT_VCInvalidLabelNameConflictType";
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public VCInvalidLabelNameConflictType()
            : base(new VCInvalidLabelNameConflictHandler())
        { }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            // Removing for now until the conflict handler can handle this for any adaptter via an interface and not just for TFS
            // AddSupportedResolutionAction(new VCInvalidLabelNameAutomaticRenameAction());
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
            VCInvalidLabelNameConflictType conflictInstance = new VCInvalidLabelNameConflictType();

            return new VCInvalidLabelNameConflict(conflictInstance, conflictAction, message);
        }

        private static readonly Guid m_conflictTypeReferenceName = new Guid("1D70B6A4-D431-4871-A019-6C60AC3099FF");
        private static readonly string m_conflictTypeFriendlyName = "Invalid label name conflict type";
    }
}

