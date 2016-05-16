// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Linking
{
    [Serializable]
    public abstract class ComplexLinkType : LinkType
    {
        protected ComplexLinkType()
        {
        }

        protected ComplexLinkType(
            string referenceName,
            string friendlyName,
            ArtifactType sourceArtifactType,
            ArtifactType targetArtifactType,
            ExtendedLinkProperties extendedLinkProperties)
            :base(referenceName, friendlyName, sourceArtifactType, targetArtifactType)
        {
            if (null == extendedLinkProperties)
            {
                throw new ArgumentNullException("extendedLinkProperties");
            }
            ExtendedProperties = extendedLinkProperties;
        }

        public ExtendedLinkProperties ExtendedProperties
        {
            get; 
            set;
        }
    }
}