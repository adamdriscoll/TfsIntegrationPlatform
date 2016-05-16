// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace Microsoft.TeamFoundation.Migration.Toolkit.Linking
{
    public interface IArtifactHandler
    {
        /// <summary>
        /// Tries to extract the tool-specific id from an artifact.
        /// </summary>        
        /// <param name="artifact"></param>
        /// <param name="id">Tool-specific id</param>
        /// <returns>True if URI was processed</returns>
        bool TryExtractArtifactId(IArtifact artifact, out string id);

        /// <summary>
        /// Creates a typed artifact from the tool-specific id.
        /// </summary>
        /// <param name="artifactType"></param>
        /// <param name="id">Tool-specific id</param>
        /// <param name="artifact"></param>
        /// <returns>True if artifact was created.</returns>
        bool TryCreateArtifactFromId(ArtifactType artifactType, string id, out IArtifact artifact);
    }
}