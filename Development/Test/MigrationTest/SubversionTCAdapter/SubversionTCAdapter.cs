// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Diagnostics;
using System.IO;
using MigrationTestLibrary;
using SharpSvn;
using System.Net;
using System.Collections.ObjectModel;
using System.Linq;

namespace SubversionTCAdapter
{
    [TCAdapterDescription(m_adapterGuid, m_adapterName)]
    public class SubversionTCAdapter : IVCTestCaseAdapter
    {
        public const string AddComment = "Subversion add";
        public const string BranchComment = "Subversion Branch";
        public const string CleanUpComment = "Subversion Cleanup";
        public const string DeleteComment = "Subversion delete";
        public const string EditComment = "Subversion Edit";
        public const string UndeleteComment = "Subversion undelete";
        public const string MergeComment = "Subversion Merge";
        public const string MultiActionComment = "Subversion multi action";
        public const string RenameComment = "Subversion rename";

        private const string m_adapterGuid = "95ECF05F-966F-4391-8CB1-534B03A392DA";
        private const string m_adapterName = "Subversion TestCase Adapter";

        private SvnClient m_client;
        private Uri m_serverUri;
        private string m_userName;
        private string m_password;
        private string m_localPath;
        private string m_filterPath;

        public AdapterType AdapterType
        {
            get { return AdapterType.Subversion; }
        }

