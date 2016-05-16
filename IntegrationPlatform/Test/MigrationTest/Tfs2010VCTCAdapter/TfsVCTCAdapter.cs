// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using MigrationTestLibrary;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System.IO;

namespace Tfs2008VCTCAdapter
{
    [TCAdapterDescription(m_adapterGuid, m_adapterName)]
    public class TfsVCTestCaseAdapter : ITfsVCTestCaseAdapter
    {
        private const string m_adapterGuid = "0A2595BE-5DA5-4fb7-A298-BB05C40C5CC0";
        private const string m_adapterName = "TFS 2008 VC TestCase Adapter";

        #region constants
        public const string AddComment = "Migration Test add";
        public const string BranchComment = "Migration test Branch";
        public const string CleanUpComment = "Migration Test Cleanup";
        public const string DeleteComment = "Migration Test delete";
        public const string EditComment = "Migration test Edit";
        public const string UndeleteComment = "Migration Test undelete";
        public const string MergeComment = "Migration test Merge";
        public const string MultiActionComment = "Migration Test multi action";
        public const string RenameComment = "Migration Test rename";
        #endregion

        private Workspace m_workspace;
        private VersionControlServer m_tfsClient;
        private string m_workspaceServerPath;
        private string m_workspaceLocalPath;

        public AdapterType AdapterType
        {
            get;
            private set;
        }

        public bool IsTfsAdapter { get { return true; } }

        public void Initialize(EndPoint env)
        {
            Trace.TraceInformation("TfsVCTestCaseAdapter: Initialize BEGIN");

            AdapterType = env.AdapterType;
            string serverPath = "test" + DateTime.Now.ToString("yyyy'-'MM'-'dd'_'HH'_'mm'_'ss");
            m_workspaceServerPath = String.Format("$/{0}/mt/{1}/{2}", env.TeamProject, serverPath, env.TestName);
            m_workspaceLocalPath = Path.Combine(TestUtils.TextReportRoot, "ws" + TestUtils.GetRandomAsciiString(3));

            try
            {
                TfsTeamProjectCollection tfsProjectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(env.ServerUrl));
                m_tfsClient = (VersionControlServer)tfsProjectCollection.GetService(typeof(VersionControlServer));

                // Try deleting an existing test workspace if any
                m_workspace = m_tfsClient.GetWorkspace(env.WorkspaceName, Environment.UserName);
                CleanUpWorkspace();
            }
            catch (WorkspaceNotFoundException) { }

            // create workspace and map the path
            m_workspace = m_tfsClient.CreateWorkspace(env.WorkspaceName, Environment.UserName);
            m_workspace.Map(m_workspaceServerPath, m_workspaceLocalPath);

            Trace.TraceInformation("Created a workspace: {0}", m_workspace);
            Trace.TraceInformation("TfsVCTestCaseAdapter: Initialize END");

        }

        public void Cleanup()
        {
            Trace.TraceInformation("TfsVCTestCaseAdapter: Cleanup BEGIN");

            CleanUpTfs(m_workspaceServerPath.Remove(m_workspaceServerPath.LastIndexOf('/')));
            CleanUpWorkspace();

            if (Directory.Exists(TestUtils.TextReportRoot))
            {
                TestUtils.DeleteDirectory(m_workspaceLocalPath);
                Trace.TraceInformation("Deleted directory {0}", m_workspaceLocalPath);
            }

            Trace.TraceInformation("TfsVCTestCaseAdapter: Cleanup END");
        }

        public Workspace Workspace
        {
            get
            {
                return m_workspace;
            }
        }

