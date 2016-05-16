// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Linking
{
    internal class TfsChangeListHandler : TfsArtifactHandlerBase
    {
        public TfsChangeListHandler()
            : base(new ChangeListArtifactType())
        {}

        public override bool TryExtractArtifactId(IArtifact artifact, out string id)
        {
            if (!base.TryExtractArtifactId(artifact, out id))
            {
                return false;
            }

            if (artifact.Uri.StartsWith(LinkingConstants.VcChangelistPrefix, false, CultureInfo.InvariantCulture))
            {
                id = IdFromUri(artifact.Uri);
                return id.Length > 0;
            }
            return false;
        }

        public override bool TryCreateArtifactFromId(ArtifactType artifactType, string id, out IArtifact artifact)
        {
            if (!base.TryCreateArtifactFromId(artifactType, id, out artifact))
            {
                return false;
            }

            artifact = new Toolkit.Linking.Artifact(UriFromId(id), m_handledArtifactType);
            return true;
        }

        /// <summary>
        /// Creates URI from the change list number
        /// </summary>
        /// <param name="id">Changelist number</param>
        /// <returns>URI</returns>
        internal static string UriFromId(
            string id)
        {
            return LinkingConstants.VcChangelistPrefix + id;
        }

        /// <summary>
        /// Extracts change list from the URI.
        /// </summary>
        /// <param name="uri">URI</param>
        /// <returns>change list</returns>
        internal static string IdFromUri(
            string uri)
        {
            Debug.Assert(uri.StartsWith(LinkingConstants.VcChangelistPrefix), "Invalid URI!");
            return uri.Substring(LinkingConstants.VcChangelistPrefix.Length);
        }

        /// <summary>
        /// Checks whether given URI belongs to a VC changelist.
        /// </summary>
        /// <param name="uri">URI</param>
        /// <returns>True if URI belongs to a VC changelist</returns>
        internal static bool IsMyUri(
            string uri)
        {
            return uri.StartsWith(LinkingConstants.VcChangelistPrefix, StringComparison.InvariantCulture);
        }
    }
}