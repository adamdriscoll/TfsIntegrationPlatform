// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Toolkit.WIT
{

    class WITBasicConflictAnalysisService : IConflictAnalysisService
    {
        Dictionary<Guid, List<string>> m_conflictedWorkItems = new Dictionary<Guid, List<string>>();

        #region IConflictAnalysisService Members

        public ChangeGroupService TargetChangeGroupService
        {
            get;
            set;
        }

        public ConflictManager ConflictManager
        {
            get;
            set;
        }

        public ITranslationService TranslationService
        {
            get;
            set;
        }

        public Guid TargetSystemId
        {
            get;
            set;
        }

        public Guid SourceSystemId
        {
            get;
            set;
        }

        public Session Configuration
        {
            get;
            set;
        }

        public void Analyze()
        {
            m_conflictedWorkItems.Clear();
            m_conflictedWorkItems[SourceSystemId] = new List<string>();
            m_conflictedWorkItems[TargetSystemId] = new List<string>();

            if (null == Configuration || null == TargetChangeGroupService || Guid.Empty == TargetSystemId)
            {
                return;
            }

            if (!TargetDeltaContainsMappedItem())
            {
                return;
            }

            try
            {
                FlattenDeltaTable(m_mappedWorkItemsInSourceDeltaTable, m_mappedWorkItemAttachmentUpdatesInSource, true);
                FlattenDeltaTable(m_mappedWorkItemsInTargetDeltaTable, m_mappedWorkItemAttachmentUpdatesInTarget, false);

                // work item basic edit/edit conflict detection
                using (SymDiff<string> mappedWorkItemDiff = new SymDiff<string>(
                        m_mappedWorkItemsInSourceDeltaTable.ToArray(),
                        m_mappedWorkItemsInTargetDeltaTable.ToArray(),
                        StringComparer.InvariantCultureIgnoreCase))
                {
                    if (mappedWorkItemDiff.LeftOnly.Count != m_mappedWorkItemsInSourceDeltaTable.Count
                        || mappedWorkItemDiff.RightOnly.Count != m_mappedWorkItemsInTargetDeltaTable.Count)
                    {

                        TraceManager.TraceInformation("Edit/Edit conflicted work items are detected from source system ({0}): ",
                                                      Configuration.MigrationSources[SourceSystemId].ServerUrl);

                        IEnumerable<string> conflictedWorkItems = m_mappedWorkItemsInSourceDeltaTable.Except<string>(mappedWorkItemDiff.LeftOnly);
                        foreach (string targetWiId in conflictedWorkItems)
                        {
                            TryResolvePerWorkItemConflicts(targetWiId);
                        }
                    }
                }

                // try cleaning up the collections used for WIT revision analysis ASAP
                m_mappedWorkItemsInSourceDeltaTable.Clear();
                m_mappedWorkItemsInTargetDeltaTable.Clear();
                m_perMappedItemEdits.Clear();
                m_perMappedItemEditsTargetDelta.Clear();

                // attachment diff and take default policy (TakeTheirs)
                foreach (var attUpdateSource in m_mappedWorkItemAttachmentUpdatesInSource)
                {
                    if (m_mappedWorkItemAttachmentUpdatesInTarget.ContainsKey(attUpdateSource.Key))
                    {
                        IMigrationFileAttachment[] sourceSideAttachments = GetAttachmentList(attUpdateSource.Value);
                        List<MigrationAction> targetSideAttUpdateActions = m_mappedWorkItemAttachmentUpdatesInTarget[attUpdateSource.Key];
                        IMigrationFileAttachment[] targetSideAttachments = GetAttachmentList(targetSideAttUpdateActions);

                        SymDiff<IMigrationFileAttachment> attSymDiff = new SymDiff<IMigrationFileAttachment>(
                            sourceSideAttachments,
                            targetSideAttachments,
                            m_migrAttachComparer);

                        IEnumerable<IMigrationFileAttachment> sourceOnlyAttachments = sourceSideAttachments.Intersect(attSymDiff.LeftOnly);

                        foreach (MigrationAction action in attUpdateSource.Value)
                        {
                            if (!sourceOnlyAttachments.Contains(action.SourceItem as IMigrationFileAttachment))
                            {
                                action.State = ActionState.Skipped;
                                action.ChangeGroup.Save(action);
                            }
                        }
                    }
                }
            }
            finally
            {
                // in case we throw in stage 1 (edit/edit conflict) analysis, we want to make sure to
                // clean up the collections used for WIT revision analysis
                m_mappedWorkItemsInSourceDeltaTable.Clear();
                m_mappedWorkItemsInTargetDeltaTable.Clear();
                m_perMappedItemEdits.Clear();
                m_perMappedItemEditsTargetDelta.Clear();

                // clean up the collections used for attachment analysis
                m_mappedWorkItemAttachmentUpdatesInSource.Clear();
                m_mappedWorkItemAttachmentUpdatesInTarget.Clear();
                m_sourceToTargetWorkItemIdMapping.Clear();
                m_targetToSourceWorkItemIdMapping.Clear();
            }
        }

        #endregion


        private void TryResolvePerWorkItemConflicts(
            string targetWiId)
        {
            string sourceSideItemId = m_targetToSourceWorkItemIdMapping[targetWiId];
            TraceManager.TraceInformation("Edit/Edit conflicted work item: {0}", sourceSideItemId);

            int editeditConflictId = int.MinValue;
            bool previousConflictIsResolved = true;
            foreach (var migrInstrActionFromSource in m_perMappedItemEdits[sourceSideItemId])
            {
                if (!previousConflictIsResolved)
                {
                    // previous revision of the work item has conflict, push *this revision to backlog as well
                    // *: this revision (migration instruction) will be obsoleted and the corresponding delta entry reactived
                    ChangeGroup migrationInstructionEntry = migrInstrActionFromSource.ChangeGroup;
                    ChangeGroup entryToBlock = null;
                    if (migrationInstructionEntry.ReflectedChangeGroupId.HasValue)
                    {
                        entryToBlock = TargetChangeGroupService.ReactivateDeltaEntry(migrationInstructionEntry);
                    }

                    if (entryToBlock == null)
                    {
                        entryToBlock = migrationInstructionEntry;
                    }

                    if (entryToBlock.Actions.Count <= 0)
                    {
                        continue;
                    }

                    // an edit/edit conflict must have been backlogged already
                    Debug.Assert(editeditConflictId != int.MinValue, "Edit/edit conflict Id is not available");

                    string conflictDetails = ChainOnBackloggedItemConflictType.CreateConflictDetails(sourceSideItemId, migrInstrActionFromSource.Version);
                    string scopeHint = ChainOnBackloggedItemConflictType.CreateScopeHint(sourceSideItemId);
                    MigrationConflict chainedConflict = new ChainOnBackloggedItemConflictType().CreateConflict(conflictDetails,scopeHint, entryToBlock.Actions.First());
                    ConflictManager.BacklogUnresolvedConflict(ConflictManager.SourceId, chainedConflict, false);
                }
                else
                {
                    ChangeGroup sourceGroupForResolution = null;
                    if (migrInstrActionFromSource.ChangeGroup.ReflectedChangeGroupId.HasValue)
                    {
                        sourceGroupForResolution = TargetChangeGroupService.ReactivateDeltaEntry(migrInstrActionFromSource.ChangeGroup);
                    }

                    if (sourceGroupForResolution == null)
                    {
                        sourceGroupForResolution = migrInstrActionFromSource.ChangeGroup;
                    }
                    ConflictResolutionResult resolutionResult = TryResolveSingleRevConflict(sourceSideItemId, 
                                                                                            sourceGroupForResolution.Actions.First(),
                                                                                            targetWiId, 
                                                                                            m_perMappedItemEditsTargetDelta[targetWiId]);
                    previousConflictIsResolved = resolutionResult.Resolved;

                    if (previousConflictIsResolved)
                    {
                        // conflict is auto-resolved, 
                        // obsolete the reactivated delta entry and reactivate the migration instruction
                        if (sourceGroupForResolution.Status != ChangeStatus.Skipped
                            && sourceGroupForResolution != migrInstrActionFromSource.ChangeGroup)
                        {
                            TargetChangeGroupService.ReactivateMigrationInstruction(migrInstrActionFromSource.ChangeGroup,
                                                                                    sourceGroupForResolution);
                        }

                        continue;
                    }
                    else
                    {
                        // save the persisted unresolved conflict id, so that the following changes can "chain-on" it
                        editeditConflictId = resolutionResult.ConflictInternalId;
                    }
                }
            }

            if (!previousConflictIsResolved)
            {
                bool chainOnBackloggedItemConflictIsCreated = false;
                foreach (var targetAction in m_perMappedItemEditsTargetDelta[targetWiId])
                {
                    Debug.Assert(editeditConflictId != int.MinValue, "Edit/edit conflict Id is not available");
                    MigrationConflict chainedConflict = new ChainOnConflictConflictType().CreateConflict(
                        ChainOnConflictConflictType.CreateConflictDetails(editeditConflictId),
                        ChainOnConflictConflictType.CreateScopeHint(editeditConflictId),
                        targetAction);
                    // action is from the target system, use the TargetSystem migration source Id
                    ConflictManager.BacklogUnresolvedConflict(TargetSystemId, chainedConflict, false);

                    if (!chainOnBackloggedItemConflictIsCreated)
                    {
                        string conflictDetails = ChainOnBackloggedItemConflictType.CreateConflictDetails(targetWiId, targetAction.Version);
                        string scopeHint = ChainOnBackloggedItemConflictType.CreateScopeHint(sourceSideItemId);
                        MigrationConflict conflict = new ChainOnBackloggedItemConflictType().CreateConflict(conflictDetails, scopeHint, targetAction);
                        ConflictManager.BacklogUnresolvedConflict(TargetSystemId, conflict, false);

                        chainOnBackloggedItemConflictIsCreated = true;
                    }
                }
            }
        }

        private ConflictResolutionResult TryResolveSingleRevConflict(
            string sourceSideItemId,
            IMigrationAction sourceAction,
            string targetWiId,
            List<MigrationAction> targetActions)
        {
            foreach (var targetAction in targetActions)
            {
                ConflictResolutionResult resolutionResult = TryResolveConflict(sourceSideItemId, sourceAction, targetWiId, targetAction);
                if (!resolutionResult.Resolved)
                {
                    return resolutionResult;
                }
            }

            return new ConflictResolutionResult(true, ConflictResolutionType.Other);
        }

        private ConflictResolutionResult TryResolveConflict(
            string sourceSideItemId,
            IMigrationAction sourceSideAction,
            string targetWiId,
            IMigrationAction targetSideAction)
        {
            string conflictDetails = WITEditEditConflictType.CreateConflictDetails(sourceSideItemId, 
                                                                                   sourceSideAction, 
                                                                                   targetWiId, 
                                                                                   targetSideAction);
            string scopeHint = WITEditEditConflictType.CreateScopeHint(sourceSideItemId, 
                                                                       sourceSideAction.Version,
                                                                       targetWiId,
                                                                       targetSideAction.Version);
            MigrationConflict editEditConflict = new WITEditEditConflictType().CreateConflict(conflictDetails, scopeHint, sourceSideAction);

            List<MigrationAction> resolutionActions;
            return ConflictManager.TryResolveNewConflict(ConflictManager.SourceId, editEditConflict, out resolutionActions);
        }

        private IMigrationFileAttachment[] GetAttachmentList(List<MigrationAction> actions)
        {
            var attachments = new List<IMigrationFileAttachment>(actions.Count);
            foreach (MigrationAction action in actions)
            {
                attachments.Add(action.SourceItem as IMigrationFileAttachment);
            }
            return attachments.ToArray();
        }

        private bool TargetDeltaContainsMappedItem()
        {
            int pageNumber = 0;
            int pageSize = 50;
            IEnumerable<ChangeGroup> changeGroups = TargetChangeGroupService.NextDeltaTablePage(pageNumber++, pageSize, true);

            if (changeGroups.Count() == 0)
            {
                return false;
            }

            do
            {
                foreach (ChangeGroup changeGroupEntry in changeGroups)
                {
                    foreach (MigrationAction action in changeGroupEntry.Actions)
                    {
                        if (action.Action == WellKnownChangeActionId.Edit 
                            || action.Action == WellKnownChangeActionId.AddAttachment
                            || action.Action == WellKnownChangeActionId.DelAttachment)
                        {
                            string targetWorkItemId = GetSourceWorkItemIdFromActionDescription(action.MigrationActionDescription);
                            string sourceWorkItemId = TranslationService.TryGetTargetItemId(targetWorkItemId, TargetSystemId);
                            if (!string.IsNullOrEmpty(sourceWorkItemId))
                            {
                                return true;
                            }                        
                        }
                    }
                }

                changeGroups = TargetChangeGroupService.NextDeltaTablePage(pageNumber++, pageSize, true);
            }
            while (changeGroups.Count() > 0);

            return false;
        }

        private void FlattenDeltaTable(
            List<string> outMappedWorkItemUpdates,
            Dictionary<string, List<MigrationAction>> outMappedAttachmentUpdates,
            bool getSourceSideCopyInMigrationInstructionTable)
        {
            Guid sourceId = getSourceSideCopyInMigrationInstructionTable ?
                SourceSystemId : TargetSystemId;
            Guid targetId = getSourceSideCopyInMigrationInstructionTable ?
                TargetSystemId : SourceSystemId;

            int pageNumber = 0;
            int pageSize = 50;
            IEnumerable<ChangeGroup> changeGroups = null;
            do
            {
                changeGroups = getSourceSideCopyInMigrationInstructionTable ? 
                    TargetChangeGroupService.NextMigrationInstructionTablePage(pageNumber++, pageSize, true, true)
                    : TargetChangeGroupService.NextDeltaTablePage(pageNumber++, pageSize, true);
                foreach (ChangeGroup changeGroupEntry in changeGroups)
                {
                    foreach (MigrationAction action in changeGroupEntry.Actions)
                    {
                        if (action.Action.Equals(WellKnownChangeActionId.SyncContext))
                        {
                            continue;
                        }

                        else if (action.Action == WellKnownChangeActionId.Add)
                        {
                            // do nothing for adding item, be it created by sync engine or not
                            continue;
                        }

                        else if (action.Action == WellKnownChangeActionId.Edit)
                        {
                            string sourceSideItemId = GetSourceWorkItemIdFromActionDescription(action.MigrationActionDescription);
                            string targetWorkItemId = TryGetTargetWorkItemId(
                                action.MigrationActionDescription,
                                sourceId,
                                getSourceSideCopyInMigrationInstructionTable);
                            if (string.IsNullOrEmpty(targetWorkItemId))
                            {
                                // the source work item has never been migrated yet
                                continue;
                            }

                            if (getSourceSideCopyInMigrationInstructionTable)
                            {
                                if (ConflictManager.IsItemInBacklog(sourceId, targetId, sourceSideItemId))
                                {
                                    // previous revision of the work item has conflict, push this revision to backlog as well
                                    string conflictDetails = ChainOnBackloggedItemConflictType.CreateConflictDetails(sourceSideItemId, action.Version);
                                    string scopeHint = ChainOnBackloggedItemConflictType.CreateScopeHint(sourceSideItemId);

                                    if (changeGroupEntry.ReflectedChangeGroupId.HasValue)
                                    {
                                        // we reactivate the original delta entry and discard this migration instruction entry
                                        // and then backlog the delta entry, to preserve correct order of the revisions
                                        ChangeGroup deltaChangeGroupEntry = TargetChangeGroupService.ReactivateDeltaEntry(changeGroupEntry);

                                        if (null != deltaChangeGroupEntry)
                                        {

                                            MigrationConflict chainedConflict = new ChainOnBackloggedItemConflictType().CreateConflict(
                                                                                    conflictDetails,
                                                                                    scopeHint,
                                                                                    deltaChangeGroupEntry.Actions.First());

                                            ConflictManager.BacklogUnresolvedConflict(ConflictManager.SourceId, chainedConflict, false);
                                            break;
                                        }
                                    }

                                    // if we can't find the original delta entry successfully, we block this migration instruction entry
                                    MigrationConflict conflict = new ChainOnBackloggedItemConflictType().CreateConflict(
                                                                            conflictDetails,
                                                                            scopeHint,
                                                                            action);

                                    ConflictManager.BacklogUnresolvedConflict(ConflictManager.SourceId, conflict, false);

                                    if (!m_conflictedWorkItems[targetId].Contains(targetWorkItemId))
                                    {
                                        // record the target-side work item to be in conflict
                                        m_conflictedWorkItems[targetId].Add(targetWorkItemId);
                                    }
                                    break;
                                }

                                if (!m_perMappedItemEdits.ContainsKey(m_targetToSourceWorkItemIdMapping[targetWorkItemId]))
                                {
                                    m_perMappedItemEdits.Add(m_targetToSourceWorkItemIdMapping[targetWorkItemId], new List<MigrationAction>());
                                }
                                m_perMappedItemEdits[m_targetToSourceWorkItemIdMapping[targetWorkItemId]].Add(action);
                            }
                            else
                            {
                                // check if the revision is a sync copy from the other side
                                if (TranslationService.IsSyncGeneratedAction(action, sourceId))
                                {
                                    continue;
                                }

                                if (m_conflictedWorkItems[sourceId].Contains(sourceSideItemId))
                                {
                                    string conflictDetails = ChainOnBackloggedItemConflictType.CreateConflictDetails(sourceSideItemId, action.Version);
                                    string scopeHint = ChainOnBackloggedItemConflictType.CreateScopeHint(sourceSideItemId);
                                    MigrationConflict conflict = new ChainOnBackloggedItemConflictType().CreateConflict(
                                                                        conflictDetails,
                                                                        scopeHint,
                                                                        action);

                                    ConflictManager.BacklogUnresolvedConflict(TargetSystemId, conflict, false);
                                    break;
                                }

                                if (!m_perMappedItemEditsTargetDelta.ContainsKey(targetWorkItemId))
                                {
                                    m_perMappedItemEditsTargetDelta.Add(targetWorkItemId, new List<MigrationAction>());
                                }
                                m_perMappedItemEditsTargetDelta[targetWorkItemId].Add(action);
                            }

                            // item is mapped to an item on target system
                            if (!outMappedWorkItemUpdates.Contains(targetWorkItemId))
                            {
                                outMappedWorkItemUpdates.Add(targetWorkItemId);
                            }
                        }

                        else if (action.Action == WellKnownChangeActionId.AddAttachment)
                        {
                            string targetWorkItemId = TryGetTargetWorkItemId(
                                action.MigrationActionDescription,
                                sourceId,
                                getSourceSideCopyInMigrationInstructionTable);

                            if (string.IsNullOrEmpty(targetWorkItemId))
                            {
                                continue;
                            }

                            if (!outMappedAttachmentUpdates.ContainsKey(targetWorkItemId))
                            {
                                outMappedAttachmentUpdates.Add(targetWorkItemId, new List<MigrationAction>());
                            }

                            List<MigrationAction> attachmentList = outMappedAttachmentUpdates[targetWorkItemId];

                            IMigrationFileAttachment sourceItem = action.SourceItem as IMigrationFileAttachment;
                            if (null == sourceItem)
                            {
                                throw new MigrationException(MigrationToolkitResources.InvalidSourceItemForAttachmentOperation);
                            }
                            if (!attachmentList.Contains(action))
                            {
                                attachmentList.Add(action);
                            }
                        }

                        else if (action.Action == WellKnownChangeActionId.DelAttachment)
                        {
                            // do nothing for now
                            continue;
                        }
                    }
                }
            }
            while (changeGroups.Count() > 0);
        }

        private string GetSourceWorkItemIdFromActionDescription(XmlDocument xmlDocument)
        {
            string sourceWorkItemId = xmlDocument.DocumentElement.Attributes["WorkItemID"].Value;
            Debug.Assert(!string.IsNullOrEmpty(sourceWorkItemId),
                "Source Work Item ID is missing in the change action description document.");
            return sourceWorkItemId;
        }

        private string TryGetTargetWorkItemId(
            XmlDocument xmlDocument, 
            Guid sourceId,
            bool getSourceSideCopyInMigrationInstructionTable)
        {
            string sourceWorkItemId = GetSourceWorkItemIdFromActionDescription(xmlDocument);

            if (!getSourceSideCopyInMigrationInstructionTable)
            {
                return sourceWorkItemId;
            }

            string targetWorkItemId = TranslationService.TryGetTargetItemId(sourceWorkItemId, sourceId);
            if (!string.IsNullOrEmpty(targetWorkItemId))
            {
                if (!m_sourceToTargetWorkItemIdMapping.ContainsKey(sourceWorkItemId))
                {
                    m_sourceToTargetWorkItemIdMapping.Add(sourceWorkItemId, targetWorkItemId);
                }
                if (!m_targetToSourceWorkItemIdMapping.ContainsKey(targetWorkItemId))
                {
                    m_targetToSourceWorkItemIdMapping.Add(targetWorkItemId, sourceWorkItemId);
                }
            }
            return targetWorkItemId;
        }

        private static bool DecideSidenessInConfig(Guid sourceId, Session sessionConfig)
        {
            bool isLeft = false;
            if (new Guid(sessionConfig.LeftMigrationSourceUniqueId) == sourceId)
            {
                isLeft = true;
            }
            else
            {
                Debug.Assert(new Guid(sessionConfig.RightMigrationSourceUniqueId) == sourceId);
            }
            return isLeft;
        }
        

        List<string> m_mappedWorkItemsInSourceDeltaTable = new List<string>();
        Dictionary<string, List<MigrationAction>> m_mappedWorkItemAttachmentUpdatesInSource = new Dictionary<string, List<MigrationAction>>();
        List<string> m_mappedWorkItemsInTargetDeltaTable = new List<string>();
        Dictionary<string, List<MigrationAction>> m_mappedWorkItemAttachmentUpdatesInTarget = new Dictionary<string, List<MigrationAction>>();

        DefaultMigrationFileAttachmentComparer m_migrAttachComparer = new DefaultMigrationFileAttachmentComparer();
        
        Dictionary<string, string> m_targetToSourceWorkItemIdMapping = new Dictionary<string, string>();
        Dictionary<string, string> m_sourceToTargetWorkItemIdMapping = new Dictionary<string, string>();
        Dictionary<string, List<MigrationAction>> m_perMappedItemEdits = new Dictionary<string, List<MigrationAction>>(); // key-ed on source item id
        Dictionary<string, List<MigrationAction>> m_perMappedItemEditsTargetDelta = new Dictionary<string, List<MigrationAction>>();
    }
}