        public string WorkspaceLocalPath
        {
            get
            {
                return m_localPath;
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
            Trace.TraceInformation("SVNTCAdapter AddFile {0}", localPath);
            Debug.Assert(localPath.StartsWith(m_localPath));
            TestUtils.CreateRandomFile(localPath, 10);

            // we need to make sure the parent folder(s) are already checked into the subversion
            // otherwise we have to do it here.  Subversion won't let you check in a file if the 
            // parent is not already checked in.

            AddPath(localPath);

            return 0;
        }

        public int AddFiles(string[] localPaths)
        {
            foreach (string path in localPaths)
            {
                AddFile(path);
            }
            return 0;
        }

        public int AddFolder(string localPath)
        {
            Trace.TraceInformation("SVNTCAdapter AddFolder {0}", localPath);
            Directory.CreateDirectory(localPath);
            m_client.Add(localPath);
            m_client.Commit(localPath, new SvnCommitArgs() { LogMessage = AddComment });
            return 0;
        }

        public int EditFile(string localPath)
        {
            Trace.TraceInformation("SVNTCAdapter EditFile {0}", localPath);
            TestUtils.EditRandomFile(localPath);
            m_client.Commit(localPath, new SvnCommitArgs() { LogMessage = EditComment });
            return 0;
        }

        public int EditFile(string localPath, string copyFromFilePath)
        {
            return EditFile(localPath, copyFromFilePath, EditComment);
        }

        public int EditFile(string localPath, string copyFromFilePath, string checkinComment)
        {
            Trace.TraceInformation("SVNTCAdapter EditFile {0} {1} {2}", localPath, copyFromFilePath, checkinComment);
            File.Copy(copyFromFilePath, localPath, true);
            m_client.Commit(localPath, new SvnCommitArgs() { LogMessage = checkinComment });
            return 0;
        }

        public int RenameItem(string oldPath, string newPath)
        {
            return RenameItem(oldPath, newPath, RenameComment);
        }

        public int RenameItem(string oldPath, string newPath, string checkinComment)
        {
            Trace.TraceInformation("SVNTCAdapter RenameItem {0} {1} {2}", oldPath, newPath, checkinComment);
            m_client.Move(oldPath, newPath);
            m_client.Commit(newPath, new SvnCommitArgs() { LogMessage = checkinComment });
            return 0;
        }

        public int DeleteItem(string localPath)
        {
            return DeleteItem(localPath, DeleteComment);
        }

        public int DeleteItem(string localPath, string checkinComment)
        {
            Trace.TraceInformation("SVNTCAdapter DeleteItem {0} {1}", localPath, checkinComment);
            m_client.Delete(localPath);
            m_client.Commit(localPath, new SvnCommitArgs() { LogMessage = checkinComment });
            string parent = Directory.GetParent(localPath).FullName;
            m_client.Update(parent);

            return 0;
        }

        public void UndeleteFile(string serverPath, int changesetId)
        {
            throw new NotImplementedException();
        }

        public int BranchItem(MigrationItemStrings sourceItem, MigrationItemStrings targetItem)
        {
            return BranchItem(sourceItem.LocalPath, targetItem.LocalPath);
        }

        public int BranchItem(MigrationItemStrings branchItem)
        {
            return BranchItem(branchItem.LocalPath, branchItem.NewLocalPath);
        }

        private int BranchItem(string source, string target)
        {
            Trace.TraceInformation("SVNTCAdapter BranchItem {0} {1}", source, target);
            SvnTarget sourceTarget = SvnTarget.FromString(source);
            m_client.Copy(sourceTarget, target);
            m_client.Commit(target, new SvnCommitArgs() { LogMessage = BranchComment });

            return GetLatestRevisionNumber();
        }

        private int AddPath(string localPath)
        {
            bool retry;
            string addPath = localPath;

            do
            {
                try
                {
                    m_client.Add(addPath, SvnDepth.Infinity);
                    m_client.Commit(addPath, new SvnCommitArgs() { LogMessage = AddComment });
                    retry = false;
                }
                catch (SharpSvn.SvnInvalidNodeKindException e)
                {
                    // back up a folder and retry
                    Trace.TraceInformation("Failed to Add path {0} {1}", addPath, e.ToString());
                    addPath = Path.GetDirectoryName(addPath);
                    retry = true;
                    Trace.TraceInformation("Retrying add of folder {0}", addPath);
                }
            } while (retry);
            return 0;
        }

        public int MergeItem(MigrationItemStrings mergeItem, int mergeFromChangeset)
        {
            Trace.TraceInformation("SVNTCAdapter MergeItem {0} {1}", mergeItem.LocalPath, mergeItem.NewLocalPath);
            SvnTarget sourceTarget = SvnTarget.FromString(mergeItem.LocalPath);
            SvnRevisionRange range = new SvnRevisionRange(mergeFromChangeset, SvnRevision.Head);
            m_client.Merge(mergeItem.NewLocalPath, sourceTarget, range);
            m_client.Commit(mergeItem.NewLocalPath, new SvnCommitArgs() { LogMessage = MergeComment });

            return GetLatestRevisionNumber();
        }

        public int MergeItem(MigrationItemStrings mergeItem, int mergeFrom, int mergeTo)
        {
            Trace.TraceInformation("SVNTCAdapter MergeItem {0} {1}", mergeItem.LocalPath, mergeItem.NewLocalPath);
            SvnTarget sourceTarget = SvnTarget.FromString(mergeItem.LocalPath);
            SvnRevisionRange range = new SvnRevisionRange(mergeFrom, mergeTo);
            m_client.Merge(mergeItem.NewLocalPath, sourceTarget, range);
            m_client.Commit(mergeItem.NewLocalPath, new SvnCommitArgs() { LogMessage = MergeComment });

            return GetLatestRevisionNumber();
        }

        public int Rollback(int rollbackFromVersion, int rollbackToVersion)
        {
            throw new NotImplementedException("SubversionTCAdapter doesn't support rollback yet");
        }

        public string FilterString
        {
            get
            {
                return m_filterPath;
            }
        }

        /// <summary>
        /// Initialize the Subversion adapter based on the TestEnvironment and the test EndPoint data
        /// </summary>
        /// <param name="endPoint"></param>
        public void Initialize(MigrationTestLibrary.EndPoint endPoint)
        {
            Trace.TraceInformation("SubversionTCAdapter: Initialize BEGIN");
            m_serverUri = new Uri(endPoint.ServerUrl, UriKind.Absolute);

            foreach (Setting s in endPoint.CustomSettingsList)
            {
                if (String.Equals(s.Key, "Username", StringComparison.OrdinalIgnoreCase))
                {
                    m_userName = s.Value;
                }
                if (String.Equals(s.Key, "Password", StringComparison.OrdinalIgnoreCase))
                {
                    m_password = s.Value;
                }
            }

            if (String.IsNullOrEmpty(m_userName))
            {
                m_userName = Environment.UserName;
                endPoint.CustomSettingsList.Add(new Setting() { Key = "Username", Value = m_userName });
            }
            Trace.TraceInformation("Using SVN username '{0}'", m_userName);

            if (String.IsNullOrEmpty(m_password))
            {
                m_password = m_userName;
                endPoint.CustomSettingsList.Add(new Setting() { Key = "Password", Value = m_password });
            }

            // set these values to something so the product code doesn't complain later
            endPoint.TeamProject = "unusedSVNProjectName";

            //Create a new instance of the svn client
            m_client = new SvnClient();

            Trace.TraceInformation("Setting SVN credentials");
            //Use the credentials if we have any
            if (!string.IsNullOrEmpty(m_userName))
            {
                m_client.Authentication.DefaultCredentials = new NetworkCredential(m_userName, m_password);
            }

            //Ensure that we can access the repository. The easist way is to query any information. We will receive an exception if a connection cannot be established
            //Trace.TraceInformation("Testing SVN access");
            //m_client.GetRepositoryRoot(m_serverUri);

            // endPoint.ServerUrl includes the repository
            string serverRoot = endPoint.ServerUrl;
            string localRoot = endPoint.LocalStorageLocation;
            if (String.IsNullOrEmpty(localRoot))
            {
                localRoot = Environment.ExpandEnvironmentVariables(@"%systemdrive%\SvnTest");
            }

            Directory.CreateDirectory(localRoot);

            // now map the 'mt' test folder, adding it if it does not exist
            string serverTestRootFolder = String.Format("{0}/{1}", serverRoot, "tests");
            string localTestRootFolder = String.Format(@"{0}\{1}", localRoot, "tests");

            // assume that the test root folder is already in SVN
            try
            {
                Trace.TraceInformation("Checking out SVN test root folder");
                m_client.CheckOut(new SvnUriTarget(serverTestRootFolder), localTestRootFolder);
            }
            catch (SharpSvn.SvnRepositoryIOException)
            {
                Trace.TraceInformation("Adding base test folder to SVN: {0} -> {1}", localRoot, serverRoot);

                // backup and map just the root
                Trace.TraceInformation("Checking out SVN root folder");
                m_client.CheckOut(new SvnUriTarget(serverRoot), localRoot);

                // create and map the main test folder
                Directory.CreateDirectory(localTestRootFolder);
                m_client.Add(localTestRootFolder);
                m_client.Commit(localTestRootFolder, new SvnCommitArgs() { LogMessage = "Adding automated tests root folder" });
                Trace.TraceInformation("Committed SVN root folder");
            }

            // create the new folder specific for this test
            string testFolder = String.Format(@"{0}.{1}", DateTime.Now.ToString("yyyyMMdd.HHmmss"), endPoint.TestName);

            m_filterPath = String.Format(@"/{0}/{1}", "tests", testFolder);
            m_localPath = String.Format(@"{0}\{1}", localTestRootFolder, testFolder);
            string serverTestFolder = String.Format("{0}/{1}", serverTestRootFolder, testFolder);
            //m_filterPath = serverTestFolder;

            Directory.CreateDirectory(m_localPath);
            Trace.TraceInformation("Adding test folder to SVN: {0} -> {1}", m_localPath, serverTestFolder);
            m_client.Add(m_localPath);
            m_client.Commit(m_localPath, new SvnCommitArgs() { LogMessage = "Adding test folder" });
            Trace.TraceInformation("Committed test folder for this test");

            Trace.TraceInformation("m_workspaceLocalPath: {0}", m_localPath);
            Trace.TraceInformation("m_filterPath: {0}", m_filterPath);

            Trace.TraceInformation("SubversionTCAdapter: Initialize END");
        }

        private int GetLatestRevisionNumber()
        {
            //Create the configuration that just queries the head revision record. disable all other options to ensure that the query is quick
            var logargs = new SvnLogArgs()
            {
                Start = new SvnRevision(SvnRevisionType.Head),
                End = new SvnRevision(SvnRevisionType.Head),
                RetrieveAllProperties = false,
                RetrieveChangedPaths = false,
                RetrieveMergedRevisions = false,
            };

            //Execute the actual query
            Collection<SvnLogEventArgs> logItems;
            if (!m_client.GetLog(m_localPath, logargs, out logItems))
            {
                throw logargs.LastException;
            }

            //The repository is empty If we do not retrieve any results
            if (0 == logItems.Count)
            {
                Trace.TraceWarning("The repository '{0}' does not contain any changes", m_localPath);
                return 0;
            }

            return (int)(logItems.First().Revision);
        }

        public void Cleanup()
        {
            Trace.TraceInformation("SubversionTCAdapter: Cleanup BEGIN");

            Trace.TraceInformation("Deleting test folder to SVN: {0}", m_localPath);
            try
            {
                m_client.Update(m_localPath);
                m_client.Delete(m_localPath);
                m_client.Commit(m_localPath, new SvnCommitArgs() { LogMessage = "Deleting test folder" });

                if (Directory.Exists(m_localPath))
                {
                    Directory.Delete(m_localPath, true);
                }
            }
            catch
            {
                Trace.TraceError("FAILED to cleanup SVN test folder");
            }

            Trace.TraceInformation("SubversionTCAdapter: Cleanup END");
        }
    }
}
