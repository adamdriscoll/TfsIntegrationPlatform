// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TfsVCTest;
using MigrationTestLibrary;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace PerfTest
{
    [TestClass]
    public class VCPerfTest : TfsVCTestCaseBase
    {
        protected override string TestProjectName
        {
            get
            {
                return "VCPerfTest";
            }
        }

        ///<summary>
        ///Scenario: Migrate more than 100 merges to test merge compression logic
        ///Expected Result: Server Histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("peigu")]
        [Description("Migrate more than 100 merges to test merge compression logic.")]
        public void BatchMergeTest()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source/", "target/", TestEnvironment, true);

            MigrationItemStrings[] mergeFiles = new MigrationItemStrings[200];

            // Add files
            // Create a tree structure so that we can test path compression logic.
            for (int i = 0; i < 10; i++)
            {
                mergeFiles[i] = new MigrationItemStrings(string.Format("source/mergeFile{0}.txt", i), null, TestEnvironment, true);
                pendAdd(mergeFiles[i].LocalPath);
            }
            for (int i = 10; i < 50; i++)
            {
                mergeFiles[i] = new MigrationItemStrings(string.Format("source/sub1/mergeFile{0}.txt", i), null, TestEnvironment, true);
                pendAdd(mergeFiles[i].LocalPath);
            }
            for (int i = 50; i < 150; i++)
            {
                mergeFiles[i] = new MigrationItemStrings(string.Format("source/sub2/mergeFile{0}.txt", i), null, TestEnvironment, true);
                pendAdd(mergeFiles[i].LocalPath);
            }
            for (int i = 150; i < 200; i++)
            {
                mergeFiles[i] = new MigrationItemStrings(string.Format("source/sub1/sub11/mergeFile{0}.txt", i), null, TestEnvironment, true);
                pendAdd(mergeFiles[i].LocalPath);
            }

            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), AddComment);

            // The branch
            int branchChangeset = SourceAdapter.BranchItem(branch);

            #region Setup after Branch operation
            for (int i = 0; i < 200; i++)
            {
                pendEdit(mergeFiles[i].LocalPath);
            }

            #endregion Setup after Branch operation

            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), MergeComment);

            //The big merge
            SourceAdapter.MergeItem(branch, branchChangeset);

            RunAndValidate();
        }

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
    }
}
