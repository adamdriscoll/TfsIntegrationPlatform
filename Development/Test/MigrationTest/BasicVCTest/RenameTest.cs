// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTestLibrary;

namespace BasicVCTest
{
    [TestClass]
    public class RenameTest : BasicVCTestCaseBase
    {
        ///<summary>
        ///Scenario: Reverse the Case of a file and a folder in TFS and migrate the change to TFS
        ///Expected Result: Rename is migrated sucessfully
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Reverse the Case of a file and a folder in TFS and migrate the change to TFS")]
        public void RenameCaseTest()
        {
            MigrationItemStrings folder;
            MigrationItemStrings file;

            if ((SourceAdapter.AdapterType == AdapterType.TFS2008VC) ||
            (SourceAdapter.AdapterType == AdapterType.TFS2010VC))
            {
                // change case only
                folder = new MigrationItemStrings("FOLDER", "folder", TestEnvironment, true);
                file = new MigrationItemStrings("file.txt", "FILE.txt", TestEnvironment, true);

            }
            else
            {
                // non-TFS adapters don't support case-only rename
                folder = new MigrationItemStrings("FOLDER1", "folder2", TestEnvironment, true);
                file = new MigrationItemStrings("file1.txt", "FILE2.txt", TestEnvironment, true);
            }

            SourceAdapter.AddFolder(folder.LocalPath);
            SourceAdapter.AddFile(file.LocalPath);

            SourceAdapter.RenameItem(folder.LocalPath, folder.NewLocalPath);
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
            MigrationItemStrings file = new MigrationItemStrings("folder/file.txt", "folder1/file.txt", TestEnvironment, true);

            SourceAdapter.AddFolder(folder.LocalPath);
            SourceAdapter.AddFile(file.LocalPath);
            SourceAdapter.DeleteItem(file.LocalPath);
            SourceAdapter.AddFile(file.LocalPath);
            SourceAdapter.DeleteItem(file.LocalPath);

            SourceAdapter.RenameItem(folder.LocalPath, folder.NewLocalPath);

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
            SourceAdapter.DeleteItem(file.LocalPath);
            SourceAdapter.AddFile(file.LocalPath);
            SourceAdapter.RenameItem(folder.LocalPath, folder.NewLocalPath);

            RunAndValidate();
        }
    }
}