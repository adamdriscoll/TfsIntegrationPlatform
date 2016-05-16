// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter.Linking
{
    internal class TfsExternalArtifactHandler : TfsArtifactHandlerBase
    {
        public TfsExternalArtifactHandler()
            :base(new ExternalArtifactType())
        {}

        public override bool TryExtractArtifactId(IArtifact artifact, out string id)
        {
            if (!base.TryExtractArtifactId(artifact, out id))
            {
                return false;
            }

            if (IsMyUri(artifact.Uri))
            {
                id = artifact.Uri;
                return true;
            }
            id = null;
            return false;
        }

        public override bool TryCreateArtifactFromId(ArtifactType artifactType, string id, out IArtifact artifact)
        {
            if (!base.TryCreateArtifactFromId(artifactType, id, out artifact))
            {
                return false;
            }

            artifact = new Toolkit.Linking.Artifact(id, m_handledArtifactType);
            return true;
        }

        /// <summary>
        /// Checks whether URI describes an external artifact.
        /// </summary>
        /// <param name="uri">URI</param>
        /// <returns>True if URI belongs to an external artifact</returns>
        internal static bool IsMyUri(
            string uri)
        {
            return uri.StartsWith(
                LinkingConstants.ExternalArtifactPrefix, StringComparison.InvariantCulture);
        }
    }
}