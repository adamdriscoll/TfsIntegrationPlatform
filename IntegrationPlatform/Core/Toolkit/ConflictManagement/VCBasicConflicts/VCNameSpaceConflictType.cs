// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Basic version control name space conflict. E.g. Rename|Rename conflicts.
    /// Scope hint is a VCPathAndIntegerRangeScopeInterpreter
    /// </summary>
    public class VCNameSpaceContentConflictType : VCContentConflictType
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
                return "TFSIT_VCNamespaceConflictType";
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public VCNameSpaceContentConflictType()
            : base(new VCContentConflictHandler())
        { }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            this.AddSupportedResolutionAction(new VCContentConflictUserMergeChangeAction());
        }

        public static MigrationConflict CreateConflict(
            MigrationAction conflictAction)
        {
            if (conflictAction == null)
            {
                throw new ArgumentNullException("conflictAction");
            }
            VCNameSpaceContentConflictType conflictInstance = new VCNameSpaceContentConflictType();

            return new VCNameSpaceContentConflict(conflictInstance, conflictAction);
        }

        private static readonly Guid m_conflictTypeReferenceName = new Guid("C1D23312-B0B1-456C-B6E4-AF22C3531480");
        private static readonly string m_conflictTypeFriendlyName = "VC namespace conflict type";
    }
}
