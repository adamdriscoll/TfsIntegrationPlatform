// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Migration.SubversionAdapter.Interop.Subversion.ObjectModel;
using Microsoft.TeamFoundation.Migration.SubversionAdapter.SubversionOM;
using Microsoft.TeamFoundation.Migration.Toolkit;
using System.Diagnostics;
using System.Globalization;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.SubversionAdapter
{
    #region Delegates

    /// <summary>
    /// The message/warning/error callback
    /// </summary>
    /// <param name="message">The string to log</param>
    internal delegate void ProcessorCallback(string message);

    /// <summary>
    /// The basic TFS analysis algorithm delegate type
    /// </summary>
    /// <param name="change">The change being processed</param>
    /// <param name="group">The change group this change is to be a part of</param>
    internal delegate void SubversionAnalysisAlgorithm(Change change, ChangeGroup group);

    #endregion

    internal class SubversionAnalysisAlgorithms
    {
        #region Private Members

        private SubversionVCAnalysisProvider m_provider;
        private Dictionary<ChangeAction, SubversionAnalysisAlgorithm> m_subversionChangeTranslators = new Dictionary<ChangeAction, SubversionAnalysisAlgorithm>();

        private Dictionary<string, Change> m_deleteLookupTable;
        private Dictionary<string, string> m_renameLookupTable;
        private Repository m_currentRepository = null;

        internal ChangeSet CurrentChangeset
        {
            get;
            set;
        }

        private Repository CurrentRepository
        {
            get
            {
                if (m_currentRepository == null)
                {
                    m_currentRepository = Repository.GetRepository(new Uri(CurrentChangeset.Repository));
                }
                return m_currentRepository;
            }
        }

        #endregion

        #region Constructor

        public SubversionAnalysisAlgorithms(SubversionVCAnalysisProvider provider)
        {
            m_provider = provider;
            BuildChangeTranslators();
        }

        #endregion

        #region Methods

        /// <summary>
        /// The main entry for all changes that are not mapped
        /// </summary>
        /// <param name="change">The change that needs to be analyzed</param>
        /// <param name="changeGroup">The changegroup that stores the analysis result</param>
        internal void ExecuteNonMapped(Change change, ChangeGroup changeGroup, int[] mappedChanges)
        {
            //Check wether the action is a folder and a recusrive action. Recursive actions are branch and delete
            //We have to check wether the mapped change is a child of the recursive action because in that case we would be affected of this change too
            if ((change.ChangeAction == ChangeAction.Copy || change.ChangeAction == ChangeAction.Delete || change.ChangeAction == ChangeAction.Add) &&
                 change.ItemType == WellKnownContentType.VersionControlledFolder)
            {
                //now we have to check wether the current mapping is a child of the action. 
                //In this case we have to execute the action because it recursively affects us
                foreach(var mapping in m_provider.ConfigurationManager.MappedServerPaths)
                {
                    if(PathUtils.IsChildItem(change.FullServerPath, mapping))
                    {
                        if (change.ChangeAction == ChangeAction.Copy)
                        {
                            analyzeNotMappedBranchOperations(mapping, change, changeGroup, mappedChanges);
                        }
                        else if (change.ChangeAction == ChangeAction.Delete)
                        {
                            analyzeNotMappedDeleteOperations(mapping, change, changeGroup);
                        }
                        else if (change.ChangeAction == ChangeAction.Add)
                        {
                            analyzeNotMappedAddOperations(mapping, change, changeGroup);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The main entry point. This method accepts the change and the changegroup and initiates the internal processing and analysis of the change
        /// </summary>
        /// <param name="change">The change that needs to be analyzed</param>
        /// <param name="changeGroup">The changegroup that stores the analysis result</param>
        internal void Execute(Change change, ChangeGroup changeGroup)
        {
            if (change == null)
            {
                throw new ArgumentNullException("change");
            }

            if (changeGroup == null)
            {
                throw new ArgumentNullException("changeGroup");
            }

            Debug.Assert(changeGroup.Status != ChangeStatus.Complete);

            SubversionAnalysisAlgorithm algorithm;
            if (!m_subversionChangeTranslators.TryGetValue(change.ChangeAction, out algorithm))
            {
                algorithm = Unhandled;
            }

            algorithm(change, changeGroup);
        }

        /// <summary>
        /// Has to be called after each analysis pass. This method flushes all temporary data and executes final cleanup tasks
        /// </summary>
        /// <param name="group"></param>
        internal void Finish(ChangeGroup group)
        {
            //post process delete operations
            FlushDeleteLookupTableToDatabase(group);
            m_deleteLookupTable = null;

            m_renameLookupTable = null;
        }

        #endregion

        #region Handlers

        #region None

        /// <summary>
        /// None or void Action. Nothing to do
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void None(Change change, ChangeGroup group)
        {
            //Nothing to do here
        }

        #endregion

        #region Add

        /// <summary>
        /// Adds an add action to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void Add(Change change, ChangeGroup group)
        {
            createMigrationAction(change, null, group, WellKnownChangeActionId.Add);
        }

        /// <summary>
        /// This method is used to analyze wether there is a add action on a parent item. If there is a add on a parent item we have to remap it
        /// so that we pend the proper add operation on this item. Therefore we have to inject an add action for the mapping level rather than
        /// on the original and highest level
        /// </summary>
        /// <param name="mapping">The URI to the mapped item that is the child of the actual real change</param>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        private void analyzeNotMappedAddOperations(Uri mapping, Change change, ChangeGroup changeGroup)
        {
            //we have to construct a fake change in order to pend it properly
            var fakeChange = new Change(change.Changeset, mapping.AbsolutePath, null, -1, change.ItemType, change.ChangeAction);
            createMigrationAction(fakeChange, null, changeGroup, WellKnownChangeActionId.Add);
        }

        #endregion

        #region Edit

        /// <summary>
        /// Adds an edit action to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void Edit(Change change, ChangeGroup group)
        {
            //Subversion reports edit on folders as well if any property changed. Therfore we can skip this
            if (change.ItemType != WellKnownContentType.VersionControlledFolder)
            {
                createMigrationAction(change, null, group, WellKnownChangeActionId.Edit);
            }
        }

        #endregion

        #region Delete

        /// <summary>
        /// Adds an add action to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void Delete(Change change, ChangeGroup group)
        {
            //We just initialize the lookup table. The actual delete operations will be created in the flush of the lookup table
            //This is needed because rename operations have to delete specific delete actions
            InitializeDeleteLookupTables(CurrentChangeset);
        }

        /// <summary>
        /// Post processing of the delete operation
        /// </summary>
        /// <param name="group">The change grouing this change is to be a part of</param>
        private void FlushDeleteLookupTableToDatabase(ChangeGroup group)
        {
            if (null != m_deleteLookupTable && 0 != m_deleteLookupTable.Count)
            {
                //We order the collection descending. Therfore we are processing nested files first and traverse up until we reach the root folder
                var records = m_deleteLookupTable.Values.OrderByDescending(x => x.FullServerPath.Length);
                foreach (var record in records)
                {
                    createMigrationAction(record, null, group, WellKnownChangeActionId.Delete);
                }
            }
        }

        /// <summary>
        /// This method is used to analyze wether there is a delete action on a parent item. If there is a delete on a parent item we have to remap it
        /// so that we pend the proper delete operation on this item. Therefore we have to inject a proper delete action for the mapping level rather than
        /// on the original and highest level
        /// </summary>
        /// <param name="mapping">The URI to the mapped item that is the child of the actual real change</param>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        private void analyzeNotMappedDeleteOperations(Uri mapping, Change change, ChangeGroup changeGroup)
        {
            //we have to construct a fake change in order to pend it properly
            var fakeChange = new Change(change.Changeset, mapping.AbsolutePath, null, -1, change.ItemType, change.ChangeAction);
            createMigrationAction(fakeChange, null, changeGroup ,WellKnownChangeActionId.Delete);
        }

        private void InitializeDeleteLookupTables(ChangeSet changeSet)
        {
            if (null == m_deleteLookupTable)
            {
                var deletes = changeSet.Changes.Where(x => x.ChangeAction == ChangeAction.Delete && m_provider.IsPathMapped(x.FullServerPath)).ToList();
                m_deleteLookupTable = new Dictionary<string, Change>(deletes.Count);

                foreach (var delete in deletes)
                {
                    if (delete.ItemType == WellKnownContentType.VersionControlledFile || delete.ItemType == WellKnownContentType.VersionControlledFolder || delete.ItemType == WellKnownContentType.VersionControlledArtifact)
                    {
                        if (!m_deleteLookupTable.ContainsKey(delete.FullServerPath))
                        {
                            m_deleteLookupTable.Add(delete.FullServerPath, delete);
                        }
                    }
                    else
                    {
                        var message = string.Format(SubversionVCAdapterResource.ChangeTypeNotFoundExceptionMessage, delete.ItemType);
                        Debug.Fail(message);
                        throw new NotSupportedException(message);
                    }
                }
            }
        }

        #endregion

        #region Branch

        /// <summary>
        /// Adds an add action to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void Branch(Change change, ChangeGroup group)
        {
            //Determine wether the source path is mapped. 
            //If this isnt the case, we are going to raise a conflict for this
            //We have to perform this check for branch and rename operations.
            if (!m_provider.IsPathMapped(change.CopyFromFullServerPath))
            {
                raiseBranchParentNotFoundConflictForBranch(group, change);
                return;
            }

            //Initialize the tables that are needed for rename detection
            InitializeDeleteLookupTables(CurrentChangeset);
            InitializeRenameLookupTable(CurrentChangeset);

            if (IsRenameCase(change, group))
            {
                Rename(change, group);
            }
            else
            {
                if (change.ItemType == WellKnownContentType.VersionControlledFolder)
                {
                    ProcessFolderBranch(change, group);
                }
                else
                {
                    ProcessFileBranch(change, group);
                }
            }
        }

        /// <summary>
        /// This method is used to analyze wether there is a branch action on a parent item. If there is a branch on a parent item we have to remap it
        /// so that we pend the proper branch operation on this item. Therefore we have to inject a proper branch action for the mapping level rather than
        /// on the original and highest level
        /// </summary>
        /// <param name="mapping">The URI to the mapped item that is the child of the actual real change</param>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        private void analyzeNotMappedBranchOperations(Uri mapping, Change change, ChangeGroup group, int[] mappedChanges)
        {
            //we already know the destination of the branch. This is the currently mapped folder. The source of the branch has to be computed
            //The source can easily be retrieved by rebasing the source path
            var copyFromPath = PathUtils.RebaseUri(mapping, new Uri(change.FullServerPath), new Uri(change.CopyFromFullServerPath));

            //we also have to calculate the copy from value. The best case is if the copyfrom revision contains actual changes and therefore is in the mapping
            //in the worst case scenario we have to remap it to the previous change that has been applied to one of our chages.
            var revision = remapBranchRevisionNumber(CurrentRepository, copyFromPath, change.CopyFromRevision, mappedChanges);
            if (revision > 0)
            {
                var newChangeAction = new Change(change.Changeset, mapping.AbsolutePath, copyFromPath.AbsolutePath, change.CopyFromRevision, change.ItemType, change.ChangeAction);
                ProcessFolderBranch(newChangeAction, group);
            }
            else
            {
                var newChangeAction = new Change(change.Changeset, mapping.AbsolutePath, copyFromPath.AbsolutePath, change.CopyFromRevision, change.ItemType, change.ChangeAction);
                raiseBranchParentNotFoundConflictForBranch(group, newChangeAction);
            }
        }

        private int remapBranchRevisionNumber(Repository repositroy, Uri path, int revision, int [] mappedChanges)
        {
            //try to find the value within the mappedchanges array. This is the fastest way to get the value. If it is not 
            //in it then we have to query subversion for it
            if (null != mappedChanges && mappedChanges.Length >= 0)
            {
                if (revision >= mappedChanges[0] && revision <= mappedChanges[mappedChanges.Length - 1])
                {
                    //The value must be in the array because we are in the middle. Either we find it or it does not exists
                    //furthermore we can start searching from the beginning because the array is ordered.
                    int previous = -1;
                    foreach (var value in mappedChanges)
                    {
                        if (value <= revision)
                        {
                            previous = value;
                        }
                        else
                        {
                            break;
                        }
                    }

                    return previous;
                }
            }

            //it must had been migrated earlier--> access the repository for it
            var previousRecord = repositroy.QueryHistory(path, revision, 1, false).Keys;
            if(previousRecord != null)
            {
                return previousRecord.First();
            }
            else
            {
                return -1;
            }
        }

        private void ProcessFolderBranch(Change change, ChangeGroup group)
        {
            var sourceBaseUri = change.CopyFromFullServerPath;
            var destinationBaseUri = change.FullServerPath;
            var repositoryUri = new Uri(change.Changeset.Repository);
            Repository repository = Repository.GetRepository(repositoryUri);

            //We have to query the list of all files based on the branch source. 
            //Subversion only pends one recursive branch action rather than a action for all files
            //Therefore we have to resolve this list manually
                
            var items = repository.GetItems(change.CopyFromFullServerPath, change.CopyFromRevision, true);

            //pend an action for every single item
            //We do not have to handle the root folder individually since it is one item in the collection already
            //we do not have to cover the branch | edit case either because there is an additional record in the changelist alredy
            foreach (var sourceItem in items)
            {
                var destinationUri = PathUtils.RebaseUri(new Uri(sourceItem.FullServerPath),
                                                            new Uri(sourceBaseUri),
                                                            new Uri(destinationBaseUri));

                var destinationPath = PathUtils.ExtractPath(repositoryUri, destinationUri);

                var migrationItem = new SubversionMigrationItem(repositoryUri,
                                                                destinationUri,
                                                                change.Changeset.Revision,
                                                                sourceItem.ItemType);

                group.CreateAction(WellKnownChangeActionId.Branch,
                                    migrationItem,
                                    sourceItem.Path,
                                    destinationPath.OriginalString,
                                    change.CopyFromRevision.ToString(),
                                    null,
                                    change.ItemType.ReferenceName,
                                    null);
            }
        }

        private void ProcessFileBranch(Change change, ChangeGroup group)
        {
            //Remark: We do not really check wether the revision exists. We assume that the
            //source repository is consistent. If the revision does not exists though,
            //the destinatino adapter will have an issue to execute the branch and will
            //therefore rise and proper exception. Therefore this issue will be handleded during 
            //the migration

            group.CreateAction(WellKnownChangeActionId.Branch,
                               new SubversionMigrationItem(change),
                               change.CopyFromPath,
                               change.Path,
                               change.CopyFromRevision.ToString(),
                               null,
                               change.ItemType.ReferenceName,
                               null);

            //we have to check wether the file has been modified. If this is the case we have to pend an aditional edit
            if (HasContentChanges(change))
            {
                createMigrationAction(change, change.Path, group, WellKnownChangeActionId.Edit);
            }
        }

        private bool HasContentChanges(Change change)
        {
            // Todo
            if (change.ItemType == WellKnownContentType.VersionControlledFolder)
            {
                return false;
            }

            //check wether the file as an aditional edit. The method returns zero records if the files are equal; one record otherwise
            //Note, this method is a little bit ineffective. svn stores the md5 sums of the copy source and the copy destination
            //However, this information does not seem to be exported via the api and therefore we cant use it. Hopefully the diff summary calls
            //are quick because we just query the diff and do not construct the full text diffs.
            return CurrentRepository.GetDiffSumary(change.CopyFromFullServerPath, change.CopyFromRevision, change.FullServerPath, change.Changeset.Revision);
        }

        private void raiseBranchParentNotFoundConflictForBranch(ChangeGroup group, Change conflictChange)
        {
            MigrationConflict branchParentNotFoundConflict = VCBranchParentNotFoundConflictType.CreateConflict(conflictChange.Path);
            List<MigrationAction> retActions;

            ConflictResolutionResult resolutionResult = m_provider.ConflictManager.TryResolveNewConflict(group.SourceId, branchParentNotFoundConflict, out retActions);
            if ((resolutionResult.Resolved) && (resolutionResult.ResolutionType == ConflictResolutionType.UpdatedConflictedChangeAction))
            {
                InitializeDeleteLookupTables(CurrentChangeset);
                if (conflictChange.ItemType == WellKnownContentType.VersionControlledFolder)
                {
                    resolveParentNotFoundConflictForFolder(group, conflictChange);
                }
                else if (conflictChange.ItemType == WellKnownContentType.VersionControlledFile)
                {
                    resolveParentNotFoundConflictForFile(group, conflictChange);
                }
                else 
                {
                    var message = string.Format(SubversionVCAdapterResource.ChangeTypeNotFoundExceptionMessage, conflictChange.ItemType);
                    Debug.Fail(message);
                    throw new NotSupportedException(message);
                }
            }
            else
            {
                throw new MigrationUnresolvedConflictException(branchParentNotFoundConflict);
            }
        }

        private void resolveParentNotFoundConflictForFile(ChangeGroup group, Change conflictChange)
        {
            createMigrationAction(conflictChange, conflictChange.Path, group, WellKnownChangeActionId.Add);

            //There might be subsequent delete operations that could be pending due to rename or move operations. 
            //Therefore we have to check the delete table for every single file and folder and drop it if needed
            DropNotNeededDeleteActions(conflictChange.FullServerPath);
        }

        private void resolveParentNotFoundConflictForFolder(ChangeGroup group, Change change)
        {
            //We have to query the list of all files based on the branch source. 
            //Subversion only pends one recursive branch action rather than a action for all files
            //Therefore we have to resolve this list manually
            var items = CurrentRepository.GetItems(change.FullServerPath, change.Changeset.Revision, true);

            //pend an action for every single item
            //We do not have to handle the root folder individually since it is one item in the collection already
            foreach (var sourceItem in items)
            {
                var migrationItem = new SubversionMigrationItem(sourceItem, change.Changeset.Revision);

                group.CreateAction(WellKnownChangeActionId.Add,
                                   migrationItem,
                                   null,
                                   sourceItem.Path,
                                   null,
                                   null,
                                   sourceItem.ItemType.ReferenceName,
                                   null);

                //There might be subsequent delete operations that could be pending due to rename or move operations. 
                //Therefore we have to check the delete table for every single file and folder and drop it if needed
                DropNotNeededDeleteActions(sourceItem.FullServerPath);
            }
        }

        private void DropNotNeededDeleteActions(String path)
        {
            var itemsToRemove = new List<string>();

            foreach (var key in m_deleteLookupTable.Keys)
            {
                if (PathUtils.IsChildItem(path, key))
                {
                    itemsToRemove.Add(key);
                }
            }

            foreach (var item in itemsToRemove)
            {
                m_deleteLookupTable.Remove(item);
            }
        }

        #endregion

        #region Rename

        /// <summary>
        /// Revise the rename from name according to pended parent renames 
        /// E.g. rename fld1->fld2, rename fld1/1.txt->fld3/2.txt,
        /// If we already the pend rename for fld1->fld2, we need to revise the 2nd rename to fld2/1.txt->fld3/2.txt
        /// </summary>
        /// <param name="root">The path string that defines the end of the recursion</param>
        /// <param name="item">The item that has to be revised</param>
        /// <returns>The revised path for the item</returns>
        private string reviseSourceName(string root, string item)
        {
            //we start at the root level of the repository
            if (string.Equals(root, item, StringComparison.OrdinalIgnoreCase))
            {
                //we reached the root. We can stop the recursion and traverse back up
                return item;
            }
            else
            {
                var itemName = PathUtils.GetItemName(item);
                var parent = PathUtils.GetParent(item);

                //traverse down the tree until we reach the root. Then we can start reassembeling the path
                var intermediate = reviseSourceName(root, parent);
                var revisedPath = PathUtils.Combine(intermediate, itemName);

                if (m_renameLookupTable.ContainsKey(revisedPath))
                {
                    //The path has been renamed. we return the renamed path. This one will be used for reassembeling the renamed path
                    return m_renameLookupTable[revisedPath];
                }
                else
                {
                    //The lookup table does not contain a record. Therefore we do not have to manipulate the path
                    return revisedPath;
                }
            }
        }

        private void InitializeRenameLookupTable(ChangeSet changeSet)
        {
            if (null != m_renameLookupTable)
            {
                return;
            }

            m_renameLookupTable = new Dictionary<string, string>();

            //we just have to analyze all folders that have been branched. 
            // Todo The source of the branch must be the previous revision because it cant be a rename otherwise
            var branchedFolders = changeSet.Changes.Where(x => x.ItemType == WellKnownContentType.VersionControlledFolder &&
                                                            x.ChangeAction == ChangeAction.Copy &&
                                                            //x.CopyFromRevision == changeSet.Revision - 1 &&
                                                            m_provider.IsPathMapped(x.FullServerPath));

            //We have to analyse the top level folders first because subsequent or nested renames are possible. In that case we have to revise the source names accordingly
            //We can sort the collection using the length of the copyfromfullserver path. 
            //The copy from always points to the source of the rename. This also applies for nested operations
            //Rename: A -> B --> CopyFrom: A
            //Rename: A\A ->B\C --> CopyFrom: A\A
            //Due to the ordering we ensure that we process A before A\A. This is necessary for the recursive resolve process
            var branchedFoldersSorted = branchedFolders.OrderBy(x => x.CopyFromFullServerPath.Length);
            foreach (var folder in branchedFoldersSorted)
            {
                //we have to execute all the rename of the folder to create the destination folder
                //therefore we apply all the rename operations on the copyfromfullserverpath property
                var revisedCopyFromFullServerPath = reviseSourceName(changeSet.Repository, folder.CopyFromFullServerPath);

                //after we have applied all rename operations, we can simply check wether we have a related delete for this folder
                //if we have a delete, then it is a rename case; if not, then not
                if (m_deleteLookupTable.ContainsKey(revisedCopyFromFullServerPath))
                {
                    m_renameLookupTable.Add(revisedCopyFromFullServerPath, folder.FullServerPath);
                }
            }
        }

        /// <summary>
        /// This method analyzes wether a branch is a simple branch or actually a rename case
        /// In subversion, every rename operation is a combination of branch / delete
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="changeGroup">The change grouing this change is to be a part of</param>
        /// <returns>true if it is a rename case; false otherwise</returns>
        private bool IsRenameCase(Change change, ChangeGroup changeGroup)
        {
            //Root cannot be a rename case. Therefore we could skip that
            if (PathUtils.AreEqual(new Uri(change.Changeset.Repository), new Uri(change.CopyFromFullServerPath)))
                return false;

            //we have to revise the parent path of this item only.
            //We do not have to do that for the item name itself because the item name remains stable in the copy from field
            var parent = PathUtils.GetParent(change.CopyFromFullServerPath);
            var item = PathUtils.GetItemName(change.CopyFromFullServerPath);

            var intermediate = reviseSourceName(change.Changeset.Repository, parent);
            var revisedPath = PathUtils.Combine(intermediate, item);

            //we check wether there is a delete record corosponding to this branch. 
            //If this is the case we have to check the start revision. 
            //It is a rename if we branched from the previous revision
            if (m_deleteLookupTable.ContainsKey(revisedPath))
            {
                // Todo Don't need the revision check
                return true;
                /*if (change.CopyFromRevision == change.Revision - 1)
                {
                    return true;
                }*/
            }

            return false;
        }

        /// <summary>
        /// Adds an rename action to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        private void Rename(Change change, ChangeGroup group)
        {
            var parent = PathUtils.GetParent(change.CopyFromFullServerPath);
            var item = PathUtils.GetItemName(change.CopyFromFullServerPath);

            var intermediate = reviseSourceName(change.Changeset.Repository, parent);
            var revisedPath = PathUtils.Combine(intermediate, item);

            //we crate the rename action
            group.CreateAction(WellKnownChangeActionId.Rename,
                               new SubversionMigrationItem(change),
                               change.CopyFromPath,
                               change.Path,
                               change.CopyFromRevision.ToString(),
                               null,
                               change.ItemType.ReferenceName,
                               null);

            //and then we are going to remove the delete record from the lookup table.
            //The lookup table will be flushed later and therefore this delete operation will be skipped
            if (m_deleteLookupTable.ContainsKey(revisedPath))
            {
                m_deleteLookupTable.Remove(revisedPath);
            }
            else
            {
                Debug.Fail("Trying to remove a key from the deletelookup table. This case should never occur");
            }

            //we have to check wether the file has been modified. If this is the case we have to pend an aditional edit
            if (HasContentChanges(change))
            {
                createMigrationAction(change, change.Path, group, WellKnownChangeActionId.Edit);
            }
        }

        #endregion

        #region Unhandeled

        /// <summary>
        /// Called when the requested change type is unknown.  This indicates a previously unseen change operation and means that a
        /// new algorithm method needs to be created.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void Unhandled(Change change, ChangeGroup group)
        {
            //TODO pend a conflict
        }

        #endregion

        #endregion

        #region Private Helpers

        private void BuildChangeTranslators()
        {
            addHandler(ChangeAction.Add, Add);
            addHandler(ChangeAction.Copy, Branch);
            addHandler(ChangeAction.Modify, Edit);
            addHandler(ChangeAction.Delete, Delete);
            addHandler(ChangeAction.Replace, Edit);
        }

        private void addHandler(ChangeAction changeAction, SubversionAnalysisAlgorithm algorithm)
        {
            Debug.Assert(m_subversionChangeTranslators != null);

            if (m_subversionChangeTranslators.ContainsKey(changeAction))
            {
                throw new MigrationException(string.Format(CultureInfo.InvariantCulture, "There already exists a handler for: {0}", changeAction));
            }

            m_subversionChangeTranslators[changeAction] = algorithm;
        }

        /// <summary>
        /// Create a basic action.
        /// </summary>
        /// <param name="changeItem">The actual change</param>
        /// <param name="group">The change group that is the container for the current changes</param>
        /// <param name="actionId">The change action id that describes the change type</param>
        /// <returns></returns>
        private IMigrationAction createMigrationAction(Change changeItem, string fromPath, ChangeGroup group, Guid actionId)
        {
            IMigrationAction action = group.CreateAction(
                actionId,
                new SubversionMigrationItem(changeItem),
                fromPath,
                changeItem.Path,
                null,
                null,
                changeItem.ItemType.ReferenceName,
                null);

            return action;
        }

        #endregion
    }
}
