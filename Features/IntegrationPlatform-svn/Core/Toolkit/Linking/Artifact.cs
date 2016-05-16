// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Linking
{
    public class Artifact : IArtifact
    {
        public Artifact(string uri, ArtifactType artifactType)
        {
            if (null == uri)
            {
                throw new ArgumentNullException("uri");
            }

            if (null == artifactType)
            {
                throw new ArgumentNullException("artifactType");
            }

            Uri = uri;
            ArtifactType = artifactType;
        }

        #region IArtifact Members
        public string Uri
        {
            get; 
            set;
        }

        public ArtifactType ArtifactType
        {
            get;
            set;
        }

        #endregion
    }
}