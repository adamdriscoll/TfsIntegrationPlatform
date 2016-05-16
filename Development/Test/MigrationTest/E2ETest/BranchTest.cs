// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTestLibrary;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;

namespace TfsVCTest
{

    [TestClass]
    public class BranchTest : TfsVCTestCaseBase
    {
        [TestInitialize]
        public override void Initialize()
        {
            base.Initialize();
        }

        ///<summary>
        ///Scenario: Migrate a branch of a file
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(1), Owner("wlennon")]
        [Description("Migrate a branch of a file")]
        public void BranchFileTest()
        {
            MigrationItemStrings file = new MigrationItemStrings("source/file.txt", "target/file.txt", TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);
            SourceAdapter.BranchItem(file);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Migrate a branch of an empty folder
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("wlennon")]
        [Description("Migrate a branch of an empty folder")]
        public void BranchEmptyFolderTest()
        {
            MigrationItemStrings folder = new MigrationItemStrings("source/folder", "target/folder", TestEnvironment, true);

            SourceAdapter.AddFolder(folder.LocalPath);
            SourceAdapter.BranchItem(folder);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Migrate a branch of a populated folder
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(1), Owner("wlennon")]
        [Description("Migrate a branch of a populated folder")]
        public void BranchPopulatedFolderTest()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source/", "target/", TestEnvironment, true);
            MigrationItemStrings file1 = new MigrationItemStrings("source/folder/file1.txt", null, TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings("source/folder/file2.txt", null, TestEnvironment, true);

            SourceAdapter.AddFolder(branch.LocalPath);
            SourceAdapter.AddFile(file1.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);
            SourceAdapter.EditFile(file2.LocalPath);

            SourceAdapter.BranchItem(branch);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Migrate a branch of a folder from a version before some of the items were added
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(4), Owner("wlennon")]
        [Description("Migrate a branch of a folder from a version before some of the items were added")]
        public void BranchPartailTest()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source/", "target/", TestEnvironment, true);
            MigrationItemStrings file1 = new MigrationItemStrings("source/file1.txt", null, TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings("source/file2.txt", null, TestEnvironment, true);

            SourceAdapter.AddFolder(branch.LocalPath);
            SourceAdapter.AddFile(file1.LocalPath);
            int branchFrom = SourceAdapter.AddFile(file2.LocalPath);
            SourceAdapter.EditFile(file2.LocalPath);
            SourceAdapter.EditFile(file1.LocalPath);

            SourceWorkspace.Get(VersionSpec.Latest, GetOptions.Overwrite);
            SourceWorkspace.PendBranch(branch.ServerPath, branch.NewServerPath, VersionSpec.ParseSingleSpec(branchFrom.ToString(), Environment.UserName));

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Migrate a branch of a cyclical rename
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("wlennon")]
        [Description("Migrate a merge of a cyclical rename")]
        public void BranchCyclicRenameTest()
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

            SourceAdapter.EditFile(m_extraFile.LocalPath);

            SourceWorkspace.PendRename(folder1.ServerPath, temp.ServerPath);
            SourceWorkspace.PendRename(folder2.ServerPath, folder1.ServerPath);
            SourceWorkspace.PendRename(temp.ServerPath, folder2.ServerPath);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), RenameComment);

            SourceAdapter.BranchItem(branch);

            RunAndValidate();
        }
    }
}