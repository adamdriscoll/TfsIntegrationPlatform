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
    public class TfsCheckinConflictType : ConflictType
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
                return "TFSIT_TfsCheckinConflictType";
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public TfsCheckinConflictType()
            : base(new TfsCheckinConflictHandler())
        { }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            this.AddSupportedResolutionAction(new TfsCheckinSkipAction());
            this.AddSupportedResolutionAction(new TfsCheckinAutoResolveAction());
        }

        /// <summary>
        /// Gets the scope string interpreter of this conflict type.
        /// </summary>
        public override IApplicabilityScopeInterpreter ScopeInterpreter
        {
            get
            {
                return new ChangeGroupScopeInterpreter();
            }
        }

        public static MigrationConflict CreateConflict(
            string changeGroupName)
        {
            if (changeGroupName == null)
            {
                throw new ArgumentNullException("changeGroupName");
            }
            TfsCheckinConflictType conflictInstance = new TfsCheckinConflictType();

            return new TfsCheckinConflict(conflictInstance, changeGroupName);
        }

        private static readonly Guid m_conflictTypeReferenceName = new Guid("14AF7EB3-7D1E-48cd-8ADC-6496EFC796D2");
        private static readonly string m_conflictTypeFriendlyName = "TFS checkin conflict type";
    }
}
