// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTestLibrary;
using MigrationTestLibrary.Conflict;

namespace BasicVCTest
{
    [TestClass]
    public class MappingTest : BasicVCTestCaseBase
    {
        ///<summary>
        ///Scenario: basic cloak test
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("peigu")]
        [Description("Migrate add and edit when some of the files paths are cloaked")]
        public void BasicCloakTest()
        {
            MigrationItemStrings cloakedFile = new MigrationItemStrings("cloak/file.txt", "cloak/file.txt", TestEnvironment, true);
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
        ///Scenario: Migrate an added file
        ///Expected Result: The file is migrated sucessfully
        ///</summary>
        [TestMethod(), Priority(1), Owner("peigu")]
        [Description("Migrate an added file")]
        public void AddMappingRoot()
        {
            MigrationItemStrings folderOnSource = new MigrationItemStrings("folder", "folder", TestEnvironment, true);
            MigrationItemStrings folderOnTarget = new MigrationItemStrings("folder", "folder", TestEnvironment, false);

            SourceAdapter.AddFolder(folderOnSource.LocalPath);
            TargetAdapter.AddFolder(folderOnTarget.LocalPath);

            //Remap the migration
            string source = TestEnvironment.FirstSourceServerPath;
            string target = TestEnvironment.FirstTargetServerPath;
            TestEnvironment.Mappings.Clear();
            TestEnvironment.AddMapping(new MappingPair(
                source + SrcPathSeparator + folderOnSource.Name, 
                target + TarPathSeparator + folderOnTarget.Name, false));

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Migrate a branch and merge when the branch is the root of the migration
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Migrate a branch and merge when the branch is the root of the migration")]
        public void MapBranchRootTest()
        {
            
            MigrationItemStrings branch = new MigrationItemStrings("source", "target", TestEnvironment, true);
            MigrationItemStrings file = new MigrationItemStrings(TestUtils.URIPathCombine(branch.Name, "file.txt"), TestUtils.URIPathCombine(branch.NewName, "file.txt"), TestEnvironment, true);
            SourceAdapter.AddFile(file.LocalPath);

            //Remap the migration
            string source = TestEnvironment.FirstSourceServerPath;
            string target = TestEnvironment.FirstTargetServerPath;
            TestEnvironment.Mappings.Clear();
            TestEnvironment.AddMapping(new MappingPair(source + SourceAdapter.PathSeparator + branch.Name, 
                target + TargetAdapter.PathSeparator + branch.Name, false));
            TestEnvironment.AddMapping(new MappingPair(source + SourceAdapter.PathSeparator + branch.NewName,
                target + TargetAdapter.PathSeparator + branch.NewName, false));
            TestEnvironment.AddMapping(new MappingPair(source + SourceAdapter.PathSeparator + m_extraFile.Name,
                target + TargetAdapter.PathSeparator + m_extraFile.Name, false));

            int branchChangeset = SourceAdapter.BranchItem(branch.ServerPath, branch.NewServerPath);

            SourceAdapter.EditFile(file.LocalPath);

            SourceAdapter.MergeItem(branch, branchChangeset);

            SourceAdapter.EditFile(m_extraFile.LocalPath);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Trailing slashes in source and target pathes
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("hykwon")]
        [Description("Mappings with trailing slashes")]
        public void TrailingSlashesInMappingTest()
        {
            MigrationItemStrings file = new MigrationItemStrings("file.txt", "FILE.txt", TestEnvironment, true);
            SourceAdapter.AddFile(file.LocalPath);

            string source = TestEnvironment.FirstSourceServerPath + SrcPathSeparator;
            string target = TestEnvironment.FirstTargetServerPath + TarPathSeparator;
            TestEnvironment.Mappings.Clear();
            TestEnvironment.AddMapping(new MappingPair(source, target));
            
            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Trailing slash in target path
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("hykwon")]
        [Description("Target path with trailing slash")]
        public void TrailingSlashInTargetPathTest()
        {
            MigrationItemStrings file = new MigrationItemStrings("file.txt", "FILE.txt", TestEnvironment, true);
            SourceAdapter.AddFile(file.LocalPath);

            string source = TestEnvironment.FirstSourceServerPath;
            string target = TestEnvironment.FirstTargetServerPath + TarPathSeparator;
            TestEnvironment.Mappings.Clear();
            TestEnvironment.AddMapping(new MappingPair(source, target));

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Trailing slash in source path
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("hykwon")]
        [Description("Source path with trailing slash")]
        public void TrailingSlashInSourcePathTest()
        {
            MigrationItemStrings file = new MigrationItemStrings("file.txt", "FILE.txt", TestEnvironment, true);
            SourceAdapter.AddFile(file.LocalPath);

            string source = TestEnvironment.FirstSourceServerPath + SrcPathSeparator;
            string target = TestEnvironment.FirstTargetServerPath;
            TestEnvironment.Mappings.Clear();
            TestEnvironment.AddMapping(new MappingPair(source, target));

            RunAndValidate();
        }
    }
}
