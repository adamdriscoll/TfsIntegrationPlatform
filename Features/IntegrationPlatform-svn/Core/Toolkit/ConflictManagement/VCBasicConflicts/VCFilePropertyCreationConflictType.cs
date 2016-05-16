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
    public class VCFilePropertyCreationConflictType : ConflictType
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
                return "TFSIT_VCFilePropertyCreationConflictType";
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public VCFilePropertyCreationConflictType()
            : base(new VCFilePropertyCreationConflictHandler())
        { }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            AddSupportedResolutionAction(new VCFilePropertyCreationConflictSkipAction());
            AddSupportedResolutionAction(new VCFilePropertyCreationConflictRetryAction());
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

        public static MigrationConflict CreateConflict(string message, string filePropertyName)
        {
            VCFilePropertyCreationConflictType conflictInstance = new VCFilePropertyCreationConflictType();

            return new VCFilePropertyCreationConflict(conflictInstance, message, filePropertyName);
        }

        private static readonly Guid m_conflictTypeReferenceName = new Guid("41B67268-B925-4aea-9307-6E5A107B7FE2");
        private static readonly string m_conflictTypeFriendlyName = MigrationToolkitResources.Conflict_FilePropertyCreation_Name;
    }
}

