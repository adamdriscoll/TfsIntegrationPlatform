// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts
{
    /// <summary>
    /// ExcessivePathConflictType class
    /// </summary>
    public class ExcessivePathConflictType : ConflictType
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public ExcessivePathConflictType()
            : base(new ExcessivePathConflictHandler())
        {
        }

        /// <summary>
        /// Creates a conflict of this type.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static MigrationConflict CreateConflict(string path, Node.TreeType treeType)
        {
            return new MigrationConflict(
                new ExcessivePathConflictType(),
                MigrationConflict.Status.Unresolved,
                CreateConflictDetails(path, treeType),
                CreateScopeHint(path));
        }

        /// <summary>
        /// Gets the reference name of this conflict type.
        /// </summary>
        public override Guid ReferenceName
        {
            get
            {
                return ExcessivePathConflictTypeConstants.ReferenceName;
            }
        }

        /// <summary>
        /// Gets the friendly name of this conflict type.
        /// </summary>
        public override string FriendlyName
        {
            get
            {
                return ExcessivePathConflictTypeConstants.FriendlyName;
            }
        }

        public override string HelpKeyword
        {
            get
            {
                return "TFSIT_ExcessivePathConflictType";
            }
        }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            foreach (var action in ExcessivePathConflictTypeConstants.SupportedActions)
            {
                AddSupportedResolutionAction(action);
            }
        }

        /// <summary>
        /// Creates the scope hint of this type of conflict.
        /// /SourceItemId/AttachmentFileName
        /// Note: Source side item Id is expected.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static string CreateScopeHint(string path)
        {
            return string.Format("/{0}", path);
        }

        public static string CreateConflictDetails(string path, Node.TreeType treeType)
        {
            string pathType = "Unknown";
            switch (treeType)
            {
                case Node.TreeType.Area:
                    pathType = "Area Path";
                    break;
                case Node.TreeType.Iteration:
                    pathType = "Iteration Path";
                    break;
                default:
                    break;
            }

            return string.Format("{0},{1}", pathType, path);
        }

        public override string TranslateConflictDetailsToReadableDescription(string dtls)
        {
            const string Unknown = "Unknown";
            if (string.IsNullOrEmpty(dtls))
            {
                return Unknown;
            }

            if (dtls.StartsWith("/"))
            {
                // old details format, /<path>, created by calling CreateScopeHint(path)
                return string.Format("Path '{0}' can potentially block Work Items created under it.", dtls.Substring(1));
            }
            else
            {
                int indexOfDelimiter = dtls.IndexOf(",", StringComparison.OrdinalIgnoreCase);

                string pathType = (indexOfDelimiter > 0) ? dtls.Substring(0, indexOfDelimiter) : Unknown;
                string path = (indexOfDelimiter >= 0 && indexOfDelimiter < dtls.Length - 1) ? dtls.Substring(indexOfDelimiter + 1) : Unknown;
                return string.Format("{0} '{1}' can potentially block Work Items created under it.", pathType, path);
            }
        }
    }
}
