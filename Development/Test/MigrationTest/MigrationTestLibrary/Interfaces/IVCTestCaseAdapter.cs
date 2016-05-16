// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MigrationTestLibrary
{
    public interface IVCTestCaseAdapter : ITestCaseAdapter
    {
        AdapterType AdapterType { get; }
        string WorkspaceLocalPath { get; }
        char PathSeparator { get; }

        int AddFile(string localPath);
        int AddFiles(string[] localPaths);
        int AddFolder(string localPath);
        int EditFile(string localPath);
        int EditFile(string localPath, string copyFromFilePath);
        int EditFile(string localPath, string copyFromFilePath, string checkinComment);
        int RenameItem(string oldPath, string newPath);
        int RenameItem(string oldPath, string newPath, string checkinComment);
        int DeleteItem(string localPath);
        int DeleteItem(string localPath, string checkinComment);

        void UndeleteFile(string serverPath, int changesetId);
        int BranchItem(MigrationItemStrings branchItem);
        int BranchItem(MigrationItemStrings sourceBranchItem, MigrationItemStrings targetBranchItem);
        int MergeItem(MigrationItemStrings mergeItem, int mergeFromChangeset);
        int MergeItem(MigrationItemStrings mergeItem, int mergeFrom, int mergeTo);

        /// <summary>
        /// Rollback the specified changeset. On TFS rollback version is the changeset to be rolled back.
        /// </summary>
        /// <param name="rollbackVersion"></param>
        /// <returns></returns>
        int Rollback(int rollbackFromVersion, int rollbackToVersion);
    }
}
