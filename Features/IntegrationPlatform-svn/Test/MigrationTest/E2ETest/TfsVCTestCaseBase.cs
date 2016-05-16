// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using MigrationTestLibrary;
using Tfs2008VCTCAdapter;
using System.Diagnostics;
using System.IO;
using ServerDiff;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit.ServerDiff;

namespace TfsVCTest
{
    public abstract class TfsVCTestCaseBase : VCMigrationTestCaseBase
    {
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
        
        protected Workspace SourceWorkspace
        {
            get
            {
                return ((ITfsVCTestCaseAdapter)SourceAdapter).Workspace;
            }
        }

        protected VersionControlServer SourceTfsClient
        {
            get
            {
                return ((ITfsVCTestCaseAdapter)SourceAdapter).TfsClient;
            }
        }

        protected override string TestProjectName
        {
            get
            {
                return "TfsVCTest";
            }
        }

        protected override void InitializeTestCase()
        {
        }

        protected override void VerifyMigration(bool AddOnBranchSourceNotFound)
        {
             if (IsTfsToTfsMigration())
             {
                // This is Tfs to Tfs migration, use TfsServerDiff            
                Trace.TraceInformation("==================== TfsServerDiff BEGIN ====================");
                TfsServerDiff diff = new TfsServerDiff(new Guid(base.VCSession.SessionUniqueId), AddOnBranchSourceNotFound, true);
                diff.QueryHistory();
                diff.RemoveSimilarHistory();
                TfsServerDiff.LogFailures(diff);

                Assert.IsTrue(diff.VerifyContentMatchAtLatest(), "The latest content is different");

                // If the migration is from TFS2008 to TFS2008 or TFS2010 to TFS2010, verify changeset metadata.
                if (TestEnvironment.SourceTFSVersion == TestEnvironment.TargetTFSVersion)
                {
                    // verify change types in all changesets
                    Assert.AreEqual(0, diff.TargetChanges.Count, "Some changes were left in the target system");
                    Assert.AreEqual(0, diff.SourceChanges.Count, "Some changes were left in the source system");
                }
                Trace.TraceInformation("==================== TfsServerDiff END ====================");
            }
            else
            {
                Trace.TraceInformation("==================== VCServerDiff BEGIN ====================");
                Guid sessionGuid = new Guid(base.VCSession.SessionUniqueId);
                ServerDiffEngine diff = new ServerDiffEngine(sessionGuid, false, true, SessionTypeEnum.VersionControl);
                VCDiffComparer diffComparer = new VCDiffComparer(diff);
                diff.RegisterDiffComparer(diffComparer);

                Assert.IsTrue(diff.VerifyContentsMatch(null, null), "The latest content is different");
                Trace.TraceInformation("==================== VCServerDiff END ====================");
            }
        }

        protected bool VerifyContents()
        {
            Trace.TraceInformation("====================VerifyContents BEGIN ====================");
            Guid sessionGuid = new Guid(base.VCSession.SessionUniqueId);
            ServerDiffEngine diff = new ServerDiffEngine(sessionGuid, false, true, SessionTypeEnum.VersionControl);
            VCDiffComparer diffComparer = new VCDiffComparer(diff);
            diff.RegisterDiffComparer(diffComparer);
            Trace.TraceInformation("==================== VerifyContents END ====================");

            return diff.VerifyContentsMatch(null, null);
        }

        protected void VerifySnapshotMigration(int snapShotChangesetId)
        {
            if (IsTfsToTfsMigration())
            {
                TfsServerDiff diff = new TfsServerDiff(new Guid(base.VCSession.SessionUniqueId), true, true);
                diff.QueryHistory();
                diff.RemoveSimilarHistory();

                foreach (Changeset changeset in diff.SourceChanges)
                {
                    Assert.IsTrue(changeset.ChangesetId <= snapShotChangesetId, "History difference exist in source system before snapshot {0}", changeset.ChangesetId);
                }
            }
        }

        protected void VerifyHistory(int sourceChangesetCount, int targetChangesetCount)
        {
            if (IsTfsToTfsMigration())
            {
                // If the migration is from TFS2008 to TFS2008 or TFS2010 to TFS2010, verify changeset metadata.
                if (TestEnvironment.SourceTFSVersion == TestEnvironment.TargetTFSVersion)
                {
                    TfsServerDiff diff = new TfsServerDiff(new Guid(base.VCSession.SessionUniqueId), true, true);
                    diff.QueryHistory();
                    diff.RemoveSimilarHistory();

                    Assert.AreEqual(sourceChangesetCount, diff.SourceChanges.Count, "# of source changesets is incorrect");
                    Assert.AreEqual(targetChangesetCount, diff.TargetChanges.Count, "# of target chagnesets is incorrect");
                }
            }
        }

        protected void Run(bool useExistingConfiguration, bool AddOnBranchSourceNotFound)
        {
            Run(useExistingConfiguration, AddOnBranchSourceNotFound, true);
        }
 
        public void Run(bool useExistingConfiguration, bool AddOnBranchSourceNotFound, bool updateExtraFile)
        {
            if (updateExtraFile)
            {
                SourceAdapter.EditFile(m_extraFile.LocalPath);
            }

            if (Configuration == null)
            {
                // Generate a configuration file
                Configuration = ConfigurationCreator.CreateConfiguration(TestConstants.ConfigurationTemplate.SingleVCSession, TestEnvironment);
                ConfigurationCreator.CreateConfigurationFile(Configuration, ConfigurationFileName);

                foreach (var session in Configuration.SessionGroup.Sessions.Session)
                {
                    VCSession = session;
                    break;
                }
            }

            string directoryBeforeMigration = Environment.CurrentDirectory;
            
            StartMigration();

            Environment.CurrentDirectory = directoryBeforeMigration;
        }

        protected void Run()
        {
            Run(false, false);
        }

        /// <summary>
        /// Resolve all merge conflicts with the specified resolution so that it may be checked in
        /// </summary>
        /// <param name="resolution">resolution to use</param>
        protected void ResolveConflicts(Resolution resolution)
        {
            Conflict[] conflicts = SourceWorkspace.QueryConflicts(new string[] { "$/" }, true);
            foreach (Conflict conflict in conflicts)
            {
                conflict.Resolution = resolution;
                SourceWorkspace.ResolveConflict(conflict);
            }
        }

        private bool IsTfsToTfsMigration()
        {
            return ((TestEnvironment.TargetTFSVersion == TFSVersionEnum.TFS2008) || (TestEnvironment.TargetTFSVersion == TFSVersionEnum.TFS2010))
                && ((TestEnvironment.SourceTFSVersion == TFSVersionEnum.TFS2008) || (TestEnvironment.SourceTFSVersion == TFSVersionEnum.TFS2010));
        }
    }
}
