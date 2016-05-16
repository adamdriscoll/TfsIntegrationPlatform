// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MigrationTestLibrary;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter;

namespace ClearCaseTCAdapter
{
    [TCAdapterDescription(m_adapterGuid, m_adapterName)]
    public class ClearCaseTCAdapter : IVCTestCaseAdapter
    {
        #region constants
        public const string AddComment = "ClearCaseTCAdapter add";
        public const string RenameComment = "ClearCaseTCAdapter rename";
        public const string EditComment = "ClearCaseTCAdapter edit";
        public const string DeleteComment = "ClearCaseTCAdapter delete";
        #endregion

        private const string m_adapterGuid = "E6EE3EF6-6B1B-470c-AB89-82B5C418BDB3";
        private const string m_adapterName = "ClearCase TestCase Adapter";

        private ClearCaseServer m_clearCaseServer;
        private string m_workspaceServerPath;
        private string m_workspaceLocalPath;
        private string m_workspaceLocalRoot;
        private string m_viewPath;
        private string m_storageLocation;
        private string m_storageLocationLocalPath;

        public AdapterType AdapterType
        {
            get { return AdapterType.ClearCaseDetailedHistory; }
        }

        public char PathSeparator
        {
            get
            {
                return '\\';
            }
        }

        public void Initialize(EndPoint env)
        {
            string curDirectory = Directory.GetCurrentDirectory();

            string viewName = env.ViewName;

            string vobName = env.VobName;

            if (!vobName.StartsWith(@"\"))
            {
                vobName = @"\" + vobName;
            }

            // set these values to something so the product code doesn't complain later
            env.TeamProject = viewName;
            env.ServerUrl = vobName;

            string localPath = "test" + DateTime.Now.ToString("yyyy'-'MM'-'dd'_'HH'_'mm'_'ss");

            m_storageLocation = env.UncStorageLocation;
            m_storageLocationLocalPath = env.LocalStorageLocation;

            Trace.TraceInformation("VobName = {0}", vobName);
            Trace.TraceInformation("ViewName = {0}", viewName);
            Trace.TraceInformation("UncStorageLocation = {0}", m_storageLocation);
            Trace.TraceInformation("LocalStorageLocation = {0}", m_storageLocationLocalPath);

            localPath = String.Format("mt\\{0}\\{1}", localPath, env.TestName);

            m_workspaceServerPath = Path.Combine(vobName, localPath);

            m_viewPath = string.Format("{0}\\{1}", m_storageLocation, viewName);
            m_workspaceLocalRoot = string.Format("{0}\\{1}{2}", m_storageLocation, viewName, vobName);
            m_workspaceLocalPath = Path.Combine(m_workspaceLocalRoot, localPath);

            List<string> vobList = new List<string>();
            vobList.Add(vobName);
            m_clearCaseServer = ClearCaseServer.GetInstance(m_storageLocation, m_storageLocationLocalPath, viewName, vobList, "main"); // vobName + localPath
            m_clearCaseServer.Initialize();
            m_clearCaseServer.Update(m_viewPath);

            Directory.SetCurrentDirectory(curDirectory);
        }

        public void Cleanup()
        {
            Trace.TraceInformation("ClearCaseTCAdapter: Cleanup BEGIN");

            cleanupTestFolder();

            Trace.TraceInformation("ClearCaseTCAdapter: Cleanup END");
            ClearCaseServer.CleanUp();
        }

        private void cleanupTestFolder()
        {
            // Todo, implement the code to delete the test folder.
        }

        public string FilterString
        {
            get
            {
                return m_workspaceServerPath;
            }
        }

        public string WorkspaceLocalPath
        {
            get
            {
                return m_workspaceLocalPath;
            }
        }

        public int AddFile(string localPath)
        {
            Debug.Assert(localPath.StartsWith(m_workspaceLocalRoot));

            string curDirectory = Directory.GetCurrentDirectory();

            string path = localPath.Substring(m_workspaceLocalRoot.Length);
            path = TrimLeadingPathSeparator(path);
            string[] paths = path.Split('\\');
            string filename = paths[paths.Length - 1];

            // create and check out directories
            Execute("cd {0}", m_workspaceLocalRoot);
            try
            {
                Execute("co -nc .");
            }
            catch (ClearToolCommandException)
            {
                // Ignore error which is likely to be that . is already checked out to the view 
                // due to a previous test not cleanly up properly and we can continue.
            }

            for (int i = 0; i < paths.Length - 1; i++)
            {
                TryCreateAndCheckOutDir(paths[i]);
                Execute("cd {0}", paths[i]);
            }

            // create and check-in the file
            TestUtils.CreateRandomFile(localPath, 10);
            Execute("mkelem -ci -nc {0}", filename);

            // check-in all checked-out directories
            if (paths.Length > 1)
            {
                for (int i = paths.Length - 2; i >= 0; i--)
                {
                    Execute("ci -nc .");
                    Execute("cd ..");
                }
                Execute("ci -c '{0}' .", AddComment);
            }

            Directory.SetCurrentDirectory(curDirectory);
            return 0;
        }

        public int AddFiles(string[] localPaths)
        {
            foreach (string localPath in localPaths)
            {
                AddFile(localPath);
            }

            return 0;
        }

        public int AddFolder(string localPath)
        {
            Debug.Assert(localPath.StartsWith(m_workspaceLocalRoot));

            string curDirectory = Directory.GetCurrentDirectory();

            string path = localPath.Substring(m_workspaceLocalRoot.Length);
            path = TrimLeadingPathSeparator(path);
            string[] paths = path.Split('\\');

            // create and check out directories
            Execute("cd {0}", m_workspaceLocalRoot);
            try
            {
                Execute("co -nc .");
            }
            catch (ClearToolCommandException)
            {
                // Ignore error which is likely to be that . is already checked out to the view 
                // due to a previous test not cleanly up properly and we can continue.
            }

            for (int i = 0; i <= paths.Length - 1; i++)
            {
                TryCreateAndCheckOutDir(paths[i]);
                Execute("cd {0}", paths[i]);
            }

            // check-in all checked-out directories
            for (int i = paths.Length - 1; i >= 0; i--)
            {
                Execute("ci -nc .");
                Execute("cd ..");
            }
            Execute("ci -c '{0}' .", AddComment);

            Directory.SetCurrentDirectory(curDirectory);
            return 0;
        }

        public int EditFile(string localPath)
        {
            return EditFile(localPath, null, EditComment);
        }

        public int EditFile(string localPath, string copyFromFilePath)
        {
            return EditFile(localPath, copyFromFilePath, EditComment);
        }

        public int EditFile(string localPath, string copyFromFilePath, string checkinComment)
        {
            Debug.Assert(localPath.StartsWith(m_workspaceLocalRoot));

            string curDirectory = Directory.GetCurrentDirectory();

            string path = localPath.Substring(m_workspaceLocalRoot.Length);
            path = TrimLeadingPathSeparator(path);
            string[] paths = path.Split('\\');
            string filename = paths[paths.Length - 1];

            // create and check out directories
            Execute("cd {0}", m_workspaceLocalRoot);
            try
            {
                Execute("co -nc .");
            }
            catch (ClearToolCommandException)
            {
                // Ignore error which is likely to be that . is already checked out to the view 
                // due to a previous test not cleanly up properly and we can continue.
            }

            for (int i = 0; i < paths.Length - 1; i++)
            {
                TryCreateAndCheckOutDir(paths[i]);
                Execute("cd {0}", paths[i]);
            }

            // edit
            Execute("co -nc {0}", filename);
            if (string.IsNullOrEmpty(copyFromFilePath))
            {
                TestUtils.EditRandomFile(localPath);
            }
            else
            {
                File.Copy(copyFromFilePath, localPath, true);
            }

            Execute("ci -nc {0}", filename);

            // check-in all checked-out directories
            if (paths.Length > 1)
            {
                for (int i = paths.Length - 2; i >= 0; i--)
                {
                    Execute("ci -nc .");
                    Execute("cd ..");
                }
                Execute("ci -c '{0}' .", checkinComment);
            }

            Directory.SetCurrentDirectory(curDirectory);
            return 0;
        }

        public int RenameItem(string oldPath, string newPath)
        {
            return RenameItem(oldPath, newPath, RenameComment);
        }

        public int RenameItem(string oldPath, string newPath, string checkinComment)
        {
            Debug.Assert(oldPath.StartsWith(m_workspaceLocalRoot));

            string curDirectory = Directory.GetCurrentDirectory();

            string path1 = TrimLeadingPathSeparator(oldPath.Substring(m_workspaceLocalRoot.Length));
            string path2 = TrimLeadingPathSeparator(newPath.Substring(m_workspaceLocalRoot.Length));
            string[] pathList1 = path1.Split('\\');
            string[] pathList2 = path2.Split('\\');
            string oldItemName = pathList1[pathList1.Length - 1];
            string newItemName = pathList2[pathList2.Length - 1];

            // create and check out directories
            Execute("cd {0}", m_workspaceLocalRoot);
            try
            {
                Execute("co -nc .");
            }
            catch (ClearToolCommandException)
            {
                // Ignore error which is likely to be that . is already checked out to the view 
                // due to a previous test not cleanly up properly and we can continue.
            }

            for (int i = 0; i < pathList1.Length - 1; i++)
            {
                TryCreateAndCheckOutDir(pathList1[i]);
                Execute("cd {0}", pathList1[i]);
            }

            // create and check-in the file
            Execute("mv {0} {1}", oldItemName, newItemName);

            // check-in all checked-out directories
            if (pathList1.Length > 1)
            {
                for (int i = pathList1.Length - 2; i >= 0; i--)
                {
                    Execute("ci -nc .");
                    Execute("cd ..");
                }
                Execute("ci -c '{0}' .", checkinComment);
            }

            Directory.SetCurrentDirectory(curDirectory);
            return 0;
        }

        public int DeleteItem(string localPath)
        {
            return DeleteItem(localPath, DeleteComment);
        }

        public int DeleteItem(string localPath, string checkinComment)
        {
            Debug.Assert(localPath.StartsWith(m_workspaceLocalRoot));

            string curDirectory = Directory.GetCurrentDirectory();

            string path = localPath.Substring(m_workspaceLocalRoot.Length);
            path = TrimLeadingPathSeparator(path);
            string[] paths = path.Split('\\');
            string filename = paths[paths.Length - 1];

            // create and check out directories
            Execute("cd {0}", m_workspaceLocalRoot);
            try
            {
                Execute("co -nc .");
            }
            catch (ClearToolCommandException)
            {
                // Ignore error which is likely to be that . is already checked out to the view 
                // due to a previous test not cleanly up properly and we can continue.
            }

            for (int i = 0; i < paths.Length - 1; i++)
            {
                TryCreateAndCheckOutDir(paths[i]);
                Execute("cd {0}", paths[i]);
            }

            // remove
            Execute("rmname {0}", filename);

            // check-in all checked-out directories
            if (paths.Length > 1)
            {
                for (int i = paths.Length - 2; i >= 0; i--)
                {
                    Execute("ci -nc .");
                    Execute("cd ..");
                }
                Execute("ci -c '{0}' .", checkinComment);
            }

            Directory.SetCurrentDirectory(curDirectory);
            return 0;
        }

        public int BranchItem(MigrationItemStrings sourceItem, MigrationItemStrings targetItem)
        {
            throw new NotImplementedException();
        }

        public int BranchItem(MigrationItemStrings branchItem)
        {
            throw new NotImplementedException();
        }

        public void UndeleteFile(string serverPath, int changesetId)
        {
            throw new NotImplementedException();
        }

        public int MergeItem(MigrationItemStrings mergeItem, int mergeFromChangeset)
        {
            throw new NotImplementedException();
        }

        public int MergeItem(MigrationItemStrings mergeItem, int mergeFrom, int mergeTo)
        {
            throw new NotImplementedException();
        }

        public int Rollback(int rollbackFromVersion, int rollbackToVersion)
        {
            throw new NotImplementedException("ClearCaseDetailedHistoryTCAdapter doesn't support rollback yet");
        }

        // remove a first leading path separator
        private string TrimLeadingPathSeparator(string path)
        {
            if ((!string.IsNullOrEmpty(path)) && (IsPathSeparator(path[0])))
            {
                return path.Length == 1 ? string.Empty : path.Substring(1);
            }

            return path;
        }

        private bool IsPathSeparator(char ch)
        {
            return (ch == PathSeparator);
        }

        private void Execute(string format, params object[] args)
        {
            string cmd = String.Format(format, args);
            m_clearCaseServer.ExecuteClearToolCommand(cmd);
        }

        private void TryCreateAndCheckOutDir(string path)
        {
            if (!Directory.Exists(path))
            {
                // create and check out
                Execute("mkdir -nc {0}", path);
            }
            else
            {
                // check out
                Execute("co -nc {0}", path);
            }
        }
    }
}
