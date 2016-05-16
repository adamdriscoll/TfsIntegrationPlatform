// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Linking
{
    public class ArtifactLink : ILink
    {
        public ArtifactLink(
            string sourceArtifactId, 
            IArtifact sourceArtifact, 
            IArtifact targetArtifact, 
            string comment, 
            LinkType linkType)
        {
            Initialize(sourceArtifactId, sourceArtifact, targetArtifact, comment, linkType, false);
        }

        public ArtifactLink(
            string sourceArtifactId,
            IArtifact sourceArtifact,
            IArtifact targetArtifact,
            string comment,
            LinkType linkType,
            bool isLocked)
        {
            Initialize(sourceArtifactId, sourceArtifact, targetArtifact, comment, linkType, isLocked);
        }

        private void Initialize(
            string sourceArtifactId, 
            IArtifact sourceArtifact, 
            IArtifact targetArtifact, 
            string comment, 
            LinkType linkType,
            bool isLocked)
        {
            if (string.IsNullOrEmpty(sourceArtifactId))
            {
                throw new ArgumentNullException("sourceArtifactId");
            }

            if (null == sourceArtifact)
            {
                throw new ArgumentNullException("sourceArtifact");
            }

            if (null == targetArtifact)
            {
                throw new ArgumentNullException("targetArtifact");
            }

            if (null == linkType)
            {
                throw new ArgumentNullException("linkType");
            }

            SourceArtifactId = sourceArtifactId;
            SourceArtifact = sourceArtifact;
            TargetArtifact = targetArtifact;
            Comment = comment;
            LinkType = linkType;
            IsLocked = isLocked;
        }

        public string SourceArtifactId
        {
            get; set;
        }

        public IArtifact SourceArtifact
        {
            get; set;
        }

        public IArtifact TargetArtifact
        {
            get; set;
        }

        public LinkType LinkType
        {
            get; set;
        }

        public string Comment
        {
            get; set;
        }

        public bool IsLocked
        {
            get; set;
        }

        public ILink Redirect(IArtifact newTargetArtifact)
        {
            return new ArtifactLink(SourceArtifactId, SourceArtifact, newTargetArtifact, Comment, LinkType);
        }
    }
}