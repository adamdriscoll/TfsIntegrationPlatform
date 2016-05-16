// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.TeamFoundation.Migration.EntityModel;
using System.Xml.Serialization;
using System.IO;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Basic work item edit/edit conflict
    /// Scope hint is in the form of "/item id/item revision"
    /// </summary>
    public class WITEditEditConflictType : ConflictType
    {
        /// <summary>
        /// Gets the reference name of this conflict type.
        /// </summary>
        public override Guid ReferenceName
        {
            get
            {
                return s_conflictTypeReferenceName;
            }
        }

        /// <summary>
        /// Gets the friendly name of this conflict type.
        /// </summary>
        public override string FriendlyName
        {
            get
            {
                return s_conflictTypeFriendlyName;
            }
        }

        public override string HelpKeyword
        {
            get
            {
                return "TFSIT_WITEditEditConflictType";
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public WITEditEditConflictType()
            :base(new WITEditEditConflictHandler())
        { }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            this.AddSupportedResolutionAction(new WITEditEditConflictIgnoreByFieldChangeAction());
            this.AddSupportedResolutionAction(new WITEditEditConflictTakeSourceChangesAction());
            this.AddSupportedResolutionAction(new WITEditEditConflictTakeTargetChangesAction());
        }

        public static string CreateConflictDetails(
            string sourceSideItemId,
            IMigrationAction sourceSideAction,
            string targetWiId,
            IMigrationAction targetSideAction)
        {
            // source_item_id::source_item_version::target_item_id::target_item_version::source_change_action_id::target_change_action_id
            return string.Format(
                "{0}::{1}::{2}::{3}::{4}::{5}",
                sourceSideItemId,
                sourceSideAction.Version,
                targetWiId,
                targetSideAction.Version,
                sourceSideAction.ActionId,
                targetSideAction.ActionId);
        }

        public static string CreateScopeHint(
            string sourceItemId,
            string sourceItemRevision,
            string targetItemId,
            string targetItemRevision)
        {
            return string.Format("/{0}/{1}/{2}/{3}", sourceItemId, sourceItemRevision, targetItemId, targetItemRevision);
        }

        public override string TranslateConflictDetailsToReadableDescription(string dtls)
        {
            if (string.IsNullOrEmpty(dtls))
            {
                throw new ArgumentNullException("dtls");
            }

            string[] splits = dtls.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries);
            switch (splits.Length)
            {
                case 3:
                    // backward compatible: we used to have 'source_item_id::source_item_version::target_item_id'
                    return string.Format(
                        "Source item {0} (revision {1}) has edit/edit conflict on target item {2}.",
                        splits[0], splits[1], splits[2]);                
                case 4:
                    return string.Format(
                        "Source item {0} (revision {1}) has edit/edit conflict on target item {2} (revision {3}).",
                        splits[0], splits[1], splits[2], splits[3]);
                case 6:
                    return PrintDetailedConflictInfo(splits);
                default:
                    throw new ArgumentException(string.Format(
                        MigrationToolkitResources.InvalidWitEditEditConflictDetailsString, dtls));
            }       
        }

        private string PrintDetailedConflictInfo(string[] splits)
        {
            // [0]source_item_id
            // [1]source_item_version
            // [2]target_item_id
            // [3]target_item_version
            // [4]source_change_action_id
            // [5]target_change_action_id
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Source item {0} (revision {1}) has edit/edit conflict on target item {2} (revision {3}).\n",
                            splits[0], splits[1], splits[2], splits[3]);
            sb.AppendLine();
            //sb.AppendLine("Source item {0} (revision {1} changes:");

            XmlDocument sourceChanges = null;
            XmlDocument targetChanges = null;

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                sourceChanges = LoadChangeDocumentByActionId(splits[4], context);
                targetChanges = LoadChangeDocumentByActionId(splits[5], context);
            }

            if (sourceChanges == null || targetChanges == null)
            {
                return sb.ToString();
            }

            AppendChangeDetails(sb, splits[0], splits[1], sourceChanges);
            sb.AppendLine();
            AppendChangeDetails(sb, splits[2], splits[3], targetChanges);

            return sb.ToString();
        }

        private void AppendChangeDetails(StringBuilder sb, string itemId, string itemVersion, XmlDocument changeDoc)
        {
            sb.AppendFormat("Item {0} (revision {1}) changes:\n", itemId, itemVersion);
            var fieldChanges = WorkItemField.ExtractFieldChangeDetails(changeDoc);
            foreach (WorkItemField field in fieldChanges)
            {
                sb.AppendFormat("  {0}: {1}\n", field.FieldName, field.FieldValue);
            }
        }

        private XmlDocument LoadChangeDocumentByActionId(
            string changeActionIdStr, 
            RuntimeEntityModel context)
        {
            long actionId;
            if (!long.TryParse(changeActionIdStr, out actionId))
            {
                return null;
            }
            var changeActionQuery = context.RTChangeActionSet.Where(a => a.ChangeActionId == actionId);
            if (changeActionQuery.Count() != 1)
            {
                return null;
            }

            XmlDocument sourceChanges = new XmlDocument();
            sourceChanges.LoadXml(changeActionQuery.First().ActionData);
            return sourceChanges;
        }

        public static bool TryGetConflictedTargetChangeActionId(
            string editEditConflictDetails, 
            out long targetChangeActionId)
        {
            targetChangeActionId = long.MinValue;

            // source_item_id::source_item_version::target_item_id::target_item_version::source_change_action_id::target_change_action_id
            // [0]source_item_id
            // [1]source_item_version
            // [2]target_item_id
            // [3]target_item_version
            // [4]source_change_action_id
            // [5]target_change_action_id
            string[] dtls = editEditConflictDetails.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries);

            if (dtls.Length != 6)
            {
                return false;
            }

            return long.TryParse(dtls[5], out targetChangeActionId);
        }

        public WITEditEditConflictTypeDetails GetConflictDetails(MigrationConflict conflict)
        {
            if (!conflict.ConflictType.ReferenceName.Equals(this.ReferenceName))
            {
                throw new InvalidOperationException();
            }

            if (string.IsNullOrEmpty(conflict.ConflictDetails))
            {
                throw new ArgumentNullException("conflict.ConflictDetails");
            }

            WITEditEditConflictTypeDetails details;
            string[] splits = conflict.ConflictDetails.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries);
            switch (splits.Length)
            {
                case 3:
                    // backward compatible: we used to have 'source_item_id::source_item_version::target_item_id'
                    details = new WITEditEditConflictTypeDetails(splits[0], splits[1], splits[2]);
                    break;

                case 4:
                    details = new WITEditEditConflictTypeDetails(splits[0], splits[1], splits[2], splits[3]);
                    break;
                case 6:
                    details = new WITEditEditConflictTypeDetails(splits[0], splits[1], splits[2], splits[3], splits[4], splits[5]);
                    break;
                default:
                    throw new ArgumentException(string.Format(
                        MigrationToolkitResources.InvalidWitEditEditConflictDetailsString, conflict.ConflictDetails));
            }
            return details;
        }

        private static readonly Guid s_conflictTypeReferenceName = new Guid("2D38897D-E180-4818-9AC6-4BB92CC7BDE8");
        private static readonly string s_conflictTypeFriendlyName = "WIT edit/edit conflict type";
    }

    [Serializable]
    public class WITEditEditConflictTypeDetails
    {
        public WITEditEditConflictTypeDetails()
        { }

        public WITEditEditConflictTypeDetails(
            string sourceItemId,
            string sourceItemRevision,
            string targetWorkItemID,
            string targetRevision,
            string sourceActionId,
            string targetActionId
            )
        {
            SourceWorkItemID = sourceItemId;
            SourceWorkItemRevision = sourceItemRevision;
            TargetWorkItemID = targetWorkItemID;
            TargetSideActionRevision = targetRevision;
            SourceSideActionID = sourceActionId;
            TargetSideActionID = targetActionId;
        }

        public WITEditEditConflictTypeDetails(
            string sourceItemId,
            string sourceItemRevision,
            string targetWorkItemID
            )
        {
            SourceWorkItemID = sourceItemId;
            SourceWorkItemRevision = sourceItemRevision;
            TargetWorkItemID = targetWorkItemID;
        }

        public WITEditEditConflictTypeDetails(
            string sourceItemId,
            string sourceItemRevision,
            string targetWorkItemID,
            string targetRevision
            )
        {
            SourceWorkItemID = sourceItemId;
            SourceWorkItemRevision = sourceItemRevision;
            TargetWorkItemID = targetWorkItemID;
            TargetSideActionRevision = targetRevision;
        }

        public string SourceWorkItemID { get; set; }
        public string SourceWorkItemRevision { get; set; }
        public string TargetWorkItemID { get; set; }
        public string TargetSideActionRevision { get; set; }
        public string SourceSideActionID { get; set; }
        public string TargetSideActionID { get; set; }
    }
}
