// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Linking
{
    public class TfsRevisionFileHandler : TfsArtifactHandlerBase
    {
        public TfsRevisionFileHandler()
            :base(new VersionControlledFileArtifactType())
        {}

        public override bool TryExtractArtifactId(IArtifact artifact, out string id)
        {
            if (!base.TryExtractArtifactId(artifact, out id))
            {
                return false;
            }

            if (IsMyUri(artifact.Uri))
            {
                id = artifact.Uri.Substring(LinkingConstants.VcRevisionFilePrefix.Length);
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

            artifact = new Toolkit.Linking.Artifact(LinkingConstants.VcRevisionFilePrefix + id, m_handledArtifactType);
            return true;
        }

        /// <summary>
        /// Checks whether URI points to a VC file revision.
        /// </summary>
        /// <param name="uri">URI</param>
        /// <returns>True if URI points to a VC file revision</returns>
        internal static bool IsMyUri(
            string uri)
        {
            return uri.StartsWith(LinkingConstants.VcRevisionFilePrefix, StringComparison.InvariantCulture);
        }
    }
}