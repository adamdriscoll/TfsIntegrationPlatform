// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ClearQuestOleServer;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.Linking
{
    [Serializable]
    public class ClearQuestDuplicateRecordLinkType : LinkType, ILinkHandler
    {
        private const string REFERENCE_NAME = "ClearQuestAdapter.LinkType.Duplicate";
        private const string FRIENDLY_NAME = "ClearQuest duplicate record link type";
        private static readonly ArtifactType s_sourceArtifactType = new ClearQuestRecordArtifactType();
        private static readonly ArtifactType s_targetArtifactType = new ClearQuestRecordArtifactType();
        private static readonly ExtendedLinkProperties s_extendedProperties = new ExtendedLinkProperties(ExtendedLinkProperties.Topology.Network);

        public ClearQuestDuplicateRecordLinkType()
            : base(REFERENCE_NAME, FRIENDLY_NAME, s_sourceArtifactType, s_targetArtifactType, s_extendedProperties)
        {}

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

        public void ExtractLinkChangeActions(
            Session session,
            OAdEntity hostRecord, 
            List<LinkChangeGroup> linkChangeGroups)
        {
            string hostRecDispName = CQWrapper.GetEntityDisplayName(hostRecord);
            string hostRecEntityDefName = CQWrapper.GetEntityDefName(hostRecord);
            string hostRecMigrItemId = UtilityMethods.CreateCQRecordMigrationItemId(hostRecEntityDefName, hostRecDispName);

            var linkChangeGroup = new LinkChangeGroup(hostRecMigrItemId, LinkChangeGroup.LinkChangeGroupStatus.Created, false);

            if (!CQWrapper.HasDuplicates(hostRecord))
            {
                return;
            }

            object[] dupRecObjs = CQWrapper.GetDuplicates(hostRecord) as object[];
            
            foreach (object dupRecObj in dupRecObjs)
            {
                OAdLink aLink = dupRecObj as OAdLink;

                if (null != aLink)
                {
                    OAdEntity childRecord = CQWrapper.GetChildEntity(aLink) as OAdEntity;
                    if (null != childRecord)
                    {                      
                        string childRecDispName = CQWrapper.GetEntityDisplayName(childRecord);
                        string childRecEntityDefName = CQWrapper.GetEntityDefName(childRecord);
                        string childRecMigrItemId = UtilityMethods.CreateCQRecordMigrationItemId(childRecEntityDefName, childRecDispName);

                        ILink dupLink = new ArtifactLink(hostRecDispName, 
                                                         new Artifact(hostRecMigrItemId, new ClearQuestRecordArtifactType()),
                                                         new Artifact(childRecMigrItemId, new ClearQuestRecordArtifactType()),
                                                         string.Empty,
                                                         this);
                        LinkChangeAction action = new LinkChangeAction(WellKnownChangeActionId.Add, 
                                                                       dupLink, 
                                                                       LinkChangeAction.LinkChangeActionStatus.Created, 
                                                                       false);
                        linkChangeGroup.AddChangeAction(action);
                    }
                    else
                    {
                        // [teyang] TODO replace debug assertion with a conflict?
                        Debug.Assert(false, "null == childRecord");
                    }
                }
                else
                {
                    // [teyang] TODO replace debug assertion with a conflict?
                    Debug.Assert(false, "null == aLink");
                }
            }

            linkChangeGroups.Add(linkChangeGroup);
        }

        public bool Update(
            ClearQuestMigrationContext migrationContext,
            Session cqSession,
            OAdEntity hostRecord, 
            LinkChangeAction linkChangeAction)
        {
            if (null == linkChangeAction)
            {
                throw new ArgumentNullException("linkChangeAction");
            }

            if (!linkChangeAction.Link.LinkType.ReferenceName.Equals(REFERENCE_NAME))
            {
                throw new ArgumentException("Link type mismatch.");
            }

            string childRecEntityDefName;            
            if (!ClearQuestRecordArtifactHandler.TryExtractRecordDefName(linkChangeAction.Link.TargetArtifact, 
                                                                         out childRecEntityDefName))
            {
                return false;
            }
            string childRecDispName;
            if (!ClearQuestRecordArtifactHandler.TryExtractRecordDispName(linkChangeAction.Link.TargetArtifact,
                                                                          out childRecDispName))
            {
                return false;
            }
 
            OAdEntity childEntity = CQWrapper.GetEntity(cqSession, childRecEntityDefName, childRecDispName);
            if (null == childEntity)
            {
                return false;
            }

            // check if hostRecord already has a duplicate of this childRecord
            bool duplicateAlreadyExist = HasDuplicateRecord(hostRecord, childRecEntityDefName, childRecDispName);

            // find out the child entity's current state
            // find the current state
            string childEntityDefName = CQWrapper.GetEntityDefName(childEntity);

            OAdFieldInfo aFldInfo = CQWrapper.GetEntityFieldValue(childEntity, migrationContext.GetStateField(childEntityDefName));
            string srcState = CQWrapper.GetFieldValue(aFldInfo);
            OAdEntityDef childEntityDef = CQWrapper.GetEntityDef(cqSession, childEntityDefName);

            if (linkChangeAction.ChangeActionId.Equals(WellKnownChangeActionId.Add))
            {                
                if (duplicateAlreadyExist)
                {
                    // [teyang] TODO
                    return false;
                }

                string[] changeActionNames = CQUtilityMethods.FindAllActionNameByTypeAndSourceState(
                                                childEntityDef, 
                                                srcState, 
                                                CQConstants.ACTION_DUPLICATE);

                string changeActionName = string.Empty;
                if (changeActionNames.Length == 0)
                {
                    // [teyang] TODO conflict
                }
                else if (changeActionNames.Length > 1)
                {
                    // [teyang] TODO conflict
                }
                else
                {
                    changeActionName = changeActionNames[0];
                }

                if (!string.IsNullOrEmpty(changeActionName))
                {
                    CQWrapper.MarkEntityAsDuplicate(cqSession, childEntity, hostRecord, changeActionName);

                    string retVal = CQWrapper.Validate(childEntity);
                    if (string.IsNullOrEmpty(retVal))
                    {
                        // [teyang] TODO conflict
                    }

                    retVal = CQWrapper.Commmit(childEntity);
                    if (string.IsNullOrEmpty(retVal))
                    {
                        // [teyang] TODO conflict
                    }
                }
            }
            else if (linkChangeAction.ChangeActionId.Equals(WellKnownChangeActionId.Delete))
            {
                if (!duplicateAlreadyExist)
                {
                    // [teyang] TODO
                    return false;
                }

                string[] changeActionNames = CQUtilityMethods.FindAllActionNameByTypeAndSourceState(
                                                childEntityDef,
                                                srcState,
                                                CQConstants.ACTION_UNDUPLICATE);

                string changeActionName = string.Empty;
                if (changeActionNames.Length == 0)
                {
                    // [teyang] TODO conflict
                }
                else if (changeActionNames.Length > 1)
                {
                    // [teyang] TODO conflict
                }
                else
                {
                    changeActionName = changeActionNames[0];
                }

                if (!string.IsNullOrEmpty(changeActionName))
                {
                    CQWrapper.UnmarkEntityAsDuplicate(cqSession, childEntity, changeActionName);

                    string retVal = CQWrapper.Validate(childEntity);
                    if (string.IsNullOrEmpty(retVal))
                    {
                        // [teyang] TODO conflict
                    }

                    retVal = CQWrapper.Commmit(childEntity);
                    if (string.IsNullOrEmpty(retVal))
                    {
                        // [teyang] TODO conflict
                    }
                }
            }
            else
            {
                //throw new MigrationException(TfsWITAdapterResources.ErrorUnsupportedChangeAction);
            }

            return true;
        }

        #endregion

        bool HasDuplicateRecord(
            OAdEntity hostRecord,
            string childRecordEntityTypeName,
            string childRecordDisplayName)
        {
            // check if hostRecord already has a duplicate of this childRecord
            if (!CQWrapper.HasDuplicates(hostRecord))
            {
                return false;
            }

            object[] dupRecObjs = CQWrapper.GetDuplicates(hostRecord) as object[];
            foreach (object dupRecObj in dupRecObjs)
            {
                OAdLink aLink = dupRecObj as OAdLink;
                if (null != aLink)
                {
                    OAdEntity record = CQWrapper.GetChildEntity(aLink) as OAdEntity;
                    if (null != record)
                    {
                        string recDispName = CQWrapper.GetEntityDisplayName(record);
                        string recEntityDefName = CQWrapper.GetEntityDefName(record);
                        if (CQStringComparer.EntityName.Equals(recEntityDefName, childRecordEntityTypeName)
                            && CQStringComparer.RecordName.Equals(recDispName, childRecordDisplayName))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
