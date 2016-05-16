// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class GenericConflictType : ConflictType
    {
        public static MigrationConflict CreateConflict(Exception ex)
        {
            return new MigrationConflict(
                new GenericConflictType(), 
                MigrationConflict.Status.Unresolved, 
                ex.ToString(), 
                CreateScopeHint(ex));
        }

        /// <summary>
        /// Gets the reference name of this conflict type.
        /// </summary>
        public override Guid ReferenceName
        {
            get
            {
                return new Guid(s_conflictTypeReferenceName);
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

        public override bool IsCountable
        {
            get
            {
                return true;
            }
        }

        public GenericConflictType()
            :base (new GenericConflictHandler())
        {}

        private static string CreateScopeHint(Exception ex)
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
            AddSupportedResolutionAction(new ManualConflictResolutionAction());
            AddSupportedResolutionAction(new AutomaticResolutionAction());
        }       
        
        private const string s_conflictTypeReferenceName = "F6DAB314-2792-40d9-86CC-B40F5B827D86";
        private const string s_conflictTypeFriendlyName = "Runtime Error";
    }
}