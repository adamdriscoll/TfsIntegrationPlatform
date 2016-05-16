// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Genereic label creation conflict
    /// This type is a fall back to use when we cannot determine a more specific cause when the creation of a label fails
    /// </summary>
    public class VCLabelCreationConflictType : ConflictType
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
                return "TFSIT_VCLabelCreationConflictType";
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public VCLabelCreationConflictType()
            : base(new VCLabelCreationConflictHandler())
        { }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            AddSupportedResolutionAction(new VCLabelCreationConflictSkipAction());
            AddSupportedResolutionAction(new VCLabelCreationConflictRetryAction());
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

        public static MigrationConflict CreateConflict(string message, string labelName)

        {
            VCLabelCreationConflictType conflictInstance = new VCLabelCreationConflictType();

            return new VCLabelCreationConflict(conflictInstance, message, labelName);
        }

        private static readonly Guid m_conflictTypeReferenceName = new Guid("94462F91-1829-4bf5-B345-7DF8D94C6A20");
        private static readonly string m_conflictTypeFriendlyName = MigrationToolkitResources.Conflict_LabelCreation_Name;
        private static readonly string m_conflictTypeDescription = MigrationToolkitResources.Conflict_LabelCreation_Description;

    }
}

