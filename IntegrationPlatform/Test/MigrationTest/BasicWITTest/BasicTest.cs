// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTestLibrary;
using MigrationTestLibrary.Conflict;

namespace BasicWITTest
{
    /// <summary>
    /// WIT basic tests
    /// </summary>
    [TestClass]
    public class BasicTest : BasicWITTestCaseBase
    {
        const string TestCQHyperLinkBaseUrl = "http://localhost/cqweb/url/default.asp?id={0}";

        private void VerifyNoConflicts()
        {
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            if (conflicts.Count > 0)
            {
                foreach (RTConflict conflict in conflicts)
                {
                    Trace.TraceError("Conflict: {0} {1} {2}", conflict.Id, conflict.ConflictedChangeAction, conflict.ConflictDetails );
                }
            }
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");
        }

        ///<summary>
        /// Establish context
        /// Once we verify that this test case is successful, 
        /// we can skip context sync in other test cases to speed up
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Establish context")]
        public void EstablishContextTest()
        {
            RunAndNoValidate();

            // verify there's no conflicts raised           
            VerifyNoConflicts();
        }

        ///<summary>
        /// Migrate a work item
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Migrate a work item")]
        public void AddWorkItemTest()
        {
            // add a work item on source side
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title", "description1");

            RunAndNoValidate();

            // verify there's no conflicts raised           
            VerifyNoConflicts();

            // verify migration result
            VerifySyncResult();
        }

        ///<summary>
        /// Migrate a work item with attachment
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Migrate a work item with attachment")]
        public void Attachment_AttachmentTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_EnableBypassRulesOnTarget);

            // add a work item on source side
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title", "description1");

            WITAttachmentChangeAction action1 = new WITAttachmentChangeAction();
            action1.AddAttachment(new WITAttachment("attachment1.txt", "comment 1"));
            SourceAdapter.UpdateAttachment(workitemId, action1);

            RunAndNoValidate();

            // verify there's no conflicts raised           
            VerifyNoConflicts();

            // verify migration result
            VerifySyncResult();
        }

        ///<summary>
        /// Migrate a work item with attachment
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Migrate a work item with 2 attachments in to consecutive sync cycles")]
        public void Attachment_IncrementalAttachmentTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_EnableBypassRulesOnTarget);

            // add a work item on source side
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title", "description1");

            // add the first attachment
            WITAttachmentChangeAction action1 = new WITAttachmentChangeAction();
            action1.AddAttachment(new WITAttachment("attachment1.txt", "comment 1"));
            SourceAdapter.UpdateAttachment(workitemId, action1);

            // sync
            RunAndNoValidate();

            // add the second attachment
            WITAttachmentChangeAction action2 = new WITAttachmentChangeAction();
            action2.AddAttachment(new WITAttachment("attachment2.txt", "comment 2"));
            SourceAdapter.UpdateAttachment(workitemId, action2);

            // sync again
            RunAndNoValidate(true);

            // verify there's no conflicts raised           
            VerifyNoConflicts();

            // verify migration result
            VerifySyncResult();
        }

        ///<summary>
        /// Migrate a work item with link
        ///</summary>
        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("Migrate a work item with a link")]
        public void Linking_BasicLinkTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(TestEnvironment_AddCQWebUrlBase);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(TestEnvironment_AddCQHyperLinkTypeMapping);

            // add a work item on source side
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title", "description1");

            WITLinkChangeAction action1 = new WITLinkChangeAction(LinkChangeActionType.Add);
            action1.AddLink(new WITLink("link1"));
            SourceAdapter.UpdateWorkItemLink(workitemId, action1);

            RunAndNoValidate();

            // verify there's no conflicts raised           
            VerifyNoConflicts();

            string sourceHyperLinkUrl = string.Format(TestCQHyperLinkBaseUrl, workitemId.ToString());

            // verify migration result
            VerifySyncResult();
        }

        void TestEnvironment_AddCQHyperLinkTypeMapping(MigrationTestEnvironment env, Configuration config)
        {
            Debug.Assert(config.SessionGroup.Sessions.Session.Count == 1);

            Guid leftSrcUniqueId = new Guid(config.SessionGroup.Sessions.Session[0].LeftMigrationSourceUniqueId);
            Guid leftProviderId = new Guid(config.SessionGroup.MigrationSources[leftSrcUniqueId].ProviderReferenceName);
            if (leftProviderId.Equals(new Guid("D9637401-7385-4643-9C64-31585D77ED16"))) // CQ adapter id
            {

                LinkTypeMapping mapping = new LinkTypeMapping();
                mapping.LeftLinkType = "ClearQuestAdapter.LinkType.Web.RecordHyperLink";
                mapping.LeftMigrationSourceUniqueId = config.SessionGroup.Sessions.Session[0].LeftMigrationSourceUniqueId;
                mapping.RightLinkType = "Microsoft.TeamFoundation.Migration.TFS.LinkType.WorkItemToHyperlink";
                mapping.RightMigrationSourceUniqueId = config.SessionGroup.Sessions.Session[0].RightMigrationSourceUniqueId;

                config.SessionGroup.Linking.LinkTypeMappings.LinkTypeMapping.Add(mapping);
            }
        }

        void TestEnvironment_AddCQWebUrlBase(MigrationTestEnvironment env, Configuration config)
        {
            Debug.Assert(config.SessionGroup.Sessions.Session.Count == 1);

            Guid leftSrcUniqueId = new Guid(config.SessionGroup.Sessions.Session[0].LeftMigrationSourceUniqueId);
            Guid leftProviderId = new Guid(config.SessionGroup.MigrationSources[leftSrcUniqueId].ProviderReferenceName);
            if (leftProviderId.Equals(new Guid("D9637401-7385-4643-9C64-31585D77ED16"))) // CQ adapter id
            {
                Microsoft.TeamFoundation.Migration.BusinessModel.MigrationSource migrSrc = config.SessionGroup.MigrationSources[leftSrcUniqueId];
                if (null != migrSrc)
                {
                    CustomSetting setting = new CustomSetting();
                    setting.SettingKey = "CQWebRecordUrlFormat";
                    setting.SettingValue = TestCQHyperLinkBaseUrl;
                    migrSrc.CustomSettings.CustomSetting.Add(setting);
                }
            }
        }

    }
}
