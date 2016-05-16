// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace Microsoft.TeamFoundation.Migration.Toolkit.Linking
{
    /// <summary>
    /// Public interface to generate links between source and target artifacts during migration
    /// </summary>
    public interface ILink
    {
        /// <summary>
        /// ArtifactID of the source
        /// </summary>
        string SourceArtifactId
        {
            get;
            set;
        }
        /// <summary>
        /// Source Artifact
        /// </summary>
        IArtifact SourceArtifact
        {
            get;
            set;
        }
        /// <summary>
        /// Target Artifact
        /// </summary>
        IArtifact TargetArtifact
        {
            get;
            set;
        }
        /// <summary>
        /// Defines the type of link between Source and Target
        /// </summary>
        LinkType LinkType
        {
            get;
            set;
        }
        /// <summary>
        /// Comment section enables us to provide a comment for the type of link established between source and target
        /// </summary>
        string Comment
        {
            get;
            set;
        }

        /// <summary>
        /// Annotating the link to be locked, such that it cannot be modified without an endpoint-specific special process
        /// </summary>
        bool IsLocked
        {
            get;
            set;
        }

        ILink Redirect(IArtifact newTargetArtifact);
    }
}
