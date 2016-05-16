// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Branch parent not found conflict in VC
    /// Scope hint is path
    /// </summary>
    public class VCBranchParentNotFoundConflictType : ConflictType
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
        public VCBranchParentNotFoundConflictType()
            : base(new VCBranchParentNotFoundConflictHandler())
        { }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            this.AddSupportedResolutionAction(new VCChangeToAddOnBranchParentNotFoundAction());
            this.AddSupportedResolutionAction(new VCRetryOnBranchParentNotFoundAction());
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

        public override string HelpKeyword
        {
            get
            {
                return "TFSIT_VCBranchParentNotFoundConflictType";
            }
        }

        public static MigrationConflict CreateConflict(
            string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }
            VCBranchParentNotFoundConflictType conflictInstance = new VCBranchParentNotFoundConflictType();

            return new VCBranchParentNotFoundConflict(conflictInstance, path);
        }

        private static readonly Guid m_conflictTypeReferenceName = new Guid("314CAC91-1A21-4829-BB96-F5E86D92A19C");
        private static readonly string m_conflictTypeFriendlyName = "VC branch parent not found conflict type";
    }
}

