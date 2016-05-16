// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Diagnostics;
using System.Globalization;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Linking
{
    internal class TfsHyperlinkHandler : TfsArtifactHandlerBase
    {
        public TfsHyperlinkHandler()
            : base(new HyperlinkArtifactType())
        {}

        public override bool TryExtractArtifactId(IArtifact artifact, out string id)
        {
            if (!base.TryExtractArtifactId(artifact, out id))
            {
                return false;
            }

            if (artifact.Uri.StartsWith(LinkingConstants.HyperlinkPrefix, false, CultureInfo.InvariantCulture))
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
        /// Creates URI from id.
        /// </summary>
        /// <param name="id">Id</param>
        /// <returns>URI</returns>
        internal static string UriFromId(
            string id)
        {
            return LinkingConstants.HyperlinkPrefix + id;
        }

        /// <summary>
        /// Extracts hyperlink id from URI.
        /// </summary>
        /// <param name="uri">Source URI</param>
        /// <returns>Id</returns>
        internal static string IdFromUri(
            string uri)
        {
            Debug.Assert(uri.StartsWith(LinkingConstants.HyperlinkPrefix), "Invalid hyperlink!");
            return uri.Substring(LinkingConstants.HyperlinkPrefix.Length);
        }
    }
}