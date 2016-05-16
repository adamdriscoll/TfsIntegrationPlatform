// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.Toolkit;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Tfs2008VCAdapter
{
    /// <summary>
    /// Checkin conflict in VC
    /// Scope hint is changeset (AKA changegroup name)
    /// </summary>
    public class UnhandledChangeTypeConflictType : ConflictType
    {
        public static Guid ConflictTypeReferenceName
        {
            get
            {
                return m_conflictTypeReferenceName;
            }
        }

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
        public UnhandledChangeTypeConflictType(IEnumerable<Microsoft.TeamFoundation.VersionControl.Client.ChangeType> validChangeTypes)
            : base(new UnhandledChangeTypeConflictHandler(validChangeTypes))
        { }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            this.AddSupportedResolutionAction(new UnhandledChangeTypeSkipAction());
            this.AddSupportedResolutionAction(new UnhandledChangeTypeMapAction());
        }

        /// <summary>
        /// Gets the scope string interpreter of this conflict type.
        /// </summary>
        public override IApplicabilityScopeInterpreter ScopeInterpreter
        {
            get
            {
                return new ChangeTypeScopeInterpreter();
            }
        }

        public static MigrationConflict CreateConflict(
            string changeType, IEnumerable<Microsoft.TeamFoundation.VersionControl.Client.ChangeType> validChangeTypes)
        {
            if (changeType == null)
            {
                throw new ArgumentNullException("changeType");
            }
            UnhandledChangeTypeConflictType conflictInstance = new UnhandledChangeTypeConflictType(validChangeTypes);

            return new UnhandledChangeTypeConflict(conflictInstance, changeType);
        }

        private static readonly Guid m_conflictTypeReferenceName = new Guid("361CD4E0-9955-42e0-A57C-EC3ADE589E77");
        private static readonly string m_conflictTypeFriendlyName = "Unhandled ChangeType conflict type";
    }
}
