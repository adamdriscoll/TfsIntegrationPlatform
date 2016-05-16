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
    /// Test scenarios for rollback operations
    /// </summary>
    [TestClass]
    public class RollbackTest : Tfs2TfsVCTestCase
    {
        ///<summary>
        ///Scenario: 
        ///  1. edit a.txt -> a.txt
        ///  2. rollback the edit
        ///  3. Migrate a.txt
        ///</summary>
        [TestMethod(), Priority(1), Owner("peigu")]
        [Description("RollbackTest RollbackEdit")]
        public void RollbackEditTest()
        {
            string fileName = "a.txt";
            MigrationItemStrings sourceFile = new MigrationItemStrings(fileName, null, TestEnvironment, true);

            SourceAdapter.AddFile(sourceFile.LocalPath);

            // 1. Edit a.txt
            int editChangesetId = SourceAdapter.EditFile(sourceFile.LocalPath);

            // 2. Rollback the edit
            SourceAdapter.Rollback(editChangesetId, editChangesetId);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: 
        ///  1. undelete|edit a.txt
        ///  2. rollback the edit
        ///  3. Migrate a.txt
        ///</summary>
        [TestMethod(), Priority(1), Owner("peigu")]
        [Description("RollbackTest RollbackEdit")]
        public void RollbackEditDeleteTest()
        {
            MigrationItemStrings sourceFile = new MigrationItemStrings("a.txt", null, TestEnvironment, true);

            SourceAdapter.AddFile(sourceFile.LocalPath);

            int deleteChangesetId = SourceAdapter.DeleteItem(sourceFile.LocalPath);

            int deletionId = SourceWorkspace.VersionControlServer.GetChangeset(deleteChangesetId).Changes[0].Item.DeletionId;
            SourceWorkspace.Get();
            SourceWorkspace.PendUndelete(sourceFile.ServerPath, deletionId);

            TestUtils.EditRandomFile(sourceFile.LocalPath);
            SourceWorkspace.PendEdit(sourceFile.LocalPath);
            int editChangesetId = SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), "UndeleteEdit");

            // 2. Rollback the undelete|edit
            SourceAdapter.Rollback(editChangesetId, editChangesetId);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: 
        ///  1. delete a.txt -> a.txt
        ///  2. rollback the delete
        ///  3. Migrate a.txt
        ///</summary>
        [TestMethod(), Priority(1), Owner("peigu")]
        [Description("RollbackTest RollbackUnDeleteTest")]
        public void RollbackUnDeleteTest()
        {
            string fileName = "a.txt";
            MigrationItemStrings sourceFile = new MigrationItemStrings(fileName, null, TestEnvironment, true);

            SourceAdapter.AddFile(sourceFile.LocalPath);

            // 1. delete a.txt
            int deleteChangesetId = SourceAdapter.DeleteItem(sourceFile.LocalPath);

            // 2. Rollback the delete
            SourceAdapter.Rollback(deleteChangesetId, deleteChangesetId);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: 
        ///  1. rename a.txt -> b.txt
        ///  2. rollback the rename
        ///  3. Migrate
        ///</summary>
        [TestMethod(), Priority(1), Owner("peigu")]
        [Description("RollbackTest RollbackUnDeleteTest")]
        public void RollbackRenameTest()
        {
            MigrationItemStrings sourceFile = new MigrationItemStrings("a.txt", "b.txt", TestEnvironment, true);

            SourceAdapter.AddFile(sourceFile.LocalPath);

            // 1. rename a.txt
            int renameChangesetId = SourceAdapter.RenameItem(sourceFile.ServerPath, sourceFile.NewServerPath);

            // 2. Rollback the rename
            SourceAdapter.Rollback(renameChangesetId, renameChangesetId);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: 
        ///  1. edit a.txt 
        ///  2. rename a.txt -> b.txt
        ///  3. rollback both edit and rename
        ///  4. Migrate a.txt
        ///</summary>
        [TestMethod(), Priority(1), Owner("peigu")]
        [Description("RollbackTest RollbackRenameEditTest")]
        public void RollbackRenameEditTest()
        {
            MigrationItemStrings sourceFile = new MigrationItemStrings("a.txt", "b.txt", TestEnvironment, true);

            SourceAdapter.AddFile(sourceFile.LocalPath);

            // 1. edit a.txt
            int editChangesetId = SourceAdapter.EditFile(sourceFile.LocalPath);

            int renameChangesetId = SourceAdapter.RenameItem(sourceFile.ServerPath, sourceFile.NewServerPath, "Rename a.txt to b.txt");

            // 2. Rollback the edit
            SourceAdapter.Rollback(editChangesetId, renameChangesetId);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: 
        ///  1. edit a.txt 
        ///  2. delete a.txt 
        ///  3. rollback both edit and delete
        ///  4. Migrate a.txt
        ///</summary>
        [TestMethod(), Priority(1), Owner("peigu")]
        [Description("RollbackTest RollbackUndeleteEdit")]
        public void RollbackUndeleteEditTest()
        {
            MigrationItemStrings sourceFile = new MigrationItemStrings("a.txt", null, TestEnvironment, true);

            SourceAdapter.AddFile(sourceFile.LocalPath);

            // 1. edit a.txt
            int editChangesetId = SourceAdapter.EditFile(sourceFile.LocalPath);

            int deleteChangesetId = SourceAdapter.DeleteItem(sourceFile.LocalPath);

            // 2. Rollback the edit
            SourceAdapter.Rollback(editChangesetId, deleteChangesetId);

            RunAndValidate();
        }


        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("RollbackTest RollbackAddTest")]
        public void RollbackAddTest()
        {
            MigrationItemStrings sourceFile = new MigrationItemStrings("a.txt", null, TestEnvironment, true);

            int AddChangesetId = SourceAdapter.AddFile(sourceFile.LocalPath);

            // 2. Rollback the Add
            SourceAdapter.Rollback(AddChangesetId, AddChangesetId);

            RunAndValidate();
        }
    }
}
