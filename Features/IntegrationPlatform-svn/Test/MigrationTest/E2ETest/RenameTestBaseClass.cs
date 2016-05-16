// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using Microsoft.TeamFoundation.VersionControl.Client;
using MigrationTestLibrary;

namespace TfsVCTest
{
    public class RenameTestCaseBase : TfsVCTestCaseBase
    {
        #region Scenarios

        protected void UndeleteSourceRenameScenario(bool useSource)
        {
            MigrationItemStrings deletedFile = new MigrationItemStrings("file.txt", null, TestEnvironment, useSource);
            MigrationItemStrings renamedFile = new MigrationItemStrings("file.txt", "renamedFile.txt", TestEnvironment, useSource);

            SourceAdapter.AddFile(deletedFile.LocalPath);
            int deletionChangeSet = SourceAdapter.DeleteItem(deletedFile.ServerPath);

            SourceAdapter.AddFile(renamedFile.LocalPath);

            SourceWorkspace.Get(VersionSpec.Latest, GetOptions.Overwrite);
            SourceWorkspace.PendRename(renamedFile.LocalPath, renamedFile.NewLocalPath);
            SourceAdapter.UndeleteFile(deletedFile.ServerPath, deletionChangeSet);
        }

        protected void DeleteFolderRenameToSourceScenario(bool useSoruce)
        {
            MigrationItemStrings parent = new MigrationItemStrings("Parent/", "newParent/", TestEnvironment, useSoruce);
            MigrationItemStrings file1 = new MigrationItemStrings("child/def.txt", "Parent/child/def.txt", TestEnvironment, useSoruce);
            MigrationItemStrings file2 = new MigrationItemStrings("Parent/child/def.txt", null, TestEnvironment, useSoruce);
            MigrationItemStrings child = new MigrationItemStrings("child/", "Parent/child/", TestEnvironment, useSoruce);

            SourceAdapter.AddFolder(parent.LocalPath);


            SourceAdapter.AddFile(file1.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);

            SourceWorkspace.PendDelete(child.NewServerPath);
            SourceWorkspace.PendRename(child.ServerPath, child.NewServerPath);
            SourceWorkspace.PendRename(parent.ServerPath, parent.NewServerPath);

            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), RenameComment);
        }

        protected void RenameParentDeleteChildScenario(bool useSource)
        {
            MigrationItemStrings file1 = new MigrationItemStrings("folder1/file1.txt", "folder2/file1.txt", TestEnvironment, useSource);
            MigrationItemStrings file2 = new MigrationItemStrings("folder1/file2.txt", "folder2/file2.txt", TestEnvironment, useSource);
            MigrationItemStrings folder1 = new MigrationItemStrings("folder1/", "folder2/", TestEnvironment, useSource);

            SourceAdapter.AddFile(file1.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);

            SourceWorkspace.PendRename(folder1.ServerPath, folder1.NewServerPath);
            SourceWorkspace.PendDelete(file1.NewServerPath);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), MultiActionComment);
        }

        protected void TwoDeleteUnDeleteScenario(bool useSource)
        {
            MigrationItemStrings file1 = new MigrationItemStrings("file.txt", null, TestEnvironment, useSource);
            MigrationItemStrings file2 = new MigrationItemStrings("file.txt", null, TestEnvironment, useSource);

            SourceAdapter.AddFile(file1.LocalPath);

            int file1ChangeSetId = SourceAdapter.DeleteItem(file1.ServerPath);

            SourceAdapter.AddFile(file2.LocalPath);

            int file2ChangeSetId = SourceAdapter.DeleteItem(file2.ServerPath);

            SourceAdapter.UndeleteFile(file1.ServerPath, file1ChangeSetId);

            SourceAdapter.DeleteItem(file1.ServerPath);

            SourceAdapter.UndeleteFile(file2.ServerPath, file2ChangeSetId);
        }

        protected void RenameOrderScenario(bool useSource)
        {
            MigrationItemStrings parentDirectory = new MigrationItemStrings("Parent", "NewDir", TestEnvironment, useSource);
            MigrationItemStrings subDirectory1 = new MigrationItemStrings("Parent/SubDir1", "SubDir1", TestEnvironment, useSource);
            MigrationItemStrings subDirectory2 = new MigrationItemStrings("Parent/SubDir2", "SubDir2", TestEnvironment, useSource);

            SourceAdapter.AddFolder(parentDirectory.LocalPath);
            SourceAdapter.AddFolder(subDirectory1.LocalPath);
            SourceAdapter.AddFolder(subDirectory2.LocalPath);

            SourceWorkspace.Get(VersionSpec.Latest, GetOptions.Overwrite);
            SourceWorkspace.PendRename(subDirectory1.LocalPath, subDirectory1.NewLocalPath);
            SourceWorkspace.PendRename(subDirectory2.LocalPath, subDirectory2.NewLocalPath);
            SourceWorkspace.PendRename(parentDirectory.LocalPath, parentDirectory.NewLocalPath);
            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), RenameComment);
        }

        protected void ChainedRenameEdit(bool useSource)
        {
            MigrationItemStrings file1 = new MigrationItemStrings("CodeGenProject.vb", null, TestEnvironment, useSource);
            MigrationItemStrings file2 = new MigrationItemStrings("CodeGenSolution.vb", null, TestEnvironment, useSource);
            MigrationItemStrings file3 = new MigrationItemStrings("CodeGenTable.vb", null, TestEnvironment, useSource);

            SourceAdapter.AddFile(file1.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);

            SourceWorkspace.Get(VersionSpec.Latest, GetOptions.Overwrite);
            TestUtils.EditRandomFile(file1.LocalPath);
            SourceWorkspace.PendEdit(file1.LocalPath);
            TestUtils.EditRandomFile(file2.LocalPath);
            SourceWorkspace.PendEdit(file2.LocalPath);
            SourceWorkspace.PendRename(file1.LocalPath, file3.LocalPath);
            SourceWorkspace.PendRename(file2.LocalPath, file1.LocalPath);

            SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), "ChainedRenameEdit test");
        }

        #endregion Scenarios

    }
}
