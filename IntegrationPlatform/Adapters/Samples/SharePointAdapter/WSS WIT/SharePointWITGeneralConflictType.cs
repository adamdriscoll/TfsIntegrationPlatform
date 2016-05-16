//------------------------------------------------------------------------------
// <copyright file="SharePointWITGeneralConflictType.cs" company="Microsoft Corporation">
//      Copyright © Microsoft Corporation.  All Rights Reserved.
// </copyright>
//
//  This code released under the terms of the 
//  Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//------------------------------------------------------------------------------

namespace Microsoft.TeamFoundation.Integration.SharePointWITAdapter
{
    using System;
    using System.Globalization;
    using Microsoft.TeamFoundation.Migration.Toolkit;

    /// <summary>
    /// TFS WIT Adapter General Conflict Type
    /// </summary>
    public class SharePointWITGeneralConflictType : ConflictType
    {
        /// <summary>
        /// Constructor. 
        /// Conflict is created without an associated change action.
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public static MigrationConflict CreateConflict(Exception exception)
        {
            return new MigrationConflict(
                new SharePointWITGeneralConflictType(),
                MigrationConflict.Status.Unresolved,
                exception.ToString(),
                CreateScopeHint(Guid.NewGuid().ToString()));
        }

        /// <summary>
        /// Constructor.
        /// Conflict is created with an associated change action.
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="conflictedAction"></param>
        /// <returns></returns>
        public static MigrationConflict CreateConflict(Exception exception, IMigrationAction conflictedAction)
        {
            return new SharePointWITGeneralConflictType().CreateConflict(exception.ToString(),
                                                               CreateScopeHint(Guid.NewGuid().ToString()),
                                                               conflictedAction);
        }

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

        /// <summary>
        /// Initializes a new instance of the <see cref="SharePointWITGeneralConflictType"/> class.
        /// </summary>
        public SharePointWITGeneralConflictType()
            : base(new SharePointWITGeneralConflictHandler())
        { }

        /// <summary>
        /// Creates the scope hint of this type of conflict.
        /// /SourceItemId/AttachmentFileName
        /// Note: Source side item Id is expected.
        /// </summary>
        /// <param name="sourceItemId">The source item id.</param>
        /// <returns></returns>
        public static string CreateScopeHint(string sourceItemId)
        {
            return string.Format(CultureInfo.CurrentCulture, "/{0}/{1}", sourceItemId, Guid.NewGuid().ToString());
        }

        /// <summary>
        /// Registers the default supported resolution actions.
        /// </summary>
        protected override void RegisterDefaultSupportedResolutionActions()
        {
            AddSupportedResolutionAction(new ManualConflictResolutionAction());
        }

        private static readonly Guid s_conflictTypeReferenceName = new Guid("{606531DF-231A-496B-9996-50F239481988}");
        private const string s_conflictTypeFriendlyName = "TFS WIT general conflict type";        
    }
}
