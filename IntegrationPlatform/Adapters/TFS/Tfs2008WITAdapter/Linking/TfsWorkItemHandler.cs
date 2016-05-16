// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Diagnostics;
using System.Globalization;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter.Linking
{
    public class TfsWorkItemHandler : TfsArtifactHandlerBase
    {
        public TfsWorkItemHandler()
            : base(new WorkItemArtifactType())
        {}

        public override bool TryExtractArtifactId(IArtifact artifact, out string id)
        {
            if (!base.TryExtractArtifactId(artifact, out id))
            {
                return false;
            }

            if (artifact.Uri.StartsWith(LinkingConstants.WorkItemPrefix, false, CultureInfo.InvariantCulture))
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
        /// <param name="id">work item id</param>
        /// <returns>Work item URI</returns>
        public static string UriFromId(
            string id)
        {
            return LinkingConstants.WorkItemPrefix + id;
        }

        /// <summary>
        /// Extracts work item id from URI.
        /// </summary>
        /// <param name="uri">URI</param>
        /// <returns>ID</returns>
        public static string IdFromUri(
            string uri)
        {
            Debug.Assert(uri.StartsWith(LinkingConstants.WorkItemPrefix), "Invalid URI!");
            return uri.Substring(LinkingConstants.WorkItemPrefix.Length);
        }
    }
}