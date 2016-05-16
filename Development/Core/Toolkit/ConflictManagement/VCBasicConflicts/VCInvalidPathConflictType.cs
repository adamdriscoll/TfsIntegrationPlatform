// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Invalid path conflict in TFS
    /// Scope hint is path
    /// </summary>
    public class VCInvalidPathConflictType : ConflictType
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
                return "TFSIT_TFSInvalidPathConflictType";
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public VCInvalidPathConflictType()
            : base(new VCInvalidPathConflictHandler())
        { }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            this.AddSupportedResolutionAction(new VCInvalidPathSkipAction());
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
            MigrationAction conflictAction,
            string message,
            string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }
            VCInvalidPathConflictType conflictInstance = new VCInvalidPathConflictType();

            return new VCInvalidPathConflict(conflictInstance, conflictAction, message, path);
        }

        private static readonly Guid m_conflictTypeReferenceName = new Guid("D661A693-6F2D-447e-A175-AAB682A9B769");
        private static readonly string m_conflictTypeFriendlyName = "VC invalid path conflict type";
    }
}

