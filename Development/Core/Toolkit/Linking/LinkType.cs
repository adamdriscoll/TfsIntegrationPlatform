// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Linking
{
    [Serializable]
    public class LinkType
    {
        public LinkType()
        {}

        public LinkType(
            string referenceName,
            string friendlyName,
            ArtifactType sourceArtifactType,
            ArtifactType targetArtifactType,
            ExtendedLinkProperties extendedLinkProperties)
        {
            if (string.IsNullOrEmpty(referenceName))
            {
                throw new ArgumentNullException("referenceName");
            }

            if (string.IsNullOrEmpty(friendlyName))
            {
                throw new ArgumentNullException("friendlyName");
            }

            if (null == sourceArtifactType)
            {
                throw new ArgumentNullException("sourceArtifactType");
            }

            if (null == targetArtifactType)
            {
                throw new ArgumentNullException("targetArtifactType");
            }

            if (null == extendedLinkProperties)
            {
                throw new ArgumentNullException("extendedLinkProperties");
            }

            ReferenceName = referenceName;
            FriendlyName = friendlyName;
            SourceArtifactType = sourceArtifactType;
            TargetArtifactType = targetArtifactType;
            ExtendedProperties = extendedLinkProperties;
        }

        public string ReferenceName
        {
            get;
            set;
        }

        public string FriendlyName
        {
            get;
            set;
        }

        public ArtifactType SourceArtifactType
        {
            get;
            set;
        }

        public ArtifactType TargetArtifactType
        {
            get;
            set;
        }

        public ExtendedLinkProperties ExtendedProperties
        {
            get; 
            set;
        }

        public virtual bool GetsActionsFromLinkChangeHistory
        {
            get { return false; }
        }

        public virtual LinkChangeAction CreateLinkDeletionAction(
            string sourceItemUri, string targetArtifactUrl, string linkTypeReferenceName)
        {
            TraceManager.TraceWarning("CreateLinkDeletionAction is not implemented");
            return null;
        }
    }
}