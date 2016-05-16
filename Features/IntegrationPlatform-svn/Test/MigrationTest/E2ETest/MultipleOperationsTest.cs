// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTestLibrary;
using ServerDiff;
using MigrationTestLibrary.Conflict;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using System.Collections.Generic;

namespace TfsVCTest
{
    /// <summary>
    /// Summary description for MultipleOPerationsTest
    /// </summary>
    [TestClass]
    public class MultipleOperationsTest : TfsVCTestCaseBase
    {
        private MigrationItemStrings source;
        private MigrationItemStrings target;
        private MigrationItemStrings file;

        [TestInitialize()]
        public override void Initialize()
        {
            base.Initialize();

            source = new MigrationItemStrings("source" + SrcPathSeparator, null, TestEnvironment, true);
            target = new MigrationItemStrings("target" + SrcPathSeparator, null, TestEnvironment, true);
            file = new MigrationItemStrings("file.txt", null, TestEnvironment, true);
        }

        [TestCleanup()]
        public override void Cleanup()
        {
            source = null;
            target = null;
            file = null;

            base.Cleanup();
        }


        #region Pend Tfs Operations

        private void EditUndelete(MigrationItemStrings file)
        {
            SourceAdapter.AddFile(file.LocalPath);
            PendUndelete(file);
            TestUtils.EditRandomFile(file.LocalPath);
            SourceWorkspace.PendEdit(file.LocalPath);
        }

        private void PendUndelete(MigrationItemStrings folder)
        {
            int deletionId = SourceTfsClient.GetChangeset(SourceAdapter.DeleteItem(folder.ServerPath)).Changes[0].Item.DeletionId;

            SourceWorkspace.PendUndelete(folder.ServerPath, deletionId);
        }

        private void PendRenameDelete(MigrationItemStrings folder, MigrationItemStrings file)
        {
            SourceWorkspace.PendRename(folder.LocalPath, folder.NewLocalPath);
            SourceWorkspace.PendDelete(file.NewServerPath);
        }

        private void PendRenameEdit(MigrationItemStrings file)
        {
            TestUtils.EditRandomFile(file.LocalPath);

            SourceWorkspace.PendEdit(file.LocalPath);
            SourceWorkspace.PendRename(file.LocalPath, file.NewLocalPath);
        }

        private void PendUndeleteRename(MigrationItemStrings file)
        {
            PendUndelete(file);
            SourceWorkspace.PendRename(file.LocalPath, file.NewLocalPath);
        }

        private void PendMergeUndelete(MigrationItemStrings item, int changesetId)
        {
            MergeDeletePendUndelete(item, changesetId);

            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), MultiActionComment);

            SourceWorkspace.Merge(source.LocalPath, target.LocalPath,
                VersionSpec.ParseSingleSpec(changesetId.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);

            resolveConflictAcceptThiers();
        }

        private void MergeDeletePendUndelete(MigrationItemStrings item, int changesetId)
        {
            int deletionId = SourceTfsClient.GetChangeset(SourceAdapter.DeleteItem(item.ServerPath)).Changes[0].Item.DeletionId;

            SourceWorkspace.Merge(source.LocalPath, target.LocalPath,
                VersionSpec.ParseSingleSpec(changesetId.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);

            // Check in only the merge|delete; so that the undelete may be merged over
            SourceWorkspace.CheckIn( SourceWorkspace.GetPendingChanges(), MergeComment);

            // pend undelete
            SourceWorkspace.PendUndelete(item.ServerPath, deletionId);
        }

        private void PendDeleteUndelete(MigrationItemStrings folder)
        {
            PendUndelete(folder);
            SourceWorkspace.PendDelete(file.ServerPath);
        }

        #endregion

        #region Testcase helper methods

        private int AddBranch()
        {
            file = new MigrationItemStrings(source.Name + "file.txt", source.Name + "renamed-file.txt", TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);
            return SourceAdapter.BranchItem(source.ServerPath, target.ServerPath);
        }

        private void resolveConflictAcceptThiers()
        {
            Conflict[] conflicts = SourceWorkspace.QueryConflicts(new string[] { "$/" }, true);

            foreach(Conflict c in conflicts)
            {
                c.Resolution = Resolution.AcceptTheirs;
                SourceWorkspace.ResolveConflict(c);
            }
        }


        private void CheckinMergeResolve(int changesetId)
        {
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), MultiActionComment);

