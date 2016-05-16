// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon
{
    /// <summary>
    /// Checkin conflict in VC
    /// Scope hint is changeset (AKA changegroup name)
    /// </summary>
    public class TfsCheckinFailureConflictType : ConflictType
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
                return "TFSIT_TfsCheckinFailureConflictType";
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public TfsCheckinFailureConflictType()
            : base(new TfsCheckinFailureConflictHandler())
        { }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            this.AddSupportedResolutionAction(new TfsCheckinFailureRetryAction());
            this.AddSupportedResolutionAction(new TfsCheckinFailureManualResolveAction());
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

        /// <summary>
        /// Gets a flag to indicate whether the conflicts of this type is countable
        /// </summary>
        /// <remarks>
        /// The framework only saves one active conflict in the storage with a counter for the coutable conflicts.
        /// </remarks>
        public override bool IsCountable
        {
            get
            {
                return true;
            }
        }

        public static MigrationConflict CreateConflict(
            string changeGroupName,
            string conflictDetails)
        {
            if (changeGroupName == null)
            {
                throw new ArgumentNullException("changeGroupName");
            }
            TfsCheckinFailureConflictType conflictInstance = new TfsCheckinFailureConflictType();

            return new TfsCheckinFailureConflict(conflictInstance, changeGroupName, conflictDetails);
        }

        private static readonly Guid m_conflictTypeReferenceName = new Guid("CFD6F940-DDF7-4794-A7E4-BB8959FF9533");
        private static readonly string m_conflictTypeFriendlyName = "TFS checkin failure conflict type";
    }
}
