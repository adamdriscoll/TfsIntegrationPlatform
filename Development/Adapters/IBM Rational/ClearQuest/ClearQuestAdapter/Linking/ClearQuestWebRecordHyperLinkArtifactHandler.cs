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
    /// <summary>
    /// Artifact handler for ClearQuestWebRecordHyperLinkArtifact(s)
    /// </summary>
    public class ClearQuestWebRecordHyperLinkArtifactHandler : ClearQuestArtifactHandlerBase
    {
        public ClearQuestWebRecordHyperLinkArtifactHandler()
            : base(new ClearQuestWebRecordHyperLinkArtifactType())
        {
        }

        public override bool TryCreateArtifactFromId(
            ArtifactType artifactType, 
            string id, 
            out IArtifact artifact)
        {
            if (!base.TryCreateArtifactFromId(artifactType, id, out artifact))
            {
                return false;
            }

            artifact = new Artifact(id, m_handledArtifactType);
            return true;
        }

        public override bool TryExtractArtifactId(
            IArtifact artifact, 
            out string id)
        {
            if (!base.TryExtractArtifactId(artifact, out id))
            {
                return false;
            }

            id = artifact.Uri;
            return true;
        }
    }
}
