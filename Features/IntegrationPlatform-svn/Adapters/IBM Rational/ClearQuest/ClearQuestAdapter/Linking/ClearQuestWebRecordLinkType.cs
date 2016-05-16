// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClearQuestOleServer;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.Linking
{
    /// <summary>
    /// ClearQuest Web provides web-access to records. This link type is fabricated and creates one "hyper link" to each subject CQ record.
    /// </summary>
    [Serializable]
    public class ClearQuestWebRecordLinkType : LinkType, ILinkHandler
    {
        private const string REFERENCE_NAME = "ClearQuestAdapter.LinkType.Web.RecordHyperLink";
        private const string FRIENDLY_NAME = "ClearQuest Web Record Hyper Link Type";
        private static readonly ArtifactType s_sourceArtifactType = new ClearQuestRecordArtifactType();
        private static readonly ArtifactType s_targetArtifactType = new ClearQuestWebRecordHyperLinkArtifactType();
        private static readonly ExtendedLinkProperties s_extendedProperties = new ExtendedLinkProperties(ExtendedLinkProperties.Topology.Network);

        private string m_urlFormat = "{0}"; // this is the default
        private const string CQIdFieldName = "id";  // do not localize

        public ClearQuestWebRecordLinkType()
            : base(REFERENCE_NAME, FRIENDLY_NAME, s_sourceArtifactType, s_targetArtifactType, s_extendedProperties)
        {

        }

        public ClearQuestWebRecordLinkType(string urlFormat)
            : base(REFERENCE_NAME, FRIENDLY_NAME, s_sourceArtifactType, s_targetArtifactType, s_extendedProperties)
        {
            m_urlFormat = urlFormat;
        }

        public override LinkChangeAction CreateLinkDeletionAction(string sourceItemUri, string targetArtifactUrl, string linkTypeReferenceName)
        {
            ClearQuestRecordArtifactHandler handler = new ClearQuestRecordArtifactHandler();
            IArtifact srcArtifact;
            IArtifact tgtArtifact;
            if (!handler.TryCreateArtifactFromId(s_sourceArtifactType, sourceItemUri, out srcArtifact)
                || !handler.TryCreateArtifactFromId(s_targetArtifactType, targetArtifactUrl, out tgtArtifact))
            {
                return null;
            }

            string dispName;
            if (!ClearQuestRecordArtifactHandler.TryExtractRecordDispName(srcArtifact, out dispName))
            {
                return null;
            }

            ILink link = new ArtifactLink(dispName, srcArtifact, tgtArtifact, string.Empty, this);

            LinkChangeAction action = new LinkChangeAction(WellKnownChangeActionId.Delete,
                                                           link,
                                                           LinkChangeAction.LinkChangeActionStatus.Created,
                                                           false);
            return action;
        }

        #region ILinkHandler Members

        public void ExtractLinkChangeActions(ClearQuestOleServer.Session session, ClearQuestOleServer.OAdEntity hostRecord, List<LinkChangeGroup> linkChangeGroups)
        {
            string recordId = CQWrapper.GetFieldValue(CQWrapper.GetEntityFieldValue(hostRecord, CQIdFieldName));
            string url = string.Format(m_urlFormat, recordId);

            var linkChangeGroup = new LinkChangeGroup(recordId, LinkChangeGroup.LinkChangeGroupStatus.Created, false);

            string hostEntityDefName = CQWrapper.GetEntityDefName(hostRecord);
            string hostEntityDispName = CQWrapper.GetEntityDisplayName(hostRecord);
            string hostRecordId = UtilityMethods.CreateCQRecordMigrationItemId(hostEntityDefName, hostEntityDispName);

            ArtifactLink link = new ArtifactLink(hostRecordId, new Artifact(hostRecordId, s_sourceArtifactType), new Artifact(url, s_targetArtifactType), string.Empty, this);
            LinkChangeAction action = new LinkChangeAction(WellKnownChangeActionId.Add, link, LinkChangeAction.LinkChangeActionStatus.Created, false);

            linkChangeGroup.AddChangeAction(action);
            linkChangeGroups.Add(linkChangeGroup);
        }

        public bool Update(ClearQuestMigrationContext migrationContext, ClearQuestOleServer.Session session, ClearQuestOleServer.OAdEntity hostRecord, LinkChangeAction linkChangeAction)
        {
            // this link type is fabricated. Do nothing for submission.
            linkChangeAction.Status = LinkChangeAction.LinkChangeActionStatus.Completed;
            return true;
        }

        #endregion
    }
}
