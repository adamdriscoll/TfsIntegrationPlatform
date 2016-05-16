// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.Linking
{
    [Serializable]
    public class ClearQuestRecordArtifactType : ArtifactType
    {
        [XmlIgnore]
        public const string REFERENCE_NAME = "ClearQuestAdapter.ArtifactType.Record";

        [XmlIgnore]
        public const string FRIENDLY_NAME = "ClearQuest Record artifact type";

        public ClearQuestRecordArtifactType()
            : base(REFERENCE_NAME, FRIENDLY_NAME, WellKnownContentType.WorkItem.ReferenceName)
        { }
    }
}
