// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.Linking
{
    public abstract class ClearQuestArtifactHandlerBase : IArtifactHandler
    {
        protected readonly ArtifactType m_handledArtifactType;

        protected ClearQuestArtifactHandlerBase(ArtifactType handledArtifactType)
        {
            if (null == handledArtifactType)
            {
                throw new ArgumentNullException("handledArtifactType");
            }
            m_handledArtifactType = handledArtifactType;
        }

        protected bool CanHandleArtifactType(ArtifactType artifactType)
        {
            if (null == artifactType)
            {
                return false;
            }

            return CQStringComparer.LinkArtifactType.Equals(artifactType.ReferenceName, m_handledArtifactType.ReferenceName);
        }

        #region IArtifactHandler Members

        public virtual bool TryExtractArtifactId(
            IArtifact artifact, 
            out string id)
        {
            if (null == artifact)
            {
                throw new ArgumentNullException("artifact");
            }

            if (string.IsNullOrEmpty(artifact.Uri))
            {
                throw new ArgumentException("artifact.Uri is empty");
            }

            id = string.Empty;
            return CanHandleArtifactType(artifact.ArtifactType);
        }

        public virtual bool TryCreateArtifactFromId(
            ArtifactType artifactType, 
            string id, 
            out IArtifact artifact)
        {
            if (null == artifactType)
            {
                throw new ArgumentNullException("artifactType");
            }

            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            artifact = null;
            return CanHandleArtifactType(artifactType);
        }

        #endregion
    }
}
