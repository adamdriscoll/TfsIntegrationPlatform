// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class ChainOnConflictConflictType : ConflictType
    {
        /// <summary>
        /// Gets the reference name of this conflict type.
        /// </summary>
        public override Guid ReferenceName
        {
            get
            {
                return s_conflictTypeReferenceName;
            }
        }

        /// <summary>
        /// Gets the friendly name of this conflict type.
        /// </summary>
        public override string FriendlyName
        {
            get
            {
                return s_conflictTypeFriendlyName;
            }
        }

        public ChainOnConflictConflictType()
            : base(new ChainOnConflictConflictHandler())
        {
        }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            // this conflict type is handled by the conflict manager automatically
            // we do not register specific resolution actions
        }  

        public static string CreateConflictDetails(
            int internalConflictId)
        {
            return string.Format(
                "The conflicted change action is blocked, because conflict #{0} is not resolved yet.",
                internalConflictId);
        }

        public static string CreateScopeHint(int internalConflictId)
        {
            return string.Format("{0}", internalConflictId);
        }

        private static readonly Guid s_conflictTypeReferenceName = new Guid("F6BFB484-EE70-4ffc-AAB3-4F659B0CAF7F");
        private static readonly string s_conflictTypeFriendlyName = "Chain on conflict conflict type";
    }
}
