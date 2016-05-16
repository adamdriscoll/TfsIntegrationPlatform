﻿// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.IO;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTestLibrary;

namespace TfsVCTest
{
    [TestClass]
    public class RenameTest : RenameTestCaseBase
    {
        static string stringOf150Chars = "12345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890";

        [TestInitialize]
        public override void Initialize()
        {
            base.Initialize();
        }

        ///<summary>
        ///Scenario: Reverse the Case of a file and a folder in TFS and migrate the change to TFS
        ///Expected Result: Rename is migrated sucessfully
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Reverse the Case of a file and a folder in TFS and migrate the change to TFS")]
        public void RenameCaseTest()
        {
            MigrationItemStrings folder = new MigrationItemStrings("FOLDER", "folder", TestEnvironment, true);
            MigrationItemStrings file = new MigrationItemStrings("file.txt", "FILE.txt", TestEnvironment, true);

            SourceAdapter.AddFolder(folder.LocalPath);
            SourceAdapter.AddFile(file.LocalPath);

            SourceAdapter.RenameItem(folder.ServerPath, folder.NewServerPath);
            SourceAdapter.RenameItem(file.LocalPath, file.NewLocalPath);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Multiple deletes of same file under a renamed folder
        ///Expected Result: Rename is migrated successfully
        ///</summary>
        [TestMethod(), Priority(2), Owner("andrhs")]
        [Description("Multiple deletes of same file under a renamed folder")]
        public void RenameWithDeletes()
        {
            MigrationItemStrings folder = new MigrationItemStrings("folder", "folder1", TestEnvironment, true);
            MigrationItemStrings file = new MigrationItemStrings("folder/file.txt", "folder/file.txt", TestEnvironment, true);

            SourceAdapter.AddFolder(folder.LocalPath);
            SourceAdapter.AddFile(file.LocalPath);
            SourceAdapter.DeleteItem(file.ServerPath);
            SourceAdapter.AddFile(file.LocalPath);
            SourceAdapter.DeleteItem(file.ServerPath);

            SourceAdapter.RenameItem(folder.ServerPath, folder.NewServerPath);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: rename 1.txt to 2.txt and rename 3.txt to 1.txt in same changeset
        ///Expected Result: Rename is migrated successfully
        ///</summary>
        [TestMethod(), Priority(2), Owner("andrhs")]
        [Description("Rename with SourceRename")]
        public void RenameWithSourceRename()
        {
            MigrationItemStrings file1 = new MigrationItemStrings("1.txt", "2.txt", TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings("3.txt", "1.txt", TestEnvironment, true);

            SourceAdapter.AddFile(file1.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);

            SourceWorkspace.Get(VersionSpec.Latest, GetOptions.Overwrite);
            SourceWorkspace.PendRename(file1.ServerPath, file1.NewServerPath);
            SourceWorkspace.PendRename(file2.ServerPath, file2.NewServerPath);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), RenameComment);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Rename child from long to short and rename parent from short to long. 
        ///      And rename child from short to long and rename parent from long to short.
        ///Expected Result: Rename is migrated sucessfully
        ///</summary>
        [TestMethod(), Priority(2), Owner("peigu")]
        [Description("Rename child from long to short and rename parent from short to long")]
        public void RenameParentLongerChildshorterTest()
        {
            MigrationItemStrings folder = new MigrationItemStrings("folder", "folder" + stringOf150Chars, TestEnvironment, true);
            MigrationItemStrings file = new MigrationItemStrings("folder/file" + stringOf150Chars + ".txt", "folder/file.txt", TestEnvironment, true);

            MigrationItemStrings folder2 = new MigrationItemStrings("folder2" + stringOf150Chars, "folder2", TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings("folder2" + stringOf150Chars + "/file.txt", "folder2/file.txt" + stringOf150Chars + ".txt", TestEnvironment, true);


            SourceAdapter.AddFolder(folder.LocalPath);
            SourceAdapter.AddFile(file.LocalPath);
            SourceAdapter.AddFolder(folder2.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);

            SourceWorkspace.Get(VersionSpec.Latest, GetOptions.Overwrite);
            SourceWorkspace.PendRename(file.LocalPath, file.NewLocalPath);
            SourceWorkspace.PendRename(folder.LocalPath, folder.NewLocalPath);
            SourceWorkspace.PendRename(folder2.LocalPath, folder2.NewLocalPath);
            SourceWorkspace.PendRename(file2.LocalPath, file2.NewLocalPath);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), "Rename folder longer and file shorter");

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Rename child from long to short and rename grand parent from short to long. 
        ///      And rename child from short to long and rename grand parent from long to short.
        ///Expected Result: Rename is migrated sucessfully
        ///</summary>
        [TestMethod(), Priority(3), Owner("peigu")]
        [Description("Rename child from long to short and rename grand parent from short to long")]
        public void RenameGrandParentLongerChildshorterTest()
        {
            MigrationItemStrings folder = new MigrationItemStrings("folder/p1/p2", "folder" + stringOf150Chars + "/p1/p2", TestEnvironment, true);
            MigrationItemStrings file = new MigrationItemStrings("folder/p1/p2/file" + stringOf150Chars + ".txt", "folder/p1/p2/file.txt", TestEnvironment, true);

            MigrationItemStrings folder2 = new MigrationItemStrings("folder2" + stringOf150Chars + "/p1/p2", "folder2/p1/p2", TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings("folder2" + stringOf150Chars + "/p1/p2" + "/file.txt", "folder2/p1/p2/file.txt" + stringOf150Chars + ".txt", TestEnvironment, true);


            SourceAdapter.AddFolder(folder.LocalPath);
            SourceAdapter.AddFile(file.LocalPath);
            SourceAdapter.AddFolder(folder2.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);

            SourceWorkspace.Get(VersionSpec.Latest, GetOptions.Overwrite);
            SourceWorkspace.PendRename(file.LocalPath, file.NewLocalPath);
            SourceWorkspace.PendRename(folder.LocalPath, folder.NewLocalPath);
            SourceWorkspace.PendRename(folder2.LocalPath, folder2.NewLocalPath);
            SourceWorkspace.PendRename(file2.LocalPath, file2.NewLocalPath);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), "Rename folder longer and file shorter");

            RunAndValidate();
        }



        ///<summary>
        ///Scenario: Undelete a file and then pend a case only rename
        ///Expected Result: Rename is migrated sucessfully
        ///</summary>
        [TestMethod(), Priority(2), Owner("peigu")]
        [Description("Undelete a file and then pend a case only rename")]
        public void UndeleteCaseOnlyRenameTest()
        {
            MigrationItemStrings folder = new MigrationItemStrings("folder", "Folder", TestEnvironment, true);
            MigrationItemStrings file = new MigrationItemStrings("file.txt", "File.txt", TestEnvironment, true);

            SourceAdapter.AddFolder(folder.LocalPath);
            SourceAdapter.AddFile(file.LocalPath);

            int deleteChangeset = SourceAdapter.DeleteItem(folder.ServerPath);

            Item item = SourceTfsClient.GetChangeset(deleteChangeset).Changes[0].Item;
            SourceWorkspace.Get();
            SourceWorkspace.PendUndelete(folder.LocalPath, item.DeletionId);
            SourceWorkspace.PendUndelete(file.LocalPath, item.DeletionId);

            SourceWorkspace.PendRename(folder.ServerPath, folder.NewServerPath);
            SourceWorkspace.PendRename(file.LocalPath, file.NewLocalPath);

            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), RenameComment);


