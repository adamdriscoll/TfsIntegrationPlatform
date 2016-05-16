// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter.Conflicts
{
    public class InvalidFieldValueConflictType : ConflictType
    {
        /// <summary>
        /// Gets the reference name of this conflict type.
        /// </summary>
        public override Guid ReferenceName
        {
            get
            {
                return InvalidFieldValueConflictTypeConstants.ReferenceName;
            }
        }

        /// <summary>
        /// Gets the friendly name of this conflict type.
        /// </summary>
        public override string FriendlyName
        {
            get
            {
                return InvalidFieldValueConflictTypeConstants.FriendlyName;
            }
        }

        public override string HelpKeyword
        {
            get
            {
                return "TFSIT_InvalidFieldValueConflictType";
            }
        }

        public InvalidFieldValueConflictType()
            : base(new InvalidFieldValueConflictHandler())
        { }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            foreach (var action in InvalidFieldValueConflictTypeConstants.SupportedActions)
            {
                AddSupportedResolutionAction(action);
            }
        }

        protected override void RegisterConflictDetailsPropertyKeys()
        {
            foreach (var key in InvalidFieldValueConflictTypeConstants.SupportedConflictDetailsPropertyKeys)
            {
                RegisterConflictDetailsPropertyKey(key);
            }
        }

        public static string CreateConflictDetails(
            string sourceItemId,
            string sourceItemRevision,
            Field invalidValuedTargetItemField)
        {
            InvalidFieldValueConflictTypeDetails dtls =
                new InvalidFieldValueConflictTypeDetails(sourceItemId, sourceItemRevision, invalidValuedTargetItemField);

            return dtls.Properties.ToString();
        }

        public static string CreateConflictDetails(
            string sourceItemId,
            string sourceItemRevision,
            string targetFieldRefName,
            string targetFieldDispName,
            string targetFieldOriginalValue,
            string targetFieldCurrentValue,
            string targetTeamProjectName,
            string targetWorkItemType,
            string targetTFSUrl)
        {
            InvalidFieldValueConflictTypeDetails dtls = new InvalidFieldValueConflictTypeDetails(
                sourceItemId, sourceItemRevision, targetFieldRefName, targetFieldDispName,
                targetFieldOriginalValue, targetFieldCurrentValue,
                targetTeamProjectName, targetWorkItemType, targetTFSUrl);

            return dtls.Properties.ToString();
        }

        /// <summary>
        /// Creates the scope hint of this type of conflict.
        /// /TeamProject/WorkItemType/Field
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static string CreateScopeHint(Field f)
        {
            if (null == f)
            {
                throw new ArgumentNullException("f");
            }

            return string.Format("/{0}/{1}/{2}",
                f.WorkItem.Project.Name,
                f.WorkItem.Type.Name,
                f.ReferenceName);
        }

        /// <summary>
        /// Creates the scope hint of this type of conflict.
        /// /TeamProject/WorkItemType/Field
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static string CreateScopeHint(
            string targetTeamProjectName,
            string targetWorkItemType,
            string targetFieldRefName)
        {
            return string.Format("/{0}/{1}/{2}",
                targetTeamProjectName,
                targetWorkItemType,
                targetFieldRefName);
        }

        public override string TranslateConflictDetailsToReadableDescription(string dtls)
        {
            if (string.IsNullOrEmpty(dtls))
            {
                throw new ArgumentNullException("dtls");
            }

            InvalidFieldValueConflictTypeDetails details = null;
            try
            {
                ConflictDetailsProperties properties = ConflictDetailsProperties.Deserialize(dtls);
                details = new InvalidFieldValueConflictTypeDetails(properties);
            }
            catch (Exception)
            {
                try
                {
                    GenericSerializer<InvalidFieldValueConflictTypeDetails> serializer =
                        new GenericSerializer<InvalidFieldValueConflictTypeDetails>();
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
                    "Source work item {0} (revision {1}) contains invalid field change on {2} from '{3}' to '{4}' in target project '{5}'.",
                    details.SourceWorkItemID, details.SourceWorkItemRevision,
                    details.TargetFieldRefName, details.TargetFieldOriginalValue,
                    details.TargetFieldCurrentValue, details.TargetTeamProject);
            }
            else
            {
                return dtls;
            }
        }

        public InvalidFieldValueConflictTypeDetails GetConflictDetails(MigrationConflict conflict)
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
                return new InvalidFieldValueConflictTypeDetails(conflict.ConflictDetailsProperties);
            }
            catch (Exception)
            {
                GenericSerializer<InvalidFieldValueConflictTypeDetails> serializer =
                        new GenericSerializer<InvalidFieldValueConflictTypeDetails>();
                return serializer.Deserialize(conflict.ConflictDetails);
            }
        }
    }

    [Serializable]
    public class InvalidFieldValueConflictTypeDetails
    {
        public InvalidFieldValueConflictTypeDetails()
        { }

        public InvalidFieldValueConflictTypeDetails(
            string sourceItemId,
            string sourceItemRevision,
            Field invalidValuedTargetItemField)
        {
            Initialize(sourceItemId, sourceItemRevision,
                       invalidValuedTargetItemField.ReferenceName,
                       invalidValuedTargetItemField.Name,
                       (invalidValuedTargetItemField.OriginalValue == null)
                        ? "@@NULL@@"
                        : invalidValuedTargetItemField.OriginalValue.ToString(),
                       (invalidValuedTargetItemField.Value == null)
                        ? "@@NULL@@"
                        : invalidValuedTargetItemField.Value.ToString(),
                       invalidValuedTargetItemField.WorkItem.Project.Name,
                       invalidValuedTargetItemField.WorkItem.Type.Name,
                       invalidValuedTargetItemField.WorkItem.Store.TeamFoundationServer.Uri.AbsoluteUri,
                       invalidValuedTargetItemField.Status.ToString());
        }

        public InvalidFieldValueConflictTypeDetails(
            string sourceItemId,
            string sourceItemRevision,
            string targetFieldRefName,
            string targetFieldDispName,
            string targetFieldOriginalValue,
            string targetFieldCurrentValue,
            string targetTeamProjectName,
            string targetWorkItemType,
            string targetTFSUrl)
        {
            Initialize(sourceItemId, sourceItemRevision, targetFieldRefName, targetFieldDispName,
                       targetFieldOriginalValue, targetFieldCurrentValue, targetTeamProjectName,
                       targetWorkItemType, targetTFSUrl, string.Empty);
        }

        internal InvalidFieldValueConflictTypeDetails(ConflictDetailsProperties detailsProperties)
        {
            string sourceItemId, sourceItemRevision, targetFieldRefName, targetFieldDispName, targetFieldOriginalValue, 
                   targetFieldCurrentValue, targetTeamProjectName, targetWorkItemType, targetTFSUrl;

            if (detailsProperties.Properties.TryGetValue(
                    InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_SourceWorkItemID, out sourceItemId)
                && detailsProperties.Properties.TryGetValue(
                    InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_SourceWorkItemRevision, out sourceItemRevision)
                && detailsProperties.Properties.TryGetValue(
                    InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_TargetTeamProject, out targetTeamProjectName)
                && detailsProperties.Properties.TryGetValue(
                    InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_TargetWorkItemType, out targetWorkItemType)
                && detailsProperties.Properties.TryGetValue(
                    InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_TargetFieldRefName, out targetFieldRefName)
                && detailsProperties.Properties.TryGetValue(
                    InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_TargetFieldDispName, out targetFieldDispName)
                && detailsProperties.Properties.TryGetValue(
                    InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_TargetFieldOriginalValue, out targetFieldOriginalValue)
                && detailsProperties.Properties.TryGetValue(
                    InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_TargetFieldCurrentValue, out targetFieldCurrentValue)
                && detailsProperties.Properties.TryGetValue(
                    InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_TargetTeamFoundationServerUrl, out targetTFSUrl))
            {
                // for backward compatibility, reason is read from the property bag separately
                string reason;
                if (!detailsProperties.Properties.TryGetValue(
                    InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_Reason, out reason))
                {
                    reason = string.Empty;
                }

                Initialize(sourceItemId, sourceItemRevision, targetFieldRefName, targetFieldDispName,
                       targetFieldOriginalValue, targetFieldCurrentValue, targetTeamProjectName,
                       targetWorkItemType, targetTFSUrl, reason);
            }
            else
            {
                throw new ArgumentException("detailsProperties do not contain all expected values for the conflict type");
            }
        }

        private void Initialize(
            string sourceItemId,
            string sourceItemRevision,
            string targetFieldRefName,
            string targetFieldDispName,
            string targetFieldOriginalValue,
            string targetFieldCurrentValue,
            string targetTeamProjectName,
            string targetWorkItemType,
            string targetTFSUrl,
            string reason)
        {
            SourceWorkItemID = sourceItemId;
            SourceWorkItemRevision = sourceItemRevision;
            TargetFieldRefName = targetFieldRefName;
            TargetFieldDispName = targetFieldDispName;
            TargetFieldOriginalValue = (targetFieldOriginalValue == null)
                                       ? "@@NULL@@"
                                       : targetFieldOriginalValue.ToString();
            TargetFieldCurrentValue = (targetFieldCurrentValue == null)
                                      ? "@@NULL@@"
                                      : targetFieldCurrentValue.ToString();

            TargetTeamProject = targetTeamProjectName;
            TargetWorkItemType = targetWorkItemType;
            TargetTeamFoundationServerUrl = targetTFSUrl;
            Reason = reason;
        }

        public string SourceWorkItemID { get; set; }
        public string SourceWorkItemRevision { get; set; }
        public string TargetTeamProject { get; set; }
        public string TargetWorkItemType { get; set; }
        public string TargetFieldRefName { get; set; }
        public string TargetFieldDispName { get; set; }
        public string TargetFieldOriginalValue { get; set; }
        public string TargetFieldCurrentValue { get; set; }
        public string TargetTeamFoundationServerUrl { get; set; }

        [XmlIgnore]
        public string Reason { get; set; }

        [XmlIgnore]
        public ConflictDetailsProperties Properties
        {
            get
            {
                ConflictDetailsProperties detailsProperties = new ConflictDetailsProperties();
                detailsProperties.Properties.Add(
                    InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_SourceWorkItemID,
                    this.SourceWorkItemID);
                detailsProperties.Properties.Add(
                    InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_SourceWorkItemRevision,
                    this.SourceWorkItemRevision);
                detailsProperties.Properties.Add(
                    InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_TargetTeamProject,
                    this.TargetTeamProject);
                detailsProperties.Properties.Add(
                    InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_TargetWorkItemType,
                    this.TargetWorkItemType);
                detailsProperties.Properties.Add(
                    InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_TargetFieldRefName,
                    this.TargetFieldRefName);
                detailsProperties.Properties.Add(
                    InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_TargetFieldDispName,
                    this.TargetFieldDispName);
                detailsProperties.Properties.Add(
                    InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_TargetFieldOriginalValue,
                    this.TargetFieldOriginalValue);
                detailsProperties.Properties.Add(
                    InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_TargetFieldCurrentValue,
                    this.TargetFieldCurrentValue);
                detailsProperties.Properties.Add(
                    InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_TargetTeamFoundationServerUrl,
                    this.TargetTeamFoundationServerUrl);
                detailsProperties.Properties.Add(
                    InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_Reason,
                    this.Reason ?? string.Empty);

                return detailsProperties;
            }
        }
    }
}