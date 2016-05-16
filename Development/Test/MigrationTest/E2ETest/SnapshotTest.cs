// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTestLibrary;
using MigrationTestLibrary.Conflict;
using ServerDiff;

namespace TfsVCTest
{
    /// <summary>
    /// Test scenarios for basic operations
    /// </summary>
    [TestClass]
    public class SnapshotTest : TfsVCTestCaseBase
    {
        ///<summary>
        ///Scenario: Migrate a snapshot, and then normal history
        ///Expected Result: Successful migration
        ///</summary>
        [TestMethod(), Priority(1), Owner("peigu")]
        [Description("Migrate a snapshot, and then normal history.")]
        public void BasicSnapshotTest()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source/", "target/", TestEnvironment, true);

            MigrationItemStrings[] addFiles = new MigrationItemStrings[20];

            // Add files
            // Create a tree structure so that we can test path compression logic.
            int snapshotChangesetId = 0;
            for (int i = 0; i < 20; i++)
            {
                addFiles[i] = new MigrationItemStrings(string.Format("source/addFile{0}.txt", i), null, TestEnvironment, true);
                if (i == 15)
                {
                    snapshotChangesetId = SourceAdapter.AddFile(addFiles[i].LocalPath);
                }
                else
                {
                    SourceAdapter.AddFile(addFiles[i].LocalPath);
                }
            }
            TestEnvironment.SnapshotStartPoints = new Dictionary<string, string>();
            TestEnvironment.SnapshotStartPoints.Add(TestEnvironment.SourceEndPoint.TeamProject, snapshotChangesetId.ToString());
            TestEnvironment.SnapshotBatchSize = 3;

            Run();

            VerifySnapshotMigration(snapshotChangesetId);

            Assert.IsTrue(VerifyContents());
        }

        ///<summary>
        ///Scenario: Migrate a configuration file with session snapshot, and path snapshot
        ///Expected Result: Successful migration
        ///</summary>
        [TestMethod(), Priority(1), Owner("peigu")]
        [Description("Migrate a configuration file with session snapshot, and path snapshot.")]
        public void PathSnapshotTest()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source/", "target/", TestEnvironment, true);

            MigrationItemStrings[] addFiles = new MigrationItemStrings[20];

            // Add files
            // Create a tree structure so that we can test path compression logic.
            int sessionSnapshotChangesetId = 0;
            for (int i = 0; i < 20; i++)
            {
                addFiles[i] = new MigrationItemStrings(string.Format("source/path1/addFile{0}.txt", i), null, TestEnvironment, true);
                if (i == 15)
                {
                    sessionSnapshotChangesetId = SourceAdapter.AddFile(addFiles[i].LocalPath);
                }
                else
                {
                    SourceAdapter.AddFile(addFiles[i].LocalPath);
                }
            }

            int path2SnapshotChangesetId = 0;

            for (int i = 0; i < 20; i++)
            {
                addFiles[i] = new MigrationItemStrings(string.Format("source/path2/addFile{0}.txt", i), null, TestEnvironment, true);
                if (i == 15)
                {
                    path2SnapshotChangesetId = SourceAdapter.AddFile(addFiles[i].LocalPath);
                }
                else
                {
                    SourceAdapter.AddFile(addFiles[i].LocalPath);
                }
            }
            TestEnvironment.SnapshotStartPoints = new Dictionary<string, string>();
            TestEnvironment.SnapshotStartPoints.Add(TestEnvironment.SourceEndPoint.TeamProject, sessionSnapshotChangesetId.ToString());
            TestEnvironment.SnapshotBatchSize = 3;

            // We need to map to the sub item level
            MappingPair rootMapping = TestEnvironment.Mappings[0];
            TestEnvironment.Mappings.Clear();
            TestEnvironment.Mappings.Add(new MappingPair(rootMapping.SourcePath + "/source/path1", rootMapping.TargetPath + "/source/path1", false));
            TestEnvironment.Mappings.Add(new MappingPair(rootMapping.SourcePath + "/source/path2", rootMapping.TargetPath + "/source/path2", false, path2SnapshotChangesetId.ToString(), null));

