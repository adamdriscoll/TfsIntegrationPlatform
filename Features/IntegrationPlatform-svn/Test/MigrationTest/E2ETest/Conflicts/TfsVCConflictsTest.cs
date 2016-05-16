// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTestLibrary;
using MigrationTestLibrary.Conflict;
using ServerDiff;

namespace TfsVCTest.TfsVCConflict
{
    /// <summary>
    /// Test scenarios for TFS path too long conflict
    /// </summary>
    [TestClass]
    public class VCInvalidPathConflictTest : TfsVCTestCaseBase
    {
        ///<summary>
        ///Scenario: Migrate adds on the a file with a path longer than 260 character. (This requires the target mapping to be at least 1 character longer than the source mapping)
        ///Expected Result: 
        ///</summary>
        [TestMethod(), Priority(1), Owner("peigu")]
        [Description("Migrate an added file with long path")]
        public void VCInvalidPathConflict()
        {
            // Make sure that target mapping path > the source mapping path
            string sourceMapping = TestEnvironment.FirstSourceServerPath;
            string targetMapping = TestEnvironment.FirstTargetServerPath;
            int lengthDiff = sourceMapping.Length - targetMapping.Length;
            if (lengthDiff >= 0)
            {
                String path = new String('d', lengthDiff + 1);
                TestEnvironment.Mappings.Clear();
                TestEnvironment.AddMapping(new MappingPair(sourceMapping, TestUtils.URIPathCombine(targetMapping, path), false));
            }

            Assert.IsTrue(TestEnvironment.FirstSourceServerPath.Length < TestEnvironment.FirstTargetServerPath.Length,
                "This test only works if the source mapping is shorter than the target mapping");

            int fileNameLength = 258 - TestEnvironment.FirstSourceServerPath.Length; // 259 - 1 for '/'

            Assert.IsTrue(fileNameLength > 0, "Source mapping is too long");

            StringBuilder fileNameBuilder = new StringBuilder();

            for (int i = 0; i < fileNameLength; i++)
            {
                fileNameBuilder.Append('a');
            }

            string fileName = fileNameBuilder.ToString();

            MigrationItemStrings file = new MigrationItemStrings(fileName, fileName, TestEnvironment, true);
            MigrationItemStrings fileInTargetSystem = new MigrationItemStrings(fileName, fileName, TestEnvironment, false);

            SourceAdapter.AddFile(file.LocalPath);
            try
            {
                Run();
            }
            catch (Exception)
            {
            }

            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();

            // verify we have edit/edit conflict
            Assert.IsTrue(conflicts.Count == 1, "There should be invalid field value conflict");
            Assert.IsTrue(ConflictConstant.VCInvalidPathConflict.Equals(conflicts[0].ConflictTypeReference.Value.ReferenceName), "It should be an invalid path conflict");
        }

        ///<summary>
        ///Scenario: Start migration from a snapshot. Have a branch from a version earlier than the snapshot. 
        /// Resolve the conflict by skip, so that Branch will change to Add. 
        ///Expected Result: Successful migration
        ///</summary>
        [TestMethod(), Priority(1), Owner("peigu")]
        [Description("Branch history information not found, change Branch to Add")]
        public void TFSHistoryNotFoundConflict()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source/", "target/", TestEnvironment, true);

            MigrationItemStrings file1 = new MigrationItemStrings("source/file1.txt", null, TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings("source/file2.txt", null, TestEnvironment, true);

            int firstChangeset = SourceAdapter.AddFile(file1.LocalPath);
            int editBeforeSnapShot = SourceAdapter.EditFile(file1.LocalPath);

            int snapshotChangesetId = SourceAdapter.AddFile(file2.LocalPath);

            SourceAdapter.BranchItem(branch.ServerPath, branch.NewServerPath);

            TestEnvironment.SnapshotBatchSize = 10;
            TestEnvironment.SnapshotStartPoints = new Dictionary<string, string>();
            TestEnvironment.SnapshotStartPoints.Add(TestEnvironment.SourceTeamProject, snapshotChangesetId.ToString());

            Run();

            // Resolve "Branch source path not found conflict" using "$/" scope.
            ConflictResolver conflictManager = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictManager.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be only 1 conflict");
            conflictManager.TryResolveConflict(conflicts[0], ConflictConstant.TFSHistoryNotFoundSkipAction, string.Format("{0}-{1}", firstChangeset, snapshotChangesetId)); // Add on branch source not found

            Run(true, true);
            Assert.IsTrue(VerifyContents());

        }
    }
}
