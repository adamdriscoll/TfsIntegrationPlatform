// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTestLibrary;
using MigrationTestLibrary.Conflict;
using ServerDiff;
using Tfs2008VCTCAdapter;

namespace TfsVCTest
{
    [TestClass]
    public class MergeTest : TfsVCTestCaseBase
    {
        /// <summary>
        /// Perform a merge specifying the start and end changeset
        /// </summary>
        /// <param name="branch">The source server pah is the server namd the target path is the NewServerPath</param>
        /// <param name="mergeFrom">changeset to start merge from</param>
        /// <param name="mergeTo">changeset to merge up to</param>
        private void MergeTfsItem(MigrationItemStrings branch, int mergeFrom, int mergeTo)
        {
            string mergeComment = "Migration test merge";

            SourceWorkspace.Merge(branch.ServerPath, branch.NewServerPath,
                VersionSpec.ParseSingleSpec(mergeFrom.ToString(), Environment.UserName),
                VersionSpec.ParseSingleSpec(mergeTo.ToString(), Environment.UserName), LockLevel.None, RecursionType.Full, MergeOptions.None);

            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), mergeComment);
        }

        /// <summary>
        /// Checkin a merge conflict resolved with the specified resolution then verify its migration
        /// </summary>
        /// <param name="resolution">The resolution to test</param>
        private void ResolutionScenario(Resolution resolution)
        {
            MigrationItemStrings source = new MigrationItemStrings("source", null, TestEnvironment, true);
            MigrationItemStrings target = new MigrationItemStrings("target", null, TestEnvironment, true);
            MigrationItemStrings file1 = new MigrationItemStrings("source/File.txt", null, TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings("source/File2.txt", "target/File2.txt", TestEnvironment, true);

            int changeid = SourceAdapter.AddFile(file1.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);

            SourceAdapter.BranchItem(source, target);

            SourceAdapter.EditFile(file2.LocalPath);
            SourceAdapter.EditFile(file2.NewLocalPath);

            SourceWorkspace.Merge(source.ServerPath, target.ServerPath,
                VersionSpec.ParseSingleSpec(changeid.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);
            ResolveConflicts(resolution);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), MergeComment);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Migrate a merge with all the possible actions on different files
        ///Expected Result: Server Histories are the same
        ///</summary>
        [TestMethod(), Priority(1), Owner("curtisp")]
        [Description("Migrate a merge with all the possible actions on different files")]
        public void MergeCrudTest()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source/", "target/", TestEnvironment, true);
            MigrationItemStrings addedFile = new MigrationItemStrings("source/Addedfile.txt", null, TestEnvironment, true);
            MigrationItemStrings editFile = new MigrationItemStrings("source/Editedfile.txt", null, TestEnvironment, true);
            MigrationItemStrings deleteFile = new MigrationItemStrings("source/DeletedFile.txt", null, TestEnvironment, true);
            MigrationItemStrings undeleteFile = new MigrationItemStrings("source/UndeletedFile.txt", null, TestEnvironment, true);
            MigrationItemStrings branchedFile = new MigrationItemStrings("source/folder/branchedFile.txt", "source/folder2/branchFile.txt", TestEnvironment, true);
            MigrationItemStrings mergeFile = new MigrationItemStrings("source/folder/mergeFile.txt", "source/folder2/mergeFile.txt", TestEnvironment, true);

            #region Setup before the branch
            //All the files except for the Added file should exist before the branch
            SourceAdapter.AddFile(editFile.LocalPath);
            SourceAdapter.AddFile(deleteFile.LocalPath);
            SourceAdapter.AddFile(undeleteFile.LocalPath);
            SourceAdapter.AddFile(branchedFile.LocalPath);
            SourceAdapter.AddFile(mergeFile.LocalPath);

            int deletetionChangeset = SourceAdapter.DeleteItem(undeleteFile.ServerPath);
            int mergeFileChangeset = SourceAdapter.BranchItem(mergeFile);
            SourceAdapter.EditFile(mergeFile.LocalPath);
            #endregion

            int branchChangeset = SourceAdapter.BranchItem(branch);

            #region Setup after Branch operation
            SourceAdapter.AddFile(addedFile.LocalPath);
            SourceAdapter.EditFile(editFile.LocalPath);
            SourceAdapter.DeleteItem(deleteFile.ServerPath);
            SourceAdapter.UndeleteFile(undeleteFile.ServerPath, deletetionChangeset);
            SourceAdapter.BranchItem(branchedFile);
            SourceAdapter.MergeItem(mergeFile, mergeFileChangeset);
            #endregion Setup after Branch operation

            //The big merge
            SourceAdapter.MergeItem(branch, branchChangeset);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Migrate an out of scope merge with all the possible actions on different files
        ///Expected Result: Merge is skipped
        ///</summary>
        [TestMethod(), Priority(1), Owner("peigu")]
        [Description("Migrate an out of scope merge with all the possible actions on different files")]
        public void MergeCrudOutOfScopeTest()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source/", "target/", TestEnvironment, true);
            MigrationItemStrings addedFile = new MigrationItemStrings("source/Addedfile.txt", null, TestEnvironment, true);
            MigrationItemStrings editFile = new MigrationItemStrings("source/Editedfile.txt", null, TestEnvironment, true);
            MigrationItemStrings deleteFile = new MigrationItemStrings("source/DeletedFile.txt", null, TestEnvironment, true);
            MigrationItemStrings undeleteFile = new MigrationItemStrings("source/UndeletedFile.txt", null, TestEnvironment, true);
            MigrationItemStrings branchedFile = new MigrationItemStrings("source/folder/branchedFile.txt", "source/folder2/branchFile.txt", TestEnvironment, true);
            MigrationItemStrings mergeFile = new MigrationItemStrings("source/folder/mergeFile.txt", "source/folder2/mergeFile.txt", TestEnvironment, true);

            #region Setup before the branch
            //All the files except for the Added file should exist before the branch
            SourceAdapter.AddFile(editFile.LocalPath);
            SourceAdapter.AddFile(deleteFile.LocalPath);
            SourceAdapter.AddFile(undeleteFile.LocalPath);
            SourceAdapter.AddFile(branchedFile.LocalPath);
            SourceAdapter.AddFile(mergeFile.LocalPath);

            int deletetionChangeset = SourceAdapter.DeleteItem(undeleteFile.ServerPath);
            int mergeFileChangeset = SourceAdapter.BranchItem(mergeFile);
            SourceAdapter.EditFile(mergeFile.LocalPath);
            #endregion

            int branchChangeset = SourceAdapter.BranchItem(branch);

            #region Setup after Branch operation
            SourceAdapter.AddFile(addedFile.LocalPath);
            SourceAdapter.EditFile(editFile.LocalPath);
            SourceAdapter.DeleteItem(deleteFile.ServerPath);
            SourceAdapter.UndeleteFile(undeleteFile.ServerPath, deletetionChangeset);
            SourceAdapter.BranchItem(branchedFile);
            SourceAdapter.MergeItem(mergeFile, mergeFileChangeset);
            #endregion Setup after Branch operation

            //The big merge
            SourceAdapter.MergeItem(branch, branchChangeset);

            // Add the mapping scope
            MappingPair rootMapping = TestEnvironment.Mappings[0];
            TestEnvironment.Mappings.Clear();
            TestEnvironment.Mappings.Add(new MappingPair(rootMapping.SourcePath, rootMapping.TargetPath, false, null, null,
                rootMapping.SourcePath + '/' + "target/", rootMapping.TargetPath + '/' + "target/"));

            RunAndValidate(true, true);
        }

        #region help function for Batch merge tests
        private void pendAdd(string localFile)
        {
            TestUtils.CreateRandomFile(localFile, 10);
            SourceWorkspace.PendAdd(localFile);
        }

        private void pendEdit(string localPath)
        {
            SourceWorkspace.Get(VersionSpec.Latest, GetOptions.Overwrite);
            TestUtils.EditRandomFile(localPath);
            SourceWorkspace.PendEdit(localPath);
        }
        #endregion

        ///<summary>
        ///Scenario: Migrate a merge delete
        ///Expected Result: Delete Migrates, merge is skipped
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Migrate a merge delete")]
        public void MergeDeleteTest()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source/", "target/", TestEnvironment, true);
            MigrationItemStrings deleteFile = new MigrationItemStrings("source/DeletedFile.txt", null, TestEnvironment, true);

            SourceAdapter.AddFile(deleteFile.LocalPath);
            int branchChangeset = SourceAdapter.BranchItem(branch);

            SourceAdapter.DeleteItem(deleteFile.ServerPath);
            SourceAdapter.MergeItem(branch, branchChangeset);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Migrate a merge with adds in the target
        ///Expected Result: Extra files in the target are not removed.
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Migrate a merge with adds in the target")]
        public void MergeExtraTargetItemTest()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source/", "target/", TestEnvironment, true);
            MigrationItemStrings mergeFile = new MigrationItemStrings(TestUtils.URIPathCombine(branch.Name, "mergeFile.txt"), null, TestEnvironment, true);
            MigrationItemStrings targetFile = new MigrationItemStrings(TestUtils.URIPathCombine(branch.NewName, "targetFile.txt"), null, TestEnvironment, true);

            SourceAdapter.AddFile(mergeFile.LocalPath);
            int branchChangeset = SourceAdapter.BranchItem(branch);

            SourceAdapter.EditFile(mergeFile.LocalPath);

            SourceAdapter.AddFile(targetFile.LocalPath);

            SourceAdapter.MergeItem(branch, branchChangeset);

            RunAndValidate();
        }


        ///<summary>
        ///Scenario: Migrate a baseless merge
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(1), Owner("curtisp")]
        [Description("Migrate a baseless merge")]
        public void MergeBaselessTest()
        {
            MigrationItemStrings source = new MigrationItemStrings("source", null, TestEnvironment, true);
            MigrationItemStrings target = new MigrationItemStrings("target", null, TestEnvironment, true);
            MigrationItemStrings file1 = new MigrationItemStrings("source/File.txt", null, TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings("source/File2.txt", null, TestEnvironment, true);

            SourceAdapter.AddFolder(source.LocalPath);
            SourceAdapter.AddFolder(target.LocalPath);
            int changeid = SourceAdapter.AddFile(file1.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);

            SourceAdapter.EditFile(file2.LocalPath);

            SourceWorkspace.Merge(source.ServerPath, target.ServerPath,
                VersionSpec.ParseSingleSpec(changeid.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.Baseless);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), MergeComment);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Migrate a reverse merge
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Migrate a reverse merge")]
        public void ReverseMergeTest()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source/", "target/", TestEnvironment, true);
            MigrationItemStrings file = new MigrationItemStrings("source/file.txt", "target/file.txt", TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);
            int branchChangeset = SourceAdapter.BranchItem(branch);
            SourceAdapter.EditFile(file.NewLocalPath);

            SourceWorkspace.Merge(branch.NewServerPath, branch.ServerPath,
                VersionSpec.ParseSingleSpec(branchChangeset.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), MergeComment);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Migrate 3 merges that each conttain part of the change history
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Migrate 3 merges that each conttain part of the change history")]
        public void PartialMergeTest()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source/", "target/", TestEnvironment, true);
            MigrationItemStrings file = new MigrationItemStrings("source/file.txt", "target/file.txt", TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings("file2.txt", "target/file2.txt", TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);
            int branchChangeset = SourceAdapter.BranchItem(branch);

            SourceAdapter.EditFile(file.LocalPath);
            SourceAdapter.EditFile(file.LocalPath);
            SourceAdapter.EditFile(m_extraFile.LocalPath);
            int mergeOne = SourceAdapter.EditFile(file2.LocalPath);
            SourceAdapter.EditFile(file.LocalPath);
            SourceAdapter.EditFile(m_extraFile.LocalPath);
            SourceAdapter.EditFile(file.LocalPath);
            int mergeTwo = SourceAdapter.EditFile(file2.LocalPath);
            SourceAdapter.EditFile(file.LocalPath);
            SourceAdapter.EditFile(file2.LocalPath);
            SourceAdapter.EditFile(m_extraFile.LocalPath);

            SourceAdapter.MergeItem(branch, branchChangeset, mergeOne);
            SourceAdapter.MergeItem(branch, branchChangeset, mergeTwo);
            SourceAdapter.MergeItem(branch, branchChangeset, SourceTfsClient.GetLatestChangesetId());

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Migrate a 3 reverse merges that each conttain part of the change history
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(3), Owner("curtisp")]
        [Description("Migrate 3 reverse merges that each conttain part of the change history")]
        public void ReversePartialMergeTest()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source/", "target/", TestEnvironment, true);
            MigrationItemStrings file = new MigrationItemStrings("source/file.txt", "target/file.txt", TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings("source/file2.txt", "target/file2.txt", TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);

            int branchChangeset = SourceAdapter.BranchItem(branch);

            SourceAdapter.EditFile(file.NewLocalPath);
            SourceAdapter.EditFile(file.NewLocalPath);
            SourceAdapter.EditFile(m_extraFile.LocalPath);
            int mergeOne = SourceAdapter.EditFile(file2.NewLocalPath);
            SourceAdapter.EditFile(file.NewLocalPath);
            SourceAdapter.EditFile(m_extraFile.LocalPath);
            SourceAdapter.EditFile(file.NewLocalPath);
            int mergeTwo = SourceAdapter.EditFile(file2.NewLocalPath);
            SourceAdapter.EditFile(file.NewLocalPath);
            SourceAdapter.EditFile(file2.NewLocalPath);
            SourceAdapter.EditFile(m_extraFile.LocalPath);

            string mergeComment = "Migration test merge";
            SourceWorkspace.Merge(branch.NewServerPath, branch.ServerPath,
                VersionSpec.ParseSingleSpec(branchChangeset.ToString(), Environment.UserName),
                VersionSpec.ParseSingleSpec(mergeOne.ToString(), Environment.UserName), LockLevel.None, RecursionType.Full, MergeOptions.None);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), mergeComment);

            SourceWorkspace.Merge(branch.NewServerPath, branch.ServerPath,
                VersionSpec.ParseSingleSpec(branchChangeset.ToString(), Environment.UserName),
                VersionSpec.ParseSingleSpec(mergeTwo.ToString(), Environment.UserName), LockLevel.None, RecursionType.Full, MergeOptions.None);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), mergeComment);

            SourceWorkspace.Merge(branch.NewServerPath, branch.ServerPath,
                VersionSpec.ParseSingleSpec(branchChangeset.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), mergeComment);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Migrate a non-recursive merge
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Migrate a non-recursive merge")]
        public void MergeNonRecursiveTest()
        {
            MigrationItemStrings source = new MigrationItemStrings("source", "source2", TestEnvironment, true);
            MigrationItemStrings target = new MigrationItemStrings("target", null, TestEnvironment, true);
            MigrationItemStrings file = new MigrationItemStrings("source/file.txt", "target/file.txt", TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);
            int branchChangeset = SourceAdapter.BranchItem(source, target);

            SourceAdapter.EditFile(m_extraFile.LocalPath);
            SourceAdapter.EditFile(file.LocalPath);
            SourceAdapter.RenameItem(source.ServerPath, source.NewServerPath, "Rename source");

            SourceWorkspace.Merge(source.NewServerPath, target.ServerPath,
                VersionSpec.ParseSingleSpec(branchChangeset.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.None, MergeOptions.ForceMerge);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), MergeComment);

            SourceAdapter.EditFile(m_extraFile.LocalPath);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Migrate a merge where conflicts were resolved as AcceptYours
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Migrate a merge where conflicts were resolved as AcceptYours")]
        public void MergeAcceptYoursTest()
        {
            ResolutionScenario(Resolution.AcceptYours);
        }

        ///<summary>
        ///Scenario: Migrate a merge where conflicts were resolved as AcceptTheirs
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Migrate a merge where conflicts were resolved as AcceptTheirs")]
        public void MergeAcceptTheirsTest()
        {
            ResolutionScenario(Resolution.AcceptTheirs);
        }

        ///<summary>
        ///Scenario: Migrate a merge where conflicts were resolved as AcceptMerge
        ///Expected Result: Server histories are the same
        ///</summary>
        [Ignore]//This test pops up a dialog that require user intervention.  Should only be run once per release. 
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Migrate a merge where conflicts were resolved as AcceptMerge")]
        public void MergeAcceptMergeTest()
        {
            ResolutionScenario(Resolution.AcceptMerge);
        }

        ///<summary>
        ///Scenario: Migrate a merge where there were no changes to merge
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Migrate a merge where there were no changes to merge")]
        public void MergeForceTest()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source", "target", TestEnvironment, true);
            MigrationItemStrings file = new MigrationItemStrings("source/file.txt", null, TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);
            int branchChangeset = SourceAdapter.BranchItem(branch);

            SourceAdapter.EditFile(m_extraFile.LocalPath);
            SourceAdapter.EditFile(file.LocalPath);


            SourceWorkspace.Merge(branch.ServerPath, branch.NewServerPath,
                VersionSpec.ParseSingleSpec(branchChangeset.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), MergeComment);

            SourceWorkspace.Merge(branch.ServerPath, branch.NewServerPath,
                VersionSpec.ParseSingleSpec(branchChangeset.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.ForceMerge);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), MergeComment);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Migrate a merge where files were moved to a subfolder
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Migrate a merge where files were moved to a subfolder")]
        public void MergeMovesTest()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source/", "target/", TestEnvironment, true);
            MigrationItemStrings file = new MigrationItemStrings("source/file.txt", "source/sub/file.txt", TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings("source/file2.txt", "source/sub/file2.txt", TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);

            int branchChangeset = SourceAdapter.BranchItem(branch);

            SourceAdapter.EditFile(file.LocalPath);
            SourceAdapter.EditFile(file.LocalPath);
            SourceAdapter.EditFile(m_extraFile.LocalPath);

            SourceAdapter.RenameItem(file.ServerPath, file.NewServerPath);
            SourceAdapter.RenameItem(file2.ServerPath, file2.NewServerPath);

            SourceWorkspace.Merge(branch.ServerPath, branch.NewServerPath,
                VersionSpec.ParseSingleSpec(branchChangeset.ToString(),
                Environment.UserName), VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);
            ResolveConflicts(Resolution.AcceptTheirs);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), MergeComment);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Migrate a merge of a cyclical rename
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Migrate a merge of a cyclical rename")]
        public void MergeCyclicRenameTest()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source/", "target/", TestEnvironment, true);

            MigrationItemStrings folder1 = new MigrationItemStrings("source/folder1/", null, TestEnvironment, true);
            MigrationItemStrings folder2 = new MigrationItemStrings("source/folder2/", null, TestEnvironment, true);
            MigrationItemStrings temp = new MigrationItemStrings("source/temp/", null, TestEnvironment, true);

            MigrationItemStrings file1 = new MigrationItemStrings("source/folder1/file1.txt", null, TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings("source/folder2/file2.txt", null, TestEnvironment, true);

            SourceAdapter.AddFolder(folder1.LocalPath);
            SourceAdapter.AddFolder(folder2.LocalPath);

            SourceAdapter.AddFile(file1.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);

            int branchChangeset = SourceAdapter.BranchItem(branch);

            SourceAdapter.EditFile(m_extraFile.LocalPath);

            SourceWorkspace.PendRename(folder1.ServerPath, temp.ServerPath);
            SourceWorkspace.PendRename(folder2.ServerPath, folder1.ServerPath);
            SourceWorkspace.PendRename(temp.ServerPath, folder2.ServerPath);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), RenameComment);

            SourceWorkspace.Merge(branch.ServerPath, branch.NewServerPath,
                VersionSpec.ParseSingleSpec(branchChangeset.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.AlwaysAcceptMine);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), MergeComment);

            Run();

            if (TestEnvironment.SourceEndPoint.AdapterType == AdapterType.TFS2008VC)
            {
                VerifyHistory(0, 0);
                // For orcas, there should be no difference. 
            }
            else
            {
                VerifyHistory(1, 1);
                // Need more comparison here. 
                // On dev 10 server, rename becomes sourceDelete+targetbranch. So the rename-from-name will exist in the original place as a deleted item. 
                // The merge will then merge the item as a Merge|Delete. We will skip the change in this case. 
            }
        }

        ///<summary>
        ///Scenario: Migrate a merge of a changeset with a cyclical rename of files in a folder that's also part of a cyclic rename
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("billbar")]
        [Description("Migrate a merge of a changeset with a cyclical rename of files in a folder that's also part of a cyclic rename")]
        public void MergeTwoCyclicRenamesTest()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source/", "target/", TestEnvironment, true);

            MigrationItemStrings folder1 = new MigrationItemStrings("source/folder1/", null, TestEnvironment, true);
            MigrationItemStrings folder2 = new MigrationItemStrings("source/folder2/", null, TestEnvironment, true);
            MigrationItemStrings folder3 = new MigrationItemStrings("source/folder3/", null, TestEnvironment, true);
            MigrationItemStrings temp = new MigrationItemStrings("source/temp/", null, TestEnvironment, true);

            MigrationItemStrings file1 = new MigrationItemStrings("source/folder1/file1.txt", null, TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings("source/folder1/file2.txt", null, TestEnvironment, true);
            MigrationItemStrings tempFile = new MigrationItemStrings("source/folder1/tempFile.txt", null, TestEnvironment, true);

            SourceAdapter.AddFolder(folder1.LocalPath);
            SourceAdapter.AddFolder(folder2.LocalPath);
            SourceAdapter.AddFolder(folder3.LocalPath);

            SourceAdapter.AddFile(file1.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);

            int branchChangeset = SourceAdapter.BranchItem(branch);

            // Create a cyclic rename of two files
            SourceWorkspace.PendRename(file1.ServerPath, tempFile.ServerPath);
            SourceWorkspace.PendRename(file2.ServerPath, file1.ServerPath);
            SourceWorkspace.PendRename(tempFile.ServerPath, file2.ServerPath);

            // Create a three-way cyclic rename of the parent folder with two other folders
            SourceWorkspace.PendRename(folder1.ServerPath, temp.ServerPath);
            SourceWorkspace.PendRename(folder2.ServerPath, folder1.ServerPath);
            SourceWorkspace.PendRename(folder3.ServerPath, folder2.ServerPath);
            SourceWorkspace.PendRename(temp.ServerPath, folder3.ServerPath);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), RenameComment);

            SourceWorkspace.Merge(branch.ServerPath, branch.NewServerPath,
                VersionSpec.ParseSingleSpec(branchChangeset.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.AlwaysAcceptMine);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), MergeComment);

            Run();

            if (TestEnvironment.SourceEndPoint.AdapterType == AdapterType.TFS2008VC)
            {
                VerifyHistory(0, 0);
                // For orcas, there should be no difference. 
            }
            else
            {
                VerifyHistory(1, 1);
                // Need more comparison here. 
                // On dev 10 server, rename becomes sourceDelete+targetbranch. So the rename-from-name will exist in the original place as a deleted item. 
                // The merge will then merge the item as a Merge|Delete. We will skip the change in this case. 
            }
        }

        ///<summary>
        ///Scenario: Migrate a merge of a changeset with a cyclical rename of files in a parent folder that's renamed
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("billbar")]
        [Description("Migrate a merge of a changeset with a cyclical rename of files in a parent folder that's renamed")]
        public void MergeCyclicRenameInRenamedFolderTest()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source/", "target/", TestEnvironment, true);

            MigrationItemStrings folder1 = new MigrationItemStrings("source/folder1/", null, TestEnvironment, true);
            MigrationItemStrings folder2 = new MigrationItemStrings("source/folder2/", null, TestEnvironment, true);

            MigrationItemStrings file1 = new MigrationItemStrings("source/folder1/file1.txt", null, TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings("source/folder1/file2.txt", null, TestEnvironment, true);
            MigrationItemStrings tempFile = new MigrationItemStrings("source/folder1/tempFile.txt", null, TestEnvironment, true);

            SourceAdapter.AddFolder(folder1.LocalPath);
            SourceAdapter.AddFolder(folder2.LocalPath);

            SourceAdapter.AddFile(file1.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);

            int branchChangeset = SourceAdapter.BranchItem(branch);

            // Create a cyclic rename of two files
            SourceWorkspace.PendRename(file1.ServerPath, tempFile.ServerPath);
            SourceWorkspace.PendRename(file2.ServerPath, file1.ServerPath);
            SourceWorkspace.PendRename(tempFile.ServerPath, file2.ServerPath);

            // Rename the parent folder
            SourceWorkspace.PendRename(folder1.ServerPath, folder2.ServerPath);

            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), RenameComment);

            SourceWorkspace.Merge(branch.ServerPath, branch.NewServerPath,
                VersionSpec.ParseSingleSpec(branchChangeset.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.AlwaysAcceptMine);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), MergeComment);

            Run();

            if (TestEnvironment.SourceEndPoint.AdapterType == AdapterType.TFS2008VC)
            {
                VerifyHistory(0, 0);
                // For orcas, there should be no difference. 
            }
            else
            {
                VerifyHistory(0, 0);
                // Need more comparison here. 
                // On dev 10 server, rename becomes sourceDelete+targetbranch. So the rename-from-name will exist in the original place as a deleted item. 
                // The merge will then merge the item as a Merge|Delete. We will skip the change in this case. 
            }
        }

        ///<summary>
        ///Scenario: Repro steps for the bug 686690. BranchMergeRenameEdit test.
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Repro steps for the Dev10 bug 686690: VC adapter: ProjSrvC - throwOnMissingItem in pendRenames")]
        public void BranchMergeRenameEditTest()
        {
            #region repro steps
            //1. add $/test/main/a/1.txt 
            //2. branch main to main-branch
            //3. add $/test/main/b/1.txt
            //4. tf merge $/test/main $/test/main-branch /r
            //5. rename $/test/main/b/1.txt to $/test/main/b/2.txt 
            //6. tf merge $/test/main $/test/main-branch /r
            //    resolve a conflict by taking the source branch change
            //    edit $/test/main-branch/2.txt
            //    check in
            //    (2.txt == merge,rename,edit)
            //7. add $/test/main/b/1.txt 
            //8. tf merge $/test/main $/test/main-branch /r
            //9. create a configuration file and map server paths like below:
            //<FilterPair>
            //<FilterItem MigrationSourceUniqueId="1ebfa76e-6f49-4ec8-b25d-03aac1b05085" FilterString="$/test/main-branch" />
            //<FilterItem MigrationSourceUniqueId="67502947-0a21-4a6a-b169-7857d7e9e641" FilterString="$/test/main-branch2" />
            //</FilterPair>
            //10. start migration console app using the config file generated in the step 9
            //11. conflict detection - branch root not found
            //12. resolve the conflict and allow $/ for scope
            //13. re-start the migration consonle app
            //14. the exception gets thrown
            #endregion

            // 1. add $/test/main/a/1.txt 
            MigrationItemStrings path1 = new MigrationItemStrings("main", "main-branch", TestEnvironment, true);
            MigrationItemStrings file1 = new MigrationItemStrings("main/a/1.txt", null, TestEnvironment, true);

            SourceAdapter.AddFolder(path1.LocalPath);
            SourceAdapter.AddFile(file1.LocalPath);

            // 2. branch main to main-branch
            SourceAdapter.BranchItem(path1);

            // 3. add $/test/main/b/1.txt
            MigrationItemStrings file2 = new MigrationItemStrings("main/b/1.txt", "main/b/2.txt", TestEnvironment, true);
            MigrationItemStrings file3 = new MigrationItemStrings("main-branch/b/1.txt", "main-branch/b/2.txt", TestEnvironment, true);
            int mergeFromVersion = SourceAdapter.AddFile(file2.LocalPath);

            // 4. tf merge $/test/main $/test/main-branch /r
            SourceWorkspace.Merge(path1.ServerPath, path1.NewServerPath,
                VersionSpec.ParseSingleSpec(mergeFromVersion.ToString(), Environment.UserName),
                VersionSpec.Latest,
                LockLevel.None, RecursionType.Full, MergeOptions.None);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), MergeComment);


            // 5. rename $/test/main/b/1.txt to $/test/main/b/2.txt
            SourceAdapter.RenameItem(file2.ServerPath, file2.NewServerPath);

            // 6. tf merge $/test/main $/test/main-branch /r
            SourceWorkspace.Merge(path1.ServerPath, path1.NewServerPath,
                VersionSpec.ParseSingleSpec(mergeFromVersion.ToString(), Environment.UserName),
                VersionSpec.Latest,
                LockLevel.None, RecursionType.Full, MergeOptions.None);
            ResolveConflicts(Resolution.AcceptTheirs);
            SourceAdapter.EditFile(file3.NewLocalPath); // merge,rename,edit (edit the pending item 2.txt)

            // 7. add $/test/main/b/1.txt  
            mergeFromVersion = SourceAdapter.AddFile(file2.LocalPath); // reuse the same file name

            // 8. tf merge $/test/main $/test/main-branch /r
            SourceWorkspace.Merge(path1.ServerPath, path1.NewServerPath,
                VersionSpec.ParseSingleSpec(mergeFromVersion.ToString(), Environment.UserName),
                VersionSpec.Latest,
                LockLevel.None, RecursionType.Full, MergeOptions.None);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), MergeComment);

            // 9. migrate main-branch only 
            string source = TestEnvironment.FirstSourceServerPath + SrcPathSeparator + "main-branch";
            string target = TestEnvironment.FirstTargetServerPath + TarPathSeparator + "main-branch";
            TestEnvironment.Mappings.Clear();
            TestEnvironment.AddMapping(new MappingPair(source, target));

            // 10. start migration
            Run();
            VerifyHistory(4, 0);
            
            // Resolve "Branch source path not found conflict" using "$/" scope.
            ConflictResolver conflictManager = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictManager.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be only 1 conflict");
            conflictManager.TryResolveConflict(conflicts[0], new VCChangeToAddOnBranchSourceNotMappedAction(), "$/"); // Add on branch source not found

            if (TestEnvironment.SourceEndPoint.IsTfsAdapter && 
                TestEnvironment.SourceEndPoint.AdapterType >= AdapterType.TFS2010VC)
            {
                Run();

                // verify expected differences
                VerifyHistory(1, 1);

                //Changeset targetChangeset = diff.TargetChanges[0];
                //Changeset sourceChangeset = diff.SourceChanges[0];
                //diff.ChangesetDiff(ref targetChangeset, ref sourceChangeset);
                //Assert.AreEqual(1, sourceChangeset.Changes.Length);
                //Assert.AreEqual(ChangeType.Merge | ChangeType.Undelete | ChangeType.Edit,
                //    sourceChangeset.Changes[0].ChangeType & ~ChangeType.Encoding);
                //Assert.AreEqual(1, targetChangeset.Changes.Length);
                //Assert.AreEqual(ChangeType.Add | ChangeType.Edit,
                //    targetChangeset.Changes[0].ChangeType & ~ChangeType.Encoding);

                // verify content matches
                Assert.IsTrue(VerifyContents(), "Content mismatch");
            }
            else
            {
                RunAndValidate(true, true);
            }
        }
    }
}