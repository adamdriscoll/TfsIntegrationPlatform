// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Basic version control edit/edit conflict
    /// Scope hint is a VCPathAndIntegerRangeScopeInterpreter
    /// </summary>
    public class VCContentConflictType : ConflictType
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
                return "TFSIT_VCContentConflictType";
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public VCContentConflictType()
            : base(new VCContentConflictHandler())
        { }

        public VCContentConflictType(IConflictHandler conflictHandler)
            : base(conflictHandler)
        { }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            this.AddSupportedResolutionAction(new VCContentConflictTakeLocalChangeAction());
            this.AddSupportedResolutionAction(new VCContentConflictTakeOtherChangesAction());
            this.AddSupportedResolutionAction(new VCContentConflictUserMergeChangeAction());
        }

        /// <summary>
        /// Gets the scope string interpreter of this conflict type.
        /// </summary>
        public override IApplicabilityScopeInterpreter ScopeInterpreter
        {
            get
            {
                return new VCPathAndPostfixScopeInterpreter();
            }
        }

        public static MigrationConflict CreateConflict(
            MigrationAction conflictAction, MigrationAction otherSideConflictAction)
        {
            if (conflictAction == null)
            {
                throw new ArgumentNullException("conflictAction");
            }
            if (otherSideConflictAction == null)
            {
                throw new ArgumentNullException("otherSideConflictAction");
            }
            VCContentConflictType conflictInstance = new VCContentConflictType();

            return new VCContentConflict(conflictInstance, conflictAction, otherSideConflictAction);
        }

        public static MigrationConflict CreateConflict(ChangeGroup changeGroup, string conflictDetails, string actionPath)
        {
            if (changeGroup == null)
            {
                throw new ArgumentNullException("changeGroup");
            }
            if (string.IsNullOrEmpty(actionPath))
            {
                throw new ArgumentNullException("actionPath");
            }

            VCContentConflictType conflictInstance = new VCContentConflictType();

            return new VCContentConflict(conflictInstance, changeGroup, conflictDetails, actionPath);
        }

        private static readonly Guid m_conflictTypeReferenceName = new Guid("B7361EB4-D6AE-4a9e-8560-602184D9B7D9");
        private static readonly string m_conflictTypeFriendlyName = "VC content conflict type";
    }
}
