// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ServerDiff
{
    internal enum WitDiffType
    {
        None,
        NotMirrored,
        DefinitionMismatch,
        DataMismatch,
        AttachmentCount,
        AttachmentMismatch,
        AttachmentMissing,
        LinkCount,
        LinkMismatch,
        LinkMisssing,
        InvalidDefinition
    }

    internal class WitDiffField
    {
        public string FieldName { get; set; }
        public string SourceValue { get; set; }
        public string TargetValue { get; set; }

        public WitDiffField(string fieldName, string sourceValue, string targetValue)
        {
            FieldName = fieldName;
            SourceValue = sourceValue;
            TargetValue = targetValue;
        }
    }

    internal class WitDiffAttachment : WitDiffField
    {
        public string SourceAttachmentName { get; set; }

        public WitDiffAttachment(string sourceAttachmentName, string fieldName, string sourceValue, string targetValue) : base(fieldName, sourceValue, targetValue)
        {
            SourceAttachmentName = sourceAttachmentName;
        }
    }

    // Currently doesn't add anything to WitDiffField, but using separately class for clarity
    // and in case something needs to be added
    internal class WitDiffLink : WitDiffField
    {
        public WitDiffLink(string fieldName, string sourceValue, string targetValue)
            : base(fieldName, sourceValue, targetValue)
        {
        }
    }

    internal class WitDiffMissingField
    {
        public string FieldName { get; set; }
        public string SourceName { get; set; }

        public WitDiffMissingField(string fieldName, string sourceName)
        {
            FieldName = fieldName;
            SourceName = sourceName;
        }
    }

    internal class WitDiffPair
    {
        private List<WitDiffField> m_mismatchedFields = new List<WitDiffField>();
        private List<WitDiffMissingField> m_missingFields = new List<WitDiffMissingField>();
        private List<WitDiffAttachment> m_mismatchedAttachments = new List<WitDiffAttachment>();
        private List<string> m_missingAttachments = new List<string>();
        private List<WitDiffLink> m_mismatchedLinks = new List<WitDiffLink>();  

        public WitDiffPair(WitDiffType diffType, string side1Name, IWITDiffItem side1DiffItem, string side2Name, IWITDiffItem side2DiffItem)
        {
            DiffType = diffType;
            Side1Name = side1Name;
            Side1DiffItem = side1DiffItem;
            Side2Name = side2Name;
            Side2DiffItem = side2DiffItem;
        }

        public WitDiffPair(WitDiffType diffType, string side1Name, IWITDiffItem side1DiffItem)
        {
            DiffType = diffType;
            Side1Name = side1Name;
            Side1DiffItem = side1DiffItem;
        }

        public WitDiffType DiffType { get; set; }
        public string Side1Name { get; set; }
        public IWITDiffItem Side1DiffItem { get; set; }
        public string Side2Name { get; set; }
        public IWITDiffItem Side2DiffItem { get; set; }

        public List<WitDiffField> DiffFields
        {
            get
            {
                return m_mismatchedFields;
            }
        }

        public List<WitDiffMissingField> MissingFields
        {
            get
            {
                return m_missingFields;
            }
        }

        public List<WitDiffAttachment> DiffAttachments
        {
            get
            {
                return m_mismatchedAttachments;
            }
        }

        public List<string> MissingAttachments
        {
            get
            {
                return m_missingAttachments;
            }
        }

        public List<WitDiffLink> DiffLinks
        {
            get
            {
                return m_mismatchedLinks;
            }
        }

        public void AddMismatchField(WitDiffField diffField)
        {
            m_mismatchedFields.Add(diffField);
        }

        public void AddMissingField(WitDiffMissingField missingField)
        {
            m_missingFields.Add(missingField);
        }

        public void AddMissingAttachment(string missingAttachment)
        {
            m_missingAttachments.Add(missingAttachment);
        }

        public void AddMistmatchedAttachment(WitDiffAttachment diffAttachment)
        {
            m_mismatchedAttachments.Add(diffAttachment);
        }

        public void AddMistmatchedLink(WitDiffLink diffLink)
        {
            m_mismatchedLinks.Add(diffLink);
        }
    }

    internal class WitDiffResult
    {
        private ServerDiffEngine m_serverDiffEngine;
        private bool m_keepLists;
        private bool m_logDiffs;
        private List<WitDiffPair> m_witDiffPairs;
        private List<string> m_processingErrors;

        public int WorkItemCount { get; set; }
        public int MissingWorkItemCount { get; set; }
        public int ContentMismatchCount { get; set; }
        public int AttachmentMismatchCount { get; set; }
        public int LinkMismatchCount { get; set; }
        public int ProcessingErrorCount { get; set; }

        public WitDiffResult(ServerDiffEngine diffEngine) : this(diffEngine, true)
        {
        }

        public WitDiffResult(ServerDiffEngine diffEngine, bool keepLists) : this(diffEngine, keepLists, true)
        {
        }

        public WitDiffResult(ServerDiffEngine diffEngine, bool keepLists, bool logDiffs)
        {
            m_serverDiffEngine = diffEngine;
            m_keepLists = keepLists;
            m_logDiffs = logDiffs;
            if (keepLists)
            {
                m_witDiffPairs = new List<WitDiffPair>();
                m_processingErrors = new List<string>();
            }
        }

        public List<WitDiffPair> WitDiffPairs
        {
            get
            {
                return m_witDiffPairs;
            }
        }

        private List<string> ProcessingErrors
        {
            get
            {
                return m_processingErrors;
            }
        }

        public bool AllContentsMatch
        {
            get
            {
                return MissingWorkItemCount == 0 &&
                       ContentMismatchCount == 0 &&
                       AttachmentMismatchCount == 0 &&
                       LinkMismatchCount == 0 &&
                       ProcessingErrorCount == 0;
            }
        }

        public void LogDifference(WitDiffPair diffPair)
        {

            string side1WorkItemId = diffPair.Side1DiffItem == null ? string.Empty : diffPair.Side1DiffItem.WorkItemId;
            string side2WorkItemId = diffPair.Side2DiffItem == null ? string.Empty : diffPair.Side2DiffItem.WorkItemId;
            m_serverDiffEngine.LogError(String.Format(CultureInfo.InvariantCulture, MigrationToolkitResources.WitDiffResultWorkItemMismatch,
                side1WorkItemId, diffPair.Side1Name, side2WorkItemId, diffPair.Side2Name,
                diffPair.DiffType.ToString()));

            if (diffPair.MissingFields.Count > 0)
            {
                m_serverDiffEngine.LogError(MigrationToolkitResources.WitDiffResultWorkItemFieldMissingHeader);
                foreach (WitDiffMissingField missingField in diffPair.MissingFields)
                {
                    m_serverDiffEngine.LogError(String.Format(CultureInfo.InvariantCulture, MigrationToolkitResources.WitDiffResultWorkItemFieldMissing,
                        missingField.FieldName, missingField.SourceName));
                }
            }

            if (diffPair.DiffFields.Count > 0)
            {
                m_serverDiffEngine.LogError(MigrationToolkitResources.WitDiffResultWorkItemFieldDiffHeader);
                foreach(WitDiffField diffField in diffPair.DiffFields)
                {
                    m_serverDiffEngine.LogError(String.Format(CultureInfo.InvariantCulture, MigrationToolkitResources.WitDiffResultWorkItemFieldDiffDetail,
                        diffField.FieldName, diffField.SourceValue, diffField.TargetValue));
                }
            }

            if (diffPair.MissingAttachments.Count > 0)
            {
                m_serverDiffEngine.LogError(MigrationToolkitResources.WitDiffResultWorkItemAttachmentMissingHeader);
                foreach (string missingAttachment in diffPair.MissingAttachments)
                {
                    m_serverDiffEngine.LogError(String.Format(CultureInfo.InvariantCulture, MigrationToolkitResources.WitDiffResultWorkItemAttachmentMissingDetail,
                        missingAttachment));
                }
            }

            if (diffPair.DiffAttachments.Count > 0)
            {
                m_serverDiffEngine.LogError(MigrationToolkitResources.WitDiffResultWorkItemAttachmentDiffHeader);
                foreach (WitDiffAttachment diffAttachment in diffPair.DiffAttachments)
                {
                    m_serverDiffEngine.LogError(String.Format(CultureInfo.InvariantCulture, MigrationToolkitResources.WitDiffResultWorkItemAttachmentDiffDetail,
                        diffAttachment.SourceAttachmentName, diffAttachment.FieldName, diffAttachment.SourceValue, diffAttachment.TargetValue));
                }
            }

            if (diffPair.DiffLinks.Count > 0)
            {
                m_serverDiffEngine.LogError(MigrationToolkitResources.WitDiffResultWorkItemLinkDiffHeader);
                foreach (WitDiffLink diffLink in diffPair.DiffLinks)
                {
                    m_serverDiffEngine.LogError(String.Format(CultureInfo.InvariantCulture, MigrationToolkitResources.WitDiffResultWorkItemLinkDiffDetail,
                        diffLink.FieldName, diffLink.SourceValue, diffLink.TargetValue));
                }
            }

            if (diffPair.DiffType == WitDiffType.LinkCount)
            {
                m_serverDiffEngine.LogError(String.Format(CultureInfo.InvariantCulture, MigrationToolkitResources.WitDiffResultWorkItemLinkCountDiffDetail,
                    diffPair.Side1DiffItem.LinkCount, diffPair.Side2DiffItem.LinkCount));
                foreach (string linkUri in diffPair.Side1DiffItem.LinkUris)
                {
                    m_serverDiffEngine.LogError(String.Format(CultureInfo.InvariantCulture,
                        "Work Item {0} has link to {1}",
                         diffPair.Side1DiffItem.WorkItemId, linkUri));
                }
                foreach (string linkUri in diffPair.Side2DiffItem.LinkUris)
                {
                    m_serverDiffEngine.LogError(String.Format(CultureInfo.InvariantCulture,
                        "Work Item {0} has link to {1}",
                         diffPair.Side2DiffItem.WorkItemId, linkUri));
                }
            }
        }

        public void AddProcessingError(string error)
        {
            ProcessingErrorCount++; 
            if (m_keepLists)
            {
                m_processingErrors.Add(error);
            }

            m_serverDiffEngine.LogError(MigrationToolkitResources.WitDiffResultWorkItemErrorsHeader + error);
        }

        public void Add(WitDiffPair pair)
        {
            if (m_keepLists)
            {
                m_witDiffPairs.Add(pair);
            }
            switch (pair.DiffType)
            {
                case WitDiffType.AttachmentCount:
                case WitDiffType.AttachmentMismatch:
                case WitDiffType.AttachmentMissing:
                    AttachmentMismatchCount++;
                    break;

                case WitDiffType.DataMismatch:
                case WitDiffType.DefinitionMismatch:
                case WitDiffType.InvalidDefinition:
                    ContentMismatchCount++;
                    break;

                case WitDiffType.LinkCount:
                case WitDiffType.LinkMismatch:
                case WitDiffType.LinkMisssing:
                    LinkMismatchCount++;
                    break;

                case WitDiffType.NotMirrored:
                    MissingWorkItemCount++;
                    break;

                default:
                    Debug.Fail("Unexpected WitDiffType: " + pair.DiffType.ToString());
                    return;
            }

            if (m_logDiffs)
            {
                LogDifference(pair);
            }
        }
    }
}
