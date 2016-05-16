// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTestLibrary;
using MigrationTestLibrary.Conflict;
using ServerDiff;
using System.Diagnostics;
using Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace TfsVCTest.VCBasicConflicts
{
    /// <summary>
    /// Test scenarios for VC content conflicts
    /// </summary>
    [TestClass]
    public class ContentConflictTest : TfsVCTestCaseBase
    {
        #region Add,Add conflicts
        ///<summary>
        ///Scenario: 
        ///  1. source system: check in a.txt
        ///  2. target system: check in a.txt
        ///  3. migrate a.txt -> a.txt
        ///  4. delete a.txt on source
        ///  5. resolve by VCContentConflictTakeLocalChangeAction
        ///Expected Result: 
        ///  Conflicts will be detected and resolved
        ///</summary>
        [TestMethod(), Priority(1), Owner("wlennon"), Ignore]
        [Description("ContentConflict AddAdd")]
        public void AddAddConflictTakeLocalTest()
        {
            // initial migration 
            MigrationItemStrings file1 = new MigrationItemStrings("1.txt", "1.txt", TestEnvironment, true);
            SourceAdapter.AddFile(file1.LocalPath);
            RunAndValidate();

            // add a same file on both ends
            string fileName = "a.txt";

            MigrationItemStrings sourceFile = new MigrationItemStrings(fileName, fileName, TestEnvironment, true);
            MigrationItemStrings targetFile = new MigrationItemStrings(fileName, fileName, TestEnvironment, false);

            int sourceAdd = SourceAdapter.AddFile(sourceFile.LocalPath);
            TargetAdapter.AddFile(targetFile.LocalPath);

            Run();
            VerifyHistory(2, 1);

            // resolve conflicts            
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be 1 conflict");
            VCContentConflictType contentConflict = new VCContentConflictType();
            Assert.AreEqual(contentConflict.ReferenceName, conflicts[0].ConflictTypeReference.Value.ReferenceName, "It should be vc content conflict");

            // resolve the vc content conflict. The chained conflict will be resolved automatically
            conflictResolver.TryResolveConflict(conflicts[0], new VCContentConflictTakeLocalChangeAction(), "$/");

            // continue migration
            Run();

            conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be 0 conflict");

            Assert.IsFalse(VerifyContents(), "Content should be different as we skip the add from source system.");

            SourceAdapter.EditFile(sourceFile.LocalPath);
            Run();

            Assert.IsTrue(VerifyContents());
        }

        ///<summary>
        ///Scenario: 
        ///  1. source system: check in a.txt
        ///  2. target system: check in a.txt
        ///  3. migrate a.txt -> a.txt
        ///  4. skip deleting a.txt on target
        ///  5. resolve by VCContentConflictTakeOtherChangesAction
        ///Expected Result: 
        ///  Conflicts will be detected and resolved
        ///</summary>
        [TestMethod(), Priority(1), Owner("wlennon"), Ignore]
        [Description("ContentConflict AddAdd")]
        public void AddAddConflictTakeOtherNoDeleteTest()
        {
            // initial migration 
            MigrationItemStrings file1 = new MigrationItemStrings("1.txt", "1.txt", TestEnvironment, true);
            SourceAdapter.AddFile(file1.LocalPath);
            RunAndValidate();

            // add a same file on both ends
            string fileName = "a.txt";
            MigrationItemStrings sourceFile = new MigrationItemStrings(fileName, fileName, TestEnvironment, true);
            MigrationItemStrings targetFile = new MigrationItemStrings(fileName, fileName, TestEnvironment, false);

            int sourceAdd = SourceAdapter.AddFile(sourceFile.LocalPath);
            TargetAdapter.AddFile(targetFile.LocalPath);

            Run();

            VerifyHistory(2, 1);

            // resolve conflicts            
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be 1 conflict");
            VCContentConflictType contentConflict = new VCContentConflictType();
            Assert.AreEqual(contentConflict.ReferenceName, conflicts[0].ConflictTypeReference.Value.ReferenceName, "It should be vc content conflict");

            // resolve the vc content conflict. The chained conflict will be resolved automatically
            conflictResolver.TryResolveConflict(conflicts[0], new VCContentConflictTakeOtherChangesAction(), "$/");

            // The user decided to accept other but forgot deleting the items on target system

            // start a conflict resolution thread to detect 'future' TfsCheckinConflict and resolve it asynchronously
            ConflictResolutionWorker resolutionWorker = new ConflictResolutionWorker()
            {
                ConflictResolver = conflictResolver,
            };

            resolutionWorker.ConflictResolutionList.Add(new ConflictResolutionEntry()
            {
                Conflict = ConflictConstant.TfsCheckinConflict,
                Resolution = ConflictConstant.TfsCheckinSkipAction,
            });

            resolutionWorker.ConflictResolutionList.Add(new ConflictResolutionEntry()
            {
                Conflict = ConflictConstant.TFSZeroCheckinConflict,
                Resolution = new TFSZeroCheckinSkipAction().ReferenceName,
            });

            resolutionWorker.Start();

            // restart the migraiton tool
            // migration tool will be in a loop until the TfsCheckinConflict gets resolved
            Run();

            Assert.IsTrue(VerifyContents(), "Content should be the same.");

            SourceAdapter.EditFile(sourceFile.LocalPath);
            Run();

            Trace.WriteLine("Stopping ConflictResolutionWorker");
            resolutionWorker.Stop();

            Assert.IsTrue(VerifyContents(), "Content should match!");
        }

        ///<summary>
        ///Scenario: 
        ///  1. source system: check in a.txt
        ///  2. target system: check in a.txt
        ///  3. migrate a.txt -> a.txt
        ///  4. user merge actions
        ///  5. resolve by VCContentConflictUserMergeChangeAction
        ///Expected Result: 
        ///  Conflicts will be detected and resolved
        ///</summary>
        [TestMethod(), Priority(1), Owner("peigu"), Ignore]
        [Description("ContentConflict AddAdd")]
        public void AddAddConflictUserMergeTest()
        {
            string fileName = "file.txt";

            MigrationItemStrings sourceFile = new MigrationItemStrings(fileName, fileName, TestEnvironment, true);
            MigrationItemStrings targetFile = new MigrationItemStrings(fileName, fileName, TestEnvironment, false);

            // Initial sync.
            Run();

            int deltaChangeId = SourceAdapter.AddFile(sourceFile.LocalPath);
            TargetAdapter.AddFile(targetFile.LocalPath);

            // Don't check in extra.txt
            Run(false);
            VerifyHistory(1, 1);

            // User checkin files to make the system in sync. 
            int miChangeId = TargetAdapter.EditFile(targetFile.LocalPath, sourceFile.LocalPath,
                "user: delete items on target to resolve add,add conflict **NOMIGRATION**");

            // resolve conflicts            
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be 1 conflict");
            VCContentConflictType contentConflict = new VCContentConflictType();
            Assert.AreEqual(contentConflict.ReferenceName, conflicts[0].ConflictTypeReference.Value.ReferenceName, "It should be vc content conflict");

            Dictionary<string, string> dataFields = new Dictionary<string, string>();
            dataFields.Add(VCContentConflictUserMergeChangeAction.MigrationInstructionChangeId, miChangeId.ToString());
            dataFields.Add(VCContentConflictUserMergeChangeAction.DeltaTableChangeId, deltaChangeId.ToString());
            conflictResolver.TryResolveConflict(conflicts[0], new VCContentConflictUserMergeChangeAction(), string.Format("{0};{1}", "$/", (deltaChangeId - 1).ToString()), dataFields);

            Run(false);
            Assert.IsTrue(VerifyContents());
        }
        #endregion

        #region Rename,Rename conflicts
        ///<summary>
        ///Scenario: 
        ///  1. migrate a.txt -> a.txt
        ///  2. source system: a.txt -> b.txt
        ///  3. target system: a.txt -> c.txt
        ///  4. Migrate 
        ///  5. user merge actions
        ///     target side: rename c.txt -> a.txt manually
        ///  6. resolve by VCContentConflictUserMergeChangeAction
        ///Expected Result: 
        ///  Migration will detect a conflict
        ///</summary>
        [TestMethod(), Priority(1), Owner("wlennon")]
        [Description("ContentConflict RenameRename")]
        public void RenameRenameConflictUserMergeTest()
        {
            // 1. migrate a.txt -> a.txt
            string fileName = "a.txt";
            MigrationItemStrings sourceFile = new MigrationItemStrings(fileName, "b.txt", TestEnvironment, true);
            MigrationItemStrings targetFile = new MigrationItemStrings(fileName, "c.txt", TestEnvironment, false);
            MigrationItemStrings targetUserMergeFile = new MigrationItemStrings("c.txt", "b.txt", TestEnvironment, false);

            SourceAdapter.AddFile(sourceFile.LocalPath);
            RunAndValidate();

            // 2. source system: a.txt -> b.txt
            int deltaChangeId = SourceAdapter.RenameItem(sourceFile.ServerPath, sourceFile.NewServerPath);

            // 3. target system: a.txt -> c.txt
            TargetAdapter.RenameItem(targetFile.ServerPath, targetFile.NewServerPath);

            // 4. migrate
            Run();
            VerifyHistory(2, 1);

            // 5. resolve conflicts
            // 5.1 User merge actions
            // The user decided to accept other so he reverted the rename change on target system. c.txt -> a.txt
            int miChangeId = TargetAdapter.RenameItem(targetUserMergeFile.ServerPath, targetUserMergeFile.NewServerPath,
                "Rename target items to accept other side's renames **NOMIGRATION**");

            // 5.2 resolve conflicts by VCContentConflictUserMergeChangeAction
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be 1 conflict");
            VCNameSpaceContentConflictType nameSpaceContentConflict = new VCNameSpaceContentConflictType();
            Assert.IsTrue(nameSpaceContentConflict.ReferenceName.Equals(conflicts[0].ConflictTypeReference.Value.ReferenceName),
                "It should be vc namespace content conflict");

            Dictionary<string, string> dataFields = new Dictionary<string, string>();
            dataFields.Add(VCContentConflictUserMergeChangeAction.MigrationInstructionChangeId, miChangeId.ToString());
            dataFields.Add(VCContentConflictUserMergeChangeAction.DeltaTableChangeId, deltaChangeId.ToString());

            conflictResolver.TryResolveConflict(conflicts[0], new VCContentConflictUserMergeChangeAction(), string.Format("$/;{0}", deltaChangeId), dataFields);

            // 6. Migration will detect a conflict
            Run();
            Assert.IsTrue(VerifyContents());

            SourceAdapter.EditFile(sourceFile.NewLocalPath);
            Run();
            Assert.IsTrue(VerifyContents());
        }

        #endregion

        #region Edit,Edit conflicts

        [TestMethod(), Priority(1), Owner("wlennon"), Ignore]
        [Description("ContentConflicts EditEdit")]
        public void EditMultipleEditConflictTakeOtherTest()
        {
            // 1. migrate a.txt -> a.txt
            string fileName = "a.txt";
            MigrationItemStrings sourceFile = new MigrationItemStrings(fileName, null, TestEnvironment, true);
            MigrationItemStrings targetFile = new MigrationItemStrings(fileName, null, TestEnvironment, false);
            MigrationItemStrings targetFile2 = new MigrationItemStrings("b.txt", null, TestEnvironment, false);

            SourceAdapter.AddFile(sourceFile.LocalPath);
            RunAndValidate();

            // 2. source system: a.txt -> edit
            SourceAdapter.EditFile(sourceFile.LocalPath);

            // 3. target system: a.txt -> edit
            TargetAdapter.EditFile(targetFile.LocalPath);
            TargetAdapter.EditFile(targetFile.LocalPath);

            // 4. Migration will detect a conflict
            Run();
            VerifyHistory(2, 2);

            // 5. resolve conflicts            
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be 1 conflict");
            VCContentConflictType contentConflict = new VCContentConflictType();
            Assert.AreEqual(contentConflict.ReferenceName, conflicts[0].ConflictTypeReference.Value.ReferenceName, "It should be vc content conflict");

            conflictResolver.TryResolveConflict(conflicts[0], new VCContentConflictTakeOtherChangesAction(), "$/");

            // 6. restart
            Run();

            // 7. validation
            // no remaining conflicts
            conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be NO conflict");

            // content should match
            Assert.IsTrue(VerifyContents());
        }

        ///<summary>
        ///Scenario: 
        ///  1. migrate a.txt -> a.txt
        ///  2. source system: a.txt -> edit
        ///  3. target system: a.txt -> edit
        ///  4. Migrate a.txt
        ///  5. resolve by VCContentConflictTakeOtherChangesAction
        ///Expected Result: 
        ///  Conflicts will be detected and resolved
        ///</summary>
        [TestMethod(), Priority(1), Owner("wlennon")]
        [Description("ContentConflicts EditEdit")]
        public void EditEditConflictTakeLocalTest()
        {
            // 1. migrate a.txt -> a.txt
            string fileName = "a.txt";
            MigrationItemStrings sourceFile = new MigrationItemStrings(fileName, null, TestEnvironment, true);
            MigrationItemStrings targetFile = new MigrationItemStrings(fileName, null, TestEnvironment, false);

            SourceAdapter.AddFile(sourceFile.LocalPath);
            RunAndValidate();

            // 2. source system: a.txt -> edit
            SourceAdapter.EditFile(sourceFile.LocalPath);

            // 3. target system: a.txt -> edit
            TargetAdapter.EditFile(targetFile.LocalPath);

            // 4. Migration will detect a conflict
            Run();
            VerifyHistory(2, 1);

            // 5. resolve conflicts            
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be 1 conflict");
            VCContentConflictType contentConflict = new VCContentConflictType();
            Assert.AreEqual(contentConflict.ReferenceName, conflicts[0].ConflictTypeReference.Value.ReferenceName, "It should be vc content conflict");
            conflictResolver.TryResolveConflict(conflicts[0], new VCContentConflictTakeLocalChangeAction(), "$/");

            // 6. restart
            Run();

            // 7. validation
            // no remaining conflicts
            conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be NO conflict");

            if (TestEnvironment.MigrationTestType == MigrationTestType.OneWay)
            {
                // content should not match for one-way sync
                Assert.IsFalse(VerifyContents(), "The latest content should be different as a.txt is not migrated.");

                SourceAdapter.EditFile(sourceFile.LocalPath);
                Run();
                Assert.IsTrue(VerifyContents());
            }
            else
            {
                Assert.IsTrue(VerifyContents());
            }
        }

        ///<summary>
        ///Scenario: 
        ///  1. migrate a.txt -> a.txt and b.txt
        ///  2. source system: a.txt -> edit b.txt -> edit
        ///  3. target system: a.txt -> edit
        ///  4. Migrate a.txt and b.txt
        ///  5. resolve by VCContentConflictTakeOtherChangesAction
        ///Expected Result: 
        ///  Conflicts will be detected and resolved
        ///</summary>
        [Ignore]
        [TestMethod(), Priority(1), Owner("peigu")]
        [Description("ContentConflicts EditEdit")]
        public void EditEditMultipleItemsTest()
        {
            // 1. migrate a.txt -> a.txt
            string fileName = "a.txt";
            MigrationItemStrings sourceFile = new MigrationItemStrings(fileName, null, TestEnvironment, true);
            MigrationItemStrings targetFile = new MigrationItemStrings(fileName, null, TestEnvironment, false);
            MigrationItemStrings nonConflictFile = new MigrationItemStrings("b.txt", null, TestEnvironment, true);

            SourceAdapter.AddFile(sourceFile.LocalPath);
            SourceAdapter.AddFile(nonConflictFile.LocalPath);
            RunAndValidate();

            // 2. source system: a.txt -> edit
            SourceWorkspace.Get(VersionSpec.Latest, GetOptions.Overwrite);
            TestUtils.EditRandomFile(nonConflictFile.LocalPath);
            SourceWorkspace.PendEdit(nonConflictFile.LocalPath);
            // This will checkin both a.txt and b.txt
            int contentConflictChangeset = SourceAdapter.EditFile(sourceFile.LocalPath);

            // 3. target system: a.txt -> edit
            TargetAdapter.EditFile(targetFile.LocalPath);

            // 4. Migration will detect a conflict
            Run();
            VerifyHistory(2, 1);

            // 5. resolve conflicts            
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(2, conflicts.Count, "There should be 2 conflicts");
            VCContentConflictType contentConflict = new VCContentConflictType();
            Assert.AreEqual(contentConflict.ReferenceName, conflicts[0].ConflictTypeReference.Value.ReferenceName, "It should be vc content conflict");

            conflictResolver.TryResolveConflict(conflicts[0], new VCContentConflictTakeLocalChangeAction(), "$/");

            // 6. restart
            Run();

            // TfsCheckinConflict needs to be resolved manually. Otherwise, this test will loop until time-out.

            // 7. validation
            // no remaining conflicts
            conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be NO conflict");

            // content should not match
            Assert.IsFalse(VerifyContents(), "The latest content should be different as a.txt is not migrated.");
        }

        ///<summary>
        ///Scenario: 
        ///  1. migrate a.txt -> a.txt
        ///  2. source system: a.txt -> edit
        ///  3. target system: a.txt -> edit
        ///  4. Migrate a.txt
        ///  5. resolve by VCContentConflictTakeOtherChangesAction
        ///Expected Result: 
        ///  Conflicts will be detected and resolved
        ///</summary>
        [TestMethod(), Priority(1), Owner("wlennon")]
        [Description("ContentConflicts EditEdit")]
        public void EditEditConflictUserMergeTest()
        {
            // 1. migrate a.txt -> a.txt
            string fileName = "a.txt";
            MigrationItemStrings sourceFile = new MigrationItemStrings(fileName, null, TestEnvironment, true);
            MigrationItemStrings targetFile = new MigrationItemStrings(fileName, null, TestEnvironment, false);

            SourceAdapter.AddFile(sourceFile.LocalPath);
            RunAndValidate();

            // 2. source system: a.txt -> edit
            int contentConflictChangeset = SourceAdapter.EditFile(sourceFile.LocalPath);

            // 3. target system: a.txt -> edit
            TargetAdapter.EditFile(targetFile.LocalPath);

            // 4. Migration will detect a conflict
            Run();
            VerifyHistory(2, 1);

            // 5. resolve conflicts            
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be 1 conflict");
            VCContentConflictType contentConflict = new VCContentConflictType();
            Assert.AreEqual(contentConflict.ReferenceName, conflicts[0].ConflictTypeReference.Value.ReferenceName, "It should be vc content conflict");

            int deltaChangeId = SourceAdapter.EditFile(sourceFile.LocalPath);
            int miChangeId = TargetAdapter.EditFile(targetFile.LocalPath, sourceFile.LocalPath);

            Dictionary<string, string> dataFields = new Dictionary<string, string>();
            dataFields.Add(VCContentConflictUserMergeChangeAction.MigrationInstructionChangeId, miChangeId.ToString());
            dataFields.Add(VCContentConflictUserMergeChangeAction.DeltaTableChangeId, deltaChangeId.ToString());

            conflictResolver.TryResolveConflict(conflicts[0], new VCContentConflictUserMergeChangeAction(),
                string.Format("$/;{0}", contentConflictChangeset), dataFields);

            // 6. restart
            Run();

            // 7. validation
            // no remaining conflicts
            conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be NO conflict");

            // content should match
            Assert.IsTrue(VerifyContents());
        }
        #endregion

        ///<summary>
        ///Scenario: 
        ///  Repro for Pioneer Dev10 Bug 466785
        ///  1. add a.txt, same.txt on source system
        ///  2. add b.txt, same.txt on target system
        ///  3. Sync
        ///  4. resolve by VCContentConflictTakeOtherChangesAction and sync
        ///  5. Sync
        ///  6. No conflict is expected
        ///Expected Result: 
        ///  Conflicts will be detected and resolved
        ///</summary>
        [TestMethod(), Priority(1), Owner("wlennon"), Ignore]
        [Description("AddAddConflictTakeOtherTest")]
        public void AddAddConflictTakeOtherTest()
        {
            // 1. migrate a.txt -> a.txt
            string fileName1 = "a.txt";
            string fileName2 = "b.txt";
            string sameFileName = "same.txt";

            MigrationItemStrings sourceFile1 = new MigrationItemStrings(fileName1, null, TestEnvironment, true);
            MigrationItemStrings sourceSameFile = new MigrationItemStrings(sameFileName, null, TestEnvironment, true);
            MigrationItemStrings targetFile1 = new MigrationItemStrings(fileName2, null, TestEnvironment, false);
            MigrationItemStrings targetSameFile = new MigrationItemStrings(sameFileName, null, TestEnvironment, false);

            SourceAdapter.AddFiles(new string[] { sourceFile1.LocalPath, sourceSameFile.LocalPath });
            TargetAdapter.AddFiles(new string[] { targetFile1.LocalPath, targetSameFile.LocalPath });

            // 4. Migration will detect a conflict
            Run();

            // 5. resolve conflicts            
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be 1 conflict");
            VCContentConflictType contentConflict = new VCContentConflictType();
            Assert.AreEqual(contentConflict.ReferenceName, conflicts[0].ConflictTypeReference.Value.ReferenceName, "It should be vc content conflict");
            conflictResolver.TryResolveConflict(conflicts[0], new VCContentConflictTakeOtherChangesAction(), conflicts[0].ScopeHint);

            // sync
            Run();

            // validation
            // no remaining conflicts
            conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be NO conflict");

            // content should match
            Assert.IsTrue(VerifyContents(), "The latest content should match.");
        }

        ///<summary>
        ///Scenario: 
        ///  Repro for Pioneer Dev10 Bug 466785
        ///  1. migrate a.txt -> a.txt
        ///  2. edit a.txt on source system
        ///  3. edit a.txt on target system
        ///  4. Sync
        ///  5. resolve by VCContentConflictTakeOtherChangesAction and sync
        ///  6. edit a.txt on source system
        ///  7. Sync
        ///  8. No conflict is expected
        ///Expected Result: 
        ///  Conflicts will be detected and resolved
        ///</summary>
        [TestMethod(), Priority(1), Owner("wlennon")]
        [Description("EditEditConflictTakeLocalTest")]
        public void EditEditConflictTakeOtherTest()
        {
            // 1. migrate a.txt -> a.txt
            string fileName = "a.txt";
            MigrationItemStrings sourceFile = new MigrationItemStrings(fileName, null, TestEnvironment, true);
            MigrationItemStrings targetFile = new MigrationItemStrings(fileName, null, TestEnvironment, false);

            SourceAdapter.AddFile(sourceFile.LocalPath);
            RunAndValidate();

            // 2. source system: a.txt -> edit
            SourceAdapter.EditFile(sourceFile.LocalPath);

            // 3. target system: a.txt -> edit
            TargetAdapter.EditFile(targetFile.LocalPath);

            // 4. Migration will detect a conflict
            Run();

            // 5. resolve conflicts            
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be 1 conflict");
            VCContentConflictType contentConflict = new VCContentConflictType();
            Assert.AreEqual(contentConflict.ReferenceName, conflicts[0].ConflictTypeReference.Value.ReferenceName, "It should be vc content conflict");
            conflictResolver.TryResolveConflict(conflicts[0], new VCContentConflictTakeOtherChangesAction(), conflicts[0].ScopeHint);

            // sync
            Run();

            // validation
            // no remaining conflicts
            conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be NO conflict");

            // content should match
            Assert.IsTrue(VerifyContents(), "The latest content should match.");

            // 6. edit a.txt on source system
            SourceAdapter.EditFile(sourceFile.LocalPath);

            // 7. sync
            Run();

            // validation
            // no remaining conflicts
            conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be NO conflict");

            // content should match
            Assert.IsTrue(VerifyContents(), "The latest content should match.");

        }
    }
}
