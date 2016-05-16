// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace Microsoft.TeamFoundation.Migration.Toolkit.Linking
{
    /// <summary>
    /// Public interface to define different artifact types on both source and target ends.
    /// </summary>
    public interface IArtifact
    {
        /// <summary>
        /// Uri of the artifact, either source or target end
        /// </summary>
        string Uri
        {
            get; 
            set;
        }
        /// <summary>
        /// Represents the type of artifact
        /// </summary>
        ArtifactType ArtifactType
        {
            get; 
            set;
        }
    }
}
