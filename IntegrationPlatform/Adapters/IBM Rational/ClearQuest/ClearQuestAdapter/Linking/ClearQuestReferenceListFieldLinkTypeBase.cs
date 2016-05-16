// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClearQuestOleServer;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.MigrationItem;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using System.Diagnostics;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.Linking
{
    [Serializable]
    public class ClearQuestReferenceListFieldLinkTypeBase : LinkType, ILinkHandler
    {
        private const string ReferenceNameQualifier = 
            "ClearQuestAdapter.LinkType.ReferenceListFieldRecordLink.{0}.{1}"; // do not localize

        private const string FriendlyNameFormat =
            "ClearQuest reference-list-field record link type for records of '{0}' reference-list field '{1}'"; // do not localize

        private static readonly ArtifactType s_sourceArtifactType = new ClearQuestRecordArtifactType();
        private static readonly ArtifactType s_targetArtifactType = new ClearQuestRecordArtifactType();
        private static readonly ExtendedLinkProperties s_extendedProperties = 
            new ExtendedLinkProperties(ExtendedLinkProperties.Topology.Network);

        private static IFieldSkipAlgorithm s_skipAlgorithm = new InternalFieldSkipLogic();

        private string m_hostRecordType;
        private string m_referenceFieldName;

        public ClearQuestReferenceListFieldLinkTypeBase(
            string hostRecordType,
            string referenceFieldName)
            : base(string.Format(ReferenceNameQualifier, hostRecordType, referenceFieldName),
                   string.Format(FriendlyNameFormat, hostRecordType, referenceFieldName), 
                   s_sourceArtifactType,
                   s_targetArtifactType,
                   s_extendedProperties)
        {
            m_hostRecordType = hostRecordType;
            m_referenceFieldName = referenceFieldName;
        }

        #region ILinkHandler Members

        public void ExtractLinkChangeActions(
            Session session,
            OAdEntity hostRecord,
            List<LinkChangeGroup> linkChangeGroups)
        {
            string hostRecDispName = CQWrapper.GetEntityDisplayName(hostRecord);
            string hostRecEntityDefName = CQWrapper.GetEntityDefName(hostRecord);

            if (string.IsNullOrEmpty(hostRecEntityDefName)
                || !CQStringComparer.EntityName.Equals(hostRecEntityDefName, this.m_hostRecordType))
            {
                return;
            }

            string hostRecMigrItemId = UtilityMethods.CreateCQRecordMigrationItemId(hostRecEntityDefName, hostRecDispName);

            var linkChangeGroup = new LinkChangeGroup(hostRecMigrItemId, LinkChangeGroup.LinkChangeGroupStatus.Created, false);

            OAdFieldInfo fldInfo = CQWrapper.GetEntityFieldValue(hostRecord, m_referenceFieldName);
            int cqFieldType = CQWrapper.GetFieldType(fldInfo);

            if (cqFieldType == CQConstants.FIELD_REFERENCE_LIST)
            {
                // get the current entity def handle
                OAdEntityDef curEntityDef = CQWrapper.GetEntityDef(session, hostRecEntityDefName);
                OAdEntityDef refEntityDef = CQWrapper.GetFieldReferenceEntityDef(curEntityDef, m_referenceFieldName);
                string childRecEntityDefName = CQWrapper.GetEntityDefName(refEntityDef);

                int valueStatus = CQWrapper.GetFieldValueStatus(fldInfo);
                if (valueStatus == (int)CQConstants.FieldStatus.HAS_VALUE)
                {
                    // list value required
                    object[] refFldValObjs = CQWrapper.GetFieldValueAsList(fldInfo);
                    foreach (object refFldValObj in refFldValObjs)
                    {
                        string refFldVal = refFldValObj as string;
                        if (!CQStringComparer.RecordName.Equals(refFldVal, hostRecDispName))
                        {
                            // NOT a reference to self.. Note TFS cannot have a reference to self
                            OAdEntity childRecord = CQWrapper.GetEntity(session, childRecEntityDefName, refFldVal);
                            if (null != childRecord)
                            {
                                string childRecDispName = CQWrapper.GetEntityDisplayName(childRecord);
                                string childRecMigrItemId = UtilityMethods.CreateCQRecordMigrationItemId(
                                    childRecEntityDefName, childRecDispName);

                                ILink refFieldLink = new ArtifactLink(
                                    hostRecDispName,
                                    new Artifact(hostRecMigrItemId, new ClearQuestRecordArtifactType()),
                                    new Artifact(childRecMigrItemId, new ClearQuestRecordArtifactType()),
                                    string.Empty,
                                    this);

                                LinkChangeAction action = new LinkChangeAction(
                                    WellKnownChangeActionId.Add,
                                    refFieldLink,
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
                    } // foreach obj refFldValObj
                }
            }


            linkChangeGroups.Add(linkChangeGroup);
        }

        public bool Update(
            ClearQuestMigrationContext migrationContext,
            Session session,
            OAdEntity hostRecord,
            LinkChangeAction linkChangeAction)
        {
            if (null == linkChangeAction)
            {
                throw new ArgumentNullException("linkChangeAction");
            }

            if (!linkChangeAction.Link.LinkType.ReferenceName.StartsWith(ReferenceNameQualifier))
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

            string refFieldName = linkChangeAction.Link.LinkType.ReferenceName.Substring(ReferenceNameQualifier.Length);
            if (string.IsNullOrEmpty(refFieldName))
            {
                return false;
            }

            string hostRecEntityDefName = CQWrapper.GetEntityDefName(hostRecord);
            if (string.IsNullOrEmpty(hostRecEntityDefName)
                || !CQStringComparer.EntityName.Equals(hostRecEntityDefName, this.m_hostRecordType))
            {
                return false;
            }

            // retrieve reference field information
            OAdFieldInfo refFieldInfo = CQWrapper.GetEntityFieldValue(hostRecord, refFieldName);
            int cqFieldType = CQWrapper.GetFieldType(refFieldInfo);

            if (cqFieldType != CQConstants.FIELD_REFERENCE_LIST)
            {
                // the field is not of the FIELD_REFERENCE_LIST type

                // [teyang] TODO conflict?
                return false;
            }

            // get the current entity def 
            OAdEntityDef hostRecordEntityDef = CQWrapper.GetEntityDef(session, CQWrapper.GetEntityDefName(hostRecord));
            OAdEntityDef childRecordEntityDef = CQWrapper.GetFieldReferenceEntityDef(hostRecordEntityDef, refFieldName);
            string childRecordEntityDefName = CQWrapper.GetEntityDefName(childRecordEntityDef);

            if (!CQStringComparer.EntityName.Equals(childRecordEntityDefName, childRecEntityDefName))
            {
                // the field is not designated to hold reference to the EntityType of the target artifact

                // [teyang] TODO conflict?
                return false;
            }

            int valueStatus = CQWrapper.GetFieldValueStatus(refFieldInfo);
            if (valueStatus == (int)CQConstants.FieldStatus.HAS_VALUE)
            {
                // the field already has a list of reference values set

                bool targetArtifactIsSet = false;

                // value list expected
                object[] refFldValues = CQWrapper.GetFieldValueAsList(refFieldInfo);
                foreach (object refFldObj in refFldValues)
                {
                    string refFldVal = (string)refFldObj;
                    if (CQStringComparer.RecordName.Equals(refFldVal, childRecDispName))
                    {
                        targetArtifactIsSet = true;
                        break;
                    }
                }

                if (targetArtifactIsSet)
                {
                    // the target artifact is already referenced in the field

                    // [teyang] TODO conflict?
                    return false;
                }
            }

            string[] modifyActionNames = CQUtilityMethods.FindAllChangeActionNamesByType(
                                            session, hostRecord, CQConstants.ACTION_MODIFY);

            if (modifyActionNames.Length == 0)
            {
                // [teyang] TODO conflict?
                return false;
            }
            else if (modifyActionNames.Length > 1)
            {
                // [teyang] TODO conflict?
                return false;
            }
            else
            {
                string modAction = modifyActionNames[0];

                CQWrapper.EditEntity(session, hostRecord, modAction);

                string retVal = CQWrapper.AddFieldValue(hostRecord, refFieldName, childRecDispName);

                retVal = CQWrapper.Validate(hostRecord);
                if (string.IsNullOrEmpty(retVal))
                {
                    // [teyang] TODO conflict
                    return false;
                }

                retVal = CQWrapper.Commmit(hostRecord);
                if (string.IsNullOrEmpty(retVal))
                {
                    // [teyang] TODO conflict
                    return false;
                }

                return true;
            }
        }

        #endregion

        public static List<LinkType> ExtractSupportedLinkTypes(
            Session session,
            string hostRecordType)
        {
            List<LinkType> retval = new List<LinkType>();

            OAdEntityDef aEntityDef = CQWrapper.GetEntityDef(session, hostRecordType);
            object[] fieldDefNameObjs = CQWrapper.GetFieldDefNames(aEntityDef) as object[];

            foreach (object fieldDefNameObj in fieldDefNameObjs)
            {
                string fieldDefName = fieldDefNameObj as string;
                int fieldDefType = CQWrapper.GetFieldDefType(aEntityDef, fieldDefName);

                if (fieldDefType == CQConstants.FIELD_REFERENCE_LIST)
                {
                    retval.Add(new ClearQuestReferenceListFieldLinkTypeBase(hostRecordType, fieldDefName));
                }
            }

            return retval;
        }
    }
}
