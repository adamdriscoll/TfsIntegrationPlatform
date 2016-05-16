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
        private MigrationItemStrings m_source;
        private MigrationItemStrings m_target;
        private MigrationItemStrings m_file;

        [TestInitialize()]
        public override void Initialize()
        {
            base.Initialize();

            m_source = new MigrationItemStrings("source" + SrcPathSeparator, null, TestEnvironment, true);
            m_target = new MigrationItemStrings("target" + SrcPathSeparator, null, TestEnvironment, true);
            m_file = new MigrationItemStrings("file.txt", null, TestEnvironment, true);
        }

        [TestCleanup()]
        public override void Cleanup()
        {
            m_source = null;
            m_target = null;
            m_file = null;

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

            SourceWorkspace.Merge(m_source.LocalPath, m_target.LocalPath,
                VersionSpec.ParseSingleSpec(changesetId.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);

            resolveConflictAcceptThiers();
        }

        private void MergeDeletePendUndelete(MigrationItemStrings item, int changesetId)
        {
            int deletionId = SourceTfsClient.GetChangeset(SourceAdapter.DeleteItem(item.ServerPath)).Changes[0].Item.DeletionId;

            SourceWorkspace.Merge(m_source.LocalPath, m_target.LocalPath,
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
            SourceWorkspace.PendDelete(m_file.ServerPath);
        }

        #endregion

        #region Testcase helper methods

        private int AddBranch()
        {
            m_file = new MigrationItemStrings(m_source.Name + "file.txt", m_source.Name + "renamed-file.txt", TestEnvironment, true);

            SourceAdapter.AddFile(m_file.LocalPath);
            return SourceAdapter.BranchItem(m_source, m_target);
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

            SourceWorkspace.Merge(m_source.LocalPath, m_target.LocalPath,
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
            m_file = new MigrationItemStrings(m_source.Name + "file.txt", m_target.Name + "file.txt", TestEnvironment, true);

            SourceAdapter.AddFile(m_file.LocalPath);

            SourceWorkspace.PendBranch(m_source.ServerPath, m_target.ServerPath, VersionSpec.Latest);
            SourceWorkspace.PendDelete(m_file.NewLocalPath);

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
            m_file = new MigrationItemStrings(m_source.Name + "file.txt", m_target.Name + "file.txt", TestEnvironment, true);

            SourceAdapter.AddFile(m_file.LocalPath);

            SourceWorkspace.PendBranch(m_source.ServerPath, m_target.ServerPath, VersionSpec.Latest);
            SourceAdapter.DeleteItem(m_target.ServerPath);

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
            m_file = new MigrationItemStrings("folder/subfolder/file.txt", null, TestEnvironment, true);
            MigrationItemStrings folder = new MigrationItemStrings("folder", null, TestEnvironment, true);

            SourceAdapter.AddFile(m_file.LocalPath);

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
            m_file = new MigrationItemStrings("file.txt", "newName.txt", TestEnvironment, true);

            SourceAdapter.AddFile(m_file.LocalPath);

            PendRenameEdit(m_file);

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
            m_file = new MigrationItemStrings("folder/file.txt", "newName/file.txt", TestEnvironment, true);
            MigrationItemStrings folder = new MigrationItemStrings("folder", "newName", TestEnvironment, true);

            SourceAdapter.AddFile(m_file.LocalPath);

            TestUtils.EditRandomFile(m_file.LocalPath);

            SourceWorkspace.PendEdit(m_file.LocalPath);
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
            m_file = new MigrationItemStrings(folder.Name + "file.txt", folder.NewName + "file.txt", TestEnvironment, true);

            SourceAdapter.AddFile(m_file.LocalPath);

            PendRenameDelete(folder, m_file);

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
            m_file = new MigrationItemStrings("file.txt", null, TestEnvironment, true);

            EditUndelete(m_file);

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
            m_file = new MigrationItemStrings("folder/file.txt", null, TestEnvironment, true);
            MigrationItemStrings file1 = new MigrationItemStrings("folder/file1.txt", null, TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings("folder/file2.txt", null, TestEnvironment, true);
            MigrationItemStrings file3 = new MigrationItemStrings("folder/file3.txt", null, TestEnvironment, true);

            SourceAdapter.AddFile(m_file.LocalPath);
            SourceAdapter.AddFile(file1.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);
            SourceAdapter.AddFile(file3.LocalPath);

            SourceWorkspace.PendDelete(m_file.LocalPath);
            SourceWorkspace.PendDelete(file1.LocalPath);
            SourceWorkspace.PendDelete(file2.LocalPath);
            SourceWorkspace.PendDelete(file3.LocalPath);

            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), "Multiple deletes.");

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Multiple Adds, Edits, some files are cloaked
        ///Expected Result: Successful migration
        ///</summary>
        [TestMethod(), Priority(2), Owner("peigu")]
        [Description("Add, Edit several files in the same changeset with one file being cloaked")]
        public void PartialCloakedChangesetTest()
        {
            MigrationItemStrings file1 = new MigrationItemStrings("folder/file1.txt", null, TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings("cloakfolder/file2.txt", null, TestEnvironment, true);

            MappingPair mapping = new MappingPair(TestEnvironment.FirstSourceServerPath + SrcPathSeparator + "cloakfolder",
                                                  TestEnvironment.FirstTargetServerPath + TarPathSeparator + "cloakfolder",
                                                  true);
            TestEnvironment.AddMapping(mapping);

            SourceAdapter.AddFile(file1.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);

            TestUtils.EditRandomFile(file1.LocalPath);
            TestUtils.EditRandomFile(file2.LocalPath);
            SourceWorkspace.PendEdit(file1.LocalPath);
            SourceWorkspace.PendEdit(file2.LocalPath);

            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), "Multiple edits.");

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
            m_file = new MigrationItemStrings(folder.Name + "file.txt", null, TestEnvironment, true);

            SourceAdapter.AddFile(m_file.LocalPath);

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
            m_file = new MigrationItemStrings("file.txt", "newFile.txt", TestEnvironment, true);

            SourceAdapter.AddFile(m_file.LocalPath);
            PendUndeleteRename(m_file);

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
            m_file = new MigrationItemStrings("file.txt", "newFile.txt", TestEnvironment, true);

            EditUndelete(m_file);
            SourceWorkspace.PendRename(m_file.LocalPath, m_file.NewLocalPath);

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
            m_file = new MigrationItemStrings(subfolder.Name + "file.txt", subfolder.NewName + "file.txt", TestEnvironment, true);

            SourceAdapter.AddFile(m_file.LocalPath);

            PendUndelete(folder);
            PendRenameDelete(subfolder, m_file);

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
            m_file = new MigrationItemStrings(m_source.Name + "file.txt", m_target.Name + "file.txt", TestEnvironment, true);

            SourceAdapter.AddFile(m_file.LocalPath);

            SourceWorkspace.Merge(m_file.ServerPath, m_file.NewServerPath, VersionSpec.Latest, VersionSpec.Latest);

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
            m_file = new MigrationItemStrings(m_source.Name + "file.txt", m_target.Name + "file.txt", TestEnvironment, true);

            SourceAdapter.AddFile(m_file.LocalPath);

            SourceWorkspace.Merge(m_source.ServerPath, m_target.ServerPath, VersionSpec.Latest, VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);
            int mergeChangeset = SourceAdapter.DeleteItem(m_file.NewLocalPath);

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
            m_file = new MigrationItemStrings(m_source.Name + "file.txt", m_target.Name + "file.txt", TestEnvironment, true);

            SourceAdapter.AddFile(m_file.LocalPath);

            SourceWorkspace.Merge(m_file.ServerPath, m_file.NewServerPath, VersionSpec.Latest, VersionSpec.Latest);

            int mergeChangeset = SourceAdapter.EditFile(m_file.NewLocalPath);

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
            MigrationItemStrings folder = new MigrationItemStrings(m_source.Name + "folder", m_target.Name + "folder", TestEnvironment, true);
            m_file = new MigrationItemStrings(m_source.Name + "folder/file.txt", m_target.Name + "folder/file.txt", TestEnvironment, true);

            SourceAdapter.AddFolder(folder.LocalPath);
            SourceWorkspace.Merge(folder.ServerPath, folder.NewServerPath, VersionSpec.Latest, VersionSpec.Latest);

            SourceAdapter.AddFile(m_file.LocalPath);
            SourceWorkspace.Merge(m_file.ServerPath, m_file.NewServerPath, VersionSpec.Latest, VersionSpec.Latest);
            int mergeChangeset = SourceAdapter.EditFile(m_file.NewLocalPath);

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
            MigrationItemStrings file1 = new MigrationItemStrings(m_source.Name + "file1.txt", m_source.Name + "renamed-file1.txt", TestEnvironment, true);
            SourceAdapter.AddFile(file1.LocalPath);

            MigrationItemStrings file2 = new MigrationItemStrings(m_source.Name + "file2.txt", m_source.Name + "renamed-file2.txt", TestEnvironment, true);
            SourceAdapter.AddFile(file2.LocalPath);

            int changesetId = AddBranch();

            SourceAdapter.RenameItem(m_file.ServerPath, m_file.NewServerPath, AddComment);
            SourceAdapter.RenameItem(file1.ServerPath, file1.NewServerPath, AddComment);
            SourceAdapter.RenameItem(file2.ServerPath, file2.NewServerPath, AddComment);

            SourceWorkspace.Merge(m_source.LocalPath, m_target.LocalPath,
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
            MigrationItemStrings sourceFolder = new MigrationItemStrings(m_source.Name + "folder/", m_source.Name + "renamed-folder/", TestEnvironment, true);
            m_file = new MigrationItemStrings(sourceFolder.Name + "file.txt", sourceFolder.NewName + "file.txt", TestEnvironment, true);

            int changesetId = SourceAdapter.AddFile(m_file.LocalPath);
            SourceAdapter.BranchItem(m_source, m_target);

            PendRenameDelete(sourceFolder, m_file);

            CheckinMergeResolve(changesetId);

            if (TestEnvironment.SourceEndPoint.AdapterType == AdapterType.TFS2010VC)
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
            MigrationItemStrings sourceFolder = new MigrationItemStrings(m_source.Name + "folder/", m_source.Name + "renamed-folder/", TestEnvironment, true);
            m_file = new MigrationItemStrings(sourceFolder.Name + "file.txt", sourceFolder.NewName + "file.txt", TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings(sourceFolder.Name + "file2.txt", sourceFolder.NewName + "file2.txt", TestEnvironment, true);
            MigrationItemStrings sourceFolder2 = new MigrationItemStrings(m_source.Name + "folder2/", m_source.Name + "renamed-folder2/", TestEnvironment, true);

            int changesetId = SourceAdapter.AddFile(m_file.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);
            SourceAdapter.AddFolder(sourceFolder2.LocalPath);
            SourceAdapter.BranchItem(m_source, m_target);

            PendRenameDelete(sourceFolder, m_file);
            SourceWorkspace.PendRename(sourceFolder2.LocalPath, sourceFolder2.NewLocalPath);

            CheckinMergeResolve(changesetId);

            if (TestEnvironment.SourceEndPoint.AdapterType == AdapterType.TFS2010VC)
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

            PendRenameEdit(m_file);

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

            PendUndeleteRename(m_file);
            SourceWorkspace.Merge(m_source.LocalPath, m_target.LocalPath,
                VersionSpec.ParseSingleSpec(changesetId.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);

            CheckinMergeResolve(changesetId);

            if (TestEnvironment.SourceEndPoint.AdapterType == AdapterType.TFS2010VC)
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

            SourceAdapter.DeleteItem(m_file.ServerPath);
            SourceWorkspace.Merge(m_source.LocalPath, m_target.LocalPath,
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
            MigrationItemStrings folder = new MigrationItemStrings(m_source.Name + "folder/", null, TestEnvironment, true);
            m_file = new MigrationItemStrings(folder.Name + "file.txt", null, TestEnvironment, true);

            SourceAdapter.AddFile(m_file.LocalPath);

            int changesetId = SourceAdapter.BranchItem(m_source, m_target);

            MergeDeletePendUndelete(folder, changesetId);

            SourceWorkspace.PendDelete(m_file.ServerPath);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), MultiActionComment);

            SourceWorkspace.Merge(m_source.LocalPath, m_target.LocalPath,
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

            PendMergeUndelete(m_file, changesetId);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: a.	A rename away caused by the merge ; b.	Add to the same namespace
        ///Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("peigu")]
        [Description("a.	A rename away caused by the merge ; b.	Add to the same namespace")]
        public void MergeAddSourceRename()
        {
            MigrationItemStrings folder = new MigrationItemStrings(m_source.Name + "folder", m_source.Name + "folder-rename", TestEnvironment, true);

            SourceAdapter.AddFolder(folder.LocalPath);
            int changesetId = SourceWorkspace.PendBranch(m_source.ServerPath, m_target.ServerPath, ChangesetVersionSpec.Latest);

            SourceWorkspace.PendRename(folder.LocalPath, folder.NewLocalPath);

            CheckinMergeResolve(changesetId);

            SourceAdapter.AddFolder(folder.LocalPath.Replace("source", "target") + "\\sub"); // Pend an add on the source rename item

            
            RunAndValidate();
        }

        /// <summary>
        /// Add to the rename away place.
        /// </summary>
        [TestMethod(), Priority(2), Owner("peigu")]
        [Description(" Add to the rename away place")]
        public void AddSourceRename()
        {
            MigrationItemStrings file = new MigrationItemStrings("file.txt", "file-renamed.txt", TestEnvironment, true);
            SourceAdapter.AddFile(file.LocalPath);
            SourceWorkspace.Get();
            SourceWorkspace.PendRename(file.ServerPath, file.NewServerPath);
            TestUtils.CreateRandomFile(file.LocalPath, 10);
            SourceWorkspace.PendAdd(file.LocalPath);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), "AddSourceRename");
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

            int changesetId =  SourceAdapter.BranchItem(m_source, m_target);

            // merge edit
            // merge delete
            SourceAdapter.EditFile(file1.LocalPath);
            int deletionId = SourceTfsClient.GetChangeset(SourceAdapter.DeleteItem(file1.ServerPath)).Changes[0].Item.DeletionId;

            SourceWorkspace.Merge(m_source.LocalPath, m_target.LocalPath,
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
            SourceWorkspace.Merge(m_source.LocalPath, m_target.LocalPath,
                VersionSpec.ParseSingleSpec(changesetId.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);

            resolveConflictAcceptThiers();
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), "merge undelete rename");

            // snapshot 
            TestEnvironment.SnapshotStartPoints = new Dictionary<string, string>();
            TestEnvironment.SnapshotStartPoints.Add(TestEnvironment.SourceEndPoint.TeamProject, snapshotChangesetId.ToString());
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
            
            int changesetId = SourceAdapter.BranchItem(m_source, m_target);
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
            SourceWorkspace.Merge(m_source.LocalPath, m_target.LocalPath,
                VersionSpec.ParseSingleSpec(changesetId.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);

            resolveConflictAcceptThiers();

            changesetId = SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), "merge,rename,edit");

            // snapshot 
            TestEnvironment.SnapshotStartPoints = new Dictionary<string, string>();
            TestEnvironment.SnapshotStartPoints.Add(TestEnvironment.SourceEndPoint.TeamProject, snapshotChangesetId.ToString());
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

            SourceAdapter.EditFile(m_file.LocalPath);

            SourceWorkspace.Merge(m_source.LocalPath, m_target.LocalPath,
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

            MergeDeletePendUndelete(m_file, changesetId);
            SourceAdapter.EditFile(m_file.LocalPath);

            SourceWorkspace.Merge(m_source.LocalPath, m_target.LocalPath,
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

            MergeDeletePendUndelete(m_file, changesetId);
            SourceWorkspace.PendRename(m_file.ServerPath, m_file.NewServerPath);
            SourceAdapter.EditFile(m_file.NewLocalPath);

            SourceWorkspace.Merge(m_source.LocalPath, m_target.LocalPath,
                VersionSpec.ParseSingleSpec(changesetId.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);

            resolveConflictAcceptThiers();

            if (TestEnvironment.SourceEndPoint.AdapterType == AdapterType.TFS2010VC)
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
            m_file = new MigrationItemStrings(m_source.Name + "file.txt", m_target.Name + "file.txt", TestEnvironment, true);

            SourceAdapter.AddFile(m_file.LocalPath);

            SourceWorkspace.PendBranch(m_source.ServerPath, m_target.ServerPath, VersionSpec.Latest);
            TestUtils.EditRandomFile(m_file.NewLocalPath);
            SourceWorkspace.PendEdit(m_file.NewLocalPath);

            RunAndValidate();
        }
    }
}