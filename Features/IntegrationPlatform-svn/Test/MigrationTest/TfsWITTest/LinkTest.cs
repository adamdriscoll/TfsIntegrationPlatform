// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTestLibrary;
using MigrationTestLibrary.Conflict;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.Migration.Toolkit.ServerDiff;
using System.Text;
using System.Diagnostics;

namespace TfsWitTest
{
    /// <summary>
    /// WIT link tests
    /// </summary>
    [TestClass]
    public class LinkTest : TfsWITTestCaseBase
    {
        ///<summary>
        /// Migrate a work item with link
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Migrate a work item with a link")]
        public void Link_BasicTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);

            // add a work item on source side
            string title = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId = SourceAdapter.AddWorkItem("Bug", title, "description1");

            WITLinkChangeAction action1 = new WITLinkChangeAction(LinkChangeActionType.Add);
            action1.AddLink(new WITLink("link1"));
            SourceAdapter.UpdateWorkItemLink(workitemId, action1);

            RunAndNoValidate();

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");
            
            // validate the conversion history entries
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                string sourceItemId = workitemId.ToString();
                var migrationItemQuery =
                    from mi in context.RTMigrationItemSet
                    where mi.ItemId.Equals(sourceItemId) && mi.ItemVersion.Equals("Link")
                    select mi;
                Assert.AreEqual(migrationItemQuery.Count(), 1);

