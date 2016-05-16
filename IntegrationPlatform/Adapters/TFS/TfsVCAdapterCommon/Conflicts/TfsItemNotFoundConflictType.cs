// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon
{
    /// <summary>
    /// The item cannot be found
    /// Scope hint is path.
    /// </summary>
    public class TfsItemNotFoundConflictType : ConflictType
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
                return "TFSIT_TfsItemNotFoundConflictType";
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public TfsItemNotFoundConflictType()
            : base(new TFSItemNotFoundConflictHandler())
        { }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            this.AddSupportedResolutionAction(new TfsItemNotFoundSkipAction());
            this.AddSupportedResolutionAction(new TfsItemNotFoundRetryAction());
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
            string message,
            string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }
            TfsItemNotFoundConflictType conflictInstance = new TfsItemNotFoundConflictType();

            return new TfsItemNotFoundConflict(conflictInstance, message,  path);
        }

        private static readonly Guid m_conflictTypeReferenceName = new Guid("C0BC4CFE-7804-4283-AC31-3F37D104E6B2");
        private static readonly string m_conflictTypeFriendlyName = "TFS item not found conflict type";
    }
}