            SourceWorkspace.Merge(source.LocalPath, target.LocalPath,
                VersionSpec.ParseSingleSpec(changesetId.ToString(),
                Environment.UserName), VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);

            resolveConflictAcceptThiers();
        }

        #endregion

        ///<summary>
        ///Scenario: Branch A to B; Delete B/Foo
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Branch A to B; Delete B/Foo")]
        public void BranchDeleteTest()
        {
            file = new MigrationItemStrings(source.Name + "file.txt", target.Name + "file.txt", TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);

            SourceWorkspace.PendBranch(source.ServerPath, target.ServerPath, VersionSpec.Latest);
            SourceWorkspace.PendDelete(file.NewLocalPath);

            SourceAdapter.EditFile(m_extraFile.LocalPath);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Branch A to B; Delete B
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Branch A to B; Delete B/Foo")]
        public void RecursiveBranchDeleteTest()
        {
            file = new MigrationItemStrings(source.Name + "file.txt", target.Name + "file.txt", TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);

            SourceWorkspace.PendBranch(source.ServerPath, target.ServerPath, VersionSpec.Latest);
            SourceAdapter.DeleteItem(target.ServerPath);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Delete a folder with sub-items. 
        ///Expected Result: The whole folder will be deleted. 
        ///</summary>
        [TestMethod(), Priority(2), Owner("peigu")]
        [Description("Delete folder 'folder' with sub items.")]
        public void RecursiveDeletesTest()
        {
            file = new MigrationItemStrings("folder/subfolder/file.txt", null, TestEnvironment, true);
            MigrationItemStrings folder = new MigrationItemStrings("folder", null, TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);

            SourceAdapter.DeleteItem(folder.ServerPath);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Edit foo Rename foo to bar
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Edit foo Rename foo to bar")]
        public void RenameEditTest()
        {
            file = new MigrationItemStrings("file.txt", "newName.txt", TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);

            PendRenameEdit(file);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Edit foo Rename foo to bar
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("peigu")]
        [Description("Edit folder\file.txt Rename folder to newName")]
        public void RenameFolderEditFileTest()
        {
            file = new MigrationItemStrings("folder/file.txt", "newName/file.txt", TestEnvironment, true);
            MigrationItemStrings folder = new MigrationItemStrings("folder", "newName", TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);

            TestUtils.EditRandomFile(file.LocalPath);

            SourceWorkspace.PendEdit(file.LocalPath);
            SourceWorkspace.PendRename(folder.LocalPath, folder.NewLocalPath);
            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Rename folder1; Delete folder1/foo
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Rename folder1; Delete folder1/foo")]
        public void RenameDeleteTest()
        {
            MigrationItemStrings folder = new MigrationItemStrings("folder/", "newfolder/", TestEnvironment, true);
            file = new MigrationItemStrings(folder.Name + "file.txt", folder.NewName + "file.txt", TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);

            PendRenameDelete(folder, file);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Delete folder/1.txt, Rename folder
        ///</summary>
        [TestMethod(), Priority(2), Owner("hykwon")]
        [Description("Delete folder/1.txt, Rename folder")]
        public void DeleteRenameParentTest()
        {
            MigrationItemStrings folder = new MigrationItemStrings("folder/", "renamedfolder/", TestEnvironment, true);
            MigrationItemStrings file1 = new MigrationItemStrings(folder.Name + "1.txt", null, TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings(folder.Name + "2.txt", null, TestEnvironment, true);

            SourceAdapter.AddFolder(folder.LocalPath);
            SourceAdapter.AddFile(file1.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);

            SourceAdapter.DeleteItem(file1.ServerPath);

            // Deleted item will show up as rename
            SourceAdapter.RenameItem(folder.ServerPath, folder.NewServerPath, "Rename parent folder.");

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Undelete foo; Edit foo
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Undelete foo; Edit foo")]
        public void EditUndeleteTest()
        {
            file = new MigrationItemStrings("file.txt", null, TestEnvironment, true);

            EditUndelete(file);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Undelete foo; Edit foo
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("peigu")]
        [Description("Delete several files in the same changeset.")]
        public void MultipleDeletesTest()
        {
            file = new MigrationItemStrings("folder/file.txt", null, TestEnvironment, true);
            MigrationItemStrings file1 = new MigrationItemStrings("folder/file1.txt", null, TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings("folder/file2.txt", null, TestEnvironment, true);
            MigrationItemStrings file3 = new MigrationItemStrings("folder/file3.txt", null, TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);
            SourceAdapter.AddFile(file1.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);
            SourceAdapter.AddFile(file3.LocalPath);

            SourceWorkspace.PendDelete(file.LocalPath);
            SourceWorkspace.PendDelete(file1.LocalPath);
            SourceWorkspace.PendDelete(file2.LocalPath);
            SourceWorkspace.PendDelete(file3.LocalPath);

            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), "Multiple deletes.");

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Undelete folder1; Delete folder1/foo
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Undelete folder1; Delete folder1/foo")]
        public void DeleteUndeleteTest()
        {
            MigrationItemStrings folder = new MigrationItemStrings("folder/", null, TestEnvironment, true);
            file = new MigrationItemStrings(folder.Name + "file.txt", null, TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);

            PendDeleteUndelete(folder);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Undelete foo; Rename foo to bar
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Undelete foo; Rename foo to bar")]
        public void RenameUndeleteTest()
        {
            file = new MigrationItemStrings("file.txt", "newFile.txt", TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);
            PendUndeleteRename(file);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Undelete foo; Edit foo; Rename foo to bar
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Undelete foo; Edit foo; Rename foo to bar")]
        public void RenameEditUndeleteTest()
        {
            file = new MigrationItemStrings("file.txt", "newFile.txt", TestEnvironment, true);

            EditUndelete(file);
            SourceWorkspace.PendRename(file.LocalPath, file.NewLocalPath);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Undelete folder1; Rename folder1/folder2; Delete folder1/folder2/foo
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Undelete folder1; Rename folder1/folder2;  Delete folder1/folder2/foo")]
        public void RenameDeleteUndeleteTest()
        {
            MigrationItemStrings folder = new MigrationItemStrings("folder/", "newfolder/", TestEnvironment, true);
            MigrationItemStrings subfolder = new MigrationItemStrings(folder.Name + "subFolder/", folder.Name + "newSubfolder/", TestEnvironment, true);
            file = new MigrationItemStrings(subfolder.Name + "file.txt", subfolder.NewName + "file.txt", TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);

            PendUndelete(folder);
            PendRenameDelete(subfolder, file);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Merge a folder1 to new folder2
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Merge a folder1 to new folder2")]
        public void BranchMergeTest()
        {
            file = new MigrationItemStrings(source.Name + "file.txt", target.Name + "file.txt", TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);

            SourceWorkspace.Merge(file.ServerPath, file.NewServerPath, VersionSpec.Latest, VersionSpec.Latest);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Merge a folder1 to new folder2 Delete folder2/foo
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Merge a folder1 to new folder2 Delete folder2/foo")]
        public void BranchMergeDeleteTest()
        {
            file = new MigrationItemStrings(source.Name + "file.txt", target.Name + "file.txt", TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);

            SourceWorkspace.Merge(source.ServerPath, target.ServerPath, VersionSpec.Latest, VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);
            int mergeChangeset = SourceAdapter.DeleteItem(file.NewLocalPath);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Merge a folder1 to new folder2; Edit folder2/foo
        ///Expected Result: Mapped to the correct sequence of actions; 
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Merge a folder1 to new folder2; Edit folder2/foo")]
        public void BranchMergeEditTest()
        {
            file = new MigrationItemStrings(source.Name + "file.txt", target.Name + "file.txt", TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);

            SourceWorkspace.Merge(file.ServerPath, file.NewServerPath, VersionSpec.Latest, VersionSpec.Latest);

            int mergeChangeset = SourceAdapter.EditFile(file.NewLocalPath);

            RunAndValidate();
            /* TfsServerDiff diff = RunNoValidate();
            Assert.AreEqual(1, diff.SourceChanges.Count, "The source system should have 1 unmatched change");
            Assert.AreEqual(1, diff.TargetChanges.Count, "The target system should have 1 unmatched change");
            Assert.AreEqual(diff.TargetChanges[0].Changes[1].ChangeType, ChangeType.Encoding | ChangeType.Branch | ChangeType.Edit | ChangeType.Merge, "Wrong change type");
            Assert.AreEqual(mergeChangeset, diff.SourceChanges[0].ChangesetId, "Wrong changeset");
            */
        }


        ///<summary>
        ///Scenario: Merge a folder1 to new folder2; Edit folder2/foo
        ///Expected Result: Mapped to the correct sequence of actions; 
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Merge a folder1 to new folder2; Edit folder2/foo")]
        public void BranchMergeEditFileOnlyTest()
        {
            MigrationItemStrings folder = new MigrationItemStrings(source.Name + "folder", target.Name + "folder", TestEnvironment, true);
            file = new MigrationItemStrings(source.Name + "folder/file.txt", target.Name + "folder/file.txt", TestEnvironment, true);

            SourceAdapter.AddFolder(folder.LocalPath);
            SourceWorkspace.Merge(folder.ServerPath, folder.NewServerPath, VersionSpec.Latest, VersionSpec.Latest);

            SourceAdapter.AddFile(file.LocalPath);
            SourceWorkspace.Merge(file.ServerPath, file.NewServerPath, VersionSpec.Latest, VersionSpec.Latest);
            int mergeChangeset = SourceAdapter.EditFile(file.NewLocalPath);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Rename source/file to source/renamed-file; Checkin; Merge source to target
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Rename source/file to source/renamed-file; Checkin; Merge source to target")]
        public void RenameMergeTest()
        {
            MigrationItemStrings file1 = new MigrationItemStrings(source.Name + "file1.txt", source.Name + "renamed-file1.txt", TestEnvironment, true);
            SourceAdapter.AddFile(file1.LocalPath);

            MigrationItemStrings file2 = new MigrationItemStrings(source.Name + "file2.txt", source.Name + "renamed-file2.txt", TestEnvironment, true);
            SourceAdapter.AddFile(file2.LocalPath);

            int changesetId = AddBranch();

            SourceAdapter.RenameItem(file.ServerPath, file.NewServerPath, AddComment);
            SourceAdapter.RenameItem(file1.ServerPath, file1.NewServerPath, AddComment);
            SourceAdapter.RenameItem(file2.ServerPath, file2.NewServerPath, AddComment);

            SourceWorkspace.Merge(source.LocalPath, target.LocalPath,
                VersionSpec.ParseSingleSpec(changesetId.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.ForceMerge);

            Conflict[] conflicts = SourceWorkspace.QueryConflicts(new string[] { "$/" }, true);

            foreach (Conflict conflict in conflicts)
            {
                conflict.Resolution = Resolution.AcceptTheirs;
                SourceWorkspace.ResolveConflict(conflict);
            }

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Rename source/folder1 to source/renamed-folder; Delete source/folder1/foo; Checkin; Merge source to target
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Rename source/folder1; Delete source/folder1/foo; Checkin; Merge source to target")]
        public void RenameDeleteMergeTest()
        {
            MigrationItemStrings sourceFolder = new MigrationItemStrings(source.Name + "folder/", source.Name + "renamed-folder/", TestEnvironment, true);
            file = new MigrationItemStrings(sourceFolder.Name + "file.txt", sourceFolder.NewName + "file.txt", TestEnvironment, true);

            int changesetId = SourceAdapter.AddFile(file.LocalPath);
            SourceAdapter.BranchItem(source.ServerPath, target.ServerPath);

            PendRenameDelete(sourceFolder, file);

            CheckinMergeResolve(changesetId);

            if (TestEnvironment.SourceTFSVersion == TFSVersionEnum.TFS2010)
            {
                Run();
                Assert.IsTrue(VerifyContents());
            }
            else
            {
                RunAndValidate();
            }
        }

        ///<summary>
        /// Same as RenameDeleteMergeTest except merge, rename only in the same changeset
        /// ref: Dev10 Bug 741185
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("hykwon")]
        [Description("RenameDeleteMergeTest with merge,rename changes")]
        public void RenameDeleteMergeTest2()
        {
            MigrationItemStrings sourceFolder = new MigrationItemStrings(source.Name + "folder/", source.Name + "renamed-folder/", TestEnvironment, true);
            file = new MigrationItemStrings(sourceFolder.Name + "file.txt", sourceFolder.NewName + "file.txt", TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings(sourceFolder.Name + "file2.txt", sourceFolder.NewName + "file2.txt", TestEnvironment, true);
            MigrationItemStrings sourceFolder2 = new MigrationItemStrings(source.Name + "folder2/", source.Name + "renamed-folder2/", TestEnvironment, true);

            int changesetId = SourceAdapter.AddFile(file.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);
            SourceAdapter.AddFolder(sourceFolder2.LocalPath);
            SourceAdapter.BranchItem(source.ServerPath, target.ServerPath);

            PendRenameDelete(sourceFolder, file);
            SourceWorkspace.PendRename(sourceFolder2.LocalPath, sourceFolder2.NewLocalPath);

            CheckinMergeResolve(changesetId);

            if (TestEnvironment.SourceTFSVersion == TFSVersionEnum.TFS2010)
            {
                Run();
                Assert.IsTrue(VerifyContents());
            }
            else
            {
                RunAndValidate();
            }
        }

        ///<summary>
        ///Scenario: Edit source/file; Rename source/file to source/renamedFile; Checkin; Merge source to target
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Edit source/file; Rename source/file to source/renamedFile; Checkin; Merge source to target")]
        public void RenameEditMergeTest()
        {
            int changesetId = AddBranch();

            PendRenameEdit(file);

            CheckinMergeResolve(changesetId);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Undelete B/foo; Rename B/foo to B/bar; Checkin; Merge B to A
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Undelete B/foo; Rename B/foo to B/bar; Checkin; Merge B to A")]
        public void RenameUndeleteMergeTest()
        {
            int changesetId = AddBranch();

            PendUndeleteRename(file);
            SourceWorkspace.Merge(source.LocalPath, target.LocalPath,
                VersionSpec.ParseSingleSpec(changesetId.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);

            CheckinMergeResolve(changesetId);

            if (TestEnvironment.SourceTFSVersion == TFSVersionEnum.TFS2010)
            {
                Run();

                // 1 extra merge item on source side is expected in dev10
                VerifyHistory(1, 1);

                //Changeset targetChangeset = diff.TargetChanges[0];
                //Changeset sourceChangeset = diff.SourceChanges[0];
                //diff.ChangesetDiff(ref targetChangeset, ref sourceChangeset);
                //Assert.AreEqual(1, sourceChangeset.Changes.Length);
                //Assert.AreEqual(ChangeType.Merge, sourceChangeset.Changes[0].ChangeType); 
                //Assert.AreEqual(0, targetChangeset.Changes.Length);

                // verify content matches
                Assert.IsTrue(VerifyContents());
            }
            else
            {
                RunAndValidate();
            }
        }

        ///<summary>
        ///Scenario: Delete B/foo; Checkin; Merge B to A
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Delete B/foo; Checkin; Merge B to A")]
        public void MergeDeleteTest()
        {
            int changesetId = AddBranch();

            SourceAdapter.DeleteItem(file.ServerPath);
            SourceWorkspace.Merge(source.LocalPath, target.LocalPath,
                VersionSpec.ParseSingleSpec(changesetId.ToString(),
                Environment.UserName), VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.ForceMerge);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Undelete B/folder1; Delete B/folder1/foo; Checkin; Merge B to A
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Undelete B/folder1; Delete B/folder1/foo; Checkin; Merge B to A")]
        public void MergeDeleteUndeleteTest()
        {
            MigrationItemStrings folder = new MigrationItemStrings(source.Name + "folder/", null, TestEnvironment, true);
            file = new MigrationItemStrings(folder.Name + "file.txt", null, TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);

            int changesetId = SourceAdapter.BranchItem(source.ServerPath, target.ServerPath);

            MergeDeletePendUndelete(folder, changesetId);

            SourceWorkspace.PendDelete(file.ServerPath);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), MultiActionComment);

            SourceWorkspace.Merge(source.LocalPath, target.LocalPath,
                VersionSpec.ParseSingleSpec(changesetId.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);

            resolveConflictAcceptThiers();

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Undelete B/foo; Checkin; Merege B to A
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Undelete B/foo; Checkin; Merege B to A")]
        public void MergeUndeleteTest()
        {
            int changesetId = AddBranch();

            PendMergeUndelete(file, changesetId);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: merge undelete rename -> snapshot
        /// Repro steps for Bug 372120 (VC adapter: merging undelete,rename in snapshot mode throws an exception)
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("merge undelete rename -> snapshot")]
        public void SnapshotMergeUndeleteRenameTest()
        {
            // scenario
            // branch from source to target where source is cloaked
            // item1 on source was edited, deleted 
            // item1 on source was undelete, renamed (case-only rename) (snapshot start point)
            // item1 was merged to target
            // migration uses a snapshot start point which skips migrating deletion
            // hence undelete, rename becomes add, rename
            
            // cloak branch from path
            MappingPair mapping = new MappingPair(
                TestEnvironment.FirstSourceServerPath + SrcPathSeparator + "source",
                TestEnvironment.FirstTargetServerPath + TarPathSeparator + "source",
                true);
            TestEnvironment.AddMapping(mapping);

            MigrationItemStrings file1 = new MigrationItemStrings("source/folder/file1.txt", "source/Folder/File1.txt", TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings("source/folder/file2.txt", "source/folder/File2.txt", TestEnvironment, true);
            MigrationItemStrings file3 = new MigrationItemStrings("source/folder/file3.txt", null, TestEnvironment, true);

            // branch
            SourceAdapter.AddFile(file1.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);
            SourceAdapter.AddFile(file3.LocalPath);

            int changesetId =  SourceAdapter.BranchItem(source.ServerPath, target.ServerPath);

            // merge edit
            // merge delete
            SourceAdapter.EditFile(file1.LocalPath);
            int deletionId = SourceTfsClient.GetChangeset(SourceAdapter.DeleteItem(file1.ServerPath)).Changes[0].Item.DeletionId;

            SourceWorkspace.Merge(source.LocalPath, target.LocalPath,
                VersionSpec.ParseSingleSpec(changesetId.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);

            changesetId = SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), "edit -> delete");

            // pend undelete
            SourceWorkspace.PendUndelete(file1.ServerPath, deletionId);

            // pend rename
            SourceWorkspace.PendRename(file1.LocalPath, file1.NewLocalPath);
            SourceWorkspace.PendRename(file2.LocalPath, file2.NewLocalPath);
            TestUtils.EditRandomFile(file3.LocalPath);
            SourceWorkspace.PendEdit(file3.LocalPath);

            changesetId = SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), "undelete rename");
            int snapshotChangesetId = changesetId;

            // merge undelete,rename skipping deletion
            // migrating merge,undelete,rename becomes add,rename  
            SourceWorkspace.Merge(source.LocalPath, target.LocalPath,
                VersionSpec.ParseSingleSpec(changesetId.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);

            resolveConflictAcceptThiers();
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), "merge undelete rename");

            // snapshot 
            TestEnvironment.SnapshotStartPoints = new Dictionary<string, string>();
            TestEnvironment.SnapshotStartPoints.Add(TestEnvironment.SourceTeamProject, snapshotChangesetId.ToString());
            TestEnvironment.SnapshotBatchSize = 100;

            Run();

            // Resolve "Branch source path not found conflict" using "$/" scope.
            ConflictResolver conflictManager = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictManager.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be only 1 conflict");
            conflictManager.TryResolveConflict(conflicts[0], new VCChangeToAddOnBranchSourceNotMappedAction(), "$/"); // Add on branch source not found

            Run();
            Assert.IsTrue(VerifyContents());

        }

        ///<summary>
        ///Scenario: branch,merge -> snapshot -> merge, rename, edit
        /// Repro steps for Bug 379846 (VC adapter: migrating "branch -> snapshot -> merge,rename,edit" fails with TFS check-in conflict)
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("merge undelete rename -> snapshot")]
        public void SnapshotMergeRenameEditTest()
        {
            // scenario
            // branch from source to target where source is cloaked
            // take the snapshot of target branch
            // item1 on source renamed,edited
            // item1 was merged to target
            // VC session converts merge,rename,edit to add,edit

            // cloak branch from path
            MappingPair mapping = new MappingPair(
                TestEnvironment.FirstSourceServerPath + SrcPathSeparator + "source",
                TestEnvironment.FirstTargetServerPath + TarPathSeparator + "source",
                true);
            TestEnvironment.AddMapping(mapping);


            MigrationItemStrings file1 = new MigrationItemStrings(
                string.Format("source{0}folder1{1}file1.txt", SrcPathSeparator, SrcPathSeparator),
                string.Format("source{0}folder2{1}file1.txt", SrcPathSeparator, SrcPathSeparator),
                TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings(
                string.Format("source{0}folder2{1}file2.txt", SrcPathSeparator, SrcPathSeparator),
                null, TestEnvironment, true);
            MigrationItemStrings file3 = new MigrationItemStrings(
                string.Format("source{0}folder1{1}file3.txt", SrcPathSeparator, SrcPathSeparator),
                string.Format("source{0}folder2{1}file3.txt", SrcPathSeparator, SrcPathSeparator),
                TestEnvironment, true);

            // branch
            SourceAdapter.AddFile(file1.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);
            SourceAdapter.AddFile(file3.LocalPath);
            
            int changesetId = SourceAdapter.BranchItem(source.ServerPath, target.ServerPath);
            int snapshotChangesetId = SourceAdapter.EditFile(file2.LocalPath);

            // pend edit on file1
            SourceWorkspace.PendEdit(file1.LocalPath);
            TestUtils.EditRandomFile(file1.LocalPath);
            // pend rename (file1 is moved to a different location)
            SourceWorkspace.PendRename(file1.LocalPath, file1.NewLocalPath);
            // pend edit on file2
            SourceWorkspace.PendEdit(file2.LocalPath);
            TestUtils.EditRandomFile(file2.LocalPath);
            // pend rename (file3 is moved to a different location)
            SourceWorkspace.PendEdit(file3.LocalPath);
            SourceWorkspace.PendRename(file3.LocalPath, file3.NewLocalPath);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), "rename, edit");

            
            // merge changes from source to target
            // - merge,rename,edit on file1
            // - merge,edit on file2
            // - merge,rename on file3
            SourceWorkspace.Merge(source.LocalPath, target.LocalPath,
                VersionSpec.ParseSingleSpec(changesetId.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);

            resolveConflictAcceptThiers();

            changesetId = SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), "merge,rename,edit");

            // snapshot 
            TestEnvironment.SnapshotStartPoints = new Dictionary<string, string>();
            TestEnvironment.SnapshotStartPoints.Add(TestEnvironment.SourceTeamProject, snapshotChangesetId.ToString());
            TestEnvironment.SnapshotBatchSize = 100;

            Run();

            // Resolve "Branch source path not found conflict" using "$/" scope.
            ConflictResolver conflictManager = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictManager.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be only 1 conflict");
            conflictManager.TryResolveConflict(conflicts[0], new VCChangeToAddOnBranchSourceNotMappedAction(), "$/"); // Add on branch source not found

            Run();
            Assert.IsTrue(VerifyContents());
        }

        ///<summary>
        ///Scenario: Edit B/foo; Checkin; Merege B to A
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Edit B/foo; Checkin; Merege B to A")]
        public void MergeEditTest()
        {
            int changesetId = AddBranch();

            SourceAdapter.EditFile(file.LocalPath);

            SourceWorkspace.Merge(source.LocalPath, target.LocalPath,
                VersionSpec.ParseSingleSpec(changesetId.ToString(),
                Environment.UserName), VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Undelete foo; Edit B/foo; Checkin; Merege B to A
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Undelete foo; Edit B/foo; Checkin; Merege B to A")]
        public void MergeEditUndeleteTest()
        {
            int changesetId = AddBranch();

            MergeDeletePendUndelete(file, changesetId);
            SourceAdapter.EditFile(file.LocalPath);

            SourceWorkspace.Merge(source.LocalPath, target.LocalPath,
                VersionSpec.ParseSingleSpec(changesetId.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);

            resolveConflictAcceptThiers();

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Undelete B/foo; Edit B/foo; Rename B/foo to B/bar; Checkin; Merge B to A
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Undelete B/foo; Edit B/foo; Rename B/foo to B/bar; Checkin; Merge B to A")]
        public void RenameEditUndeleteMergeTest()
        {
            int changesetId = AddBranch();

            MergeDeletePendUndelete(file, changesetId);
            SourceWorkspace.PendRename(file.ServerPath, file.NewServerPath);
            SourceAdapter.EditFile(file.NewLocalPath);

            SourceWorkspace.Merge(source.LocalPath, target.LocalPath,
                VersionSpec.ParseSingleSpec(changesetId.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);

            resolveConflictAcceptThiers();

            if (TestEnvironment.SourceTFSVersion == TFSVersionEnum.TFS2010)
            {
                Run();
                Assert.IsTrue(VerifyContents());
            }
            else
            {
                RunAndValidate();
            }
        }

        ///<summary>
        ///Scenario: Branch A to B; Edit B/Foo
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Branch A to B; Edit B/Foo")]
        public void BranchEditTest()
        {
            file = new MigrationItemStrings(source.Name + "file.txt", target.Name + "file.txt", TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);

            SourceWorkspace.PendBranch(source.ServerPath, target.ServerPath, VersionSpec.Latest);
            TestUtils.EditRandomFile(file.NewLocalPath);
            SourceWorkspace.PendEdit(file.NewLocalPath);

            RunAndValidate();
        }
    }
}