                long miId = migrationItemQuery.First().Id;
                var itemPairQuery =
                    from p in context.RTItemRevisionPairSet
                    where p.LeftMigrationItemId == miId || p.RightMigrationItemId == miId
                    select p;
                Assert.AreEqual(itemPairQuery.Count(), 1);
            }

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });
        }

        ///<summary>
        /// Multiple links
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Migrate a work item with multiple links")]
        public void Link_MultipleTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);

            // add a work item on source side
            string title = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId = SourceAdapter.AddWorkItem("Bug", title, "description1");

            WITLinkChangeAction action1 = new WITLinkChangeAction(LinkChangeActionType.Add);
            action1.AddLink(new WITLink("link1"));
            SourceAdapter.UpdateWorkItemLink(workitemId, action1);

            RunAndNoValidate();

            WITLinkChangeAction action2 = new WITLinkChangeAction(LinkChangeActionType.Add);
            action2.AddLink(new WITLink("link2"));
            SourceAdapter.UpdateWorkItemLink(workitemId, action2);

            RunAndNoValidate(true);

            WITLinkChangeAction action3 = new WITLinkChangeAction(LinkChangeActionType.Add);
            action3.AddLink(new WITLink("link3"));
            SourceAdapter.UpdateWorkItemLink(workitemId, action3);

            RunAndNoValidate(true);

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });

            int mirroredId = QueryMirroredWorkItemID(workitemId);
            Assert.AreEqual(TfsTargetAdapter.GetHyperLinkCount(mirroredId), 3);
        }

        ///<summary>
        /// Add Related Link
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Delete Related Link")]
        public void Link_AddRelatedLinkTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);

            // add a work item on source side
            string title1 = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId1 = SourceAdapter.AddWorkItem("Bug", title1, "description1");

            string title2 = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId2 = SourceAdapter.AddWorkItem("Bug", title1, "description2");

            RunAndNoValidate();

            TfsSourceAdapter.AddRelatedWorkItemLink(workitemId1, workitemId2);
            RunAndNoValidate(true);

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });

            int mirroredId = QueryMirroredWorkItemID(workitemId1);
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(mirroredId));
        }

        ///<summary>
        /// Delete Related Link
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Delete Related Link")]
        public void Link_DeleteRelatedLinkTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);

            // add a work item on source side
            string title1 = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId1 = SourceAdapter.AddWorkItem("Bug", title1, "description1");

            string title2 = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId2 = SourceAdapter.AddWorkItem("Bug", title1, "description2");

            RunAndNoValidate();

            int mirroredId = QueryMirroredWorkItemID(workitemId1);

            TfsSourceAdapter.AddRelatedWorkItemLink(workitemId1, workitemId2);
            RunAndNoValidate(true);

            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(mirroredId), "RelatedLink count doesn't match");

            TfsSourceAdapter.DeleteRelatedWorkItemLink(workitemId1, workitemId2);
            RunAndNoValidate(true);

            Assert.AreEqual(0, TfsTargetAdapter.GetRelatedLinkCount(mirroredId), "RelatedLink count doesn't match");

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });
        }

        ///<summary>
        /// The other side deleted a mirrored related link
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("The other side deleted a mirrored link")]
        public void Link_DeleteRelatedLinkFromOtherSideTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_SetBidirectionalNoContextSync);

            // add a work item on source side
            string title1 = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId1 = SourceAdapter.AddWorkItem("Bug", title1, "description1");

            string title2 = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId2 = SourceAdapter.AddWorkItem("Bug", title1, "description2");

            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(SetTargetFilterString);

            RunAndNoValidate();

            int mirroredId1 = QueryMirroredWorkItemID(workitemId1);
            int mirroredId2 = QueryMirroredWorkItemID(workitemId2);

            // source: add a related link 
            TfsSourceAdapter.AddRelatedWorkItemLink(workitemId1, workitemId2);
            RunAndNoValidate(true);
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(mirroredId1), "RelatedLink count on target system is wrong");

            // target: delete the mirrored link
            TfsTargetAdapter.DeleteRelatedWorkItemLink(mirroredId1, mirroredId2);
            RunAndNoValidate(true);

            // now source side shouldn't have any link
            Assert.AreEqual(0, TfsSourceAdapter.GetRelatedLinkCount(workitemId1), "RelatedLink count on source system is wrong");

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });
        }

        ///<summary>
        /// Both endpoints removed a same link
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Both endpoints removed a same link")]
        public void Link_DeleteRelatedLinkFromBothSideTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_SetBidirectionalNoContextSync);

            // add a work item on source side
            string title1 = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId1 = SourceAdapter.AddWorkItem("Bug", title1, "description1");

            string title2 = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId2 = SourceAdapter.AddWorkItem("Bug", title1, "description2");

            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(SetTargetFilterString);

            RunAndNoValidate();

            int mirroredId1 = QueryMirroredWorkItemID(workitemId1);
            int mirroredId2 = QueryMirroredWorkItemID(workitemId2);

            // source: add a related link 
            TfsSourceAdapter.AddRelatedWorkItemLink(workitemId1, workitemId2);
            RunAndNoValidate(true);
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(mirroredId1), "RelatedLink count on target system is wrong");

            // source: delete the link
            TfsSourceAdapter.DeleteRelatedWorkItemLink(workitemId1, workitemId2);

            // target: delete the same link
            TfsTargetAdapter.DeleteRelatedWorkItemLink(mirroredId1, mirroredId2);
            RunAndNoValidate(true);

            // now both sides shouldn't have a link
            Assert.AreEqual(0, TfsSourceAdapter.GetRelatedLinkCount(workitemId1), "RelatedLink count on source system is wrong");
            Assert.AreEqual(0, TfsTargetAdapter.GetRelatedLinkCount(workitemId1), "RelatedLink count on target system is wrong");


            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });
        }

        private void SetTargetFilterString(Configuration config)
        {
            Debug.Assert(SourceWorkItemIdList.Count != 0);

            StringBuilder sb = new StringBuilder();
            for(int i=0; i< SourceWorkItemIdList.Count; i++)
            {
                sb.AppendFormat("[TfsMigrationTool.ReflectedWorkItemId] = '{0}'", SourceWorkItemIdList[i]);
                if (i < SourceWorkItemIdList.Count - 1)
                {
                    sb.Append(" OR ");
                }
            }

            config.SessionGroup.Sessions.Session[0].Filters.FilterPair[0].FilterItem[1].FilterString = sb.ToString();
        }

        ///<summary>
        /// Migrate a work item with link
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Update link comment")]
        public void Link_UpdateTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);

            // add a work item on source side
            string title = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId = SourceAdapter.AddWorkItem("Bug", title, "description1");

            string location = "link1";
            WITLinkChangeAction action1 = new WITLinkChangeAction(LinkChangeActionType.Add);
            action1.AddLink(new WITLink(location, "comment1"));
            SourceAdapter.UpdateWorkItemLink(workitemId, action1);

            // update link comment
            string newComment = "new comment 2";
            WITLinkChangeAction action2 = new WITLinkChangeAction(LinkChangeActionType.Edit);
            action2.AddLink(new WITLink(location, newComment));
            SourceAdapter.UpdateWorkItemLink(workitemId, action2);

            RunAndNoValidate();

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });

            // Title <-> Description
            int mirroredId = QueryMirroredWorkItemID(workitemId);
            Assert.AreEqual(TfsTargetAdapter.GetHyperLinkCount(mirroredId), 1);
            WITLink tarLink = TargetAdapter.GetHyperLink(mirroredId, location);
            Assert.AreEqual(tarLink.Comment, newComment);
        }

        ///<summary>
        /// Migrate deleted links in bi-directional work flow
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Migrate delete links - bi-directional work flow")]
        public void Link_DeleteHyperLinkBidirectionalTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_SetBidirectionalNoContextSync);
            DeleteHyperLinkScenario();
        }

        ///<summary>
        /// Migrate deleted links in one-way work flow
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Migrate delete links - one-way work flow")]
        public void Link_DeleteHyperLinkOneWayTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_SetOneWayNoContextSync);
            DeleteHyperLinkScenario();
        }

        private void DeleteHyperLinkScenario()
        {
            // add a work item on source side
            string title = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId = SourceAdapter.AddWorkItem("Bug", title, "description1");

            // add a link
            WITLinkChangeAction action1 = new WITLinkChangeAction(LinkChangeActionType.Add);
            WITLink link1 = new WITLink("link1");
            action1.AddLink(link1);
            SourceAdapter.UpdateWorkItemLink(workitemId, action1);

            RunAndNoValidate();

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // delete the link
            action1 = new WITLinkChangeAction(LinkChangeActionType.Delete);
            action1.AddLink(link1);
            SourceAdapter.UpdateWorkItemLink(workitemId, action1);

            RunAndNoValidate(true);

            // verify there's no conflicts raised           
            conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });

            // TODO: Remove the following verification once ServerDiff supports link diffing.
            int mirroredId = QueryMirroredWorkItemID(workitemId);
            Assert.AreEqual(TfsSourceAdapter.GetHyperLinkCount(workitemId),
                TfsTargetAdapter.GetHyperLinkCount(mirroredId));
        }
    }
}
