// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon
{
    /// <summary>
    /// Zero checkin conflict in VC
    /// Scope hint is path
    /// </summary>
    public class TFSZeroCheckinConflictType : ConflictType
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
                return "TFSIT_TFSZeroCheckinConflictType";
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public TFSZeroCheckinConflictType()
            : base(new TFSZeroCheckinConflictHandler())
        { }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            this.AddSupportedResolutionAction(new TFSZeroCheckinSkipAction());
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
            TFSZeroCheckinConflictType conflictInstance = new TFSZeroCheckinConflictType();

            return new TFSZeroCheckinConflict(conflictInstance, changeGroupName);
        }

        private static readonly Guid m_conflictTypeReferenceName = new Guid("B271FAFE-54B8-45e3-B311-9A477CE13B31");
        private static readonly string m_conflictTypeFriendlyName = "TFS zero checkin conflict type";
    }
}
