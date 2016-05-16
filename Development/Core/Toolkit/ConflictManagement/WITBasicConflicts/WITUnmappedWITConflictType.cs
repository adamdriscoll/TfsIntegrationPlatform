// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement.WITBasicConflicts
{
    /// <summary>
    /// Unmapped Work Item Type conflict
    /// Scope is in the form of "/SourceWorkItemTypeName"
    /// </summary>
    public class WITUnmappedWITConflictType : ConflictType
    {
        /// <summary>
        /// Creates a conflict of this type.
        /// </summary>
        /// <param name="sourceWorkItemType">The source Work Item Type that does not have a mapping in the configuration.</param>
        /// <param name="conflictedAction">The conflicted change action</param>
        /// <returns></returns>
        public static MigrationConflict CreateConflict(string sourceWorkItemType, IMigrationAction conflictedAction)
        {
            var newConflict = new MigrationConflict(
                new WITUnmappedWITConflictType(),
                MigrationConflict.Status.Unresolved,
                CreateConflictDetails(sourceWorkItemType),
                CreateScopeHint(sourceWorkItemType));
            newConflict.ConflictedChangeAction = conflictedAction;
            return newConflict;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public WITUnmappedWITConflictType()
            : base(new WITUnmappedWITConflictHandler())
        { }

        /// <summary>
        /// Gets the reference name of this conflict type.
        /// </summary>
        public override Guid ReferenceName
        {
            get { return new Guid("{316E48C4-6739-413f-9718-943AA39E6239}"); }
        }

        /// <summary>
        /// Gets the friendly name of this conflict type.
        /// </summary>
        public override string FriendlyName
        {
            get { return MigrationToolkitResources.Conflict_UnmappedWIT_Name; }
        }

        public override string HelpKeyword
        {
            get
            {
                return "TFSIT_WITUnmappedWITConflictType";
            }
        }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            AddSupportedResolutionAction(new WITUnmappedWITConflictUpdateWITMappingAction());
            AddSupportedResolutionAction(new WITUnmappedWITConflictExcludeWITInSessionFilter());
            AddSupportedResolutionAction(new ManualConflictResolutionAction());
            AddSupportedResolutionAction(new SkipConflictedActionResolutionAction());
        }

        private static string CreateScopeHint(string sourceWorkItemType)
        {
            if (string.IsNullOrEmpty(sourceWorkItemType))
            {
                throw new ArgumentNullException("sourceWorkItemType");
            }

            return string.Format("/{0}", sourceWorkItemType);           
        }

        private static string CreateConflictDetails(string sourceWorkItemType)
        {
            if (string.IsNullOrEmpty(sourceWorkItemType))
            {
                throw new ArgumentNullException("sourceWorkItemType");
            }

            return string.Format(
                MigrationToolkitResources.Conflict_UnmappedWIT_Details,
                sourceWorkItemType);
        }
    }
}
