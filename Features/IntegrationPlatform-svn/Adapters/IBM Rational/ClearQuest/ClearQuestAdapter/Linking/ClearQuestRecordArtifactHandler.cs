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
    /// ClearQuestRecordArtifactHandler handles link artifacts of ClearQuestRecordArtifactType
    /// </summary>
    /// <remarks>
    /// For ClearQuestRecordArtifactType artifacts:
    /// IArtifact.Uri == Record MigrationItem Id, i.e. both are in the following form
    ///   UtilityMethods.CreateCQRecordMigrationItemId(EntityDefName, EntityDispName)
    /// </remarks>
    public class ClearQuestRecordArtifactHandler : ClearQuestArtifactHandlerBase
    {
        public ClearQuestRecordArtifactHandler()
            : base(new ClearQuestRecordArtifactType())
        { }

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

        public override bool TryExtractArtifactId(IArtifact artifact, out string id)
        {
            if (!base.TryExtractArtifactId(artifact, out id))
            {
                return false;
            }

            id = artifact.Uri;
            return true;
        }

        internal static bool TryExtractRecordDefName(IArtifact artifact, out string recordDefName)
        {
            recordDefName = string.Empty;
            if (!CQStringComparer.LinkArtifactType.Equals(artifact.ArtifactType.ReferenceName,
                                                          ClearQuestRecordArtifactType.REFERENCE_NAME))
            {
                return false;
            }

            string[] recIdSplits = UtilityMethods.ParseCQRecordMigrationItemId(artifact.Uri);
            recordDefName = recIdSplits[0];
            return true;
        }

        internal static bool TryExtractRecordDispName(IArtifact artifact, out string recordDispName)
        {
            recordDispName = string.Empty;
            if (!CQStringComparer.LinkArtifactType.Equals(artifact.ArtifactType.ReferenceName,
                                                          ClearQuestRecordArtifactType.REFERENCE_NAME))
            {
                return false;
            }

            string[] recIdSplits = UtilityMethods.ParseCQRecordMigrationItemId(artifact.Uri);
            recordDispName = recIdSplits[1];
            return true;
        }
    }
}