            Run();

            // ToDo, ideally, we should compare content at snapshot changeset and compare history after snapshot changeset. 
            Assert.IsTrue(VerifyContents());
        }


        ///<summary>
        ///Scenario: Migrate a configuration file with path SnapshotPoint and PeerSnapshotPoint
        ///Expected Result: Successful migration
        ///</summary>
        [TestMethod(), Priority(1), Owner("andrhs")]
        [Description("Migrate a configuration file with path SnapshotPoint and PeerSnapshotPoint.")]
        public void PathPeerSnapshotTest()
        {
            // Add files
            // Create a tree structure so that we can test path compression logic.
            int snapshotChangesetId1 = 0;
            int peerSnapshotChangesetId1 = 0;
            for (int i = 0; i < 4; i++)
            {
                MigrationItemStrings sourceFile = new MigrationItemStrings(string.Format("source/path1/addFile{0}.txt", i), null, TestEnvironment, true);
                MigrationItemStrings targetFile = new MigrationItemStrings(string.Format("source/path1/addFile{0}.txt", i), null, TestEnvironment, false);

                if (i < 2)
                {
                    snapshotChangesetId1 = SourceAdapter.AddFile(sourceFile.LocalPath);
                    TargetAdapter.AddFile(targetFile.LocalPath);
                    peerSnapshotChangesetId1 = TargetAdapter.EditFile(targetFile.LocalPath, sourceFile.LocalPath);
                }
                else
                {
                    SourceAdapter.AddFile(sourceFile.LocalPath);
                }
            }

            int snapshotChangesetId2 = 0;
            int peerSnapshotChangesetId2 = 0;
            for (int i = 0; i < 4; i++)
            {
                MigrationItemStrings sourceFile = new MigrationItemStrings(string.Format("source/path2/addFile{0}.txt", i), null, TestEnvironment, true);
                MigrationItemStrings targetFile = new MigrationItemStrings(string.Format("source/path2/addFile{0}.txt", i), null, TestEnvironment, false);

                if (i < 3)
                {
                    snapshotChangesetId2 = SourceAdapter.AddFile(sourceFile.LocalPath);
                    TargetAdapter.AddFile(targetFile.LocalPath);
                    peerSnapshotChangesetId2 = TargetAdapter.EditFile(targetFile.LocalPath, sourceFile.LocalPath);
                }
                else
                {
                    SourceAdapter.AddFile(sourceFile.LocalPath);
                }
            }

            // We need to map to the sub item level
            MappingPair rootMapping = TestEnvironment.Mappings[0];
            TestEnvironment.Mappings.Clear();
            TestEnvironment.Mappings.Add(
                new MappingPair(
                    rootMapping.SourcePath + "/source/path1",
                    rootMapping.TargetPath + "/source/path1",
                    false,
                    snapshotChangesetId1.ToString(),
                    peerSnapshotChangesetId1.ToString(),
                    null,
                    null,
                    peerSnapshotChangesetId1.ToString(),
                    snapshotChangesetId1.ToString()));
            TestEnvironment.Mappings.Add(
                new MappingPair(
                    rootMapping.SourcePath + "/source/path2",
                    rootMapping.TargetPath + "/source/path2",
                    false,
                    snapshotChangesetId2.ToString(),
                    peerSnapshotChangesetId2.ToString(),
                    null,
                    null,
                    peerSnapshotChangesetId2.ToString(),
                    snapshotChangesetId2.ToString()));

            Run();

            MigrationItemStrings newFile = new MigrationItemStrings("source/path1/newFile.txt", null, TestEnvironment, true);
            SourceAdapter.AddFile(newFile.LocalPath);

            Run();

            Assert.IsTrue(VerifyContents());
        }

        ///<summary>
        ///Scenario: Migrate a configuration file with session snapshot, and path snapshot. Then migrate CRUD changesets.
        ///Expected Result: Successful migration
        ///</summary>
        [TestMethod(), Priority(1), Owner("peigu")]
        [Description("Migrate a configuration file with session snapshot, and path snapshot. Then migrate CRUD changesets.")]
        public void PathSnapshotCRUDTest()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source/", "target/", TestEnvironment, true);

            MigrationItemStrings[] path1AddFiles = new MigrationItemStrings[10];
            MigrationItemStrings[] path2AddFiles = new MigrationItemStrings[10];

            // Add files
            // Create a tree structure so that we can test path compression logic.
            int sessionSnapshotChangesetId = 0;
            for (int i = 0; i < 10; i++)
            {
                path1AddFiles[i] = new MigrationItemStrings(string.Format("source/path1/addFile{0}.txt", i), null, TestEnvironment, true);
                if (i == 7)
                {
                    sessionSnapshotChangesetId = SourceAdapter.AddFile(path1AddFiles[i].LocalPath);
                }
                else
                {
                    SourceAdapter.AddFile(path1AddFiles[i].LocalPath);
                }
            }

            for (int i = 0; i < 10; i++)
            {
                path2AddFiles[i] = new MigrationItemStrings(string.Format("source/path2/file{0}.txt", i), string.Format("source/path2/file-rename{0}.txt", i), TestEnvironment, true);
                SourceAdapter.AddFile(path2AddFiles[i].LocalPath);
            }

            SourceAdapter.EditFile(path2AddFiles[2].LocalPath);

            SourceAdapter.RenameItem(path2AddFiles[3].ServerPath, path2AddFiles[3].NewServerPath, "Rename before snapshot");

            int deleteChangesetID = SourceAdapter.DeleteItem(path2AddFiles[4].ServerPath);

            SourceAdapter.DeleteItem(path2AddFiles[5].ServerPath);

            SourceAdapter.UndeleteFile(path2AddFiles[4].ServerPath, deleteChangesetID);

            int path2SnapshotChangesetId = SourceAdapter.RenameItem(path2AddFiles[6].ServerPath, path2AddFiles[6].NewServerPath, "Rename before snapshot");

            SourceAdapter.EditFile(path2AddFiles[6].NewLocalPath);
            SourceAdapter.DeleteItem(path2AddFiles[4].ServerPath);
            SourceAdapter.RenameItem(path2AddFiles[3].NewServerPath, path2AddFiles[3].ServerPath, "Rename after snapshot");


            TestEnvironment.SnapshotStartPoints = new Dictionary<string, string>();
            TestEnvironment.SnapshotStartPoints.Add(TestEnvironment.SourceEndPoint.TeamProject, sessionSnapshotChangesetId.ToString());
            TestEnvironment.SnapshotBatchSize = 3;

            // We need to map to the sub item level
            MappingPair rootMapping = TestEnvironment.Mappings[0];
            TestEnvironment.Mappings.Clear();
            TestEnvironment.Mappings.Add(new MappingPair(rootMapping.SourcePath + "/source/path1", rootMapping.TargetPath + "/source/path1", false));
            TestEnvironment.Mappings.Add(new MappingPair(rootMapping.SourcePath + "/source/path2", rootMapping.TargetPath + "/source/path2", false, path2SnapshotChangesetId.ToString(), null));

            Run();

            // ToDo, ideally, we should compare content at snapshot changeset and compare history after snapshot changeset. 
            Assert.IsTrue(VerifyContents());
        }

        ///<summary>
        ///Scenario: Migrate a configuration file with session snapshot, and path snapshot. Then migrate some branches that have version before and after snapshot.
        ///Expected Result: Successful migration
        ///</summary>
        [TestMethod(), Priority(1), Owner("peigu")]
        [Description("Migrate a configuration file with session snapshot, and path snapshot. Then migrate some branches that have version before and after snapshot.")]
        public void PathSnapshotBranchTest()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source/", "target/", TestEnvironment, true);
            MigrationItemStrings branch2 = new MigrationItemStrings("source/", "target2/", TestEnvironment, true);

            MigrationItemStrings[] addFiles = new MigrationItemStrings[10];

            // Add files
            // Create a tree structure so that we can test path compression logic.
            int sessionSnapshotChangesetId = 1;
            for (int i = 0; i < 10; i++)
            {
                addFiles[i] = new MigrationItemStrings(
                    string.Format("source/addFile{0}.txt", i), 
                    string.Format("source/addFile{0}-rename.txt", i), 
                    TestEnvironment, 
                    true);

                SourceAdapter.AddFile(addFiles[i].LocalPath);                
            }

            int editBeforeSnapshotChangesetId = SourceAdapter.EditFile(addFiles[2].LocalPath);
            int editBeforeSnapshotChangesetId2 = SourceAdapter.EditFile(addFiles[2].LocalPath);


            SourceAdapter.RenameItem(addFiles[3].ServerPath, addFiles[3].NewServerPath, "Rename before snapshot");

            int deleteChangesetID = SourceAdapter.DeleteItem(addFiles[4].ServerPath);

            SourceAdapter.DeleteItem(addFiles[5].ServerPath);

            SourceAdapter.UndeleteFile(addFiles[4].ServerPath, deleteChangesetID);

            int path2SnapshotChangesetId = SourceAdapter.RenameItem(addFiles[6].ServerPath, addFiles[6].NewServerPath, "Rename before snapshot");

            int editAfterSnapshotChangesetId = SourceAdapter.EditFile(addFiles[6].NewLocalPath);
            int editAfterSnapshotChangesetId2 = SourceAdapter.EditFile(addFiles[6].NewLocalPath);
            SourceAdapter.DeleteItem(addFiles[4].ServerPath);
            SourceAdapter.RenameItem(addFiles[3].NewServerPath, addFiles[3].ServerPath, "Rename after snapshot");

            // Branch from a version before the snapshot
            SourceWorkspace.Get(VersionSpec.Latest, GetOptions.Overwrite);
            SourceWorkspace.PendBranch(branch.ServerPath, branch.NewServerPath, new ChangesetVersionSpec(editBeforeSnapshotChangesetId));
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), "Branch from a version before snapshot");

            SourceAdapter.BranchItem(branch2);



            TestEnvironment.SnapshotStartPoints = new Dictionary<string, string>();
            TestEnvironment.SnapshotStartPoints.Add(TestEnvironment.SourceEndPoint.TeamProject, sessionSnapshotChangesetId.ToString());
            TestEnvironment.SnapshotBatchSize = 3;

            // We need to map to the sub item level
            MappingPair rootMapping = TestEnvironment.Mappings[0];
            TestEnvironment.Mappings.Clear();
            TestEnvironment.Mappings.Add(new MappingPair(rootMapping.SourcePath + "/target", rootMapping.TargetPath + "/target", false));
            TestEnvironment.Mappings.Add(new MappingPair(rootMapping.SourcePath + "/target2", rootMapping.TargetPath + "/target2", false));
            TestEnvironment.Mappings.Add(new MappingPair(rootMapping.SourcePath + "/source", rootMapping.TargetPath + "/source", false, path2SnapshotChangesetId.ToString(), null));

            Run();

            ConflictResolver conflictManager = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictManager.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be only 1 conflict");
            conflictManager.TryResolveConflict(conflicts[0],  ConflictConstant.TFSHistoryNotFoundSkipAction, "1-" + path2SnapshotChangesetId);
            Run();

            // ToDo, ideally, we should compare content at snapshot changeset and compare history after snapshot changeset. 
            Assert.IsTrue(VerifyContents());
        }

        ///<summary>
        ///Scenario: Migrate a snapshot, and then normal history - snapshot is after history with Create, Rename, Undelete, Delete
        ///Expected Result: Successful migration
        ///</summary>
        [TestMethod(), Priority(1), Owner("peigu")]
        [Description("Migrate a snapshot, and then normal history  - snapshot is after history with Create, Rename, Undelete, Delete")]
        public void SnapshotCRUDTest()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source/", "target/", TestEnvironment, true);

            MigrationItemStrings[] files = new MigrationItemStrings[10];

            // Add files
            for (int i = 0; i < 10; i++)
            {
                files[i] = new MigrationItemStrings(string.Format("source/file{0}.txt", i), string.Format("source/file-rename{0}.txt", i), TestEnvironment, true);
                SourceAdapter.AddFile(files[i].LocalPath);
            }

            SourceAdapter.EditFile(files[2].LocalPath);

            SourceAdapter.RenameItem(files[3].ServerPath, files[3].NewServerPath, "Rename before snapshot");

            int deleteChangesetID = SourceAdapter.DeleteItem(files[4].ServerPath);

            SourceAdapter.DeleteItem(files[5].ServerPath);

            SourceAdapter.UndeleteFile(files[4].ServerPath, deleteChangesetID);

            int snapshotChangesetId = SourceAdapter.RenameItem(files[6].ServerPath, files[6].NewServerPath, "Rename before snapshot");

            SourceAdapter.EditFile(files[6].NewLocalPath);
            SourceAdapter.DeleteItem(files[4].ServerPath);
            SourceAdapter.RenameItem(files[3].NewServerPath, files[3].ServerPath, "Rename after snapshot");


            TestEnvironment.SnapshotStartPoints = new Dictionary<string, string>();
            TestEnvironment.SnapshotStartPoints.Add(TestEnvironment.SourceEndPoint.TeamProject, snapshotChangesetId.ToString());
            TestEnvironment.SnapshotBatchSize = 3;

            Run();
            VerifySnapshotMigration(snapshotChangesetId);
            Assert.IsTrue(VerifyContents());
        }

        ///<summary>
        ///Scenario: Repro steps for Dev10 bug 742388
        ///Expected Result: Successful migration
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Migrate a snapshot, and then normal history.")]
        public void SnapshotUndeleteTest()
        {
            /*
             * Descripton from the bug:
             * Item is undeleted *after* snapshot.  
             * Then it is merged from version after snapshot.  
             * After merge, we pend another edit.  
             * This is on the source system.  
             * On the target, since undelete is before snapshot, we change it to add.  
             * But merge is valid because it is after snapshot, 
             * so we pend merge and that becomes branch, merge.  
             * Finally, when we pend edit, it will say that item can not be found or 
             * that you do not have permission to access it - basically, item cannot be found.
             */

            // 0. setup
            MigrationItemStrings branch = new MigrationItemStrings("main", "main-branch", TestEnvironment, true);
            MigrationItemStrings filea1 = new MigrationItemStrings("main/a1.txt", null, TestEnvironment, true);
            MigrationItemStrings filea2 = new MigrationItemStrings("main/a2.txt", null, TestEnvironment, true);
            MigrationItemStrings fileb1 = new MigrationItemStrings("main-branch/a1.txt", null, TestEnvironment, true);
            MigrationItemStrings fileb2 = new MigrationItemStrings("main-branch/a2.txt", null, TestEnvironment, true);

            SourceAdapter.AddFolder(branch.LocalPath);
            SourceAdapter.AddFile(filea1.LocalPath);
            SourceAdapter.AddFile(filea2.LocalPath);

            SourceAdapter.BranchItem(branch);

            // delete b1.txt
            int deleteChangesetId = SourceAdapter.DeleteItem(fileb1.ServerPath);

            // snapshot changeset id
            int snapshotChangesetId = SourceAdapter.EditFile(fileb2.LocalPath);

            // undelete b1.txt
            SourceAdapter.UndeleteFile(fileb1.ServerPath, deleteChangesetId);

            // merge from version after snapshot changeset id
            int mergeAfterSnapshotId = SourceAdapter.EditFile(filea1.LocalPath);
            SourceAdapter.MergeItem(branch, mergeAfterSnapshotId);

            // edit the item
            SourceAdapter.EditFile(fileb1.LocalPath);

            // migration
            TestEnvironment.SnapshotStartPoints = new Dictionary<string, string>();
            TestEnvironment.SnapshotStartPoints.Add(TestEnvironment.SourceEndPoint.TeamProject, snapshotChangesetId.ToString());
            TestEnvironment.SnapshotBatchSize = 100;

            Run();

            // ToDo, ideally, we should compare content at snapshot changeset and compare history after snapshot changeset. 
            //verifyChangesetAfterSnapshot(tfsDiff, snapshotChangesetId);
            Assert.IsTrue(VerifyContents());
        }
    }

}