        public VersionControlServer TfsClient
        {
            get
            {
                return m_tfsClient;
            }
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

        public char PathSeparator
        {
            get
            {
                return '/';
            }
        }

        public int AddFile(string localPath)
        {
            TestUtils.CreateRandomFile(localPath, 10);

            m_workspace.PendAdd(localPath);
            return m_workspace.CheckIn(m_workspace.GetPendingChanges(), AddComment);
        }

        public int AddFiles(string[] localPaths)
        {
            foreach (string localPath in localPaths)
            {
                TestUtils.CreateRandomFile(localPath, 10);
                m_workspace.PendAdd(localPath);
            }

            return m_workspace.CheckIn(m_workspace.GetPendingChanges(), AddComment);
        }

        public int AddFolder(string localPath)
        {
            Directory.CreateDirectory(localPath);

            m_workspace.PendAdd(localPath);
            return m_workspace.CheckIn(m_workspace.GetPendingChanges(), AddComment);
        }

        public int EditFile(string localPath)
        {
            m_workspace.Get(VersionSpec.Latest, GetOptions.Overwrite);
            TestUtils.EditRandomFile(localPath);
            m_workspace.PendEdit(localPath);
            return m_workspace.CheckIn(m_workspace.GetPendingChanges(), EditComment);
        }

        public int EditFile(string localPath, string copyFromFilePath)
        {
            m_workspace.Get(VersionSpec.Latest, GetOptions.Overwrite);
            m_workspace.PendEdit(localPath);
            File.Copy(copyFromFilePath, localPath, true);
            return m_workspace.CheckIn(m_workspace.GetPendingChanges(), EditComment);
        }

        public int EditFile(string localPath, string copyFromFilePath, string checkinComment)
        {
            m_workspace.Get(VersionSpec.Latest, GetOptions.Overwrite);
            m_workspace.PendEdit(localPath);
            File.Copy(copyFromFilePath, localPath, true);
            return m_workspace.CheckIn(m_workspace.GetPendingChanges(), checkinComment);
        }

        public int RenameItem(string oldServerPath, string newServerPath)
        {
            m_workspace.Get(VersionSpec.Latest, GetOptions.Overwrite);
            m_workspace.PendRename(oldServerPath, newServerPath);
            return m_workspace.CheckIn(m_workspace.GetPendingChanges(), RenameComment);
        }

        public int RenameItem(string oldServerPath, string newServerPath, string comment)
        {
            m_workspace.Get(VersionSpec.Latest, GetOptions.Overwrite);
            m_workspace.PendRename(oldServerPath, newServerPath);
            return m_workspace.CheckIn(m_workspace.GetPendingChanges(), comment);
        }

        public int BranchItem(MigrationItemStrings branchItem)
        {
            return BranchItem(branchItem.ServerPath, branchItem.NewServerPath);
        }

        public int BranchItem(MigrationItemStrings sourceItem, MigrationItemStrings targetItem)
        {
            return BranchItem(sourceItem.ServerPath, targetItem.ServerPath);
        }

        private int BranchItem(string source, string target)
        {
            m_workspace.Get(VersionSpec.Latest, GetOptions.Overwrite);
            m_workspace.PendBranch(source, target, VersionSpec.Latest);
            return m_workspace.CheckIn(m_workspace.GetPendingChanges(), BranchComment);
        }

        public void UndeleteFile(string serverPath, int changesetId)
        {
            Item item = m_workspace.VersionControlServer.GetChangeset(changesetId).Changes[0].Item;

            m_workspace.Get();
            m_workspace.PendUndelete(serverPath, item.DeletionId);
            m_workspace.CheckIn(m_workspace.GetPendingChanges(), UndeleteComment);
        }

        public int DeleteItem(string serverPath)
        {
            m_workspace.PendDelete(serverPath);
            return m_workspace.CheckIn(m_workspace.GetPendingChanges(), DeleteComment);
        }

        public int DeleteItem(string serverPath, string comment)
        {
            m_workspace.PendDelete(serverPath);
            return m_workspace.CheckIn(m_workspace.GetPendingChanges(), comment);
        }

        public int MergeItem(MigrationItemStrings mergeItem, int mergeFromChangeset)
        {
            m_workspace.Merge(mergeItem.ServerPath,
                mergeItem.NewServerPath,
                VersionSpec.ParseSingleSpec(mergeFromChangeset.ToString(), Environment.UserName),
                VersionSpec.Latest, LockLevel.None, RecursionType.Full, MergeOptions.None);
            return m_workspace.CheckIn(m_workspace.GetPendingChanges(), MergeComment);
        }

        public int MergeItem(MigrationItemStrings branch, int mergeFrom, int mergeTo)
        {
            m_workspace.Merge(branch.ServerPath, branch.NewServerPath,
                VersionSpec.ParseSingleSpec(mergeFrom.ToString(), Environment.UserName),
                VersionSpec.ParseSingleSpec(mergeTo.ToString(), Environment.UserName), LockLevel.None, RecursionType.Full, MergeOptions.None);

            return m_workspace.CheckIn(m_workspace.GetPendingChanges(), MergeComment);
        }

        public int Rollback(int changesetFrom, int changesetTo)
        {
            // Use ProcessStartInfo class
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = true;

            string pathToTools = null;
            switch (AdapterType)
            {
                case MigrationTestLibrary.AdapterType.TFS2008VC:
                    pathToTools = Environment.GetEnvironmentVariable("VS90COMNTOOLS");
                    break;
                case MigrationTestLibrary.AdapterType.TFS2010VC:
                    pathToTools = Environment.GetEnvironmentVariable("VS100COMNTOOLS");
                    break;
                case MigrationTestLibrary.AdapterType.TFS11VC:
                    pathToTools = Environment.GetEnvironmentVariable("VS110COMNTOOLS");
                    break;
                default:
                    throw new Exception(String.Format("Invalid TFS VC TestCase Adapter type: {0}", AdapterType));
            }
            if (String.IsNullOrEmpty(pathToTools))
            {
                throw new Exception(String.Format("Failed to find VS Tools for VC TestCase Adapter type: {0}", AdapterType));
            }

            string pathToTf = Path.Combine(pathToTools, @"..\IDE\TF.exe"); ;

            startInfo.FileName = pathToTf;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = string.Format("rollback /changeset:{0}~{1}", changesetFrom, changesetTo);
            startInfo.WorkingDirectory = m_workspaceLocalPath;

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                }
            }
            catch (Exception e)
            {
                // Log error.
            }

