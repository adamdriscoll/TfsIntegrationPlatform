// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter.Conflicts
{
    public class InsufficientPermissionConflictType : ConflictType
    {
        /// <summary>
        /// Creates a conflict of this type
        /// </summary>
        /// <param name="migrationSourceId">The Unique Id of the migration source, for which the conflict is reported</param>
        /// <param name="ex">The permission exception that contains the details of the conflict</param>
        /// <returns>The created conflict instance</returns>
        public static MigrationConflict CreateConflict(
            Guid migrationSourceId,
            PermissionException ex)
        {
            ConflictDetailsProperties detailsProperty = new ConflictDetailsProperties();
            detailsProperty.Properties[InsufficientPermissionConflictTypeConstants.ConflictDetailsKey_UserAlias]
                = ex.UserAlias;
            detailsProperty.Properties[InsufficientPermissionConflictTypeConstants.ConflictDetailsKey_UserDomain]
                = ex.UserDomain;
            detailsProperty.Properties[InsufficientPermissionConflictTypeConstants.ConflictDetailsKey_PermissionGroupName]
                = ex.RequiredGroupAccountDisplayName;
            detailsProperty.Properties[Constants.ConflictDetailsKey_MigrationSourceId]
                = migrationSourceId.ToString();

            return new MigrationConflict(new InsufficientPermissionConflictType(),
                                          MigrationConflict.Status.Unresolved,
                                          detailsProperty.ToString(),
                                          BasicPathScopeInterpreter.PathSeparator + migrationSourceId.ToString());
        }

        protected override void RegisterConflictDetailsPropertyKeys()
        {
            foreach (var key in InsufficientPermissionConflictTypeConstants.SupportedConflictDetailsPropertyKeys)
            {
                RegisterConflictDetailsPropertyKey(key);
            }
        }

        /// <summary>
        /// Gets the reference name of this conflict type.
        /// </summary>
        public override Guid ReferenceName
        {
            get
            {
                return InsufficientPermissionConflictTypeConstants.ReferenceName;
            }
        }

        /// <summary>
        /// Gets the friendly name of this conflict type.
        /// </summary>
        public override string FriendlyName
        {
            get
            {
                return InsufficientPermissionConflictTypeConstants.FriendlyName;
            }
        }

        public override string HelpKeyword
        {
            get
            {
                return "TFSIT_InsufficientPermissionConflictType";
            }
        }

        /// <summary>
        /// Gets whether this conflict type is countable
        /// </summary>
        public override bool IsCountable
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public InsufficientPermissionConflictType()
            : base(new InsufficientPermissionConflictHandler())
        { 
        }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            foreach (var action in InsufficientPermissionConflictTypeConstants.SupportedActions)
            {
                AddSupportedResolutionAction(action);
            }
        }
    }
}
