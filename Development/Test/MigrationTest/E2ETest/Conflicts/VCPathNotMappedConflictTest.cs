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

namespace TfsVCTest
{
    [TestClass]
    public class VCPathNotMappedConflictTest : TfsVCTestCaseBase
    {
        ///<summary>
        ///Scenario: Migrate a branch|merge|Edit when the branch source path is not mapped. 
        ///Expected Result: Migration Halts at the branch action, when resumed, it create the item sucessfully
        ///</summary>
        [TestMethod(), Priority(1), Owner("peigu")]
        [Description("Migrate a branch|merge when the branch source path is not mapped.")]
        public void BranchMergeEditWithSourceNotMappedTest()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source", "target", TestEnvironment, true);
            MigrationItemStrings file = new MigrationItemStrings("source/fld/file.txt", "target/fld/file.txt", TestEnvironment, true);

            // Add the parent folder at branch from place
            SourceAdapter.AddFolder(branch.LocalPath);

            MappingPair mapping = new MappingPair(TestEnvironment.FirstSourceServerPath + SrcPathSeparator + "source",
                                                  TestEnvironment.FirstTargetServerPath + TarPathSeparator + "source",
                                                  true); // cloaked
            TestEnvironment.AddMapping(mapping);

            // Branch the parent folder
            int branchChangeset = SourceAdapter.BranchItem(branch);

            // Add the child item
            SourceAdapter.AddFile(file.LocalPath);

            SourceWorkspace.Merge(branch.ServerPath, branch.NewServerPath,
                VersionSpec.ParseSingleSpec(branchChangeset.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);
            ResolveConflicts(Resolution.AcceptTheirs);
            SourceAdapter.EditFile(file.NewLocalPath);

            Run();
            VerifyHistory(4, 0);

            // Resolve "Branch source path not found conflict" using "$/" scope.
            ConflictResolver conflictManager = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictManager.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be only 1 conflict");
            conflictManager.TryResolveConflict(conflicts[0], new VCChangeToAddOnBranchSourceNotMappedAction(), "$/"); // Add on branch source not found

            RunAndValidate(true, true);
        }

        ///<summary>
        ///Scenario: Migrate a branch|merge|Delete when the branch source path is not mapped. 
        ///Expected Result: Migration Halts at the branch action, when resumed, it shouldn't be created at the target system.
        ///</summary>
        [TestMethod(), Priority(2), Owner("peigu")]
        [Description("Migrate a branch|merge when the branch source path is not mapped.")]
        public void BranchMergeDeleteWithSourceNotMappedTest()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source", "target", TestEnvironment, true);
            MigrationItemStrings file = new MigrationItemStrings("source/fld/file.txt", "target/fld/file.txt", TestEnvironment, true);

            // Add the parent folder at branch from place
            SourceAdapter.AddFolder(branch.LocalPath);

            MappingPair mapping = new MappingPair(TestEnvironment.FirstSourceServerPath + SrcPathSeparator + "source",
                                                  TestEnvironment.FirstTargetServerPath + TarPathSeparator + "source",
                                                  true); // cloaked
            TestEnvironment.AddMapping(mapping);

            // Branch the parent folder
            int branchChangeset = SourceAdapter.BranchItem(branch);

            // Add the child item
            SourceAdapter.AddFile(file.LocalPath);

            SourceWorkspace.Merge(branch.ServerPath, branch.NewServerPath,
                VersionSpec.ParseSingleSpec(branchChangeset.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);
            ResolveConflicts(Resolution.AcceptTheirs);
            SourceAdapter.DeleteItem(file.NewServerPath);

            Run();
            VerifyHistory(4, 0);

            // Resolve "Branch source path not found conflict" using "$/" scope.
            ConflictResolver conflictManager = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictManager.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be only 1 conflict");
            conflictManager.TryResolveConflict(conflicts[0], new VCChangeToAddOnBranchSourceNotMappedAction(), "$/"); // Add on branch source not found

            Run();
            Assert.IsTrue(VerifyContents());
        }

        ///<summary>
        ///Scenario: Migrate a Rename when the branch source path is not mapped.
        ///Expected Result: Migration Halts at the rename action, when resumed, it create the item sucessfully
        ///</summary>
        [TestMethod(), Priority(1), Owner("peigu")]
        [Description("Migrate a Rename when the branch source path is not mapped.")]
        public void RenameWithSourceNotMappedTest()
        {
            MigrationItemStrings folder = new MigrationItemStrings("rename-from", "rename-to", TestEnvironment, true);
            MigrationItemStrings file = new MigrationItemStrings("rename-from/fld/file.txt", "rename-to/fld/file.txt", TestEnvironment, true);

            // Add the child item
            SourceAdapter.AddFile(file.LocalPath);

            MappingPair mapping = new MappingPair(TestEnvironment.FirstSourceServerPath + SrcPathSeparator + "rename-from",
                                                  TestEnvironment.FirstTargetServerPath + TarPathSeparator + "rename-from",
                                                  true); // cloaked
            TestEnvironment.AddMapping(mapping);

            SourceWorkspace.Get(VersionSpec.Latest, GetOptions.Overwrite);
            SourceWorkspace.PendRename(folder.ServerPath, folder.NewServerPath);

            SourceAdapter.EditFile(file.NewLocalPath);

            Run();
            VerifyHistory(3, 0);

            // Resolve "Branch source path not found conflict" using "$/" scope.
            ConflictResolver conflictManager = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictManager.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be only 1 conflict");
            conflictManager.TryResolveConflict(conflicts[0], new VCChangeToAddOnBranchSourceNotMappedAction(), "$/"); // Add on branch source not found

            RunAndValidate(true, true);
        }
    }
}
