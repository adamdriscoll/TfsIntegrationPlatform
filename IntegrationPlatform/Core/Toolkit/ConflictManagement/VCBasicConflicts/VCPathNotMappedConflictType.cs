// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Path not mapped conflict in VC
    /// Scope hint is path
    /// </summary>
    public class VCPathNotMappedConflictType : ConflictType
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
                return "TFSIT_VCPathNotMappedConflictType";
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public VCPathNotMappedConflictType()
            : base(new VCPathNotMappedConflictHandler())
        { }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            this.AddSupportedResolutionAction(new VCChangeToAddOnBranchSourceNotMappedAction());
            this.AddSupportedResolutionAction(new VCAddPathToMappingAction());
        }

        /// <summary>
        /// Gets the scope string interpreter of this conflict type.
        /// </summary>
        public override IApplicabilityScopeInterpreter ScopeInterpreter
        {
            get
            {
                return new VCBasicPathScopeInterpreter();
            }
        }

        public static MigrationConflict CreateConflict(
            string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }
            VCPathNotMappedConflictType conflictInstance = new VCPathNotMappedConflictType();

            return new VCPathNotMappedConflict(conflictInstance, path);
        }

        private static readonly Guid m_conflictTypeReferenceName = new Guid("FCB867AF-9224-4fc4-AAC9-BFE413AADA33");
        private static readonly string m_conflictTypeFriendlyName = "VC path not mapped conflict type";
    }
}

