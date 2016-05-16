// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Linking
{
    internal class TfsLatestFileHandler : TfsArtifactHandlerBase
    {
        public TfsLatestFileHandler()
            : base(new VersionControlledFileArtifactType())
        {}

        public override bool TryExtractArtifactId(IArtifact artifact, out string id)
        {
            if (!base.TryExtractArtifactId(artifact, out id))
            {
                return false;
            }

            if (IsMyUri(artifact.Uri))
            {
                id = artifact.Uri.Substring(LinkingConstants.VcLatestFilePrefix.Length);
                return true;
            }
            
            return false;
        }

        public override bool TryCreateArtifactFromId(ArtifactType artifactType, string id, out IArtifact artifact)
        {
            if (!base.TryCreateArtifactFromId(artifactType, id, out artifact))
            {
                return false;
            }

            artifact = new Toolkit.Linking.Artifact(LinkingConstants.VcLatestFilePrefix + id, m_handledArtifactType);
            return true;
        }

        /// <summary>
        /// Checks whether URI points to a known artifact.
        /// </summary>
        /// <param name="uri">Artifact's URI</param>
        /// <returns>True if URI belongs to a known artifact</returns>
        internal static bool IsMyUri(
            string uri)
        {
            return uri.StartsWith(LinkingConstants.VcLatestFilePrefix, StringComparison.InvariantCulture);
        }
    }
}