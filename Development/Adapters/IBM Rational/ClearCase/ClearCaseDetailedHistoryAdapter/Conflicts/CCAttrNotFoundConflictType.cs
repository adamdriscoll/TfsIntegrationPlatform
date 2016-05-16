// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{
    /// <summary>
    /// The item cannot be found
    /// Scope hint is path.
    /// </summary>
    public class CCAttrTypeNotFoundConflictType : ConflictType
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

        /// <summary>
        /// Constructor.
        /// </summary>
        public CCAttrTypeNotFoundConflictType()
            : base(new CCAttrTypeNotFoundConflictHandler())
        { }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            this.AddSupportedResolutionAction(new CCAttrTypeNotFoundSkipAction());
            this.AddSupportedResolutionAction(new CCAttrTypeNotFoundRetryAction());
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

        public override string HelpKeyword
        {
            get
            {
                return "TFSIT_CCAttrTypeNotFoundConflictType";
            }
        }

        public static MigrationConflict CreateConflict(
            string message,
            string attributeTypeName)
        {
            if (attributeTypeName == null)
            {
                throw new ArgumentNullException("attributeTypeName");
            }
            CCAttrTypeNotFoundConflictType conflictInstance = new CCAttrTypeNotFoundConflictType();

            return new CCAttrTypeNotFoundConflict(conflictInstance, message, attributeTypeName);
        }

        private static readonly Guid m_conflictTypeReferenceName = new Guid("633F6875-AAE7-4c7a-9265-D4DDFE99050C");
        private static readonly string m_conflictTypeFriendlyName = CCResources.CCAttrTypeNotFoundConflictTypeFriendlyName;
    }
}
