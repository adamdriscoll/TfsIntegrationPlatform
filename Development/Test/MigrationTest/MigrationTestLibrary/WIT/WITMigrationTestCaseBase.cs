// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.TeamFoundation.Migration.Toolkit.ServerDiff;
using MigrationTestLibrary;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace MigrationTestLibrary
{
    public abstract class WITMigrationTestCaseBase : MigrationTestBase
    {
        protected IWITTestCaseAdapter SourceAdapter { get; private set; }
        protected IWITTestCaseAdapter TargetAdapter { get; private set; }

        protected List<int> SourceWorkItemIdList { get; private set; }
        protected List<int> TargetWorkItemIdList { get; private set; }

        internal abstract WitDiffResult VerifySyncResult();

        protected string TestTitle { get; set; }

        [TestInitialize]
        public override void Initialize()
        {
            base.Initialize();

            SourceWorkItemIdList = new List<int>();
            TargetWorkItemIdList = new List<int>();

            TestEnvironment.Mappings.Clear();

            EndPoint sourceTCAdapterEnv = TestEnvironment.SourceEndPoint;
            EndPoint targetTCAdapterEnv = TestEnvironment.TargetEndPoint;

            SourceAdapter = (IWITTestCaseAdapter)TestAdapterManager.LoadAdapter(sourceTCAdapterEnv.TCAdapterID);
            TargetAdapter = (IWITTestCaseAdapter)TestAdapterManager.LoadAdapter(targetTCAdapterEnv.TCAdapterID);

            SourceAdapter.Initialize(sourceTCAdapterEnv);
            TargetAdapter.Initialize(targetTCAdapterEnv);

            SourceAdapter.WorkItemAdded += OnSourceWorkItemAdded;
            TargetAdapter.WorkItemAdded += OnTargetWorkItemAdded;

            // set the title string that all work items will start with.  This is what the WIT query filter strings will use.
            TestTitle = String.Format("{0}_{1}", TestName, DateTime.Now.ToString("MM'_'HH'_'mm'_'ss"));
            SourceAdapter.TitlePrefix = TestTitle;
            TargetAdapter.TitlePrefix = TestTitle;

            Trace.TraceInformation("Loaded WIT test environment successfully");
        }

        protected void RunAndNoValidate()
        {
            RunAndNoValidate(false);
        }

        protected virtual void RunAndNoValidate(bool useExistingConfiguration)
        {
            if (!useExistingConfiguration || Configuration == null)
            {
                BuildFilterStringPair();

                // Generate a new configuration file
                Configuration = ConfigurationCreator.CreateConfiguration(TestConstants.ConfigurationTemplate.SingleWITSession, TestEnvironment);
                ConfigurationCreator.CreateConfigurationFile(Configuration, ConfigurationFileName);
            }

            MigrationApp.Start(ConfigurationFileName);
        }

        protected void RunAndValidate()
        {
            RunAndNoValidate(false);

            VerifySyncResult();
        }

        private void OnSourceWorkItemAdded(object sender, WorkItemAddedEventArgs e)
        {
            SourceWorkItemIdList.Add(e.WorkItemId);
        }

        private void OnTargetWorkItemAdded(object sender, WorkItemAddedEventArgs e)
        {
            TargetWorkItemIdList.Add(e.WorkItemId);
        }

        // Build filter string based on work item ids
        protected void BuildFilterStringPair()
        {
            string sourceFilterString = String.Format("{0}", SourceAdapter.TitleQuery);
            string targetFilterString = String.Format("{0}", TargetAdapter.TitleQuery);
            Trace.TraceInformation("sourceFilterString = '{0}", sourceFilterString);
            Trace.TraceInformation("targetFilterString = '{0}", targetFilterString);
            MappingPair mapping = new MappingPair(sourceFilterString, targetFilterString);
            TestEnvironment.AddMapping(mapping);
        }

        // from toolkit/WITTranslationService
        public string TryGetTargetItemId(string sourceWorkItemId, Guid sourceId)
        {
            Session session = Configuration.SessionGroup.Sessions.Session[0];
            Debug.Assert(session != null);
            string targetWorkItemId = String.Empty;

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var migrationItemResult =
                    from mi in context.RTMigrationItemSet
                    where mi.ItemId.Equals(sourceWorkItemId)
                        && !mi.ItemVersion.Equals(Constants.ChangeGroupGenericVersionNumber) // exclude non-versioned migration items (e.g. VC change group)
                    select mi;

                if (migrationItemResult.Count() == 0)
                {
                    Trace.TraceInformation(String.Format("Failed to find ItemID:{0} in internalID:{1}", sourceWorkItemId, sourceId));
                    return null;
                }

                RTMigrationItem sourceItem = null;
                foreach (RTMigrationItem rtMigrationItem in migrationItemResult)
                {
                    rtMigrationItem.MigrationSourceReference.Load();
                    if (rtMigrationItem.MigrationSource.UniqueId.Equals(sourceId))
                    {
                        sourceItem = rtMigrationItem;
                    }
                }
                if (null == sourceItem)
                {
                    return null;
                }

                var sessionUniqueId = new Guid(session.SessionUniqueId);
                var itemConvPairResult =
                    from p in context.RTItemRevisionPairSet
                    where (p.LeftMigrationItem.Id == sourceItem.Id || p.RightMigrationItem.Id == sourceItem.Id)
                        && (p.ConversionHistory.SessionRun.Config.SessionUniqueId.Equals(sessionUniqueId))
                    select p;

                if (itemConvPairResult.Count() == 0)
                {
                    return null;
                }

                RTItemRevisionPair itemRevisionPair = itemConvPairResult.First();
                if (itemRevisionPair.LeftMigrationItem == sourceItem)
                {
                    itemRevisionPair.RightMigrationItemReference.Load();
                    targetWorkItemId = itemRevisionPair.RightMigrationItem.ItemId;
                }
                else
                {
                    itemRevisionPair.LeftMigrationItemReference.Load();
                    targetWorkItemId = itemRevisionPair.LeftMigrationItem.ItemId;
                }
            }

            return targetWorkItemId;
        }
    }
}