            RunAndValidate();
        }


        ///<summary>
        ///Scenario: Reverse the Case of a folder and add a sub item in the same changeset. Migrate the change to TFS
        ///Expected Result: Rename and add are  migrated sucessfully
        ///</summary>
        [TestMethod(), Priority(2), Owner("peigu")]
        [Description("Reverse the Case of a folder and add a sub item in the same changeset. Migrate the change to TFS")]
        public void RenameCaseAddSubitemTest()
        {
            MigrationItemStrings folder = new MigrationItemStrings("FOLDER", "folder", TestEnvironment, true);
            MigrationItemStrings file = new MigrationItemStrings("folder/file.txt", null, TestEnvironment, true);

            SourceAdapter.AddFolder(folder.LocalPath);

            SourceWorkspace.Get(VersionSpec.Latest, GetOptions.Overwrite);
            SourceWorkspace.PendRename(folder.ServerPath, folder.NewServerPath);
            SourceAdapter.AddFile(file.LocalPath);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Migrate Two renames; one of which is a subpath of the other
        ///Expected Result: Both renames are migrated sucessfully
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Migrate Two renames; one of which is a subpath of the other")]
        public void RenameSubstringTest()
        {
            MigrationItemStrings subStringFile = new MigrationItemStrings("file.txt", "renamed-file.txt", TestEnvironment, true);
            MigrationItemStrings superStringFile = new MigrationItemStrings("file.txt1", "renamed-file.txt1", TestEnvironment, true);
            MigrationItemStrings subStringFolder = new MigrationItemStrings("folder", "renamed-folder", TestEnvironment, true);
            MigrationItemStrings superStringFolder = new MigrationItemStrings("folder1", "renamed-folder1", TestEnvironment, true);

            SourceAdapter.AddFile(subStringFile.LocalPath);
            SourceAdapter.AddFile(superStringFile.LocalPath);
            SourceAdapter.AddFolder(subStringFolder.LocalPath);
            SourceAdapter.AddFolder(superStringFolder.LocalPath);

            SourceAdapter.RenameItem(subStringFile.LocalPath, subStringFile.NewLocalPath);
            SourceAdapter.RenameItem(superStringFile.LocalPath, superStringFile.NewLocalPath);

            SourceAdapter.RenameItem(subStringFolder.LocalPath, subStringFolder.NewLocalPath);
            SourceAdapter.RenameItem(superStringFolder.LocalPath, superStringFolder.NewLocalPath);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Migrate the move a folder under a newly created folder with the same name
        ///Expected Result: Folder is moved sucessfully
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Migrate the move a folder under a newly created folder with the same name")]
        public void RenameFolderUnderItselfTest()
        {
            MigrationItemStrings file = new MigrationItemStrings("folder/file.txt", null, TestEnvironment, true);
            MigrationItemStrings movedFolder = new MigrationItemStrings("folder", "temp-folder", TestEnvironment, true);
            MigrationItemStrings tempFolder = new MigrationItemStrings("temp-folder", "folder/folder", TestEnvironment, true);
            MigrationItemStrings parentFolder = new MigrationItemStrings("folder", null, TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);

            SourceWorkspace.PendRename(movedFolder.ServerPath, movedFolder.NewServerPath);

            Directory.CreateDirectory(parentFolder.LocalPath);
            SourceWorkspace.PendAdd(parentFolder.LocalPath);

            SourceWorkspace.PendRename(tempFolder.ServerPath, tempFolder.NewServerPath);

            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), "Move a folder below itsself");

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Move a file to a newly created sub folder and rename the parent folder in the same changeset. Then migrate.
        ///Expected Result: Successful migration
        ///</summary>
        [TestMethod(), Priority(2), Owner("peigu")]
        [Description("Move a file to a newly created sub folder and rename the parent folder in the same changeset. Then migrate")]
        public void RenameParentMoveToBelowItselfTest()
        {
            MigrationItemStrings file = new MigrationItemStrings("parent/file.txt", null, TestEnvironment, true);
            MigrationItemStrings parentFolder = new MigrationItemStrings("parent", null, TestEnvironment, true);
            MigrationItemStrings newParentFolder = new MigrationItemStrings("newparent", null, TestEnvironment, true);
            MigrationItemStrings subFolder = new MigrationItemStrings("parent/subFolder", null, TestEnvironment, true);
            MigrationItemStrings newFileLocation = new MigrationItemStrings("parent/subFolder/file.txt", null, TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);

            Directory.CreateDirectory(subFolder.LocalPath);
            SourceWorkspace.PendRename(file.ServerPath, newFileLocation.ServerPath);
            SourceWorkspace.PendRename(parentFolder.ServerPath, newParentFolder.ServerPath);

            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), "Move a folder below itsself");

