// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Linking
{
    public class Tfs2010LinkProvider : TfsLinkingProviderBase 
    {
        public override void RegisterSupportedLinkOperations()
        {
            base.RegisterSupportedLinkOperations();
            SupportedChangeActions.Add(WellKnownChangeActionId.Edit);
        }

        public override void RegisterSupportedLinkTypes()
        {
            LinkType linkType = new WorkItemChangeListLinkType();
            SupportedLinkTypes.Add(linkType.ReferenceName, linkType);
            ExtractLinkChangeActionsCallback += ((ILinkHandler)linkType).ExtractLinkChangeActions;

            linkType = new WorkItemHyperlinkLinkType();
            SupportedLinkTypes.Add(linkType.ReferenceName, linkType);
            ExtractLinkChangeActionsCallback += ((ILinkHandler)linkType).ExtractLinkChangeActions;

            linkType = new WorkItemLatestFileLinkType();
            SupportedLinkTypes.Add(linkType.ReferenceName, linkType);
            ExtractLinkChangeActionsCallback += ((ILinkHandler)linkType).ExtractLinkChangeActions;

            linkType = new WorkItemRevisionFileLinkType();
            SupportedLinkTypes.Add(linkType.ReferenceName, linkType);
            ExtractLinkChangeActionsCallback += ((ILinkHandler)linkType).ExtractLinkChangeActions;

            linkType = new Tfs2010WorkItemRelatedLinkType();
            SupportedLinkTypes.Add(linkType.ReferenceName, linkType);
            ExtractLinkChangeActionsCallback += ((ILinkHandler)linkType).ExtractLinkChangeActions;
         
            foreach (RegisteredLinkType registeredLinkType in m_migrationSource.WorkItemStore.WorkItemStore.RegisteredLinkTypes)
            {
                Debug.Assert(!string.IsNullOrEmpty(registeredLinkType.Name));
                if (!TfsDefaultLinkType(registeredLinkType))
                {
                    linkType = new WorkItemExternalLinkType(registeredLinkType.Name);
                    SupportedLinkTypes.Add(linkType.ReferenceName, linkType);
                    ExtractLinkChangeActionsCallback += ((ILinkHandler)linkType).ExtractLinkChangeActions;
                }
            }

            // skipping WorkItemRelatedLinkType, using TFS2010 WorkItemLinkType(s)
            foreach (WorkItemLinkType wiLinkType in m_migrationSource.WorkItemStore.WorkItemStore.WorkItemLinkTypes)
            {
                Debug.Assert(!string.IsNullOrEmpty(wiLinkType.ReferenceName));

                ExtendedLinkProperties extendedLinkProperties = GetExtendedLinkProperties(wiLinkType);

                #region TFS 2010 specific logic
                if (!wiLinkType.ReferenceName.Equals(Tfs2010WorkItemRelatedLinkType.Tfs2010RelatedLinkTypeReferenceName, StringComparison.OrdinalIgnoreCase))
                {
                    // for backward compatibility, v1 'Related' Work Item Link Type has a special reference name 
                #endregion

                    linkType = new WorkItemLinkTypeBase(wiLinkType.ReferenceName,
                                                        wiLinkType.ReferenceName,
                                                        extendedLinkProperties,
                                                        m_migrationSource.WorkItemStore.WorkItemStore);

                    SupportedLinkTypes.Add(linkType.ReferenceName, linkType);
                    ExtractLinkChangeActionsCallback += ((ILinkHandler)linkType).ExtractLinkChangeActions;
                }
            }
        }

        protected override TfsMigrationDataSource InitializeMigrationDataSource()
        {
            return new Tfs2010MigrationDataSource();
        }

        public override NonCyclicReferenceClosure CreateNonCyclicLinkReferenceClosure(
            LinkType linkType, 
            IArtifact artifact)
        {
            if (null == linkType)
            {
                throw new ArgumentNullException("linkType");
            }
            if (null == artifact)
            {
                throw new ArgumentNullException("artifact");
            }
            Debug.Assert(linkType.ExtendedProperties.NonCircular);

            var closure = new NonCyclicReferenceClosure(linkType);

            var workItemLinkType = linkType as WorkItemLinkTypeBase;
            if (null == workItemLinkType)
            {
                return closure;
            }
            
            string id;
            var idExtractionRslt = TryExtractArtifactId(artifact, out id);
            Debug.Assert(idExtractionRslt);

            var workItemId = int.Parse(id);
            var workItem = m_migrationSource.WorkItemStore.WorkItemStore.GetWorkItem(workItemId);
            if (null == workItem)
            {
                return closure;
            }

            workItemLinkType.ExtractDirectedLinksClosure(workItem, closure);
            return closure;
        }

        public override bool TryGetSingleParentLinkSourceArtifacts(
            LinkType linkType, 
            IArtifact artifact, 
            out IArtifact[] parentArtifacts)
        {
            if (null == linkType)
            {
                throw new ArgumentNullException("linkType");
            }
            if (null == artifact)
            {
                throw new ArgumentNullException("artifact");
            }

            parentArtifacts = null;

            if (linkType.ExtendedProperties.HasOnlyOneParent
                || !linkType.ExtendedProperties.Directed)
            {
                return false;
            }

            var workItemLinkType = linkType as WorkItemLinkTypeBase;
            if (null == workItemLinkType)
            {
                return false;
            }

            string id;
            var idExtractionRslt = TryExtractArtifactId(artifact, out id);
            Debug.Assert(idExtractionRslt);

            var workItemId = int.Parse(id);
            var workItem = m_migrationSource.WorkItemStore.WorkItemStore.GetWorkItem(workItemId);
            if (null == workItem)
            {
                return false;
            }

            List<IArtifact> parents = workItemLinkType.GetDirectedLinkParents(workItem);
            parentArtifacts = parents.ToArray();
            return true;
        }

        protected ExtendedLinkProperties GetExtendedLinkProperties(WorkItemLinkType workItemLinkType)
        {
            int topologyVal = 0;
            
            if (workItemLinkType.IsDirectional)
            {
                topologyVal |= (int) ExtendedLinkProperties.ToplogyRuleAndDirectionality.Directed;
            }

            if (workItemLinkType.IsNonCircular)
            {
                topologyVal |= (int) ExtendedLinkProperties.ToplogyRuleAndDirectionality.NonCircular;
            }

            if (workItemLinkType.IsOneToMany)
            {
                topologyVal |= (int) ExtendedLinkProperties.ToplogyRuleAndDirectionality.OnlyOneParent;
            }

            return new ExtendedLinkProperties((ExtendedLinkProperties.Topology)topologyVal);
        }

        public override System.Collections.ObjectModel.ReadOnlyCollection<LinkChangeGroup> GenerateNextLinkDeltaSlice(
            LinkService linkService, 
            int maxDeltaSliceSize)
        {
            try
            {
                var linkChangeGroups = new List<LinkChangeGroup>();

                if (null == ExtractLinkChangeActionsCallback)
                {
                    return linkChangeGroups.AsReadOnly();
                }

                // load main Highwater Mark
                m_hwmLink.Reload();
                DateTime hwmLinkValue = m_hwmLink.Value;
                // search back 60 seconds to deal with potential WIT race condition
                if (!hwmLinkValue.Equals(default(DateTime)))
                {
                    hwmLinkValue = hwmLinkValue.AddSeconds(-60);
                }
                string hwmLinkValueStr = hwmLinkValue.ToString(CultureInfo.InvariantCulture);

                // load Work Items for extracting links
                string sourceId = m_migrationSource.UniqueId;
                string storeName = m_migrationSource.WorkItemStore.StoreName;

                // Get items based on primary Highwater Mark
                TraceManager.TraceInformation(TfsWITAdapterResources.GettingModifiedItems, sourceId, storeName);
                IEnumerable<TfsMigrationWorkItem> items = m_migrationSource.WorkItemStore.GetItems(ref hwmLinkValueStr);
                TraceManager.TraceInformation(TfsWITAdapterResources.ReceivedModifiedItems, sourceId, storeName);

                // Record the updated HWM value
                DateTime wiqlExecutionTime = Convert.ToDateTime(hwmLinkValueStr, CultureInfo.InvariantCulture);

                // store to be used to analyze deleted links
                WorkItemLinkStore store = new WorkItemLinkStore(new Guid(sourceId));

                // extract links
                DateTime lastWorkITemUpdateTime = DateTime.MinValue;
                var inMaxDeltaSliceSize = maxDeltaSliceSize;
                foreach (TfsMigrationWorkItem tfsMigrationWorkItem in items)
                {
                    if (tfsMigrationWorkItem.WorkItem == null)
                    {
                        continue;
                    }

                    TraceManager.TraceInformation("Generating linking delta for Work Item: {0}", tfsMigrationWorkItem.WorkItem.Id.ToString());
                    var detectedLinkChangeGroups = new List<LinkChangeGroup>();
                    ExtractLinkChangeActionsCallback(tfsMigrationWorkItem, detectedLinkChangeGroups, store);

                    if (detectedLinkChangeGroups.Count == 0)
                    {
                        TraceManager.TraceInformation("Number of links: {0}", 0);
                        continue;
                    }

                    Dictionary<string, LinkChangeGroup> perWorkItemConsolidatedLinkChangeGroup = new Dictionary<string, LinkChangeGroup>();
                    for (int i = 0; i < detectedLinkChangeGroups.Count; ++i)
                    {
                        foreach (LinkChangeAction action in detectedLinkChangeGroups[i].Actions)
                        {
                            if (!perWorkItemConsolidatedLinkChangeGroup.ContainsKey(action.Link.SourceArtifact.Uri))
                            {
                                var linkChangeGroup = new LinkChangeGroup(
                                    action.Link.SourceArtifactId, LinkChangeGroup.LinkChangeGroupStatus.Created, false);
                                perWorkItemConsolidatedLinkChangeGroup.Add(action.Link.SourceArtifact.Uri, linkChangeGroup);
                            }
                            perWorkItemConsolidatedLinkChangeGroup[action.Link.SourceArtifact.Uri].AddChangeAction(action);
                        }
                    }

                    // always make sure that the currently analyzed work item has a link change group to represent it
                    // even though the group can be empty
                    if (!perWorkItemConsolidatedLinkChangeGroup.ContainsKey(tfsMigrationWorkItem.Uri))
                    {
                        perWorkItemConsolidatedLinkChangeGroup.Add(
                            tfsMigrationWorkItem.Uri,
                            new LinkChangeGroup(TfsWorkItemHandler.IdFromUri(tfsMigrationWorkItem.Uri), LinkChangeGroup.LinkChangeGroupStatus.Created, false));
                    }


                    foreach (var workItemLinkGroup in perWorkItemConsolidatedLinkChangeGroup)
                    {
                        string workItemIdStr = TfsWorkItemHandler.IdFromUri(workItemLinkGroup.Key);
                        TraceManager.TraceInformation("Detected {0} links for Work Item '{1}'",
                            workItemLinkGroup.Value.Actions.Count, workItemIdStr);

                        if (workItemLinkGroup.Key.Equals(tfsMigrationWorkItem.Uri, StringComparison.OrdinalIgnoreCase))
                        {
                            // VERY IMPORTANT: use the RelatedArtifactsStore to detect link deletion
                            store.UpdatePerItemLinkChangeGroupsByCheckingRelatedItemRecords(
                                workItemLinkGroup.Key, workItemLinkGroup.Value, this);
                        }
                        else
                        {
                            store.UpdatePerItemLinkChangeGroupsByCheckingRelatedItemRecordsWithoutImplicitDelete(
                                workItemLinkGroup.Key, workItemLinkGroup.Value, this);
                        }

                        if (workItemLinkGroup.Value.Actions.Count > 0)
                        {
                            linkChangeGroups.Add(workItemLinkGroup.Value);
                        }
                        maxDeltaSliceSize -= workItemLinkGroup.Value.Actions.Count;

                        if (maxDeltaSliceSize <= 0)
                        {
                            // size limit reached - persist groups to DB
                            linkService.AddChangeGroups(linkChangeGroups);
                            linkChangeGroups.Clear();
                            maxDeltaSliceSize = inMaxDeltaSliceSize;
                        }
                    }

                    DateTime lastRevChangedDate = tfsMigrationWorkItem.WorkItem.ChangedDate;

                    if (lastWorkITemUpdateTime.CompareTo(lastRevChangedDate) <= 0)
                    {
                        lastWorkITemUpdateTime = lastRevChangedDate;
                    }
                }

                // persist remaining groups to DB
                linkService.AddChangeGroups(linkChangeGroups);

                // clean up the returned link change group collection
                // when the caller (toolkit) receives an empty collection, it understands there is no more
                // delta to generate for the moment, and proceeds to next phase
                linkChangeGroups.Clear();

                // update primary Highwater Mark
                //m_hwmLink.Update(newHwmLinkValue);

                string newHwmValueStr = hwmLinkValueStr;
                if (lastWorkITemUpdateTime.Equals(DateTime.MinValue))
                {
                    // no changes in this sync cycle, record the wiql query execution time
                    m_hwmLink.Update(wiqlExecutionTime);
                }
                else
                {
                    // hwm is recorded in UTC, so does the WIQL query asof time
                    lastWorkITemUpdateTime = lastWorkITemUpdateTime.ToUniversalTime();

                    if (lastWorkITemUpdateTime.CompareTo(wiqlExecutionTime) <= 0)
                    {
                        // last work item rev time is earlier than wiql query execution time, use it as hwm
                        m_hwmLink.Update(lastWorkITemUpdateTime);
                        newHwmValueStr = lastWorkITemUpdateTime.ToString();
                    }
                    else
                    {
                        m_hwmLink.Update(wiqlExecutionTime);
                    }
                }
                TraceManager.TraceInformation("Persisted WIT linking HWM: {0}", Toolkit.Constants.HwmDeltaLink);
                TraceManager.TraceInformation(TfsWITAdapterResources.UpdatedHighWatermark, newHwmValueStr);

                return linkChangeGroups.AsReadOnly();
            }
            catch (Exception exception)
            {
                MigrationConflict genericeConflict = WitGeneralConflictType.CreateConflict(exception);
                var conflictManager = m_conflictManager.GetService(typeof(ConflictManager)) as ConflictManager;
                Debug.Assert(null != conflictManager);
                List<MigrationAction> resolutionActions;
                ConflictResolutionResult resolveRslt =
                    conflictManager.TryResolveNewConflict(conflictManager.SourceId, genericeConflict, out resolutionActions);
                Debug.Assert(!resolveRslt.Resolved);
                return new List<LinkChangeGroup>().AsReadOnly();
            }
        }

        public override void RegisterConflictTypes(ConflictManager conflictManager, Guid sourceId)
        {
            base.RegisterConflictTypes(conflictManager, sourceId);
            conflictManager.RegisterConflictType(sourceId, new TFSCyclicLinkConflictType(), SyncOrchestrator.ConflictsSyncOrchOptions.Continue);
            conflictManager.RegisterConflictType(sourceId, new TFSMulitpleParentLinkConflictType(), SyncOrchestrator.ConflictsSyncOrchOptions.Continue);
            conflictManager.RegisterConflictType(sourceId, new TFSModifyLockedWorkItemLinkConflictType(), SyncOrchestrator.ConflictsSyncOrchOptions.Continue);
            conflictManager.RegisterConflictType(sourceId, new TFSLinkAccessViolationConflictType(), SyncOrchestrator.ConflictsSyncOrchOptions.Continue);
        }
    }
}