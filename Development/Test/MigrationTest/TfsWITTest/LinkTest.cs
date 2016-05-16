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
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title1", "description1");

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
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title1", "description1");

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

            int mirroredId = QueryTargetWorkItemID(workitemId);
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
            int workitemId1 = SourceAdapter.AddWorkItem("Bug", "title1", "description1");

            int workitemId2 = SourceAdapter.AddWorkItem("Bug", "title2", "description2");

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

            int mirroredId = QueryTargetWorkItemID(workitemId1);
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
            int workitemId1 = SourceAdapter.AddWorkItem("Bug", "title1", "description1");

            int workitemId2 = SourceAdapter.AddWorkItem("Bug", "title2", "description2");

            RunAndNoValidate();

            int mirroredId = QueryTargetWorkItemID(workitemId1);

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
        public void Link_DeleteRelatedLinkFromTargetTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);

            // add a work item on source side
            int sourceId1 = TfsSourceAdapter.AddWorkItem("Bug", "title1", "description1");

            int sourceId2 = TfsSourceAdapter.AddWorkItem("Bug", "title2", "description2");

            RunAndNoValidate();

            int targetId1 = QueryTargetWorkItemID(sourceId1);
            Console.WriteLine(String.Format("SourceId1: {0}, TargetId1: {1}", sourceId1, targetId1));
            int targetId2 = QueryTargetWorkItemID(sourceId2);
            Console.WriteLine(String.Format("SourceId2: {0}, TargetId2: {1}", sourceId2, targetId2));

            // source: add a related link 
            TfsSourceAdapter.AddRelatedWorkItemLink(sourceId1, sourceId2);
            RunAndNoValidate(true);

            // source side should have one link
            int sourceLinkCount = TfsSourceAdapter.GetRelatedLinkCount(sourceId1);
            Assert.AreEqual(1, sourceLinkCount, "RelatedLink count on source system should be 1");

            int targetLinkCount = TfsTargetAdapter.GetRelatedLinkCount(targetId1);
            Assert.AreEqual(1, targetLinkCount, "RelatedLink count on target system should be 1");

            // target: delete the mirrored link
            TfsTargetAdapter.DeleteRelatedWorkItemLink(targetId1, targetId2);
            RunAndNoValidate(true);

            // now target side shouldn't have any links
            targetLinkCount = TfsTargetAdapter.GetRelatedLinkCount(targetId1);
            Assert.AreEqual(0, targetLinkCount, "RelatedLink count on target system should be 0");

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            sourceLinkCount = TfsSourceAdapter.GetRelatedLinkCount(sourceId1);

            if (TestEnvironment.MigrationTestType == MigrationTestType.OneWay)
            {
                // source side should still have the original link
                Assert.AreEqual(1, sourceLinkCount, "source side link should still exist");
            }
            else
            {
                // now source side shouldn't have any links
                Assert.AreEqual(0, sourceLinkCount, "source side link should be deleted");

                // verify sync result 
                WitDiffResult result = GetDiffResult();

                // ignore Area/Iteration path mismatches
                VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });
            }
        }

        ///<summary>
        /// Both endpoints removed a same link
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Both endpoints removed a same link")]
        public void Link_DeleteRelatedLinkFromBothSideTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);

            // add a work item on source side
            int workitemId1 = SourceAdapter.AddWorkItem("Bug", "title1", "description1");

            int workitemId2 = SourceAdapter.AddWorkItem("Bug", "title2", "description2");

            RunAndNoValidate();

            int mirroredId1 = QueryTargetWorkItemID(workitemId1);
            int mirroredId2 = QueryTargetWorkItemID(workitemId2);

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

        ///<summary>
        /// Migrate a work item with link
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Update link comment")]
        public void Link_UpdateTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);

            // add a work item on source side
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title1", "description1");

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
            int mirroredId = QueryTargetWorkItemID(workitemId);
            Assert.AreEqual(TfsTargetAdapter.GetHyperLinkCount(mirroredId), 1);
            WITLink tarLink = TargetAdapter.GetHyperLink(mirroredId, location);
            Assert.AreEqual(tarLink.Comment, newComment);
        }

        ///<summary>
        /// Migrate deleted links
        ///</summary>
        [TestMethod(), Priority(1), Owner("wlennon")]
        [Description("Migrate delete links")]
        public void Link_DeleteHyperLinkTest()
        {
            // TODO:  Figure out why this tests fail against TFS 2008 but works in TFS 2010

            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);

            // add a work item on source side
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title1", "description1");

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
            int mirroredId = QueryTargetWorkItemID(workitemId);
            Assert.AreEqual(TfsSourceAdapter.GetHyperLinkCount(workitemId),
                TfsTargetAdapter.GetHyperLinkCount(mirroredId));
        }
    }
}
