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
    public class MappingTest : TfsVCTestCaseBase
    {
        ///<summary>
        ///Scenario: Migrate an item that is under one mapping, but is substring of the another mapping.
        ///Expected Result: Successful migration 
        ///</summary>
        [TestMethod(), Priority(1), Owner("peigu")]
        [Description("Migrate an item that is under one mapping, but is substring of the another mapping.")]
        public void StartsWithMappingsTest()
        {
            MigrationItemStrings fileOutSideMapping = new MigrationItemStrings("source-integration/file.txt", null, TestEnvironment, true);
            MigrationItemStrings fileUnderMapping = new MigrationItemStrings("source/file.txt", null, TestEnvironment, true);

            TestUtils.CreateRandomFile(fileOutSideMapping.LocalPath, 10);
            TestUtils.CreateRandomFile(fileUnderMapping.LocalPath, 10);

            string mergeComment = "Migration test merge";
            SourceWorkspace.PendAdd(fileOutSideMapping.LocalPath);
            SourceWorkspace.PendAdd(fileUnderMapping.LocalPath);

            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), mergeComment);

            MappingPair mapping = new MappingPair(TestEnvironment.FirstSourceServerPath + SrcPathSeparator + "source",
                                                  TestEnvironment.FirstTargetServerPath + TarPathSeparator + "source",
                                                  false);
            MappingPair mapping2 = new MappingPair(TestEnvironment.FirstSourceServerPath + SrcPathSeparator + "source-integration",
                                                  TestEnvironment.FirstTargetServerPath  + TarPathSeparator + "source-integration",
                                                  false);
            TestEnvironment.Mappings.Clear();

            TestEnvironment.AddMapping(mapping);
            TestEnvironment.AddMapping(mapping2);
            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Migrate a branch and merge when the branch target path is cloaked
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Migrate a branch and merge when the branch target path is cloaked")]
        public void CloakedTargetMappingsTest()
        {
            MigrationItemStrings file = new MigrationItemStrings("source" + SourceAdapter.PathSeparator + "file.txt", 
                "target" + SourceAdapter.PathSeparator + "file.txt", TestEnvironment, true);
            SourceAdapter.AddFile(file.LocalPath);

            MappingPair mapping = new MappingPair(TestEnvironment.FirstSourceServerPath + SrcPathSeparator + "target",
                                                  TestEnvironment.FirstTargetServerPath + TarPathSeparator + "target",
                                                  true); // cloaked
            TestEnvironment.AddMapping(mapping);

            int branchChangeset = SourceAdapter.BranchItem(file.ServerPath, file.NewServerPath);

            SourceAdapter.EditFile(m_extraFile.LocalPath);
            SourceAdapter.EditFile(file.LocalPath);

            int mergeChangset = SourceAdapter.MergeItem(file, branchChangeset);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: basic cloak test
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("peigu")]
        [Description("Migrate add and edit when some of the files paths are cloaked")]
        public void BasicCloakTest()
        {
            MigrationItemStrings cloakedFile = new MigrationItemStrings("cloak" + SourceAdapter.PathSeparator + "file.txt", null, TestEnvironment, true);
            SourceAdapter.AddFile(cloakedFile.LocalPath);

            MappingPair mapping = new MappingPair(TestEnvironment.FirstSourceServerPath + SrcPathSeparator + "cloak",
                                                  TestEnvironment.FirstTargetServerPath + TarPathSeparator + "cloak",
                                                  true); // cloaked
            TestEnvironment.AddMapping(mapping);

            SourceAdapter.EditFile(m_extraFile.LocalPath);
            SourceAdapter.EditFile(cloakedFile.LocalPath);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: basic cloak test
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("peigu")]
        [Description("Migrate edit and delete in the same changeset, while delete is on an item not mapped")]
        public void DeleteUnmappedTest()
        {
            MigrationItemStrings cloakedFile = new MigrationItemStrings("cloak" + SourceAdapter.PathSeparator + "file.txt", null, TestEnvironment, true);
            SourceAdapter.AddFile(cloakedFile.LocalPath);

            MappingPair mapping = new MappingPair(TestEnvironment.FirstSourceServerPath + SrcPathSeparator + "cloak",
                                                  TestEnvironment.FirstTargetServerPath + TarPathSeparator + "cloak",
                                                  true); // cloaked
            TestEnvironment.AddMapping(mapping);

            SourceAdapter.EditFile(cloakedFile.LocalPath);
            SourceWorkspace.Get(VersionSpec.Latest, GetOptions.Overwrite);
            TestUtils.EditRandomFile(cloakedFile.LocalPath);
            SourceWorkspace.PendDelete(cloakedFile.LocalPath);

            // Extra file and deleted file will be checked in together.
            SourceAdapter.EditFile(m_extraFile.LocalPath);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Migrate the cloated path itself
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("peigu")]
        [Description("Migrate the cloated path itself")]
        public void MigrateCloakedFolderItselfTest()
        {
            MigrationItemStrings cloakedFile = new MigrationItemStrings("cloak", "cloak", TestEnvironment, true);
            SourceAdapter.AddFolder(cloakedFile.LocalPath);

            MappingPair mapping = new MappingPair(TestEnvironment.FirstSourceServerPath + SrcPathSeparator + "cloak",
                                                  TestEnvironment.FirstTargetServerPath + TarPathSeparator + "cloak",
                                                  true); // cloaked
            TestEnvironment.AddMapping(mapping);

            SourceAdapter.EditFile(m_extraFile.LocalPath);

            Run();
            VerifyHistory(0, 0);
        }

        ///<summary>
        ///Scenario: Migrate a branch and merge when the branch source path is cloaked and ChangeToAddOnBranchSourceNotFound is not specified,
        ///         then resume with ChangeToAddOnBranchSourceNotFound set to ture
        ///Expected Result: Migration Halts at the branch action, when resumed, it finishes sucessfully
        ///</summary>
        [TestMethod(), Priority(3), Owner("curtisp")]
        [Description("Migrate a branch and merge when the branch source path is cloaked and ChangeToAddOnBranchSourceNotFound is not specified, then resume with it true")]
        public void CloakedSourceMappingsTest()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source", "target", TestEnvironment, true);
            MigrationItemStrings file = new MigrationItemStrings("source" + SourceAdapter.PathSeparator + "file.txt", 
                "source" + SourceAdapter.PathSeparator + "renamedFile.txt", TestEnvironment, true);
            SourceAdapter.AddFile(file.LocalPath);

            MappingPair mapping = new MappingPair(TestEnvironment.FirstSourceServerPath + SrcPathSeparator + "source",
                                                  TestEnvironment.FirstTargetServerPath + TarPathSeparator + "source",
                                                  true); // cloaked
            TestEnvironment.AddMapping(mapping);

            int branchChangeset = SourceAdapter.BranchItem(branch.ServerPath, branch.NewServerPath);

            SourceAdapter.EditFile(m_extraFile.LocalPath);
            SourceAdapter.EditFile(file.LocalPath);
            SourceAdapter.RenameItem(file.ServerPath, file.NewServerPath);

            SourceWorkspace.Merge(branch.ServerPath, branch.NewServerPath,
                VersionSpec.ParseSingleSpec(branchChangeset.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);
            ResolveConflicts(Resolution.AcceptTheirs);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), MergeComment);

            Run();
            VerifyHistory(5, 0);

            // Resolve "Branch source path not found conflict" using "$/" scope.
            ConflictResolver conflictManager = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictManager.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be only 1 conflict");
            conflictManager.TryResolveConflict(conflicts[0], new VCChangeToAddOnBranchSourceNotMappedAction(), "$/"); // Add on branch source not found

            RunAndValidate(true, true);
        }

        ///<summary>
        ///Scenario: Migrate a branch and merge when the branch is the root of the migration
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Migrate a branch and merge when the branch is the root of the migration")]
        public void MapBranchRootTest()
        {
            
            MigrationItemStrings branch = new MigrationItemStrings("source" + SourceAdapter.PathSeparator,
                "target" + SourceAdapter.PathSeparator,
                TestEnvironment, true);
            MigrationItemStrings file = new MigrationItemStrings(branch.Name + SourceAdapter.PathSeparator + "file.txt",
                branch.NewName + SourceAdapter.PathSeparator + "file.txt", TestEnvironment, true);
            SourceAdapter.AddFile(file.LocalPath);

            //Remap the migration
            string source = TestEnvironment.FirstSourceServerPath;
            string target = TestEnvironment.FirstTargetServerPath;
            TestEnvironment.Mappings.Clear();
            TestEnvironment.AddMapping(new MappingPair(source + SrcPathSeparator + branch.Name,
                target + TarPathSeparator + branch.Name, false));
            TestEnvironment.AddMapping(new MappingPair(source + SrcPathSeparator + branch.NewName,
                target + TarPathSeparator + branch.NewName, false));
            TestEnvironment.AddMapping(new MappingPair(source + SrcPathSeparator + m_extraFile.Name,
                target + TarPathSeparator + m_extraFile.Name, false));

            int branchChangeset = SourceAdapter.BranchItem(branch.ServerPath, branch.NewServerPath);

            SourceAdapter.EditFile(file.LocalPath);

            SourceAdapter.MergeItem(branch, branchChangeset);

            SourceAdapter.EditFile(m_extraFile.LocalPath);

            RunAndValidate();
        }
    }
}
