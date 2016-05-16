// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{
    public class CCCheckinConflictType : ConflictType
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
                return "TFSIT_CCCheckinConflictType";
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public CCCheckinConflictType()
            : base(new CCCheckinConflictHandler())
        { }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            this.AddSupportedResolutionAction(new CCCheckinConflictSkipAction());
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
            string changeGroupId)
        {
            if (changeGroupId == null)
            {
                throw new ArgumentNullException("changeGroupId");
            }
            CCCheckinConflictType conflictInstance = new CCCheckinConflictType();

            return new CCCheckinConflict(conflictInstance, message, changeGroupId);
        }

        private static readonly Guid m_conflictTypeReferenceName = new Guid("7D9EAA94-735B-43f4-9DC9-2788A550BF52");
        private static readonly string m_conflictTypeFriendlyName = CCResources.CCCheckinConflictTypeFriendlyName;
    
    }
}
