// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon
{
    /// <summary>
    /// Dos 8.3 short path conflict in TFS
    /// Scope hint is always false. So this conflict needs to be resolved manually.
    /// </summary>
    public class TFSDosShortNameConflictType : ConflictType
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
                return "TFSIT_TFSDosShortNameConflictType";
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public TFSDosShortNameConflictType()
            : base(new TFSDosShortNameConflictHandler())
        { }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            this.AddSupportedResolutionAction(new TFSDosShortNameRetryAction());
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
            string message, string changeGroupName)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException("message");
            }
            if (string.IsNullOrEmpty(changeGroupName))
            {
                 throw new ArgumentNullException("changeGroupName");
           }

            TFSDosShortNameConflictType conflictInstance = new TFSDosShortNameConflictType();

            return new TFSDosShortNameConflict(conflictInstance, message, changeGroupName);
        }

        private static readonly Guid m_conflictTypeReferenceName = new Guid("55DDCC3D-789C-4cc5-9C9A-763DBE869108");
        private static readonly string m_conflictTypeFriendlyName = "DOS (8.3) short path format conflict type";
    }
}
