// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ClearCase;
using System.Runtime.InteropServices;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{
    /// <summary>
    /// The BatchingContext defines a method of performing operations against a TFS server
    /// in batches instead of as single operations.  This reduces round-trips with the server
    /// for large changesets.
    /// 
    /// This class takes care of both the batching and also ensuring that the item exists in the
    /// workspace prior to pending the change.  For this reason it is not necessary to "Get" an 
    /// item before acting on it.  This class does not perform a "Get" though as item download is
    /// unnecessary since the item is coming from the source system during migration.
    /// </summary>
    internal class CCBatchingContext
    {
        const int CheckoutCacheSize = 100;
        ReadOnlyCollection<CCBatchedItem> m_items;
        Dictionary<Guid, List<CCBatchedItem>> m_currentItems = new Dictionary<Guid, List<CCBatchedItem>>();
        List<CCBatchedItem> m_addFilePropertyItems = new List<CCBatchedItem>();
        ClearCaseServer m_ccServer;
        CCChangeOptimizer m_changeOpt = new CCChangeOptimizer();
        string m_comment;
        string m_owner;
        Dictionary<string, string> m_pendedRenames = new Dictionary<string, string>();
        ChangeGroup m_changeGroup;
        DateTime m_latestCheckedInTime = DateTime.Now;
        HashSet<string> m_checkoutList = new HashSet<string>();
        SortedList<int, Stack<string>> m_checkoutCache = new SortedList<int, Stack<string>>();
        List<ILabel> m_labelCache = new List<ILabel>();
        List<ILabelItem> m_unattachedLabelItems = new List<ILabelItem>();
        string m_downloadRoot;
        ConflictManager m_conflictManager;
        CCConfiguration m_ccConfiguration;
        bool m_overrideTargetChange;
        StringBuilder m_batchingContextError = new StringBuilder();

        /// <summary>
        /// Creates a batching context associated with the provided workspace.
        /// </summary>
        /// <param name="workspace"></param>
        internal CCBatchingContext(ClearCaseServer ccServer, string comment, ChangeGroup group, string downloadRoot, 
            ConflictManager conflictManager, CCConfiguration ccConfiguration, bool overrideTargetChange )
        {
            if (ccServer == null)
            {
                throw new ArgumentNullException("ccServer");
            }
            if (comment == null)
            {
                throw new ArgumentNullException("comment");
            }
            if (conflictManager == null)
            {
                throw new ArgumentNullException("conflictManager");
            }
            if (ccConfiguration == null)
            {
                throw new ArgumentNullException("ccConfiguration");
            }
            m_ccServer = ccServer;
            m_comment = reviseComment(comment);
            m_changeGroup = group;
            m_downloadRoot = downloadRoot;
            m_conflictManager = conflictManager;
            m_ccConfiguration = ccConfiguration;
            m_overrideTargetChange = overrideTargetChange;
        }

        /// <summary>
        /// Given a comment, returned a valid comment that can be migrated to ClearCase.
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        private string reviseComment(string comment)
        {
            if (string.IsNullOrEmpty(comment))
            {
                return comment;
            }
            else
            {
                if (comment.Contains('\n') || comment.Contains('\r'))
                {
                    string revisedComment = comment.Replace('\r', ' ').Replace('\n', ' ');
                    TraceManager.TraceWarning(string.Format("Checkin comment was changed from {0} to {1}", comment, revisedComment));
                    return revisedComment;
                }
            }
            return comment;
        }

        /// <summary>
        /// Checkout an item. 
        /// If the item is already in the m_checkoutList, do nothing. 
        /// Otherwise, check out the file. 
        /// </summary>
        /// <param name="checkinPath"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        private void checkout(string checkoutPath, string comment)
        {
            if (!m_checkoutList.Contains(checkoutPath))
            {
                CCVersion version = GetAppropriateVersion(checkoutPath);

                CCCheckedOutFile checkedOutFile = version.CheckOut(
                    CCReservedState.ccReserved,
                    comment,
                    false,
                    CCVersionToCheckOut.ccVersion_SpecificVersion, true, false);
            }
        }

        /// <summary>
        /// Checkout an item. 
        /// If the item is already in the m_checkoutList, do nothing. 
        /// Otherwise, check out the file. 
        /// </summary>
        /// <param name="checkinPath"></param>
        /// <param name="versionToBeCheckout"></param>
        /// <param name="comment"></param>
        private void checkout(string checkinPath, CCVersion versionToBeCheckout, string comment)
        {
            if (!m_checkoutList.Contains(checkinPath))
            {
                CCCheckedOutFile checkedOutFile = versionToBeCheckout.CheckOut(
                    CCReservedState.ccReserved,
                    comment,
                    false,
                    CCVersionToCheckOut.ccVersion_SpecificVersion, true, false);
                TraceManager.TraceVerbose(String.Format("CC Adapter: checked out version {0} from branch {1} of '{2}'",
                    versionToBeCheckout.VersionNumber, versionToBeCheckout.Branch.Path, checkinPath));
            }
        }

        /// <summary>
        /// Checkin a delete action.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private void checkinDelete(CCBatchedItem item)
        {
            TraceManager.TraceInformation("Delete element '{0}' - start", item.Target);
            Debug.Assert(!ClearCasePath.IsVobRoot(item.Target), "Cannot delete a vob.");
            string checkinPath = m_ccServer.GetCheckinPathFromServerPath(item.Target);
            CCElement element = null;
            string parentPath = ClearCasePath.GetFolderName(item.Target);
            string parentCheckinPath = m_ccServer.GetCheckinPathFromServerPath(parentPath);
            ConflictResolutionType conflictResolutionType;
            element = getElementFromPath(checkinPath, parentCheckinPath, out conflictResolutionType);

            if (element == null)
            {
                // The delete has been checked in by a previous aborted migration. 
                // Or the user select to supress the conflicted change action, just return.
                return;
            }

            // Check out the parent folder.
            checkout(parentCheckinPath, m_comment);
            // Remove the element.
            element.RemoveName(m_comment, true);

            // Check in the parent folder.
            checkin(parentCheckinPath, false);
            TraceManager.TraceInformation("Delete element '{0}' - end", item.Target);
        }

        private bool versionCommentMatch(string ccComment, string comment)
        {
            // CC generates additional comments after the check-in comment
            if (ccComment.StartsWith(comment, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else
            {            
                return false;
            }
        }

        /// <summary>
        /// Get the element from checkinPath. 
        /// 1. If the element exists, return the element. 
        /// 2. If the element doesn't exist, but the latest parent checkin comment matches the current comment, return null.
        /// 3. If the element and the parent element doesn't exist, or the latest parent checkin comment doesn't match the current comment, 
        ///    raise a content conflict. 
        /// </summary>
        /// <param name="checkinPath"></param>
        /// <param name="parentCheckinPath"></param>
        /// <returns></returns>
        private CCElement getElementFromPath(string checkinPath, string parentCheckinPath, out ConflictResolutionType conflictResolutionType)
        {
            CCElement element = null;
            conflictResolutionType = ConflictResolutionType.Other;
            try
            {
                element = m_ccServer.ApplicationClass.get_Element(checkinPath);
                return element;
            }
            catch (COMException ce)
            {
                if (CCTextParser.ProcessComException(ce) != COMExceptionResult.PathNotFound)
                {
                    throw new MigrationException(ce.Message, ce);
                }

                if (m_overrideTargetChange)
                {
                    return null;
                }

                CCElement parentElement = null;
                try
                {
                    parentElement = m_ccServer.ApplicationClass.get_Element(parentCheckinPath);
                    if (versionCommentMatch(parentElement.get_Version(null).Comment, m_comment))
                    {
                        TraceManager.TraceInformation("Skip the action that has been checked in by previous aborted migration");
                        return null;
                    }
                }
                catch (COMException pce)
                {
                    throw new MigrationException(pce.Message, pce);
                }

                // raise content conflict
                MigrationConflict contentConflict = VCContentConflictType.CreateConflict(
                    m_changeGroup,  
                    string.Format("The check-in failed as element {0} doesn't exist on the ClearCase server", checkinPath),
                    checkinPath);
                List<MigrationAction> retActions;
                ConflictResolutionResult resolutionResult =
                    m_conflictManager.TryResolveNewConflict(m_changeGroup.SourceId, contentConflict, out retActions);
                if (resolutionResult.Resolved)
                {
                    conflictResolutionType = resolutionResult.ResolutionType;
                    return null;
                }
                else
                {
                    throw new MigrationUnresolvedConflictException(contentConflict);
                }
            }
        }

        /// <summary>
        /// checkin a rename action
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private void checkinRename(CCBatchedItem item)
        {
            TraceManager.TraceInformation("Rename element '{0}' - start", item.Target);
            Debug.Assert(!ClearCasePath.IsVobRoot(item.Target), "Cannot rename a vob.");
            string renameFromParentPath = ClearCasePath.GetFolderName(item.Source);
            string renameToParentPath = ClearCasePath.GetFolderName(item.Target);

            string renameFromCheckinPath = m_ccServer.GetCheckinPathFromServerPath(item.Source);
            string renameToCheckinPath = m_ccServer.GetCheckinPathFromServerPath(item.Target);
            string renameFromParentCheckinPath = m_ccServer.GetCheckinPathFromServerPath(renameFromParentPath);
            string renameToParentCheckinPath = m_ccServer.GetCheckinPathFromServerPath(renameToParentPath);
            ConflictResolutionType conflictResolutionType;
            CCElement element = getElementFromPath(renameFromCheckinPath, renameFromParentCheckinPath, out conflictResolutionType);

            if (element != null)
            {
                //1. Check out the rename from parent folder.
                checkout(renameFromParentCheckinPath, m_comment);

                //2. If different (move), check out the rename to parent folder.
                if (!ClearCasePath.Equals(renameFromParentPath, renameToParentPath))
                {
                    checkout(renameToParentCheckinPath, m_comment);
                }

                //3. Rename the element.
                element.Rename(m_ccServer.GetCheckinPathFromServerPath(item.Target), m_comment);

                //4. If different (move), checkin rename to parent folder.
                if (!ClearCasePath.Equals(renameFromParentPath, renameToParentPath))
                {
                    checkin(renameToParentCheckinPath, false);
                }

                //5. Check in rename from parent folder.
                checkin(renameFromParentCheckinPath, false);
            }
            else
            {
                if (conflictResolutionType == ConflictResolutionType.SuppressedConflictedChangeAction)
                {
                    // User selected to suppress conflicted change action, just return.
                    return;
                }
                if (!importBatchedItems(new CCBatchedItem[] { item }) && m_ccConfiguration.ClearfsimportConfiguration.ParseOutput)
                {
                    reportBatchingContextError(item);
                }
            }

            TraceManager.TraceInformation("Rename element '{0}' - end", item.Target);
        }       

        private void addFileProperties(CCBatchedItem item)
        {
            if (item.MigrationActionDescription != null)
            {
                string checkinPath = m_ccServer.GetCheckinPathFromServerPath(item.Target);

                CCElement element = null;
                CCVOBObject vobObject = null;
                try
                {
                    element = m_ccServer.ApplicationClass.get_Element(checkinPath);
                    vobObject = (CCVOBObject)GetAppropriateVersion(checkinPath, false, true);
                }
                catch (COMException ce)
                {
                    // Path may not be found in case of a delete; continue without adding any properties
                    if (CCTextParser.ProcessComException(ce) == COMExceptionResult.NotAccessible || 
                        CCTextParser.ProcessComException(ce) == COMExceptionResult.PathNotFound)
                    {
                        return;
                    }
                    throw;
                }

                FileMetadataProperties fileProperties = FileMetadataProperties.CreateFromXmlDocument(item.MigrationActionDescription);
                foreach(KeyValuePair<string, string> nameValuePair in fileProperties)
                {

                    /* Uncomment for more verbose logging
                    TraceManager.TraceVerbose(String.Format("CC Adapter: Adding addtribute {0}={1} to '{2}'",
                        nameValuePair.Key, nameValuePair.Value, checkinPath)); 
                     */

                    try
                    {
                        ICCAttributeType attributeType = element.VOB.get_AttributeType(nameValuePair.Key, true);
                        attributeType.Apply(vobObject, (object)nameValuePair.Value, string.Empty, true, false);
                    }
                    catch (COMException ex)
                    {
                        MigrationConflict conflict;
                        if (CCTextParser.ProcessComException(ex) == COMExceptionResult.AttributeTypeNotFound)
                        {
                            conflict = CCAttrTypeNotFoundConflictType.CreateConflict(ex.Message, nameValuePair.Key);
                        }
                        else
                        {
                            conflict = VCFilePropertyCreationConflictType.CreateConflict(ex.Message, nameValuePair.Key);
                        }

                        List<MigrationAction> retActions;
                        ConflictResolutionResult result = m_conflictManager.TryResolveNewConflict(m_changeGroup.SourceId, conflict, out retActions);

                        if ((result.Resolved) && (result.ResolutionType == ConflictResolutionType.SkipConflictedChangeAction))
                        {
                            TraceManager.TraceWarning(String.Format(CCResources.SkippingAddingAttribute,
                                    nameValuePair.Key, checkinPath, ex.Message));
                        }
                        else
                        {
                            throw new MigrationUnresolvedConflictException(conflict);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add a single batchedItem to current batch.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="actionGuid"></param>
        internal void AddSingleItem(MigrationAction action, Guid actionGuid)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            CCBatchedItem item = new CCBatchedItem(
                ClearCasePath.GetFullPath(action.FromPath),
                ClearCasePath.GetFullPath(action.Path),
                actionGuid, 
                action.SourceItem, 
                action.ItemTypeReferenceName, 
                action.ActionId,
                action.MigrationActionDescription);

            if (actionGuid == WellKnownChangeActionId.AddFileProperties)
            {
                m_addFilePropertyItems.Add(item);
            }
            else
            {
                m_changeOpt.Add(item);
            }
        }

        internal string Flush()
        {
            m_items = m_changeOpt.Resolve();

            TraceManager.TraceInformation("Finished scheduling!");

            if (m_items.Count > 0)
            {
                int currentPriority = m_items[0].Priority;

                foreach (CCBatchedItem ci in m_items)
                {
                    if (ci.Priority != currentPriority)
                    {
                        pendChanges();
                        currentPriority = ci.Priority;
                    }
                    if (!m_currentItems.ContainsKey(ci.Action))
                    {
                        m_currentItems.Add(ci.Action, new List<CCBatchedItem>());
                    }

                    m_currentItems[ci.Action].Add(ci);
                }

                pendChanges();

                m_changeOpt.Clear();

            }

            checkinCachedCheckouts();

            processAddFilePropertiesItems();

            CreateLabels();

            return m_latestCheckedInTime.ToString();
        }

        internal void CacheLabel(IMigrationAction labelAction)
        {
            m_labelCache.Add(new LabelFromMigrationAction(labelAction));
            foreach (ILabelItem unattachedLabelItem in m_unattachedLabelItems)
            {
                m_labelCache[m_labelCache.Count - 1].LabelItems.Add(unattachedLabelItem);
            }
            m_unattachedLabelItems.Clear();
        }

        internal void CacheLabelItem(IMigrationAction labelItemAction)
        {
            ILabelItem labelItemFromAction = new LabelItemFromMigrationAction(labelItemAction);
            if (m_labelCache.Count == 0)
            {
                // CacheLabel has not been called yet with a Label MigrationAction, so hold any
                // label items in m_unattachedLabelItems until one comes in (this code should
                // not assume the ordering)
                m_unattachedLabelItems.Add(labelItemFromAction);
            }
            else
            {
                // Add the label item to the Label for the most recently received label action
                m_labelCache[m_labelCache.Count - 1].LabelItems.Add(labelItemFromAction);
            }
        }

        private void CreateLabels()
        {
            foreach (ILabel label in m_labelCache)
            {
                if (label.LabelItems.Count > 0)
                {
                    ICCLabelType ccLabelType = AddLabelType(label);
                    if (ccLabelType != null)
                    {
                        foreach (ILabelItem labelItem in label.LabelItems)
                        {
                            AddLabelItem(ccLabelType, labelItem);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add a label type if it does not already exist
        /// </summary>
        /// <param name="labelChangeGroup"></param>
        /// <param name="firstLabelItemAction"></param>
        /// <returns></returns>
        private ICCLabelType AddLabelType(ILabel label)
        {
            CCVersion firstItemVersion;
            try
            {
                firstItemVersion = m_ccServer.ApplicationClass.get_Version(m_ccServer.GetCheckinPathFromServerPath(label.LabelItems[0].ItemCanonicalPath));
            }
            catch (COMException ce)
            {
                if (CCTextParser.ProcessComException(ce) == COMExceptionResult.NotAccessible ||
                    CCTextParser.ProcessComException(ce) == COMExceptionResult.PathNotFound)
                {
                    return null;
                }
                throw;
            }

            ICCLabelType labelType = null;
            try
            {
                // See if a label type with this name already exists 
                labelType = firstItemVersion.VOB.get_LabelType(label.Name, true);
                TraceManager.TraceInformation(String.Format(CultureInfo.InvariantCulture,
                    CCResources.UsingExistingLabelType, labelType.Name));
            }
            catch (COMException)
            {
                // Ignore exception that occurs if label type not found
            }

            if (labelType == null)
            {
                try
                {
                    labelType = firstItemVersion.VOB.CreateLabelType(label.Name, label.Comment, false, CCTypeConstraint.ccConstraint_PerElement, false, false);
                    TraceManager.TraceInformation(String.Format(CultureInfo.InvariantCulture,
                        CCResources.LabelTypeCreated, labelType.Name));
                }
                catch (COMException ex)
                {
                    MigrationConflict conflict;

                    /* TODO: Reinstate special conflict handling for invalid label name (need to change VCInvalidLabelNameConflictType)
                    if (CCTextParser.ProcessComException(ex) == COMExceptionResult.InvalidName)
                    {
                        conflict = VCInvalidLabelNameConflictType.CreateConflict((MigrationAction)firstLabelItemAction, ex.Message);
                    }
                    else
                    {
                    */
                        conflict = VCLabelCreationConflictType.CreateConflict(ex.Message, label.Name);
                    // }

                    List<MigrationAction> retActions;
                    ConflictResolutionResult result = m_conflictManager.TryResolveNewConflict(m_changeGroup.SourceId, conflict, out retActions);

                    if (result.Resolved)
                    {
                        if (result.ResolutionType == ConflictResolutionType.SkipConflictedChangeAction)
                        {
                            TraceManager.TraceWarning(String.Format(CCResources.SkippingAddingLabel, label.Name));
                        }
                        else if (result.ResolutionType == ConflictResolutionType.UpdatedConflictedChangeAction)
                        {
                            // When the VCInvalidLabelNameConflictHandler resolves the conflict, it change the Name of the ChangeGroup object (which holds the 
                            //label name for a label change group) to a different label name.  Try to create the label type again by calling this method
                            // recursively.
                            labelType = AddLabelType(label);
                        }
                        else
                        {
                            throw new MigrationUnresolvedConflictException(conflict);
                        }
                    }
                    else
                    {
                        throw new MigrationUnresolvedConflictException(conflict);
                    }
                }
            }

            return labelType;
        }

        private void RemoveLabelType(ICCLabelType labelType)
        {
            bool labelTypeRemoved = false;
            CCLabelTypes labelTypes = labelType.VOB.get_LabelTypes(true, false);
            for (int labelTypeIndex = labelTypes.Count; labelTypeIndex > 0; labelTypeIndex--)
            {
                if (string.Equals(labelTypes[labelTypeIndex].Name, labelType.Name, StringComparison.Ordinal))
                {
                    labelTypes.Remove(labelTypeIndex);
                    labelTypeRemoved = true;
                    break;
                }
            }
            if (!labelTypeRemoved)
            {
                TraceManager.TraceWarning(String.Format("Unable to undo creation of label type {0} with {1} labeled elements",
                    labelType.Name, labelTypes.Count));
            }
        }

        private void AddLabelItem(ICCLabelType labelType, ILabelItem labelItem)
        {
            try
            {
                CCVersion itemVersion = m_ccServer.ApplicationClass.get_Version(m_ccServer.GetCheckinPathFromServerPath(labelItem.ItemCanonicalPath));
                labelType.Apply(itemVersion, labelType.Comment, true, labelItem.Recurse);
                // Uncomment for verbose logging of labels applied
                // TraceManager.TraceInformation(String.Format("ClearCaseDetailedHistoryAdapter: Applied label {0} to item '{1}'", labelType.Name, itemVersion.Path));
            }
            catch (Exception ex)
            {
                // Don't create a label that doesn't label everything needed; so remove the label type
                RemoveLabelType(labelType);

                // TODO: Put in resource (along with other English strings in this adapter)
                throw new MigrationException(String.Format("Reverted creation of label type '{0}' because of error applying label to some elements: {1}",
                    labelType.Name, ex.Message));
            }
        }

        /// <summary>
        /// If the forceCheckin is set or cacheCheckout returns false, check in the file.
        /// </summary>
        /// <param name="checkinPath"></param>
        /// <param name="forceCheckIn"></param>
        private void checkin(string checkinPath, bool forceCheckin)
        {
            if (forceCheckin)
            {
                TraceManager.TraceVerbose(String.Format("CC Adapter: Checking in '{0}'", checkinPath));

                string checkInCmd = string.Format("checkin -nc -identical \"{0}\"", checkinPath);
                try
                {
                    string cmdOutput = m_ccServer.ExecuteClearToolCommand(checkInCmd);
                    List<string> versionList = ClearCaseCommandResultSpec.ParseCheckInCommand(cmdOutput);
                    if (versionList.Count > 0)
                    {
                        m_latestCheckedInTime = DateTime.Now;
                    }

                    // Create a label for cach checkin if LabelAllVersions custom setting is set.
                    if (m_ccConfiguration.LabelAllVersions)
                    {
                        string labelType = ClearCaseServer.GenerateCheckinLabelType(m_changeGroup.ChangeGroupId);
                        m_ccServer.ExecuteClearToolCommand(string.Format("mklbtype -nc {0}", string.Format("{0}@{1}", labelType, checkinPath)));
                        m_ccServer.ExecuteClearToolCommand(string.Format("mklabel {0} \"{1}\"", labelType, checkinPath));
                    }
                }
                catch (Exception)
                {
                    // Todo, handle check in exception.
                    throw;
                }
            }
            else
            {
                TraceManager.TraceVerbose(String.Format("CC Adapter: Caching checkin of '{0}'", checkinPath));

                cacheCheckout(checkinPath);
            }
        }

        /// <summary>
        /// Given a checked out path, try to cache it in checkout list. 
        /// If the checkout list is full, insert the current item and check in an item with the longest folder depth.
        /// </summary>
        /// <param name="checkinPath"></param>
        /// <returns>Return true if the item is cached, false otherwise.</returns>
        private bool cacheCheckout(string checkinPath)
        {
            if (m_checkoutList.Contains(checkinPath))
            {
                return true;
            }
            int depth = ClearCasePath.GetFolderDepth(checkinPath);
            // Add the item to checkout list and cache.
            if (!m_checkoutCache.ContainsKey(depth))
            {
                m_checkoutCache.Add(depth, new Stack<string>());
            }
            m_checkoutCache[depth].Push(checkinPath);
            m_checkoutList.Add(checkinPath);

            if (m_checkoutList.Count < CheckoutCacheSize)
            {
                return true;
            }
            else
            {
                // Cache overflows, removes the deepest folder.
                string deepestFolder = m_checkoutCache.Values[m_checkoutCache.Count - 1].Pop();
                if (m_checkoutCache.Values[m_checkoutCache.Count - 1].Count == 0)
                {
                    m_checkoutCache.RemoveAt(m_checkoutCache.Count - 1);
                }
                checkin(deepestFolder, true);
                m_checkoutList.Remove(deepestFolder);
                return false;
            }
        }

        /// <summary>
        /// Check in all checkouts cached locally
        /// </summary>
        /// <param name="elementList"></param>
        private void checkinCachedCheckouts()
        {
            if (m_checkoutList.Count == 0)
            {
                return;
            }
            StringBuilder checkedOutElementsBuilder = new StringBuilder();
            string firstItemPath = m_checkoutList.First();

            foreach (string checkoutElement in m_checkoutList)
            {
                checkedOutElementsBuilder.Append(string.Format(" \"{0}\"", checkoutElement));
            }

            m_checkoutList.Clear();
            m_checkoutCache.Clear();

            string checkedOutElements = checkedOutElementsBuilder.ToString();

            TraceManager.TraceInformation("Checkin all cached items - start");
            TraceManager.TraceVerbose("CCAdapter: Checking in multiple elements: \r\n" + checkedOutElements);

            string batchCheckInCmd = "checkin -nc -identical" + checkedOutElements;
            try
            {
                string cmdOutput = m_ccServer.ExecuteClearToolCommand(batchCheckInCmd);
                List<string> versionList = ClearCaseCommandResultSpec.ParseCheckInCommand(cmdOutput);
                if (versionList.Count > 0)
                {
                    m_latestCheckedInTime = DateTime.Now;
                }
            }
            catch (Exception)
            {
                throw;
            }
            TraceManager.TraceInformation("Checkin all cached items - end");

            // Create a label for cach checkin if LabelAllVersions custom setting is set.
            if (m_ccConfiguration.LabelAllVersions)
            {
                string labelType = ClearCaseServer.GenerateCheckinLabelType(m_changeGroup.ChangeGroupId);
                m_ccServer.ExecuteClearToolCommand(string.Format("mklbtype -nc {0}",
                    string.Format("{0}@{1}", labelType, firstItemPath)));
                m_ccServer.ExecuteClearToolCommand(string.Format("mklabel {0} {1}", labelType, checkedOutElements));
            }
        }

        internal void CancelCachedCheckouts()
        {
            if (m_checkoutList.Count == 0)
            {
                return;
            }

            // Build the list of files to be passed to uncheckout with the deepest paths first
            // in the list so that no parent is uncheckedout before a child which would cause an error
            StringBuilder checkedOutElementsBuilder = new StringBuilder();
            for (int cacheIndex = m_checkoutCache.Count - 1; cacheIndex >= 0;  cacheIndex--)
            {
                while (m_checkoutCache.Values[cacheIndex].Count > 0)
                {
                    string deepestPath = m_checkoutCache.Values[cacheIndex].Pop();
                    checkedOutElementsBuilder.Append(string.Format(" \"{0}\"", deepestPath));
                }
            }

            m_checkoutList.Clear();
            m_checkoutCache.Clear();

            string checkedOutElements = checkedOutElementsBuilder.ToString();
            TraceManager.TraceInformation("Uncheckout all cached items - start");
            TraceManager.TraceVerbose("CCAdapter: Uncheckout of multiple elements: \r\n" + checkedOutElements);

            string batchUncheckoutCmd = "uncheckout -rm" + checkedOutElements.ToString();
            try
            {
                string cmdOutput = m_ccServer.ExecuteClearToolCommand(batchUncheckoutCmd);
            }
            catch (Exception ex)
            {
                TraceManager.TraceError(String.Format(CCResources.ClearToolCommandException, "uncheckout", ex.Message));
                TraceManager.TraceWarning(CCResources.FilesLeftCheckedOutInView);
            }
            TraceManager.TraceInformation("Uncheckout all cached items - end");
        }

        private void pendChanges()
        {
            pendAdds();
            pendRenames();
            pendDeletes();
            pendEdits();
            m_currentItems.Clear();
            if (m_batchingContextError.Length > 0 && m_ccConfiguration.ClearfsimportConfiguration.ParseOutput)
            {
                raiseCheckinConflict(m_batchingContextError.ToString());
            }
        }

        private void raiseCheckinConflict(string message)
        {
            MigrationConflict conflict;
            conflict = CCCheckinConflictType.CreateConflict(message, m_changeGroup.ChangeGroupId.ToString());

            List<MigrationAction> retActions;
            ConflictResolutionResult result = m_conflictManager.TryResolveNewConflict(m_changeGroup.SourceId, conflict, out retActions);

            if ((!result.Resolved) || (result.ResolutionType != ConflictResolutionType.SkipConflictedChangeAction))
            {
                throw new MigrationUnresolvedConflictException(conflict);
            }
        }

        private void pendAdds()
        {
            List<CCBatchedItem> addItems = getCurrent(WellKnownChangeActionId.Add);

            if (addItems.Count == 0)
            {
                return;
            }

            // process parent item add first
            addItems.Sort(compareBatchedItemByTargetPathLength);
            TraceManager.TraceInformation("Add {0} elements to ClearCase", addItems.Count);
            foreach (CCBatchedItem[] batchedAdds in chunkCollection(addItems))
            {
                importBatchedItems(batchedAdds);
            }

            if (addItems.Count > 0)
            {
                m_latestCheckedInTime = DateTime.Now;
            }
        }

        internal List<T[]> chunkCollection<T>(List<T> paths)
        {
            List<T[]> batches = new List<T[]>();
            int length = paths.Count;

            int currentIndex = 0;
            int remaining = length - currentIndex;

            while (remaining > 0)
            {
                int currentBatchSize = (remaining < m_ccConfiguration.ClearfsimportConfiguration.BatchSize) ? remaining : m_ccConfiguration.ClearfsimportConfiguration.BatchSize;
                batches.Add(
                    paths.GetRange(
                        currentIndex,
                        currentBatchSize
                    ).ToArray()
                );

                currentIndex += currentBatchSize;
                remaining = length - currentIndex;
            }

            return batches;
        }

        private bool verifyPath(string path)
        {
            try
            {
                Path.GetFullPath(path);
            }
            catch (Exception e)
            {
                MigrationConflict invalidPathConflict = VCInvalidPathConflictType.CreateConflict(
                    null, 
                    string.Format("{0} \n Invalid Path - {1}", e.Message, path), path);

                List<MigrationAction> returnActions;
                ConflictResolutionResult resolutionResult = m_conflictManager.TryResolveNewConflict(
                    m_changeGroup.SourceId, invalidPathConflict, out returnActions);
                if (resolutionResult.Resolved)
                {
                    switch (resolutionResult.ResolutionType)
                    {
                        case ConflictResolutionType.SkipConflictedChangeAction:
                            return false;
                        default:
                            Debug.Fail("Unknown resolution result");
                            throw new MigrationUnresolvedConflictException(invalidPathConflict);
                    }
                }
                else
                {
                    throw new MigrationUnresolvedConflictException(invalidPathConflict);
                }
            }
            return true;
        }


        /// <summary>
        /// This method uses clearfsimport to import files and directories downloaded from TFS to ClearCase
        /// </summary>
        /// <param name="itemList"></param>
        private bool importBatchedItems(CCBatchedItem[] itemList)
        {
            if ((itemList == null) || (itemList.Length == 0))
            {
                return true;
            }
            string downloadPath;
            Dictionary<string, string> rootFoldersToBeAdded = new Dictionary<string, string>();
            List<CCBatchedItem> itemsToBeImported = new List<CCBatchedItem>();
            TraceManager.TraceInformation("Import {0} elements from source system to ClearCase", itemList.Length);

            // Calculate Vobs to be queried
            foreach (CCBatchedItem batchedItem in itemList)
            {
                if (!rootFoldersToBeAdded.ContainsKey(ClearCasePath.GetVobName(batchedItem.Target)))
                {
                    rootFoldersToBeAdded.Add(ClearCasePath.GetVobName(batchedItem.Target),
                        Path.Combine(m_downloadRoot, ClearCasePath.MakeRelative(ClearCasePath.GetVobName(batchedItem.Target))));
                }
            }

            // Find symbolic links
            HashSet<string> symbolicLinks = new HashSet<string>();
            foreach (string vob in rootFoldersToBeAdded.Keys)
            {
                m_ccServer.FindSymbolicLink(vob, symbolicLinks);
            }

            // Download the file
            TraceManager.TraceVerbose("Start download");
            foreach (CCBatchedItem batchedItem in itemList)
            {
                if (!Utils.IsElementAncestorInList(symbolicLinks, batchedItem.Target))
                {
                    downloadPath = Path.Combine(m_downloadRoot, ClearCasePath.MakeRelative(batchedItem.Target));
                    batchedItem.DownloadItem.Download(downloadPath);
                    if (!verifyPath(downloadPath))
                    {
                        // The downloadPath is invalid and was resolved as skip.
                        continue;
                    }
                    itemsToBeImported.Add(batchedItem);
                }
                else
                {
                    // Todo, raise a conflict and allow user to ignore the symbolic link.
                    TraceManager.TraceWarning(
                        string.Format("Element {0} is a symbolic link. Its content won't be imported to ClearCase server",
                        batchedItem.Target));
                }
            }
            TraceManager.TraceVerbose("End download");

            string fsImportCommand;
            Dictionary<string, ClearfsimportResult> clearfsimportOutput = null;

            if (itemsToBeImported.Count > 0)
            {
                TraceManager.TraceVerbose("Start import");

                foreach (KeyValuePair<string, string> vobFolder in rootFoldersToBeAdded)
                {
                    fsImportCommand = buildClearfsimportCommand(m_comment, vobFolder.Value, ClearCasePath.MakeRelative(vobFolder.Key));
                    clearfsimportOutput = ClearCaseServer.ExecuteClearfsimportCommand(fsImportCommand);
                    Utils.DeleteFiles(vobFolder.Value);
                }
                TraceManager.TraceVerbose("End import");

            }
            else
            {
                return true;
            }

            // Verify clearfsimport results
            bool allElementsImported = true;
            if ((clearfsimportOutput == null) || (clearfsimportOutput.Count == 0))
            {
                allElementsImported = false;
                foreach (CCBatchedItem batchedItem in itemsToBeImported)
                {
                    reportBatchingContextError(batchedItem);
                }
            }
            foreach (CCBatchedItem batchedItem in itemsToBeImported)
            {
                if (!clearfsimportOutput.ContainsKey(ClearCasePath.MakeRelative(batchedItem.Target)))
                {
                    reportBatchingContextError(batchedItem);
                    allElementsImported = false;
                }
            }
            if (!m_ccServer.UseDynamicView)
            {
                m_ccServer.Update(clearfsimportOutput.Keys.ToList());
            }
            return allElementsImported;
        }

        private void reportBatchingContextError(CCBatchedItem batchedItem)
        {
            Debug.Assert(batchedItem != null, "Pass in value for reportClearfsimportError() cannot be null");
            if (batchedItem.Action == WellKnownChangeActionId.Edit)
            {
                m_batchingContextError.AppendLine(string.Format("Failed to edit element {0} using clearfsimport command.", batchedItem.Target));
            }
            else if (batchedItem.Action == WellKnownChangeActionId.Add)
            {
                m_batchingContextError.AppendLine(string.Format("Failed to add element {0} using clearfsimport command.", batchedItem.Target));
            }
            else if (batchedItem.Action == WellKnownChangeActionId.Rename)
            {
                m_batchingContextError.AppendLine(string.Format("Failed to rename element {0} to {1}.", batchedItem.Source, batchedItem.Target));
            }
        }

        private string buildClearfsimportCommand(string comment, string sourceName, string targetVobDirectory)
        {
            StringBuilder command = new StringBuilder(string.Format("clearfsimport -comment \"{0}\" -recurse -nsetevent", comment));
            if (m_ccConfiguration.ClearfsimportConfiguration.Unco)
            {
                command = command.Append(" -unco");
            }
            if (m_ccConfiguration.ClearfsimportConfiguration.Master)
            {
                command = command.Append(" -master");
            }
            if (m_ccConfiguration.LabelAllVersions)
            {
                // Remove the -upd parameter for now. 
                // Todo: -upd is a V7 only feature. We need to generate 2 adapters and use different parameter settings for different adapters.
                command = command.Append(string.Format(" -mklabel {0}", ClearCaseServer.GenerateCheckinLabelType(m_changeGroup.ChangeGroupId)));
            }

            command = command.Append(string.Format(" \"{0}\\*\" \"{1}\"", sourceName, targetVobDirectory));
            return command.ToString();
        }
    

        private void pendDeletes()
        {
            List<CCBatchedItem> deleteItems = getCurrent(WellKnownChangeActionId.Delete);

            foreach (CCBatchedItem deleteItem in deleteItems)
            {
               reviseDeleteTargetName(deleteItem);
               checkinDelete(deleteItem);
            }

            checkinCachedCheckouts();
        }

        /// <summary>
        /// This is a TFS2010 behavior change. The delete will take the rename-from-name if it is a combination of merge|rename|delete. 
        /// We need to revert the delete target back in this situation.
        /// </summary>
        /// <param name="batchedItems"></param>
        private void reviseDeleteTargetName(CCBatchedItem deleteItem)
        {
            string currentPath = deleteItem.Target;
            while (!(ClearCasePath.IsVobRoot(currentPath) || ClearCasePath.Equals(currentPath, ClearCasePath.Separator)))
            {
                if (m_pendedRenames.ContainsKey(currentPath))
                {
                    deleteItem.Target = ClearCasePath.Combine(m_pendedRenames[currentPath],
                        ClearCasePath.MakeRelative(deleteItem.Target, currentPath));
                    return;
                }
                currentPath = ClearCasePath.GetFolderName(currentPath);
            }
            return;
        }

        private void pendRenames()
        {
            List<CCBatchedItem> renameItems = getCurrent(WellKnownChangeActionId.Rename);
            
            renameItems.Sort(compareBatchedItemByTargetPathLength);

            foreach (CCBatchedItem renameItem in renameItems)
            {
                reviseRenameFromPath(renameItem);
                checkinRename(renameItem);
                m_pendedRenames.Add(renameItem.Source, renameItem.Target);
            }

            checkinCachedCheckouts();
        }

        private void reviseRenameFromPath(CCBatchedItem renameItem)
        {
            string currentPath = renameItem.Source;
            while (!(ClearCasePath.IsVobRoot(currentPath) ||ClearCasePath.Equals(currentPath, ClearCasePath.Separator)))
            {
                if (m_pendedRenames.ContainsKey(currentPath))
                {
                    renameItem.Source = ClearCasePath.Combine(m_pendedRenames[currentPath],
                        ClearCasePath.MakeRelative(renameItem.Source, currentPath));
                    return;
                }
                currentPath = ClearCasePath.GetFolderName(currentPath);
            }
            return;
        }

        private void pendEdits()
        {
            List<CCBatchedItem> editItems = getCurrent(WellKnownChangeActionId.Edit);

            if (editItems.Count == 0)
            {
                return;
            }

            TraceManager.TraceInformation("Edit {0} elements to ClearCase", editItems.Count);
            foreach (CCBatchedItem[] batchedEdits in chunkCollection(editItems))
            {
                importBatchedItems(batchedEdits);
            }

            if (editItems.Count > 0)
            {
                m_latestCheckedInTime = DateTime.Now;
            }
        }

        private void processAddFilePropertiesItems()
        {
            foreach (CCBatchedItem addFilePropertyItem in m_addFilePropertyItems)
            {
                addFileProperties(addFilePropertyItem);
            }
        }

        private List<CCBatchedItem> getCurrent(Guid changeAction)
        {
            if (m_currentItems.ContainsKey(changeAction))
            {
                return m_currentItems[changeAction];
            }

            List<CCBatchedItem> newList = new List<CCBatchedItem>(0);
            m_currentItems.Add(changeAction, newList);

            return newList;
        }

        private CCVersion GetAppropriateVersion(string elementPath)
        {
            return GetAppropriateVersion(elementPath, false, true);
        }

        private CCVersion GetAppropriateVersion(string elementPath, bool retryOnMain, bool useViewVersion)
        {
            CCVersion version;
            if (m_ccServer.UsePrecreatedView && useViewVersion)
            {
                // Use the version defined by the view
                version = m_ccServer.ApplicationClass.get_Version(elementPath);
            }
            else
            {
                CCElement element = m_ccServer.ApplicationClass.get_Element(elementPath);
                string branchVersionString = ClearCasePath.Combine(m_ccServer.BranchVersionString, "LATEST");
                try
                {
                    version = element.get_Version(branchVersionString);
                }
                catch (COMException ex)
                {
                    if (retryOnMain)
                    {
                        string branchVersionString2 = ClearCasePath.Combine(CCResources.DefaultBranchName, "LATEST");
                        TraceManager.TraceWarning(String.Format("CCAdapter: Exception getting version {0} of {1}: {2}; retrying using version: {3}",
                            branchVersionString, element.Path, ex.Message, branchVersionString2));
                        version = element.get_Version(branchVersionString2);
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }
            return version;
        }

        /// <summary>
        /// compare target path length of two batcheditem. If x's target path length is smaller, x is less than y.
        /// </summary>
        /// <param name="x">batched item 1</param>
        /// <param name="y">batched item 2</param>
        /// <returns>positive value if x's target length is larger. negative value if y's target length is larger.
        /// 0 if length is equal or either one is null.</returns>
        private static int compareBatchedItemByTargetPathLength(CCBatchedItem x, CCBatchedItem y)
        {
            if ((x == null) || (y == null))
            {
                return 0;
            }

            return (x.Target.Length - y.Target.Length);
        }
        
    }
}