            RunAndValidate();
        }


        ///<summary>
        ///Scenario: rename, edit
        ///Expected Result: Successufly migration.
        ///</summary>
        [TestMethod(), Priority(2), Owner("hykwon")]
        [Description("RenameParentAddSubFolderTest")]
        public void RenameParentAddSubFolderTest()
        {
            MigrationItemStrings parentFolder = new MigrationItemStrings("parent", "new-parent", TestEnvironment, true);
            MigrationItemStrings file = new MigrationItemStrings("filea.txt", null, TestEnvironment, true);
            MigrationItemStrings subFolder = new MigrationItemStrings("new-parent/subdir", null, TestEnvironment, true);

            SourceAdapter.AddFolder(parentFolder.LocalPath);
            SourceAdapter.AddFile(Path.Combine(parentFolder.LocalPath, file.Name));

            SourceWorkspace.PendRename(parentFolder.ServerPath, parentFolder.NewServerPath);
            Directory.CreateDirectory(subFolder.LocalPath);
            SourceWorkspace.PendAdd(subFolder.LocalPath);
            SourceWorkspace.PendRename(Path.Combine(parentFolder.NewServerPath, file.Name), Path.Combine(subFolder.ServerPath, file.Name));

            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), "added a new sub folder. moved an item to the sub folder. rename parent folder");

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Rename $/Project/ParentDir/SubDir1 to $/Project/SubDir1; Rename $/Project/ParentDir/SubDir2 to $/Project/SubDir2; Rename $/Project/ParentDir to $/Project/ADifferentName
        ///Expected Result: The order of the renames is preserved, and migrated correctly
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Rename $/Project/ParentDir/SubDir1 to $/Project/SubDir1; Rename $/Project/ParentDir/SubDir2 to $/Project/SubDir2; Rename $/Project/ParentDir to $/Project/NewDir")]
        public void RenameOrderTest()
        {
            RenameOrderScenario(true);
            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Delete $/Project/file2.txt and Rename $/Project/file1.txt to $/Project/file2.txt in the same changeset
        ///Expected Result: Changesets are successfully migrated.
        ///</summary>
        [TestMethod(), Priority(2), Owner("peigu")]
        [Description("Delete $/Project/file2.txt and Rename $/Project/file1.txt to $/Project/file2.txt in the same changeset")]
        public void RenameToDeletedItemTest()
        {
            MigrationItemStrings renameFile = new MigrationItemStrings("file1.txt", "file2.txt", TestEnvironment, true);
            MigrationItemStrings deleteFile = new MigrationItemStrings("file2.txt", null, TestEnvironment, true);

            SourceAdapter.AddFile(renameFile.LocalPath);
            SourceAdapter.AddFile(deleteFile.LocalPath);

            SourceWorkspace.Get(VersionSpec.Latest, GetOptions.Overwrite);
            SourceWorkspace.PendDelete(deleteFile.ServerPath);
            SourceWorkspace.PendRename(renameFile.ServerPath, renameFile.NewServerPath);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), "Delete file2.txt and rename file1.txt to file2.txt");

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Rename a file and undelete another file on the source of the rename
        ///Expected Result: Correct file is undeleted and correct file is renamed.
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Rename a file and undelete another file on the source of the rename")]
        public void UndeleteSourceRenameTest()
        {
            UndeleteSourceRenameScenario(true);
            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Delete a folder then move another folder with the same name to that location and rename the parent folder
        ///Expected Result: Migrate to  properly
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Delete a folder then move another folder with the same name to that location and rename the parent folder")]
        public void DeleteFolderRenameToSourceTest()
        {
            DeleteFolderRenameToSourceScenario(true);
            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Rename a folder then delete one of its children
        ///Expected Result: Folder is renamed. Child is deleted, in both servers
        ///</summary>
        [TestMethod(), Priority(3), Owner("curtisp")]
        [Description("Rename a folder then delete one of its children")]
        public void RenameParentDeleteChildTest()
        {
            RenameParentDeleteChildScenario(true);
            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Delete two files with the same path. Undelete one file. Delete it again. Undelete the other file
        ///Expected Result: Correct file is undeleted both times.
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Delete two files with the same path. Undelete one file. Delete it again. Undelete the other file")]
        public void TwoDeleteUnDeleteTest()
        {
            RenameParentDeleteChildScenario(true);
            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Rename one file to the other to create a chained rename, then edit both files. 
        ///Expected Result: Successufly migration.
        ///Note: This test case is created from codeplex feedback "Multiple items conflict with the same change"
        ///</summary>
        [TestMethod(), Priority(2), Owner("peigu")]
        [Description("Rename one file to the other to create a chained rename, then edit both files.")]
        public void ChainedRenameEditTest()
        {
            ChainedRenameEdit(true);
            RunAndValidate();
        }


        ///<summary>
        ///Scenario: Rename a folder with both a deleted and non-deleted version of a file. 
        ///Expected Result: Rename is migrated sucessfully
        ///</summary>
        [TestMethod(), Priority(1), Owner("peigu")]
        [Description("Rename a folder with both a deleted and non-deleted version of a file.")]
        public void RenameNamespaceReuseTest()
        {
            MigrationItemStrings folder = new MigrationItemStrings("folder", "folder-rename", TestEnvironment, true);
            MigrationItemStrings file = new MigrationItemStrings("folder/file.txt", "folder/file.txt", TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);

            SourceAdapter.DeleteItem(file.ServerPath);

            SourceAdapter.AddFile(file.LocalPath);

            SourceAdapter.RenameItem(folder.ServerPath, folder.NewServerPath);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Rename a folder with both a deleted and non-deleted version of a file. 
        ///Expected Result: Rename is migrated sucessfully
        ///</summary>
        [TestMethod(), Priority(1), Owner("peigu")]
        [Description("Rename a folder with both a deleted and non-deleted version of a file.")]
        public void RenameNamespaceReuseMergeTest()
        {
            MigrationItemStrings sourceFolder = new MigrationItemStrings("source/folder", "source/folder-rename", TestEnvironment, true);
            MigrationItemStrings sourceFile = new MigrationItemStrings("source/folder/file.txt", "source/folder/file.txt", TestEnvironment, true);
            MigrationItemStrings branchItem = new MigrationItemStrings("source", "target", TestEnvironment, true);

            SourceAdapter.AddFile(sourceFile.LocalPath);
            SourceAdapter.BranchItem(branchItem);

            int deleteChangeset = SourceAdapter.DeleteItem(sourceFile.ServerPath);

            SourceAdapter.AddFile(sourceFile.LocalPath);

            SourceAdapter.RenameItem(sourceFolder.ServerPath, sourceFolder.NewServerPath);

            SourceAdapter.MergeItem(branchItem, deleteChangeset);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Rename a.txt to b.txt and add a.txt in the same changeset. 
        ///Expected Result: Rename is migrated sucessfully
        ///</summary>
        [TestMethod(), Priority(1), Owner("peigu")]
        [Description("Rename a.txt to b.txt and add a.txt in the same changeset.")]
        public void RenameAddNamespaceReuseTest()
        {
            MigrationItemStrings file = new MigrationItemStrings("a.txt", "b.txt", TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);

            SourceWorkspace.Get(VersionSpec.Latest, GetOptions.Overwrite);
            SourceWorkspace.PendRename(file.ServerPath, file.NewServerPath);

            TestUtils.CreateRandomFile(file.LocalPath, 10);

            SourceWorkspace.PendAdd(file.LocalPath);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), "Rename a.txt to b.txt and add a.txt in the same changeset.");

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Migrate the move a folder under a newly created folder with the same name
        ///Expected Result: All folders are renamed successfully with the child files moving with the implicit renames
        ///</summary>
        [TestMethod(), Priority(2), Owner("billbar")]
        [Description("Migrate a cyclic rename that involves three folders (each with a file in it)")]
        public void ThreeWayCyclicRenameTest()
        {
            MigrationItemStrings folder1 = new MigrationItemStrings("folder1/", null, TestEnvironment, true);
            MigrationItemStrings folder2 = new MigrationItemStrings("folder2/", null, TestEnvironment, true);
            MigrationItemStrings folder3 = new MigrationItemStrings("folder3/", null, TestEnvironment, true);
            MigrationItemStrings temp = new MigrationItemStrings("temp/", null, TestEnvironment, true);

            MigrationItemStrings file1 = new MigrationItemStrings("folder1/file1.txt", null, TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings("folder2/file2.txt", null, TestEnvironment, true);
            MigrationItemStrings file3 = new MigrationItemStrings("folder3/file3.txt", null, TestEnvironment, true);

            SourceAdapter.AddFolder(folder1.LocalPath);
            SourceAdapter.AddFolder(folder2.LocalPath);
            SourceAdapter.AddFolder(folder3.LocalPath);
            SourceAdapter.AddFile(file1.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);
            SourceAdapter.AddFile(file3.LocalPath);

            // Create a cyclic rename of the three folders
            SourceWorkspace.Get(VersionSpec.Latest, GetOptions.Overwrite);
            SourceWorkspace.PendRename(folder3.ServerPath, temp.ServerPath);
            SourceWorkspace.PendRename(folder2.ServerPath, folder3.ServerPath);
            SourceWorkspace.PendRename(folder1.ServerPath, folder2.ServerPath);
            SourceWorkspace.PendRename(temp.ServerPath, folder1.ServerPath);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), RenameComment);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Migrate the move a folder under a newly created folder with the same name
        ///Expected Result: All folders are renamed successfully with the child files moving with the implicit renames
        ///</summary>
        [TestMethod(), Priority(2), Owner("billbar")]
        [Description("Migrate a cyclic rename that involves three folders (each with a file in it)")]
        public void CyclicRenameAcrossFoldersTest()
        {
            MigrationItemStrings folder1 = new MigrationItemStrings("folder1/", null, TestEnvironment, true);
            MigrationItemStrings folder2 = new MigrationItemStrings("folder2/", null, TestEnvironment, true);

            MigrationItemStrings file1 = new MigrationItemStrings("folder1/file1.txt", null, TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings("folder2/file2.txt", null, TestEnvironment, true);
            MigrationItemStrings temp = new MigrationItemStrings("temp.txt", null, TestEnvironment, true);

            SourceAdapter.AddFolder(folder1.LocalPath);
            SourceAdapter.AddFolder(folder2.LocalPath);
            SourceAdapter.AddFile(file1.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);

            // Create a cyclic rename of two files in different directories
            SourceWorkspace.Get(VersionSpec.Latest, GetOptions.Overwrite);
            SourceWorkspace.PendRename(file1.ServerPath, temp.ServerPath);
            SourceWorkspace.PendRename(file2.ServerPath, file1.ServerPath);
            SourceWorkspace.PendRename(temp.ServerPath, file1.ServerPath);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), RenameComment);

            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Migrate deleted folder with a source rename and another file in it
        ///Expected Result: All files under the deleted folder are removed
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Migrate deleted folder with a source rename and another file in it")]
        public void SourceRenameAndDeleteTest()
        {
            MigrationItemStrings folder1 = new MigrationItemStrings("folder1/", null, TestEnvironment, true);
            MigrationItemStrings folder2 = new MigrationItemStrings("folder2/", null, TestEnvironment, true);

            MigrationItemStrings file1 = new MigrationItemStrings("folder1/file1.txt", null, TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings("folder1/file2.txt", "folder2/file2.txt", TestEnvironment, true);

            SourceAdapter.AddFolder(folder1.LocalPath);
            SourceAdapter.AddFolder(folder2.LocalPath);
            SourceAdapter.AddFile(file1.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);

            SourceAdapter.RenameItem(file2.LocalPath, file2.NewLocalPath);
            SourceAdapter.DeleteItem(folder1.LocalPath);


            RunAndValidate();
        }

        ///<summary>
        ///Scenario: Migrate deleted folder with a source rename and another file in it
        ///Expected Result: All files under the deleted folder are removed
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Migrate deleted folder with a source rename and another file in it")]
        public void MergeSourceRenameAndDeleteTest()
        {
            MigrationItemStrings branch = new MigrationItemStrings("source/", "target/", TestEnvironment, true);

            MigrationItemStrings folder2 = new MigrationItemStrings("folder2/", null, TestEnvironment, true);

            MigrationItemStrings file1 = new MigrationItemStrings(branch.Name + "file1.txt", null, TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings(branch.Name + "file2.txt", folder2.Name + "file2.txt", TestEnvironment, true);

            SourceAdapter.AddFolder(folder2.LocalPath);
            SourceAdapter.AddFile(file1.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);

            SourceAdapter.BranchItem(branch);

            SourceAdapter.RenameItem(file2.LocalPath, file2.NewLocalPath);
            SourceAdapter.DeleteItem(file1.LocalPath);

            SourceAdapter.MergeItem(branch, 1);

            RunAndValidate();
        }
    }
}