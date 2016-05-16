// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTestLibrary;
using MigrationTestLibrary.Conflict;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using ServerDiff;

namespace Tfs2TfsVCTest
{
    /// <summary>
    /// Test scenarios for VC content conflicts
    /// </summary>
    [TestClass]
    public class ContentConflictTest : Tfs2TfsVCTestCase
    {
        ///<summary>
        ///Scenario: 
        ///  1. migrate a.txt -> a.txt
        ///  2. source system: a.txt -> edit
        ///  3. target system: a.txt -> b.txt
        ///  4. Migrate a.txt
        ///  5. user merge actions: 
        ///     source system: revert edit
        ///     source system: a.txt -> b.txt
        ///  6. resolve by VCContentConflictUserMergeChangeAction
        ///  7. restart the migration tool
        ///Expected Result: 
        ///  Conflicts will be detected and resolved
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("ContentConflicts EditRename")]
        public void EditRenameConflictTakeMergeTest()
        {
            // 1. migrate a.txt -> a.txt
            string fileName = "a.txt";
            MigrationItemStrings sourceFile = new MigrationItemStrings(fileName, null, TestEnvironment, true);
            MigrationItemStrings targetFile = new MigrationItemStrings(fileName, "b.txt", TestEnvironment, false);

            SourceAdapter.AddFile(sourceFile.LocalPath);
            RunAndValidate();

            // 2. source system: a.txt -> b.txt
            int editChangesetId = SourceAdapter.EditFile(sourceFile.LocalPath);

            // 3. target system: a.txt -> edit
            TargetAdapter.RenameItem(targetFile.ServerPath, targetFile.NewServerPath);

            // 4. Migration will detect a conflict
            Run();

            // 5. resolve conflicts            
            // 5.1 check if we have correct conflicts in db
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be only 1 conflict");
            VCNameSpaceContentConflictType namespaceConflict = new VCNameSpaceContentConflictType();
            Assert.IsTrue(namespaceConflict.ReferenceName.Equals(conflicts[0].ConflictTypeReference.Value.ReferenceName),
                "It should be vc namespace content conflict");

            // 5.2 user actions
            //
            // revert edit on a.txt
            PendEditToRevertFile(SourceWorkspace, sourceFile, editChangesetId, editChangesetId - 1);
            int deltaChangeId = SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), "user: revert edit change on a.txt **NOMIGRATION**");

            // revert rename on target
            int miChangeId = TargetAdapter.RenameItem(targetFile.NewServerPath, targetFile.ServerPath,
                "Revert rename change on target side **NOMIGRATION**");

            // resolve conflicts
            Dictionary<string, string> dataFields = new Dictionary<string, string>();
            dataFields.Add(VCContentConflictUserMergeChangeAction.MigrationInstructionChangeId, miChangeId.ToString());
            dataFields.Add(VCContentConflictUserMergeChangeAction.DeltaTableChangeId, deltaChangeId.ToString());

            foreach (RTConflict conflict in conflicts)
            {
                conflictResolver.TryResolveConflict(conflict, new VCContentConflictUserMergeChangeAction(), "$/", dataFields);
            }


            // 6. migrate
            Run(true, true);

            // 7. validation
            // no remaining conflicts and content match
            conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be NO conflict");
            // content match
            Assert.IsTrue(VerifyContents());
        }

        ///<summary>
        ///Scenario: 
        ///  1. migrate a.txt -> a.txt
        ///  2. source system: a.txt -> b.txt
        ///  3. target system: a.txt -> edit a.txt
        ///  4. Migrate a.txt
        ///  5. user merge actions
        ///     target system: revert edit on a.txt
        ///     target system: a.txt -> b.txt
        ///  6. resolve by VCContentConflictUserMergeChangeAction
        ///  7. restart the migration tool
        ///Expected Result: 
        ///  Conflicts will be detected and resolved
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("ContentConflict RenameEdit")]
        public void RenameEditConflictUserMergeTest()
        {
            // 1. migrate a.txt -> a.txt
            string fileName = "a.txt";
            MigrationItemStrings sourceFile = new MigrationItemStrings(fileName, "b.txt", TestEnvironment, true);
            MigrationItemStrings targetFile = new MigrationItemStrings(fileName, "b.txt", TestEnvironment, false);

            SourceAdapter.AddFile(sourceFile.LocalPath);
            RunAndValidate();

            // 2. source system: a.txt -> b.txt
            int deltaChangeId = SourceAdapter.RenameItem(sourceFile.ServerPath, sourceFile.NewServerPath);

            // 3. target system: a.txt -> edit a.txt
            int editChangesetId = TargetAdapter.EditFile(targetFile.LocalPath);

            // 4. migrate
            Run();

            // 5. resolve conflicts            
            // 5.1 check if we have correct conflicts in db
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be only 1 conflict");
            VCNameSpaceContentConflictType namespaceConflict = new VCNameSpaceContentConflictType();
            Assert.IsTrue(namespaceConflict.ReferenceName.Equals(conflicts[0].ConflictTypeReference.Value.ReferenceName), 
                "It should be vc namespace content conflict");

            // 5.2 user merge actions

            // 1) revert edit on a.txt
            // revert edit on a.txt
            PendEditToRevertFile(TargetWorkspace, targetFile, editChangesetId, editChangesetId - 1);
            TargetWorkspace.CheckIn(TargetWorkspace.GetPendingChanges(), "user: revert edit change on a.txt **NOMIGRATION**");

            // 2) rename a.txt to b.txt on target system
            int miChangeId = TargetAdapter.RenameItem(targetFile.ServerPath, targetFile.NewServerPath,
                "user: rename a.txt to b.txt **NOMIGRATION**");

            Dictionary<string, string> dataFields = new Dictionary<string, string>();
            dataFields.Add(VCContentConflictUserMergeChangeAction.MigrationInstructionChangeId, miChangeId.ToString());
            dataFields.Add(VCContentConflictUserMergeChangeAction.DeltaTableChangeId, deltaChangeId.ToString());

            foreach (RTConflict conflict in conflicts)
            {
                conflictResolver.TryResolveConflict(conflict, new VCContentConflictUserMergeChangeAction(), string.Format("$/;{0}", deltaChangeId), dataFields);
            }

            // 6. migrate
            Run(true, true);

            // 7. validation
            // no remaining conflicts
            conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be NO conflict");
            // content match
            Assert.IsTrue(VerifyContents());
        }
    }
}
