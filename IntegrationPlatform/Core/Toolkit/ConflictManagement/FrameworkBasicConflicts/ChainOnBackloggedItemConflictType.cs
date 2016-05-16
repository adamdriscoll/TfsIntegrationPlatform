// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class ChainOnBackloggedItemConflictType : ConflictType
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

        public ChainOnBackloggedItemConflictType()
            : base(new ChainOnBackloggedItemConflictHandler())
        { }
        
        protected override void RegisterDefaultSupportedResolutionActions()
        {
            //todo
        }        
        
        public static string CreateConflictDetails(
            string sourceItemId,
            string sourceItemRevision)
        {
            return string.Format(
                "Source item {0} is in backlog - adding new revision '{1}' to it.",
                sourceItemId,
                sourceItemRevision);
        }

        public static string CreateScopeHint(string sourceItemId)
        {
            return string.Format("/{0}", sourceItemId);
        }

        private static readonly Guid s_conflictTypeReferenceName = new Guid("A7EFC8C6-A6CF-45e7-BFA6-471942A54F37");
        private static readonly string s_conflictTypeFriendlyName = "Chain on backlogged item conflict type";
    }
}
