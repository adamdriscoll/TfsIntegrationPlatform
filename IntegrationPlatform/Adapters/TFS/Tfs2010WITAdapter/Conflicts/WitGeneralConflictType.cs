// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts
{
    /// <summary>
    /// TFS WIT Adapter General Conflict Type
    /// </summary>
    public class WitGeneralConflictType : ConflictType
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
                new WitGeneralConflictType(), 
                MigrationConflict.Status.Unresolved, 
                exception.ToString(), 
                CreateScopeHint(exception));
        }

        /// <summary>
        /// Gets the reference name of this conflict type.
        /// </summary>
        public override Guid ReferenceName
        {
            get
            {
                return WitGeneralConflictTypeConstants.ReferenceName;
            }
        }

        /// <summary>
        /// Gets the friendly name of this conflict type.
        /// </summary>
        public override string FriendlyName
        {
            get
            {
                return WitGeneralConflictTypeConstants.FriendlyName;
            }
        }

        /// <summary>
        /// Tells the platform that this conflict type is countable
        /// </summary>
        public override bool IsCountable
        {
            get
            {
                return true;
            }
        }

        public WitGeneralConflictType()
            : base(new WitGeneralConflictHandler())
        { }

        /// <summary>
        /// Creates the scope hint of this type of conflict.
        /// /SourceItemId/AttachmentFileName
        /// Note: Source side item Id is expected.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static string CreateScopeHint(Exception ex)
        {
            if (ex.InnerException != null)
            {
                // format: /<exception type>/<exception message>/<inner exception type>/<inner exception message>
                return string.Format("{0}{1}{2}{3}{4}{5}{6}{7}",
                    BasicPathScopeInterpreter.PathSeparator, ex.GetType().ToString(),
                    BasicPathScopeInterpreter.PathSeparator, ex.Message ?? string.Empty,
                    BasicPathScopeInterpreter.PathSeparator, ex.InnerException.GetType().ToString(),
                    BasicPathScopeInterpreter.PathSeparator, ex.InnerException.Message ?? string.Empty);
            }
            else
            {
                // format: /<exception type>/<exception message>
                return string.Format("{0}{1}{2}{3}",
                    BasicPathScopeInterpreter.PathSeparator, ex.GetType().ToString(),
                    BasicPathScopeInterpreter.PathSeparator, ex.Message ?? string.Empty);
            }
        }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            foreach (var action in WitGeneralConflictTypeConstants.SupportedActions)
            {
                AddSupportedResolutionAction(action);
            }
        }
    }
}
