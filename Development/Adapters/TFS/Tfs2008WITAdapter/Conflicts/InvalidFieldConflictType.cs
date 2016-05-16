// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter.Conflicts
{
    public class InvalidFieldConflictType : ConflictType
    {
        /// <summary>
        /// Gets the reference name of this conflict type.
        /// </summary>
        public override Guid ReferenceName
        {
            get
            {
                return InvalidFieldConflictTypeConstants.ReferenceName;
            }
        }

        /// <summary>
        /// Gets the friendly name of this conflict type.
        /// </summary>
        public override string FriendlyName
        {
            get
            {
                return InvalidFieldConflictTypeConstants.FriendlyName;
            }
        }

        public override string HelpKeyword
        {
            get
            {
                return "TFSIT_InvalidFieldConflictType";
            }
        }

        public InvalidFieldConflictType()
            :base(new InvalidFieldConflictHandler())
        {
        }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            // TODO TODO TODO
            foreach (var action in InvalidFieldConflictTypeConstants.SupportedActions)
            {
                AddSupportedResolutionAction(action);
            }
        }

        protected override void RegisterConflictDetailsPropertyKeys()
        {
            foreach (var key in InvalidFieldConflictTypeConstants.SupportedConflictDetailsPropertyKeys)
            {
                RegisterConflictDetailsPropertyKey(key);
            }
        }

        public static string CreateConflictDetails(
            string sourceItemId,
            string sourceItemRevision,
            string sourceFieldRefName,
            WorkItemType targetWorkItemType)
        {
            InvalidFieldConflictTypeDetails dtls =
                new InvalidFieldConflictTypeDetails(sourceItemId, sourceItemRevision, sourceFieldRefName, targetWorkItemType);

            return dtls.Properties.ToString();
        }

        /// <summary>
        /// Creates the scope hint of this type of conflict.
        /// /TeamProject/WorkItemType
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static string CreateScopeHint(WorkItem targetWorkItem)
        {
            if (null == targetWorkItem)
            {
                throw new ArgumentNullException("targetWorkItem");
            }
            return CreateScopeHint(targetWorkItem.Project.Name, targetWorkItem.Type.Name);
        }

        public static string CreateScopeHint(
            string teamProjectName,
            string workItemType)
        {
            return string.Format("/{0}/{1}", teamProjectName, workItemType);
        }

        public override string TranslateConflictDetailsToReadableDescription(string dtls)
        {
            if (string.IsNullOrEmpty(dtls))
            {
                throw new ArgumentNullException("dtls");
            }

            InvalidFieldConflictTypeDetails details = null;
            try
            {
                ConflictDetailsProperties properties = ConflictDetailsProperties.Deserialize(dtls);
                details = new InvalidFieldConflictTypeDetails(properties);
            }
            catch (Exception)
            {
                try
                {
                    GenericSerializer<InvalidFieldConflictTypeDetails> serializer =
                        new GenericSerializer<InvalidFieldConflictTypeDetails>();

                    details = serializer.Deserialize(dtls);
                }
                catch (Exception)
                {
                    // do nothing, fall back to raw string later
                }
            }

            if (null != details)
            {
                return string.Format(
                    "Source Item {0} (revision {1}) contains Field {2} that does not exist on Work Item Type '{3}' of the target project '{4}'.",
                    details.SourceWorkItemID, details.SourceWorkItemRevision,
                    details.SourceFieldRefName, details.TargetWorkItemType, details.TargetTeamProject);
            }
            else
            {
                return dtls;
            }
        }

        public InvalidFieldConflictTypeDetails GetConflictDetails(MigrationConflict conflict)
        {
            if (!conflict.ConflictType.ReferenceName.Equals(this.ReferenceName))
            {
                throw new InvalidOperationException();
            }

            if (string.IsNullOrEmpty(conflict.ConflictDetails))
            {
                throw new ArgumentNullException("conflict.ConflictDetails");
            }

            try
            {
                // V2 conflict details, i.e. using property bag
                return new InvalidFieldConflictTypeDetails(conflict.ConflictDetailsProperties);
            }
            catch (Exception)
            {
                GenericSerializer<InvalidFieldConflictTypeDetails> serializer =
                        new GenericSerializer<InvalidFieldConflictTypeDetails>();
                return serializer.Deserialize(conflict.ConflictDetails);
            }
        }
    }



    [Serializable]
    public class InvalidFieldConflictTypeDetails
    {
        public InvalidFieldConflictTypeDetails()
        { }

        public InvalidFieldConflictTypeDetails(
            string sourceItemId,
            string sourceItemRevision,
            string sourceFieldRefName,
            WorkItemType targetWorkItemType)
        {
            SourceWorkItemID = sourceItemId;
            SourceWorkItemRevision = sourceItemRevision;
            SourceFieldRefName = sourceFieldRefName;

            TargetTeamProject = targetWorkItemType.Project.Name;
            TargetWorkItemType = targetWorkItemType.Name;
            TargetTeamFoundationServerUrl = targetWorkItemType.Store.TeamFoundationServer.Uri.AbsoluteUri;
        }

        internal InvalidFieldConflictTypeDetails(ConflictDetailsProperties detailsProperties)
        {
            string srcWitId, srcWitRev, srcFldRefName, tgtTP, tgtWit, tgtSvrUrl;
            if (detailsProperties.Properties.TryGetValue(
                    InvalidFieldConflictTypeConstants.ConflictDetailsKey_SourceWorkItemId, out srcWitId)
                && detailsProperties.Properties.TryGetValue(
                    InvalidFieldConflictTypeConstants.ConflictDetailsKey_SourceWorkItemRevision, out srcWitRev)
                && detailsProperties.Properties.TryGetValue(
                    InvalidFieldConflictTypeConstants.ConflictDetailsKey_SourceFieldRefName, out srcFldRefName)
                && detailsProperties.Properties.TryGetValue(
                    InvalidFieldConflictTypeConstants.ConflictDetailsKey_TargetTeamProject, out tgtTP)
                && detailsProperties.Properties.TryGetValue(
                    InvalidFieldConflictTypeConstants.ConflictDetailsKey_TargetWorkItemType, out tgtWit)
                && detailsProperties.Properties.TryGetValue(
                    InvalidFieldConflictTypeConstants.ConflictDetailsKey_TargetTeamFoundationServerUrl,
                    out tgtSvrUrl))
            {
                SourceWorkItemID = srcWitId;
                SourceWorkItemRevision = srcWitRev;
                SourceFieldRefName = srcFldRefName;
                TargetTeamProject = tgtTP;
                TargetWorkItemType = tgtWit;
                TargetTeamFoundationServerUrl = tgtSvrUrl;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public string SourceWorkItemID { get; set; }
        public string SourceWorkItemRevision { get; set; }
        public string SourceFieldRefName { get; set; }
        public string TargetTeamProject { get; set; }
        public string TargetWorkItemType { get; set; }
        public string TargetTeamFoundationServerUrl { get; set; }

        [XmlIgnore]
        public ConflictDetailsProperties Properties
        {
            get
            {
                ConflictDetailsProperties detailsProperties = new ConflictDetailsProperties();
                detailsProperties.Properties.Add(
                    InvalidFieldConflictTypeConstants.ConflictDetailsKey_SourceWorkItemId,
                    this.SourceWorkItemID);
                detailsProperties.Properties.Add(
                    InvalidFieldConflictTypeConstants.ConflictDetailsKey_SourceWorkItemRevision,
                    this.SourceWorkItemRevision);
                detailsProperties.Properties.Add(
                    InvalidFieldConflictTypeConstants.ConflictDetailsKey_SourceFieldRefName,
                    this.SourceFieldRefName);
                detailsProperties.Properties.Add(
                    InvalidFieldConflictTypeConstants.ConflictDetailsKey_TargetTeamProject,
                    this.TargetTeamProject);
                detailsProperties.Properties.Add(
                    InvalidFieldConflictTypeConstants.ConflictDetailsKey_TargetWorkItemType,
                    this.TargetWorkItemType);
                detailsProperties.Properties.Add(
                    InvalidFieldConflictTypeConstants.ConflictDetailsKey_TargetTeamFoundationServerUrl,
                    this.TargetTeamFoundationServerUrl);

                return detailsProperties;
            }
        }
    }
}
