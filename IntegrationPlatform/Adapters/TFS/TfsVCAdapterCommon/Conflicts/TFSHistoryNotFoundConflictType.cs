// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon
{
    /// <summary>
    /// The history information is not found
    /// Scope hint is a range of changeset ids.
    /// </summary>
    public class TFSHistoryNotFoundConflictType : ConflictType
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
                return "TFSIT_TFSHistoryNotFoundConflictType";
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public TFSHistoryNotFoundConflictType()
            : base(new TFSHistoryNotFoundConflictHandler())
        { }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            this.AddSupportedResolutionAction(new TFSHistoryNotFoundSuppressAction());
            this.AddSupportedResolutionAction(new TFSHistoryNotFoundSkipAction());
        }

        /// <summary>
        /// Gets the scope string interpreter of this conflict type.
        /// </summary>
        public override IApplicabilityScopeInterpreter ScopeInterpreter
        {
            get
            {
                return new IntegerRangeScopeInterpreter();
            }
        }

        public static MigrationConflict CreateConflict(
            string changesetId, IMigrationAction conflictAction)
        {
            if (string.IsNullOrEmpty(changesetId))
            {
                throw new ArgumentNullException("changesetId");
            }

            TFSHistoryNotFoundConflictType conflictInstance = new TFSHistoryNotFoundConflictType();

            return new TFSHistoryNotFoundConflict(conflictInstance, changesetId, conflictAction);
        }

        private static readonly Guid m_conflictTypeReferenceName = new Guid("A9781E5C-F41E-4123-9DA6-7D73A3CF9724");
        private static readonly string m_conflictTypeFriendlyName = "TFS history not found conflict type";
    }
}