            PendingChange[] pendedChanges = m_workspace.GetPendingChanges();
            if (pendedChanges.Length < 1)
            {
                return 0;
            }
            else
            {
                return m_workspace.CheckIn(pendedChanges, "Rollback tests");
            }
        }


        #region private methods
        private void CleanUpWorkspace()
        {
            Debug.Assert(m_tfsClient != null);

            try
            {
                m_tfsClient.GetWorkspace(m_workspace.Name, Environment.UserName);
                m_tfsClient.DeleteWorkspace(m_workspace.Name, Environment.UserName);

                Trace.TraceInformation("Deleted the test workspace [{0}]", m_workspace.Name);
            }
            catch (WorkspaceNotFoundException)
            {
                Trace.TraceInformation("Cleanup could not find the test workspace {0}.", m_workspace.Name);
            }
        }

        private void CleanUpTfs(string path)
        {
            if (m_workspace != null)
            {
                Trace.TraceInformation("Cleaning up workspace [{0}]", m_workspace.Name);

                try
                {
                    PendingChange[] changes = m_workspace.GetPendingChanges();
                    if (changes.Length > 0)
                    {
                        m_workspace.Undo(changes);
                        Trace.TraceInformation("\tUndo {0} pending changes", changes.Length);
                    }
                    //This will only work as long as we only need one mapping. 
                    m_workspace.PendDelete(path);
                    Trace.TraceInformation("\tPendDelete {0}", path);

                    changes = m_workspace.GetPendingChanges();
                    if (changes.Length > 0)
                    {
                        m_workspace.CheckIn(changes, CleanUpComment);
                        Trace.TraceInformation("\tCheckIn comment: {0}", CleanUpComment);
                    }
                    else
                    {
                        Trace.TraceInformation("There was nothing to cleanup in TFS.");
                    }
                }
                catch (Exception e)
                {
                    Trace.TraceError("There was an Exception when trying to cleanup in TFS.  {0}", e.ToString());
                }
            }
        }
        #endregion
    }
}
