// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit.WIT;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ServerDiff
{
    internal class WITDiffComparer : IDiffComparer
    {
        private const string c_referenceName = "ReferenceName";

        private ServerDiffEngine m_serverDiffEngine;
        private WITTranslationService m_witTranslationService;
        private SyncPoint m_latestSyncPoint;
        private WitDiffResult m_diffResult;

        public WITDiffComparer(ServerDiffEngine serverDiffEngine)
        {
            m_serverDiffEngine = serverDiffEngine;
            m_latestSyncPoint = SyncPoint.GetLatestSyncPointForSession(m_serverDiffEngine.Session);
        }

        private WITTranslationService TranslationService
        {
            get
            {
                if (m_witTranslationService == null)
                {
                    m_witTranslationService = new WITTranslationService(
                        m_serverDiffEngine.Session, 
                        new UserIdentityLookupService(m_serverDiffEngine.Config, m_serverDiffEngine.AddinManagementService));

                }
                return m_witTranslationService;
            }
        }

        private IWITDiffProvider SourceWITDiffProvider
        {
            get { return m_serverDiffEngine.SourceDiffProvider as IWITDiffProvider; }
        }

        private IWITDiffProvider TargetWITDiffProvider
        {
            get { return m_serverDiffEngine.TargetDiffProvider as IWITDiffProvider; }
        }

        public bool SkipRevCountMatch { get; set; }

        public WitDiffResult DiffResult
        {
            get { return m_diffResult;  }
        }

        public bool VerifyContentsMatch(string leftQueryCondition, string rightQueryCondition)
        {
            return VerifyContentsMatch(leftQueryCondition, rightQueryCondition, null, null);
        }

        public bool VerifyContentsMatch(
            string leftQueryCondition, 
            string rightQueryCondition,
            HashSet<string> leftFieldNamesToIgnore,
            HashSet<string> rightFieldNamesToIgnore)
        {
            if (leftFieldNamesToIgnore == null)
            {
                leftFieldNamesToIgnore = new HashSet<string>();
            }
            if (rightFieldNamesToIgnore == null)
            {
                rightFieldNamesToIgnore = new HashSet<string>();
            }
            m_diffResult = new WitDiffResult();

            Stopwatch stopWatch = Stopwatch.StartNew();

            try
            {
                foreach (var filterPair in m_serverDiffEngine.Session.Filters.FilterPair)
                {
                    try
                    {
                        string sourceFilterString = GetSourceFilterString(filterPair);
                        string targetFilterString = GetTargetFilterString(filterPair);

                        bool compareFromLeftToRight = false;
                        bool compareFromRightToLeft = false;

                        if (string.IsNullOrEmpty(rightQueryCondition))
                        {
                            // No right rightQueryCondition was specified; perform left to right whether
                            // left is specified or not
                            compareFromLeftToRight = true;
                        }
                        else
                        {
                            // RightQueryCondition was specified so perform right to left
                            compareFromRightToLeft = true;
                            if (!string.IsNullOrEmpty(leftQueryCondition))
                            {
                                // LeftQueryCondition was also specified so perform both
                                compareFromLeftToRight = true;
                            }
                        }

                        if (compareFromLeftToRight)
                        {
                            CompareWorkItemsFromOneSideToTheOther(
                                sourceFilterString,
                                m_serverDiffEngine.Session.LeftMigrationSourceUniqueId,
                                m_serverDiffEngine.LeftMigrationSource,
                                m_serverDiffEngine.RightMigrationSource,
                                SourceWITDiffProvider,
                                TargetWITDiffProvider,
                                leftQueryCondition,
                                leftFieldNamesToIgnore,
                                rightFieldNamesToIgnore);
                        }

                        if (compareFromRightToLeft)
                        {
                            CompareWorkItemsFromOneSideToTheOther(
                                targetFilterString,
                                m_serverDiffEngine.Session.RightMigrationSourceUniqueId,
                                m_serverDiffEngine.RightMigrationSource,
                                m_serverDiffEngine.LeftMigrationSource,
                                TargetWITDiffProvider,
                                SourceWITDiffProvider,
                                rightQueryCondition,
                                rightFieldNamesToIgnore,
                                leftFieldNamesToIgnore);
                        }

                    }
                    finally
                    {
                        SourceWITDiffProvider.Cleanup();
                    }
                }
            }
            catch (Exception e)
            {
                m_diffResult.ProcessingErrors.Add(e.ToString());
                throw;
            }
            finally
            {
                stopWatch.Stop();
                m_serverDiffEngine.LogInfo(String.Format(CultureInfo.InvariantCulture, ServerDiffResources.WITServerDiffTimeToRun,
                    stopWatch.Elapsed.TotalSeconds));
            }

            if (!m_diffResult.AllContentsMatch)
            {
                m_diffResult.LogDifferenceReport(m_serverDiffEngine);
            }

            return m_diffResult.AllContentsMatch;
        }

        #region private methods

        private void CompareWorkItemsFromOneSideToTheOther(
            string side1FilterString,
            string side1MigrationSourceId,
            MigrationSource side1MigrationSource,
            MigrationSource side2MigrationSource,
            IWITDiffProvider side1DiffProvider,
            IWITDiffProvider side2DiffProvider,
            string side1QueryCondition,
            HashSet<string> moreSide1FieldNamesToIgnore,
            HashSet<string> moreSide2FieldNamesToIgnore)
        {
            // Only left query condition specified
            side1DiffProvider.InitializeForDiff(side1FilterString, !m_serverDiffEngine.NoContentComparison);
            side2DiffProvider.InitializeForDiff(string.Empty, !m_serverDiffEngine.NoContentComparison);

            if (m_serverDiffEngine.NoContentComparison)
            {
                m_serverDiffEngine.LogVerbose(String.Format(CultureInfo.InvariantCulture, MigrationToolkitResources.WitDiffNoContentComparison));
            }

            foreach (IWITDiffItem side1WitDiffItem in side1DiffProvider.GetWITDiffItems(side1QueryCondition))
            {
                bool skipCurrentWorkItem = false;
                bool fieldValuesMismatch = false;
                Guid side1MigrationSourceGuid = new Guid(side1MigrationSourceId);

                IWITDiffItem side2WitDiffItem = null;
                string side2WorkItemId = TranslationService.TryGetTargetItemId(side1WitDiffItem.WorkItemId, side1MigrationSourceGuid);
                if (!string.IsNullOrEmpty(side2WorkItemId))
                {
                    side2WitDiffItem = side2DiffProvider.GetWITDiffItem(side2WorkItemId);
                }
                if (side2WitDiffItem == null)
                {
                    HandleDifference(new WitDiffPair(WitDiffType.NotMirrored, 
                        side1MigrationSource.FriendlyName, side1WitDiffItem, side2MigrationSource.FriendlyName, side2WitDiffItem));
                    continue;
                }

                if (m_serverDiffEngine.NoContentComparison)
                {
                    /* Uncomment for debugging
                    m_serverDiffEngine.LogVerbose(String.Format(CultureInfo.InvariantCulture, "Corresponding work item with Id {0} found on '{1}' for work item with Id {2} on '{3}",
                        side2WorkItemId, side2MigrationSource.FriendlyName, side1WitDiffItem.WorkItemId, side1MigrationSource.FriendlyName));
                     */
                }
                else
                {
                    TranslationService.MapWorkItemTypeFieldValues(side1WitDiffItem.WorkItemId, side1WitDiffItem.WorkItemDetails, side1MigrationSourceGuid);

                    XmlElement side1RootElement = side1WitDiffItem.WorkItemDetails.DocumentElement;
                    XmlElement side2RootElement = side2WitDiffItem.WorkItemDetails.DocumentElement;
                    HashSet<string> side2AttributesFound = new HashSet<string>();
                    foreach (XmlAttribute side1Attribute in side1RootElement.Attributes)
                    {
                        if (side1DiffProvider.IgnoreFieldInComparison(side1Attribute.Name) ||
                            side2DiffProvider.IgnoreFieldInComparison(side1Attribute.Name) ||
                            moreSide1FieldNamesToIgnore.Contains(side1Attribute.Name) ||
                            moreSide2FieldNamesToIgnore.Contains(side1Attribute.Name))
                        {
                            continue;
                        }

                        /* Uncomment for debugging
                        m_serverDiffEngine.LogVerbose(String.Format(CultureInfo.InvariantCulture,
                            "Comparing field '{0}' from source '{1}'",
                            side1Attribute.Name, side1MigrationSource.FriendlyName));
                         */

                        string side2AttributeValue = side2RootElement.GetAttribute(side1Attribute.Name);
                        if (string.IsNullOrEmpty(side2AttributeValue))
                        {
                            WitDiffPair diffPair = new WitDiffPair(WitDiffType.DefinitionMismatch, 
                                side1MigrationSource.FriendlyName, side1WitDiffItem, side2MigrationSource.FriendlyName, side2WitDiffItem);
                            diffPair.AddMissingField(new WitDiffMissingField(side1Attribute.Name, side1MigrationSource.FriendlyName));
                            skipCurrentWorkItem = HandleDifference(diffPair);
                            break;
                        }
                        side2AttributesFound.Add(side1Attribute.Name);
                        if (!string.Equals(side1Attribute.Value, side2AttributeValue, StringComparison.OrdinalIgnoreCase))
                        {
                            WitDiffPair diffPair = new WitDiffPair(WitDiffType.DataMismatch,
                                side1MigrationSource.FriendlyName, side1WitDiffItem, side2MigrationSource.FriendlyName, side2WitDiffItem);
                            diffPair.AddMismatchField(new WitDiffField(side1Attribute.Name, side1Attribute.Value, side2AttributeValue));
                            skipCurrentWorkItem = HandleDifference(diffPair);
                            break;
                        }
                        else
                        {
                            /* Uncomment for debugging
                            m_serverDiffEngine.LogVerbose(String.Format(CultureInfo.InvariantCulture, "Match: The header field '{0}' for work item {1} has value '{2}' on both sides",
                                side1Attribute.Name, side1WitDiffItem.WorkItemId, side1Attribute.Value));
                             */
                        }
                    }
                    if (skipCurrentWorkItem)
                    {
                        continue;
                    }

                    foreach (XmlAttribute side2Attribute in side2RootElement.Attributes)
                    {
                        if (!side1DiffProvider.IgnoreFieldInComparison(side2Attribute.Name) &&
                            !side2DiffProvider.IgnoreFieldInComparison(side2Attribute.Name) &&
                            !moreSide1FieldNamesToIgnore.Contains(side2Attribute.Name) &&
                            !moreSide2FieldNamesToIgnore.Contains(side2Attribute.Name) &&
                            !side2AttributesFound.Contains(side2Attribute.Name))
                        {
                            WitDiffPair diffPair = new WitDiffPair(WitDiffType.DefinitionMismatch,
                                side1MigrationSource.FriendlyName, side1WitDiffItem, side2MigrationSource.FriendlyName, side2WitDiffItem);
                            diffPair.AddMissingField(new WitDiffMissingField(side2Attribute.Name, side2MigrationSource.FriendlyName));
                            skipCurrentWorkItem = HandleDifference(diffPair);
                            break;
                        }
                    }
                    if (skipCurrentWorkItem)
                    {
                        continue;
                    }

                    XmlNodeList side1Columns = side1RootElement.SelectNodes("/WorkItemChanges/Columns/Column");
                    if (null == side1Columns)
                    {
                        m_diffResult.ProcessingErrors.Add(String.Format(CultureInfo.InvariantCulture, ServerDiffResources.InvalidXMLDocumentForDiffItem,
                            side1MigrationSource.FriendlyName));
                        continue;
                    }

                    XmlNodeList side2Columns = side2RootElement.SelectNodes("/WorkItemChanges/Columns/Column");
                    if (null == side2Columns)
                    {
                        m_diffResult.ProcessingErrors.Add(String.Format(CultureInfo.InvariantCulture, ServerDiffResources.InvalidXMLDocumentForDiffItem,
                            side2MigrationSource.FriendlyName));
                        continue;
                    }

                    Dictionary<string, XmlNode> side2ColumnsByReferenceName = new Dictionary<string, XmlNode>();
                    foreach (XmlNode side2Column in side2Columns)
                    {
                        string referenceName = GetReferenceNameFromFieldColumn(side2Column);
                        if (referenceName != null)
                        {
                            if (!side2ColumnsByReferenceName.ContainsKey(referenceName))
                            {
                                side2ColumnsByReferenceName.Add(referenceName, side2Column);
                            }
                        }
                    }

                    for (int i = 0; i < side1Columns.Count; i++)
                    {
                        XmlNode side1Column = side1Columns[i];
                        string side1ReferenceName = GetReferenceNameFromFieldColumn(side1Column);
                        if (side1ReferenceName == null)
                        {
                            WitDiffPair diffPair = new WitDiffPair(WitDiffType.InvalidDefinition, side1MigrationSource.FriendlyName, side1WitDiffItem);
                            skipCurrentWorkItem = HandleDifference(diffPair, true);
                            break;
                        }
                        XmlNode side2Column;
                        if (!side2ColumnsByReferenceName.TryGetValue(side1ReferenceName, out side2Column))
                        {
                            WitDiffPair diffPair = new WitDiffPair(WitDiffType.DefinitionMismatch,
                                side1MigrationSource.FriendlyName, side1WitDiffItem, side2MigrationSource.FriendlyName, side2WitDiffItem);

                            diffPair.AddMissingField(new WitDiffMissingField(side1ReferenceName, side1MigrationSource.FriendlyName));

                            skipCurrentWorkItem = HandleDifference(diffPair, true);
                            break;
                        }
                        string side2ReferenceName = GetReferenceNameFromFieldColumn(side2Column);
                        // Remove the side2Column from the Dictionary
                        side2ColumnsByReferenceName.Remove(side2ReferenceName);

                        string  fieldDisplayName = null;

                        const string c_displayName = "DisplayName";
                        XmlAttribute side1DisplayName = side1Column.Attributes[c_displayName];
                        if (side1DisplayName != null && !string.IsNullOrEmpty(side1DisplayName.Value))
                        {
                            fieldDisplayName = side1DisplayName.Value;
                        }
                        else
                        {
                            fieldDisplayName = side1ReferenceName;
                        }

                        if (!side1DiffProvider.IgnoreFieldInComparison(side1ReferenceName) &&
                            !side2DiffProvider.IgnoreFieldInComparison(side1ReferenceName) &&
                            !moreSide1FieldNamesToIgnore.Contains(side1ReferenceName) &&
                            !moreSide2FieldNamesToIgnore.Contains(side1ReferenceName))
                        {
                            if (!side1Column.HasChildNodes)
                            {
                                m_diffResult.ProcessingErrors.Add(String.Format(MigrationToolkitResources.WitDiffWorkItemDescriptionMissingChildNodes,
                                    side1WitDiffItem.WorkItemId, side1MigrationSource.FriendlyName, side1Column.Name));
                                continue;
                            }
                            if (!side2Column.HasChildNodes)
                            {
                                m_diffResult.ProcessingErrors.Add(String.Format(MigrationToolkitResources.WitDiffWorkItemDescriptionMissingChildNodes,
                                    side2WitDiffItem.WorkItemId, side2MigrationSource.FriendlyName, side2Column.Name));
                                continue;
                            }

                            XmlNode side1ValueNode = side1Column.FirstChild;
                            XmlNode side2ValueNode = side2Column.FirstChild;
                            if (!string.Equals(side1ValueNode.InnerText.Replace("\r\n", "\n"), side2ValueNode.InnerText.Replace("\r\n", "\n"), StringComparison.OrdinalIgnoreCase))
                            {
                                WitDiffPair diffPair = new WitDiffPair(WitDiffType.DataMismatch,
                                    side1MigrationSource.FriendlyName, side1WitDiffItem, side2MigrationSource.FriendlyName, side2WitDiffItem);
                                diffPair.AddMismatchField(new WitDiffField(fieldDisplayName, side1ValueNode.InnerText, side2ValueNode.InnerText));
                                skipCurrentWorkItem = HandleDifference(diffPair);
                                // If there's a data mismatch, continue to compare fields to report additional data mismatches
                                // unless HandleDifference returns true to skip the entire work item
                                if (skipCurrentWorkItem)
                                {
                                    break;
                                }
                                fieldValuesMismatch = true;
                            }
                            else
                            {
                                /* Uncomment for debugging
                                m_serverDiffEngine.LogVerbose(String.Format(CultureInfo.InvariantCulture, "Match: The field '{0}' for work item {1} has value '{2}' on both sides",
                                    fieldDisplayName, side1WitDiffItem.WorkItemId, side1ValueNode.InnerText));
                                 */
                            }
                        }
                    }

                    bool attachmentsMismatch = false;
                    bool linksMismatch = false;
                    if (!skipCurrentWorkItem)
                    {
                        // Compare Attachments
                        if (side1WitDiffItem.Attachments.Count != side2WitDiffItem.Attachments.Count)
                        {
                            WitDiffPair diffPair = new WitDiffPair(WitDiffType.AttachmentCount,
                                side1MigrationSource.FriendlyName, side1WitDiffItem, side2MigrationSource.FriendlyName, side2WitDiffItem);
                            skipCurrentWorkItem = HandleDifference(diffPair);
                            attachmentsMismatch = true;
                        }
                        else
                        {
                            Dictionary<string, IMigrationFileAttachment> side2AttachmentsById = new Dictionary<string, IMigrationFileAttachment>();
                            foreach (IMigrationFileAttachment side2Attachment in side2WitDiffItem.Attachments)
                            {
                                string attachmentId = GetAttachmentId(side2Attachment);
                                if (!side2AttachmentsById.ContainsKey(attachmentId))
                                {
                                    side2AttachmentsById.Add(attachmentId, side2Attachment);
                                }
                            }
                            HashSet<string> side1AttachmentIds = new HashSet<string>();
                            foreach (IMigrationFileAttachment side1Attachment in side1WitDiffItem.Attachments)
                            {
                                string side1AttachmentId = GetAttachmentId(side1Attachment);
                                if (side1AttachmentIds.Contains(side1AttachmentId))
                                {
                                    // This is a duplicate attachment; continue to ignore the duplicate
                                    continue;
                                }
                                side1AttachmentIds.Add(side1AttachmentId);
                                IMigrationFileAttachment side2Attachment;
                                if (side2AttachmentsById.TryGetValue(side1AttachmentId, out side2Attachment))
                                {
                                    side2AttachmentsById.Remove(side1AttachmentId);
                                    WitDiffAttachment diffAttachment;
                                    if (!AttachmentsMatch(side1Attachment, side2Attachment, out diffAttachment))
                                    {
                                        WitDiffPair diffPair = new WitDiffPair(WitDiffType.AttachmentMismatch,
                                            side1MigrationSource.FriendlyName, side1WitDiffItem, side2MigrationSource.FriendlyName, side2WitDiffItem);
                                        diffPair.AddMistmatchedAttachment(diffAttachment);
                                        skipCurrentWorkItem = HandleDifference(diffPair);
                                        attachmentsMismatch = true;
                                    }
                                }
                                else
                                {
                                    WitDiffPair diffPair = new WitDiffPair(WitDiffType.AttachmentMissing,
                                        side1MigrationSource.FriendlyName, side1WitDiffItem, side2MigrationSource.FriendlyName, side2WitDiffItem);
                                    diffPair.AddMissingAttachment(side1Attachment.Name);
                                    skipCurrentWorkItem = HandleDifference(diffPair);
                                    attachmentsMismatch = true;
                                }
                                if (skipCurrentWorkItem)
                                {
                                    break;
                                }
                            }

                            if (!skipCurrentWorkItem)
                            {
                                // Any attachments still in side2AttachmentsByKey were not in side1
                                foreach (IMigrationFileAttachment side2Attachment in side2AttachmentsById.Values)
                                {
                                    WitDiffPair diffPair = new WitDiffPair(WitDiffType.AttachmentMissing,
                                        side1MigrationSource.FriendlyName, side1WitDiffItem, side2MigrationSource.FriendlyName, side2WitDiffItem);
                                    diffPair.AddMissingAttachment(side2Attachment.Name);
                                    skipCurrentWorkItem = HandleDifference(diffPair);
                                    attachmentsMismatch = true;
                                }
                            }
                        }
                    }

                    if (!skipCurrentWorkItem)
                    {
                        // Compare links
                        if (side1WitDiffItem.Links.Count != side2WitDiffItem.Links.Count)
                        {
                            WitDiffPair diffPair = new WitDiffPair(WitDiffType.LinkCount,
                                side1MigrationSource.FriendlyName, side1WitDiffItem, side2MigrationSource.FriendlyName, side2WitDiffItem);
                            skipCurrentWorkItem = HandleDifference(diffPair);
                            linksMismatch = true;
                        }
                        /* Commenting out the detailed comparison of each link for now as it does not work because the link artifact URIs
                         * need to be translated.  This requires some refactoring of translation methods current embedded in the LinkEngine
                         * that are not easily used outside the context of a full running migration/sync session.
                        else
                        {
                            Dictionary<string, ILink> side2LinksById = new Dictionary<string, ILink>();
                            foreach (ILink side2Link in side2WitDiffItem.Links)
                            {
                                string side2LinkId = GetLinkId(side2Link);
                                if (!side2LinksById.ContainsKey(side2LinkId))
                                {
                                    side2LinksById.Add(side2LinkId, side2Link);
                                }
                            }
                            foreach (ILink side1Link in side1WitDiffItem.Links)
                            {
                                ILink side2Link;
                                string side1LinkId = GetLinkId(side1Link);
                                if (side2LinksById.TryGetValue(side1LinkId, out side2Link))
                                {
                                    side2LinksById.Remove(side1LinkId);
                                    WitDiffLink diffLink;
                                    if (!LinksMatch(new Guid(side1MigrationSource.InternalUniqueId), side1Link, new Guid(side2MigrationSource.InternalUniqueId), side2Link, out diffLink))
                                    {
                                        WitDiffPair diffPair = new WitDiffPair(WitDiffType.LinkMismatch,
                                            side1MigrationSource.FriendlyName, side1WitDiffItem, side2MigrationSource.FriendlyName, side2WitDiffItem);
                                        diffPair.AddMistmatchedLink(diffLink);
                                        skipCurrentWorkItem = HandleDifference(diffPair);
                                        linksMismatch = true;
                                        break;
                                    }

                                }
                                else
                                {
                                    WitDiffPair diffPair = new WitDiffPair(WitDiffType.LinkMisssing,
                                        side1MigrationSource.FriendlyName, side1WitDiffItem, side2MigrationSource.FriendlyName, side2WitDiffItem);
                                    diffPair.AddMissingLink(side1LinkId);
                                    skipCurrentWorkItem = HandleDifference(diffPair);
                                    linksMismatch = true;
                                }
                            }

                            if (!skipCurrentWorkItem)
                            {
                                // Any links still in side2LinksById were not in side1
                                foreach (ILink side2link in side2LinksById.Values)
                                {
                                    WitDiffPair diffPair = new WitDiffPair(WitDiffType.LinkMisssing,
                                        side1MigrationSource.FriendlyName, side1WitDiffItem, side2MigrationSource.FriendlyName, side2WitDiffItem);
                                    diffPair.AddMissingLink(GetLinkId(side2link));
                                    skipCurrentWorkItem = HandleDifference(diffPair);
                                    linksMismatch = true;
                                }
                            }
                        }
                        */
                    }

                    if (skipCurrentWorkItem || fieldValuesMismatch || attachmentsMismatch || linksMismatch)
                    {
                        continue;
                    }

                    m_serverDiffEngine.LogVerbose(String.Format(CultureInfo.InvariantCulture, MigrationToolkitResources.WitDiffWorkItemsMatch,
                        side1WitDiffItem.WorkItemId, side1MigrationSource.FriendlyName, side2WorkItemId, side2MigrationSource.FriendlyName));
                }
            }
        }

        private string GetLinkId(ILink link)
        {
            return link.TargetArtifact.Uri + ";" + link.LinkType.ReferenceName;
        }

        private string GetAttachmentId(IMigrationFileAttachment fileAttachment)
        {
            return fileAttachment.Name + ";" + fileAttachment.Length.ToString(CultureInfo.InvariantCulture);
        }

        private bool AttachmentsMatch(IMigrationFileAttachment attachment1, IMigrationFileAttachment attachment2, out WitDiffAttachment diffAttachment)
        {
            if (!string.Equals(attachment1.Name, attachment2.Name, StringComparison.Ordinal))
            {
                diffAttachment = new WitDiffAttachment(attachment1.Name, "Name", attachment1.Name, attachment2.Name);
                return false;
            }
            if (!string.Equals(attachment1.Comment, attachment2.Comment, StringComparison.Ordinal))
            {
                diffAttachment = new WitDiffAttachment(attachment1.Name, "Comment", attachment1.Comment, attachment2.Comment);
                return false;
            }
            if (attachment1.Length != attachment2.Length)
            {
                diffAttachment = new WitDiffAttachment(attachment1.Name, "Length", attachment1.Length.ToString(), attachment2.Length.ToString());
                return false;
            }
            diffAttachment = null;
            return true;
        }

        private bool LinksMatch(Guid link1MigrationSourceId, ILink link1, Guid link2MigrationSourceId, ILink link2, out WitDiffLink diffLink)
        {
            if (m_serverDiffEngine.DiffServiceContainer == null)
            {
                Debug.Fail("m_serverDiffEngine.DiffServiceContainer is null");
                throw new ArgumentNullException("m_serverDiffEngine.DiffServiceContainer");
            }

            ILinkTranslationService linkTranslationService = m_serverDiffEngine.DiffServiceContainer.GetService(typeof(ILinkTranslationService)) as ILinkTranslationService;
            if (linkTranslationService == null)
            {
                throw new ArgumentNullException("linkTranslationService");
            }
            if (linkTranslationService.LinkConfigurationLookupService == null)
            {
                throw new ArgumentNullException("linkTranslationService.LinkConfigurationLookupService");
            }

            string mappedSourceLinkType = linkTranslationService.LinkConfigurationLookupService.FindMappedLinkType(
                link1MigrationSourceId, link1.LinkType.SourceArtifactType.ContentTypeReferenceName);
            if (!string.Equals(mappedSourceLinkType, link2.LinkType.SourceArtifactType.ContentTypeReferenceName, StringComparison.Ordinal))
            {
                diffLink = new WitDiffLink("SourceArtifactContentType", link1.LinkType.SourceArtifactType.FriendlyName, link2.LinkType.SourceArtifactType.FriendlyName);
                return false;
            }

            string mappedTargetLinkType = linkTranslationService.LinkConfigurationLookupService.FindMappedLinkType(
                link1MigrationSourceId, link1.LinkType.TargetArtifactType.ContentTypeReferenceName);
            if (!string.Equals(mappedTargetLinkType, link2.LinkType.TargetArtifactType.ContentTypeReferenceName, StringComparison.Ordinal))
            {
                diffLink = new WitDiffLink("TargetArtifactContentType", link1.LinkType.TargetArtifactType.FriendlyName, link2.LinkType.TargetArtifactType.FriendlyName);
                return false;
            }

            /* TODO: Need to use a Translation method something like LinkEngine.TranslateChangeGroup() does to enable this
             * comparison to work
            if (!string.Equals(link1.SourceArtifact.Uri.ToString(), link2.SourceArtifact.Uri.ToString(), StringComparison.Ordinal))
            {
                diffLink = new WitDiffLink("SourceArtifactUri", link1.SourceArtifact.Uri.ToString(), link2.SourceArtifact.Uri.ToString());
                return false;
            }
            if (!string.Equals(link1.TargetArtifact.Uri.ToString(), link2.TargetArtifact.Uri.ToString(), StringComparison.Ordinal))
            {
                diffLink = new WitDiffLink("TargetArtifactUri", link1.TargetArtifact.Uri.ToString(), link2.TargetArtifact.Uri.ToString());
                return false;
            }
             */

            if (!string.Equals(link1.Comment, link2.Comment, StringComparison.Ordinal))
            {
                diffLink = new WitDiffLink("Comment", link1.Comment, link2.Comment);
                return false;
            }

            diffLink = null;
            return true;
        }

        private string GetReferenceNameFromFieldColumn(XmlNode columnNode)
        {
            string referenceName = null;
            XmlAttribute referenceNameAttr = columnNode.Attributes[c_referenceName];
            if (referenceNameAttr != null)
            {
                referenceName = referenceNameAttr.Value;
            }
            return referenceName;
        }

        /// <summary>
        /// Handle a difference found between two work items; a difference does not necessarily cause the entire
        /// diff operation to result in a difference, because we want to compare the contents at the time of the
        /// last sync point (when one direction of the migration was completed) and not reported differences caused
        /// by changes to work items since then that have not been sync'd to the other side.
        /// </summary>
        /// <param name="diffPair">A DiffPair object describing the difference just identified</param>
        /// <returns>True if differences betweeen the two work items in the pair should be ignored</returns>
        private bool HandleDifference(WitDiffPair diffPair) 
        {
            return HandleDifference(diffPair, false);
        }

        private bool HandleDifference(WitDiffPair diffPair, bool forceDiff)
        {
            // First check if the difference is a false alarm because a work item has been modified on the server since the last sync point
            bool ignoreDiff = false;
            if (!forceDiff)
            {
                ignoreDiff = IgnoreDifference(diffPair);
            }
            if (!ignoreDiff)
            {
                m_diffResult.Add(diffPair);
            }
            return ignoreDiff;
        }

        private bool IgnoreDifference(WitDiffPair diffPair)
        {
            // TODO: This assumes the HighWaterMark value for all work items is a date time
            // Should pass HighWaterMark instead? 
            DateTime sourceHighWaterMarkTime = DateTime.MinValue;
            if (m_latestSyncPoint != null && !DateTime.TryParse(m_latestSyncPoint.SourceHighWaterMarkValue, out sourceHighWaterMarkTime))
            {
                // There is no sync point for the session or we can't parse the DateTime value, so we need to assume the difference cannot be ignored
                return false;
            }

            if (diffPair.Side1DiffItem != null && diffPair.Side1DiffItem.HasBeenModifiedSince(sourceHighWaterMarkTime))
            {
                string side2workItemId = (diffPair.Side2DiffItem == null) ? 
                    MigrationToolkitResources.WitDiffUnknownWorkItemId : diffPair.Side2DiffItem.WorkItemId;
                m_serverDiffEngine.LogInfo(String.Format(CultureInfo.InvariantCulture,
                    MigrationToolkitResources.WitDiffIngoringDiff1,
                    diffPair.Side1DiffItem.WorkItemId, diffPair.Side1Name, side2workItemId, diffPair.Side2Name));
                return true;
            }

            MigrationItemId lastMigratedTargetItemId = new MigrationItemId();
            lastMigratedTargetItemId.ItemId = m_latestSyncPoint.LastMigratedTargetItemId;
            lastMigratedTargetItemId.ItemVersion = m_latestSyncPoint.LastMigratedTargetItemVersion;
            if (diffPair.Side2DiffItem != null && diffPair.Side2DiffItem.HasBeenModifiedMoreRecentlyThan(lastMigratedTargetItemId))
            {
                string side1workItemId = (diffPair.Side1DiffItem == null) ?
                    MigrationToolkitResources.WitDiffUnknownWorkItemId : diffPair.Side1DiffItem.WorkItemId;
                m_serverDiffEngine.LogInfo(String.Format(CultureInfo.InvariantCulture,
                    MigrationToolkitResources.WitDiffIngoringDiff2,
                    side1workItemId, diffPair.Side1Name, diffPair.Side2DiffItem.WorkItemId, diffPair.Side2Name));
                return true;
            }

            return false;
        }

        private string GetSourceFilterString(FilterPair filterPair)
        {
            if (Guid.Equals(new Guid(m_serverDiffEngine.Session.LeftMigrationSourceUniqueId), new Guid(filterPair.FilterItem[0].MigrationSourceUniqueId)))
            {
                return filterPair.FilterItem[0].FilterString;
            }
            else
            {
                return filterPair.FilterItem[1].FilterString;
            }
        }

        private string GetTargetFilterString(FilterPair filterPair)
        {
            if (Guid.Equals(new Guid(m_serverDiffEngine.Session.LeftMigrationSourceUniqueId), new Guid(filterPair.FilterItem[0].MigrationSourceUniqueId)))
            {
                return filterPair.FilterItem[1].FilterString;
            }
            else
            {
                return filterPair.FilterItem[0].FilterString;
            }
        }

        #endregion
    }
}