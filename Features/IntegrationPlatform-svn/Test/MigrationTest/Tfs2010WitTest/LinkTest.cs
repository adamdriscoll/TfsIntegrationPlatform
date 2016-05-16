// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Toolkit.ServerDiff;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTestLibrary;
using MigrationTestLibrary.Conflict;

namespace Tfs2010WitTest
{
    /// <summary>
    /// Tfs 2010 specific link tests
    /// </summary>
    [TestClass]
    public class LinkTest : Tfs2010WitTestCaseBase
    {
        ///<summary>
        /// Add Parent Link
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Add Parent Link")]
        public void Link_AddParentTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);

            // add a work item on source side
            string title1 = string.Format("Parent {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId1 = SourceAdapter.AddWorkItem("Bug", title1, "description1");

            string title2 = string.Format("Child {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId2 = SourceAdapter.AddWorkItem("Bug", title2, "description2");

            RunAndNoValidate();

            TfsSourceAdapter.AddParentChildLink(workitemId1, workitemId2);
            RunAndNoValidate(true);

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });

            int mirroredId1 = QueryMirroredWorkItemID(workitemId1);
            int mirroredId2 = QueryMirroredWorkItemID(workitemId2);
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(mirroredId1));
            Assert.AreEqual(false, TfsTargetAdapter.IsLinkLocked(mirroredId1, mirroredId2));
        }

        ///<summary>
        /// Add/Delete related Link bi-directional sync
        ///</summary>
        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("Multiple Add/Delete Related Link test")]
        public void Link_MultiAddDeleteRelatedLinkTestTwoWay()
        {
            for (int i = 0; i < 7; ++i)
            {
                // add a work item on source side
                string title = CreateWorkItemTitle(i.ToString());
                int id = SourceAdapter.AddWorkItem("Bug", title, "description1");
                SourceWorkItemIdList[i] = id;
            }

            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_SetBidirectionalNoContextSync);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(SetTargetFilterString);

            TfsSourceAdapter.AddRelatedWorkItemLink(SourceWorkItemIdList[1], SourceWorkItemIdList[0]);
            TfsSourceAdapter.AddRelatedWorkItemLink(SourceWorkItemIdList[1], SourceWorkItemIdList[2]);
            TfsSourceAdapter.AddRelatedWorkItemLink(SourceWorkItemIdList[1], SourceWorkItemIdList[3]);

            RunAndNoValidate();

            TfsSourceAdapter.DeleteRelatedWorkItemLink(SourceWorkItemIdList[1], SourceWorkItemIdList[0]);
            TfsSourceAdapter.DeleteRelatedWorkItemLink(SourceWorkItemIdList[1], SourceWorkItemIdList[2]);
            TfsSourceAdapter.DeleteRelatedWorkItemLink(SourceWorkItemIdList[1], SourceWorkItemIdList[3]);
            TfsSourceAdapter.AddRelatedWorkItemLink(SourceWorkItemIdList[1], SourceWorkItemIdList[4]);
            TfsSourceAdapter.AddRelatedWorkItemLink(SourceWorkItemIdList[1], SourceWorkItemIdList[5]);
            TfsSourceAdapter.AddRelatedWorkItemLink(SourceWorkItemIdList[1], SourceWorkItemIdList[6]);
            RunAndNoValidate(true);

            RunAndNoValidate(true);

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            //// verify sync result 
            //WitDiffResult result = GetDiffResult();

            //// ignore Area/Iteration path mismatches
            //VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });

            Assert.AreEqual(0, TfsSourceAdapter.GetRelatedLinkCount(SourceWorkItemIdList[0]));
            Assert.AreEqual(3, TfsSourceAdapter.GetRelatedLinkCount(SourceWorkItemIdList[1]));
            Assert.AreEqual(0, TfsSourceAdapter.GetRelatedLinkCount(SourceWorkItemIdList[2]));
            Assert.AreEqual(0, TfsSourceAdapter.GetRelatedLinkCount(SourceWorkItemIdList[3]));
            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(SourceWorkItemIdList[4]));
            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(SourceWorkItemIdList[5]));
            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(SourceWorkItemIdList[6]));

            int[] targetWorkItemIds = new int[SourceWorkItemIdList.Count];
            for (int i = 0; i < SourceWorkItemIdList.Count; ++i)
            {
                targetWorkItemIds[i] = QueryMirroredWorkItemID(SourceWorkItemIdList[i]);
            }

            Assert.AreEqual(0, TfsTargetAdapter.GetRelatedLinkCount(targetWorkItemIds[0]));
            Assert.AreEqual(3, TfsTargetAdapter.GetRelatedLinkCount(targetWorkItemIds[1]));
            Assert.AreEqual(0, TfsTargetAdapter.GetRelatedLinkCount(targetWorkItemIds[2]));
            Assert.AreEqual(0, TfsTargetAdapter.GetRelatedLinkCount(targetWorkItemIds[3]));
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(targetWorkItemIds[4]));
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(targetWorkItemIds[5]));
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(targetWorkItemIds[6]));
        }

        ///<summary>
        /// Add/Delete related Link unidirectional sync
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Multiple Add/Delete Related Link test")]
        public void Link_MultiAddDeleteRelatedLinkTestOneWay()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);

            for (int i = 0; i < 7; ++i)
            {
                // add a work item on source side
                string title = CreateWorkItemTitle(i.ToString());
                int id = SourceAdapter.AddWorkItem("Bug", title, "description1");
                SourceWorkItemIdList[i] = id;
            }

            TfsSourceAdapter.AddRelatedWorkItemLink(SourceWorkItemIdList[1], SourceWorkItemIdList[0]);
            TfsSourceAdapter.AddRelatedWorkItemLink(SourceWorkItemIdList[1], SourceWorkItemIdList[2]);
            TfsSourceAdapter.AddRelatedWorkItemLink(SourceWorkItemIdList[1], SourceWorkItemIdList[3]);

            RunAndNoValidate();

            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(SourceWorkItemIdList[0]));
            Assert.AreEqual(3, TfsSourceAdapter.GetRelatedLinkCount(SourceWorkItemIdList[1]));
            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(SourceWorkItemIdList[2]));
            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(SourceWorkItemIdList[3]));

            TfsSourceAdapter.DeleteRelatedWorkItemLink(SourceWorkItemIdList[1], SourceWorkItemIdList[0]);
            TfsSourceAdapter.DeleteRelatedWorkItemLink(SourceWorkItemIdList[1], SourceWorkItemIdList[2]);
            TfsSourceAdapter.DeleteRelatedWorkItemLink(SourceWorkItemIdList[1], SourceWorkItemIdList[3]);
            TfsSourceAdapter.AddRelatedWorkItemLink(SourceWorkItemIdList[1], SourceWorkItemIdList[4]);
            TfsSourceAdapter.AddRelatedWorkItemLink(SourceWorkItemIdList[1], SourceWorkItemIdList[5]);
            TfsSourceAdapter.AddRelatedWorkItemLink(SourceWorkItemIdList[1], SourceWorkItemIdList[6]);
            RunAndNoValidate(true);

            RunAndNoValidate(true);

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            //// verify sync result 
            //WitDiffResult result = GetDiffResult();

            //// ignore Area/Iteration path mismatches
            //VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });

            Assert.AreEqual(0, TfsSourceAdapter.GetRelatedLinkCount(SourceWorkItemIdList[0]));
            Assert.AreEqual(3, TfsSourceAdapter.GetRelatedLinkCount(SourceWorkItemIdList[1]));
            Assert.AreEqual(0, TfsSourceAdapter.GetRelatedLinkCount(SourceWorkItemIdList[2]));
            Assert.AreEqual(0, TfsSourceAdapter.GetRelatedLinkCount(SourceWorkItemIdList[3]));
            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(SourceWorkItemIdList[4]));
            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(SourceWorkItemIdList[5]));
            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(SourceWorkItemIdList[6]));

            int[] targetWorkItemIds = new int[SourceWorkItemIdList.Count];
            for (int i = 0; i < SourceWorkItemIdList.Count; ++i)
            {
                targetWorkItemIds[i] = QueryMirroredWorkItemID(SourceWorkItemIdList[i]);
            }

            Assert.AreEqual(0, TfsTargetAdapter.GetRelatedLinkCount(targetWorkItemIds[0]));
            Assert.AreEqual(3, TfsTargetAdapter.GetRelatedLinkCount(targetWorkItemIds[1]));
            Assert.AreEqual(0, TfsTargetAdapter.GetRelatedLinkCount(targetWorkItemIds[2]));
            Assert.AreEqual(0, TfsTargetAdapter.GetRelatedLinkCount(targetWorkItemIds[3]));
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(targetWorkItemIds[4]));
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(targetWorkItemIds[5]));
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(targetWorkItemIds[6]));
        }

        private string CreateWorkItemTitle(string prefix)
        {
            return string.Format("{0} {1} {2}", prefix, TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
        }

        ///<summary>
        /// Delete Parent Link
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Delete Parent Link")]
        public void Link_DeleteParentTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);

            // add a work item on source side
            string title1 = string.Format("Parent {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId1 = SourceAdapter.AddWorkItem("Bug", title1, "description1");

            string title2 = string.Format("Child {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId2 = SourceAdapter.AddWorkItem("Bug", title2, "description2");

            RunAndNoValidate();

            TfsSourceAdapter.AddParentChildLink(workitemId1, workitemId2);
            RunAndNoValidate(true);

            TfsSourceAdapter.DeleteParentChildLink(workitemId1, workitemId2);
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
            Assert.AreEqual(0, TfsTargetAdapter.GetRelatedLinkCount(mirroredId));
        }

        ///<summary>
        /// sync IsLocked link property for V2 WorkItemLink
        ///</summary>
        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("Sync Locked Link")]
        public void LockedLink_BasicSyncTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);

            // add a work item on source side
            string title1 = string.Format("Parent {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId1 = SourceAdapter.AddWorkItem("Bug", title1, "description1");

            string title2 = string.Format("Child {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId2 = SourceAdapter.AddWorkItem("Bug", title2, "description2");

            RunAndNoValidate();

            TfsSourceAdapter.AddParentChildLink(workitemId1, workitemId2, true);
            RunAndNoValidate(true);

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });

            int mirroredId1 = QueryMirroredWorkItemID(workitemId1);
            int mirroredId2 = QueryMirroredWorkItemID(workitemId2);
            Assert.IsTrue(TfsTargetAdapter.IsLinkLocked(mirroredId1, mirroredId2), "migrated link is not locked");
        }

        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("Sync Locked Link")]
        public void LockedLink_RemoveLockAndDeleteLinkSyncTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);

            // add a work item on source side
            string title1 = string.Format("Parent {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId1 = SourceAdapter.AddWorkItem("Bug", title1, "description1");

            string title2 = string.Format("Child {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId2 = SourceAdapter.AddWorkItem("Bug", title2, "description2");

            RunAndNoValidate();

            TfsSourceAdapter.AddParentChildLink(workitemId1, workitemId2, true);
            RunAndNoValidate(true);

            // verify there's no conflicts raised
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");


            TfsSourceAdapter.UpdateLinkLockOption(workitemId1, workitemId2, false);
            TfsSourceAdapter.DeleteParentChildLink(workitemId1, workitemId2);
            RunAndNoValidate(true);

            // verify there's a conflict raised
            conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be 1 conflict");

            conflictResolver.TryResolveConflict(conflicts[0],
                ConflictConstant.TFSModifyLockedWorkItemLinkConflict_ResolveByForceDeleteAction,
                conflicts[0].ScopeHint);

            RunAndNoValidate(true);

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });
        }

        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("Sync Locked Link")]
        public void LockedLink_SkipDeleteLinkSyncTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);

            // add a work item on source side
            string title1 = string.Format("Parent {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId1 = SourceAdapter.AddWorkItem("Bug", title1, "description1");

            string title2 = string.Format("Child {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId2 = SourceAdapter.AddWorkItem("Bug", title2, "description2");

            RunAndNoValidate();

            TfsSourceAdapter.AddParentChildLink(workitemId1, workitemId2, true);
            RunAndNoValidate(true);

            // verify there's no conflicts raised
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");


            TfsSourceAdapter.UpdateLinkLockOption(workitemId1, workitemId2, false);
            TfsSourceAdapter.DeleteParentChildLink(workitemId1, workitemId2);
            RunAndNoValidate(true);

            // verify there's a conflict raised
            conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be 1 conflict");

            conflictResolver.TryResolveConflict(conflicts[0],
                new SkipConflictedActionResolutionAction().ReferenceName,
                conflicts[0].ScopeHint);

            RunAndNoValidate(true);

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            //// ignore Area/Iteration path mismatches
            //VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });
            Assert.AreEqual(2, result.LinkMismatchCount);
        }

        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("Sync Locked Link")]
        public void LockedLink_SyncUnlockedFirstThenUpdateLocked()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);

            // add a work item on source side
            string title1 = string.Format("Parent {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId1 = SourceAdapter.AddWorkItem("Bug", title1, "description1");

            string title2 = string.Format("Child {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId2 = SourceAdapter.AddWorkItem("Bug", title2, "description2");

            RunAndNoValidate();

            TfsSourceAdapter.AddParentChildLink(workitemId1, workitemId2, false);
            RunAndNoValidate(true);

            // verify there's no conflicts raised
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");


            TfsSourceAdapter.UpdateLinkLockOption(workitemId1, workitemId2, true);
            RunAndNoValidate(true);

            // verify there's no conflict raised
            conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            RunAndNoValidate(true);

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });

            int mirroredId1 = QueryMirroredWorkItemID(workitemId1);
            int mirroredId2 = QueryMirroredWorkItemID(workitemId2);
            Assert.IsTrue(TfsTargetAdapter.IsLinkLocked(mirroredId1, mirroredId2), "migrated link is not locked");
        }

        ///<summary>
        /// Cyclic Link (touching same work items)
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Cyclic Link (touching same work items)")]
        public void Link_CyclicLinkSamePairBidirectionalTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_SetBidirectionalNoContextSync);

            // Legend: x->y (y is a parent of x)
            // T1: Source: a b
            // T2: Sync
            // T3: Target: a b
            // T4: Source: add a->b ==> a->b 
            //     Target: add b->a ==> b->a
            // T5: Sync
            // We should detect cyclic link and raise a conflict


            // add a work item on source side
            string titleA = string.Format("A {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemIdA = SourceAdapter.AddWorkItem("Bug", titleA, "A");

            string titleB = string.Format("B {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemIdB = SourceAdapter.AddWorkItem("Bug", titleB, "B");


            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(SetTargetFilterString);

            RunAndNoValidate();
            int mirroredIdA = QueryMirroredWorkItemID(workitemIdA);
            int mirroredIdB = QueryMirroredWorkItemID(workitemIdB);


            TfsSourceAdapter.AddParentChildLink(workitemIdB, workitemIdA);
            TfsTargetAdapter.AddParentChildLink(mirroredIdA, mirroredIdB);

            RunAndNoValidate(true);

            // verify the expected conflict          
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(2, conflicts.Count, "There should be 2 conflicts");

            Guid TFSCyclicLinkConflictTypeReferenceName = new Guid("BF1277E9-A218-4a2d-8C3C-A9501D30ECD5");
            Assert.IsTrue(TFSCyclicLinkConflictTypeReferenceName.Equals(conflicts[0].ConflictTypeReference.Value.ReferenceName),
                "It should be a TFS cyclic link reference conflict");
            Assert.IsTrue(TFSCyclicLinkConflictTypeReferenceName.Equals(conflicts[1].ConflictTypeReference.Value.ReferenceName),
                "It should be a TFS cyclic link reference conflict");

            // fix the hierarchy on target, delete b->a ==> a b
            TfsTargetAdapter.DeleteParentChildLink(mirroredIdA, mirroredIdB);

            // resolve the conflict by retrying
            conflictResolver.TryResolveConflict(conflicts[0], new ManualConflictResolutionAction(), "/");

            // restart the session
            RunAndNoValidate(true);

            // resolve the conflict by retrying
            conflictResolver.TryResolveConflict(conflicts[1], new ManualConflictResolutionAction(), "/");

            // restart the session
            RunAndNoValidate(true);

            conflicts = conflictResolver.GetConflicts();
            Assert.IsTrue(conflicts.Count == 0, "There should be no conflict");

            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(workitemIdA));
            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(workitemIdB));
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(mirroredIdA));
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(mirroredIdB));

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });
        }

        ///<summary>
        /// Cyclic Link
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Cyclic Link")]
        public void Link_CyclicLinkOneWayTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_SetOneWayNoContextSync);

            Link_CyclicLinkScenario();
        }

        ///<summary>
        /// Cyclic Link
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Cyclic Link")]
        public void Link_CyclicLinkBidirectionalTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_SetBidirectionalNoContextSync);

            Link_CyclicLinkScenarioTwoWay();

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });
        }

        private void Link_CyclicLinkScenario()
        {
            // Legend: x->y (y is a parent of x)
            // T1: Source: a->b->c  d
            // T2: Sync
            // T3: Target: a->b->c  d
            // T4: Source: delete b->c, add c->a ==> a->b  c->a  d
            //     Target: delete b->c, add b->d , d->c ==> a->b->d->c
            // T5: Sync
            // We should detect cyclic link and raise a conflict
 

            // add a work item on source side
            string titleA = string.Format("A {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemIdA = SourceAdapter.AddWorkItem("Bug", titleA, "A");

            string titleB = string.Format("B {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemIdB = SourceAdapter.AddWorkItem("Bug", titleB, "B");

            string titleC = string.Format("C {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemIdC = SourceAdapter.AddWorkItem("Bug", titleC, "C");

            string titleD = string.Format("C {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemIdD = SourceAdapter.AddWorkItem("Bug", titleD, "D");

            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(SetTargetFilterString);

            RunAndNoValidate();

            TfsSourceAdapter.AddParentChildLink(workitemIdC, workitemIdB);
            TfsSourceAdapter.AddParentChildLink(workitemIdB, workitemIdA);

            // after sync, 
            // Source: a->b->c d
            // Target: a->b->c d
            RunAndNoValidate(true);

            // Source: delete b->c, add c->a => c->a->b d
            TfsSourceAdapter.DeleteParentChildLink(workitemIdC, workitemIdB);
            TfsSourceAdapter.AddParentChildLink(workitemIdA, workitemIdC);
            // Target: delete b->c, add b->d , d->c => a->b->d->c
            int mirroredIdA = QueryMirroredWorkItemID(workitemIdA);
            int mirroredIdB = QueryMirroredWorkItemID(workitemIdB);
            int mirroredIdC = QueryMirroredWorkItemID(workitemIdC);
            int mirroredIdD = QueryMirroredWorkItemID(workitemIdD);
            TfsTargetAdapter.DeleteParentChildLink(mirroredIdC, mirroredIdB);
            TfsTargetAdapter.AddParentChildLink(mirroredIdD, mirroredIdB);
            TfsTargetAdapter.AddParentChildLink(mirroredIdC, mirroredIdD);

            // c->a from source will create cyclic link on target
            RunAndNoValidate(true);

            // verify the expected conflict          
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be 1 conflict");

            Guid TFSCyclicLinkConflictTypeReferenceName = new Guid("BF1277E9-A218-4a2d-8C3C-A9501D30ECD5");
            Assert.IsTrue(TFSCyclicLinkConflictTypeReferenceName.Equals(conflicts[0].ConflictTypeReference.Value.ReferenceName),
                "It should be a TFS cyclic link reference conflict");

            // fix the hierarchy on target, delete d->c => a->b->d c
            TfsTargetAdapter.DeleteParentChildLink(mirroredIdC, mirroredIdD);

            // resolve the conflict by taking source side changes
            conflictResolver.TryResolveConflict(conflicts[0], new ManualConflictResolutionAction(), "/");

            // restart the migration tool, after sync,
            // Source: c->a->b d
            // Target: c->a->b->d
            RunAndNoValidate(true);

            conflicts = conflictResolver.GetConflicts();
            Assert.IsTrue(conflicts.Count == 0, "There should be no conflict");

            Assert.AreEqual(2, TfsSourceAdapter.GetRelatedLinkCount(workitemIdA));
            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(workitemIdB));
            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(workitemIdC));
            Assert.AreEqual(0, TfsSourceAdapter.GetRelatedLinkCount(workitemIdD));
            Assert.AreEqual(2, TfsTargetAdapter.GetRelatedLinkCount(mirroredIdA));
            Assert.AreEqual(2, TfsTargetAdapter.GetRelatedLinkCount(mirroredIdB));
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(mirroredIdC));
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(mirroredIdD));

        }

        private void Link_CyclicLinkScenarioTwoWay()
        {
            // Legend: x->y (y is a parent of x)
            // T1: Source: a->b->c  d
            // T2: Sync
            // T3: Target: a->b->c  d
            // T4: Source: delete b->c, add c->a ==> a->b  c->a  d
            //     Target: delete b->c, add b->d , d->c ==> a->b->d->c
            // T5: Sync
            // We should detect cyclic link and raise a conflict


            // add a work item on source side
            string titleA = string.Format("A {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemIdA = SourceAdapter.AddWorkItem("Bug", titleA, "A");

            string titleB = string.Format("B {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemIdB = SourceAdapter.AddWorkItem("Bug", titleB, "B");

            string titleC = string.Format("C {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemIdC = SourceAdapter.AddWorkItem("Bug", titleC, "C");

            string titleD = string.Format("C {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemIdD = SourceAdapter.AddWorkItem("Bug", titleD, "D");

            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(SetTargetFilterString);

            RunAndNoValidate();

            TfsSourceAdapter.AddParentChildLink(workitemIdC, workitemIdB);
            TfsSourceAdapter.AddParentChildLink(workitemIdB, workitemIdA);

            // after sync, 
            // Source: a->b->c d
            // Target: a->b->c d
            RunAndNoValidate(true);

            // Source: delete b->c, add c->a => c->a->b d
            TfsSourceAdapter.DeleteParentChildLink(workitemIdC, workitemIdB);
            TfsSourceAdapter.AddParentChildLink(workitemIdA, workitemIdC);
            // Target: delete b->c, add b->d , d->c => a->b->d->c
            int mirroredIdA = QueryMirroredWorkItemID(workitemIdA);
            int mirroredIdB = QueryMirroredWorkItemID(workitemIdB);
            int mirroredIdC = QueryMirroredWorkItemID(workitemIdC);
            int mirroredIdD = QueryMirroredWorkItemID(workitemIdD);
            TfsTargetAdapter.DeleteParentChildLink(mirroredIdC, mirroredIdB);
            TfsTargetAdapter.AddParentChildLink(mirroredIdD, mirroredIdB);
            TfsTargetAdapter.AddParentChildLink(mirroredIdC, mirroredIdD);

            // c->a from source will create cyclic link on target
            RunAndNoValidate(true);

            // verify the expected conflict          
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(2, conflicts.Count, "There should be 2 conflicts");

            Guid TFSCyclicLinkConflictTypeReferenceName = new Guid("BF1277E9-A218-4a2d-8C3C-A9501D30ECD5");
            Assert.IsTrue(TFSCyclicLinkConflictTypeReferenceName.Equals(conflicts[0].ConflictTypeReference.Value.ReferenceName),
                "It should be a TFS cyclic link reference conflict");
            Assert.IsTrue(TFSCyclicLinkConflictTypeReferenceName.Equals(conflicts[1].ConflictTypeReference.Value.ReferenceName),
                "It should be a TFS cyclic link reference conflict");

            // fix the hierarchy on target, delete a->b => a b->d->c
            TfsTargetAdapter.DeleteParentChildLink(mirroredIdB, mirroredIdA);

            // resolve the conflict by retrying
            conflictResolver.TryResolveConflict(conflicts[0], new ManualConflictResolutionAction(), "/");

            // restart the migration tool, after sync,
            RunAndNoValidate(true);

            // resolve the conflict by retrying
            conflictResolver.TryResolveConflict(conflicts[1], new ManualConflictResolutionAction(), "/");

            RunAndNoValidate(true);

            conflicts = conflictResolver.GetConflicts();
            Assert.IsTrue(conflicts.Count == 0, "There should be no conflict");

            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(workitemIdA));
            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(workitemIdB));
            Assert.AreEqual(2, TfsSourceAdapter.GetRelatedLinkCount(workitemIdC));
            Assert.AreEqual(2, TfsSourceAdapter.GetRelatedLinkCount(workitemIdD));
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(mirroredIdA));
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(mirroredIdB));
            Assert.AreEqual(2, TfsTargetAdapter.GetRelatedLinkCount(mirroredIdC));
            Assert.AreEqual(2, TfsTargetAdapter.GetRelatedLinkCount(mirroredIdD));

        }

        ///<summary>
        /// Link_TwoParentsTest
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Migrating link changes from source would result in an invalid topology (two parents)")]
        public void Link_TwoParentsTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_SetOneWayNoContextSync);

            // Legend: x->y (y is a parent of x)
            // T1: Source: c->b->a
            // T2: Sync
            // T3: Target: c->b->a
            // T4: Source: delete c->b, add a->c ==> b->a->c
            //     Target: delete b->a, add a->b ==> a->b, c->b (b has two children)
            // T5: Sync

            // add a work item on source side
            string title1 = string.Format("A {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemIdA = SourceAdapter.AddWorkItem("Bug", title1, "A");

            string title2 = string.Format("B {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemIdB = SourceAdapter.AddWorkItem("Bug", title2, "B");

            string title3 = string.Format("C {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemIdC = SourceAdapter.AddWorkItem("Bug", title3, "C");

            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(SetTargetFilterString);

            RunAndNoValidate();

            // T1-T3: c->b->a
            TfsSourceAdapter.AddParentChildLink(workitemIdB, workitemIdC);
            TfsSourceAdapter.AddParentChildLink(workitemIdA, workitemIdB);
            RunAndNoValidate(true);

            // T4: 
            // Source: delete c->b, add a->c ==> b->a->c (a is the child of c)
            TfsSourceAdapter.DeleteParentChildLink(workitemIdB, workitemIdC);
            TfsSourceAdapter.AddParentChildLink(workitemIdC, workitemIdA);

            // Target: delete b->a, add a->b ==> a->b, c->b (a is the child of b) 
            int mirroredIdA = QueryMirroredWorkItemID(workitemIdA);
            int mirroredIdB = QueryMirroredWorkItemID(workitemIdB);
            int mirroredIdC = QueryMirroredWorkItemID(workitemIdC);
            TfsTargetAdapter.DeleteParentChildLink(mirroredIdA, mirroredIdB);
            TfsTargetAdapter.AddParentChildLink(mirroredIdB, mirroredIdA);

            // after sync:
            // source: b->a->c
            // target: a->b (adding a->c is conflicted)
            RunAndNoValidate(true);

            // verify the expected conflict
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be 1 conflict");

            Guid TFSMulitpleParentLinkConflictTypeReferenceName = new Guid("ADCE870C-33C0-46bc-9350-31660A463F9A");
            Assert.IsTrue(TFSMulitpleParentLinkConflictTypeReferenceName.Equals(conflicts[0].ConflictTypeReference.Value.ReferenceName),
                "It should be a TFSMulitpleParentLinkConflict");

            // fix the hierarchy on target, delete a->b
            // target: a, b, c
            TfsTargetAdapter.DeleteParentChildLink(mirroredIdB, mirroredIdA);

            // resolve the conflict by taking source side changes
            conflictResolver.TryResolveConflict(conflicts[0], new ManualConflictResolutionAction(), "/");

            // restart the migration tool, after sync
            // source: b->a->c
            // target: a->c
            RunAndNoValidate(true);

            conflicts = conflictResolver.GetConflicts();
            Assert.IsTrue(conflicts.Count == 0, "There should be no conflict");

            Assert.AreEqual(2, TfsSourceAdapter.GetRelatedLinkCount(workitemIdA));
            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(workitemIdB));
            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(workitemIdC));
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(mirroredIdA));
            Assert.AreEqual(0, TfsTargetAdapter.GetRelatedLinkCount(mirroredIdB));
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(mirroredIdC));
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
        /// Link_TwoParentsTest
        ///</summary>
        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("Migrating link changes from source should not result in an invalid topology (two parents)")]
        public void Link_TwoParentsInHistoryTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_SetOneWayNoContextSync);

            // Legend: x->y (y is a parent of x)
            // T1: Source: b->c
            // T2: Sync
            // T3: Target: b->c
            // T4: Source: delete b->c, add b->a
            // T5: Sync

            // add a work item on source side
            string title1 = string.Format("A {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemIdA = SourceAdapter.AddWorkItem("Bug", title1, "A");

            string title2 = string.Format("B {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemIdB = SourceAdapter.AddWorkItem("Bug", title2, "B");

            string title3 = string.Format("C {0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemIdC = SourceAdapter.AddWorkItem("Bug", title3, "C");

            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(SetTargetFilterString);

            RunAndNoValidate();

            // T1
            TfsSourceAdapter.AddParentChildLink(workitemIdC, workitemIdB);
            RunAndNoValidate(true);

            // T4: 
            // Source: delete c->b, add a->c ==> b->a->c (a is the child of c)
            TfsSourceAdapter.DeleteParentChildLink(workitemIdC, workitemIdB);
            TfsSourceAdapter.AddParentChildLink(workitemIdA, workitemIdB);

            RunAndNoValidate(true);

            // verify the expected conflict
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflict");
            
            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(workitemIdA));
            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(workitemIdB));
            Assert.AreEqual(0, TfsSourceAdapter.GetRelatedLinkCount(workitemIdC));

            // Target: delete b->a, add a->b ==> a->b, c->b (a is the child of b) 
            int mirroredIdA = QueryMirroredWorkItemID(workitemIdA);
            int mirroredIdB = QueryMirroredWorkItemID(workitemIdB);
            int mirroredIdC = QueryMirroredWorkItemID(workitemIdC);
            
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(mirroredIdA));
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(mirroredIdB));
            Assert.AreEqual(0, TfsTargetAdapter.GetRelatedLinkCount(mirroredIdC));
        }
    }
}
