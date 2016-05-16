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
        [TestInitialize]
        public override void Initialize()
        {
            base.Initialize();

            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);
        }

        ///<summary>
        /// Add 1 Scenario and 100 Experiences and link them together, trying to repro the dogfood link deletion bug
        ///</summary>
        [TestMethod(), Priority(1), Owner("wlennon"), Timeout(60 * 1000 * 1000)]
        [Description("Repro dogfood link deletion bug")]
        public void LinkScenarioExperienceTest()
        {
            const int ExperienceCount = 10;
            // On Left add 1 Scenario and a bunch of Experiences

            int sourceScenarioId = SourceAdapter.AddWorkItem("Scenario", "scenario title", "source scenario description");
            Console.WriteLine("Source Scenario ID = {0}", sourceScenarioId);

            int[] sourceExperienceIDs = new int[ExperienceCount];
            int[] targetExperienceIDs = new int[ExperienceCount];

            for (int index = 0; index < ExperienceCount; index++)
            {
                string title = string.Format("Experience {0}", index);
                sourceExperienceIDs[index] = SourceAdapter.AddWorkItem("Experience", title, title);
            }

            // sync
            RunAndNoValidate();

            // get the migrated target ID
            int targetScenarioId = QueryTargetWorkItemID(sourceScenarioId);
            Console.WriteLine("Target Scenario ID = {0}", targetScenarioId);

            // Modify some field of the scenario: this should create a revision of the work item that has no link changes to get migrated
            WITChangeAction sourceAction = new WITChangeAction()
            {
                History = "Adding scenario-experience links on the source side",
            };
            SourceAdapter.UpdateWorkItem(sourceScenarioId, sourceAction);

            // Link source scenario to all the experiences
            for (int index = 0; index < ExperienceCount; index++)
            {
                TfsSourceAdapter.AddScenarioExperienceLink(sourceScenarioId, sourceExperienceIDs[index]);
            }

            // Modify the history again forcing the above links to get migrated
            WITChangeAction sourceAction2 = new WITChangeAction()
            {
                AssignedTo = "billbar",
            };
            SourceAdapter.UpdateWorkItem(sourceScenarioId, sourceAction2);

            for (int index = 0; index < ExperienceCount; index++)
            {
                Console.WriteLine("Getting mirrored TargetWorkItemID for target experience: " + sourceExperienceIDs[index].ToString());
                targetExperienceIDs[index] = QueryTargetWorkItemID(sourceExperienceIDs[index]);
                Console.WriteLine("Mirrored TargetWorkItemID: " + targetExperienceIDs[index].ToString());
            }

            /*
            // on target modify field of the mirrored Ids of each of the Experience
            // This should cause them to get sync'd back to the source side
            WITChangeAction targetAction = new WITChangeAction()
            {
                History = "Touch the target experience work item",
            };
            foreach (int targetExperienceID in targetExperienceIDs)
            {
                TargetAdapter.UpdateWorkItem(targetExperienceID, targetAction);
            }
             */

            // sync
            RunAndNoValidate(true);

            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });

            Assert.AreEqual(ExperienceCount, TfsSourceAdapter.GetRelatedLinkCount(sourceScenarioId));
            Assert.AreEqual(ExperienceCount, TfsSourceAdapter.GetRelatedLinkCount(targetScenarioId));
        }

        ///<summary>
        /// Add Parent Link
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Add Parent Link")]
        public void Link_AddParentTest()
        {
            // add a work item on source side
            int workitemId1 = SourceAdapter.AddWorkItem("Bug", "title", "description1");

            int workitemId2 = SourceAdapter.AddWorkItem("Bug", "title2", "description2");

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

            int mirroredId1 = QueryTargetWorkItemID(workitemId1);
            int mirroredId2 = QueryTargetWorkItemID(workitemId2);
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(mirroredId1));
            Assert.AreEqual(false, TfsTargetAdapter.IsLinkLocked(mirroredId1, mirroredId2));
        }

        ///<summary>
        /// Add/Delete related Link 
        ///</summary>
        [TestMethod(), Priority(1), Owner("wlennon")]
        [Description("Multiple Add/Delete Related Link test")]
        public void Link_MultiAddDeleteRelatedLinkTest()
        {
            for (int i = 0; i < 7; ++i)
            {
                // add a work item on source side
                string title = "title " + i.ToString();
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
                targetWorkItemIds[i] = QueryTargetWorkItemID(SourceWorkItemIdList[i]);
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
        /// Delete Parent Link
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Delete Parent Link")]
        public void Link_DeleteParentTest()
        {
            // add a work item on source side
            int workitemId1 = SourceAdapter.AddWorkItem("Bug", "parent", "description1");

            int workitemId2 = SourceAdapter.AddWorkItem("Bug", "child", "description2");

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

            int mirroredId = QueryTargetWorkItemID(workitemId1);
            Assert.AreEqual(0, TfsTargetAdapter.GetRelatedLinkCount(mirroredId));
        }

        ///<summary>
        /// sync IsLocked link property for V2 WorkItemLink
        ///</summary>
        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("Sync Locked Link")]
        public void LockedLink_BasicSyncTest()
        {
            // add a work item on source side
            int workitemId1 = SourceAdapter.AddWorkItem("Bug", "parent", "description1");
            int workitemId2 = SourceAdapter.AddWorkItem("Bug", "child", "description2");

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

            int mirroredId1 = QueryTargetWorkItemID(workitemId1);
            int mirroredId2 = QueryTargetWorkItemID(workitemId2);
            Assert.IsTrue(TfsTargetAdapter.IsLinkLocked(mirroredId1, mirroredId2), "migrated link is not locked");
        }

        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("Sync Locked Link")]
        public void LockedLink_RemoveLockAndDeleteLinkSyncTest()
        {
            // add a work item on source side
            int workitemId1 = SourceAdapter.AddWorkItem("Bug", "parent", "description1");
            int workitemId2 = SourceAdapter.AddWorkItem("Bug", "child", "description2");

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
            // add a work item on source side
            int workitemId1 = SourceAdapter.AddWorkItem("Bug", "parent", "description1");
            int workitemId2 = SourceAdapter.AddWorkItem("Bug", "child", "description2");

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

            // TODO: Why does TwoWayRight have only one link mismatch, but the others have 2?
            Assert.AreEqual(2, result.LinkMismatchCount);
        }

        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("Sync Locked Link")]
        public void LockedLink_SyncUnlockedFirstThenUpdateLocked()
        {
            // add a work item on source side
            int workitemId1 = SourceAdapter.AddWorkItem("Bug", "parent", "description1");
            int workitemId2 = SourceAdapter.AddWorkItem("Bug", "child", "description2");

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

            int mirroredId1 = QueryTargetWorkItemID(workitemId1);
            int mirroredId2 = QueryTargetWorkItemID(workitemId2);
            Assert.IsTrue(TfsTargetAdapter.IsLinkLocked(mirroredId1, mirroredId2), "migrated link is not locked");
        }

        ///<summary>
        /// Cyclic Link (touching same work items)
        ///</summary>
        [TestMethod(), Priority(1), Owner("wlennon")]
        [Description("Cyclic Link (touching same work items)")]
        public void Link_CyclicLinkSamePairTest()
        {
            // Legend: x->y (y is a parent of x)
            // T1: Source: a b
            // T2: Sync
            // T3: Target: a b
            // T4: Source: add a->b ==> a->b 
            //     Target: add b->a ==> b->a
            // T5: Sync
            // We should detect cyclic link and raise a conflict

            // add a work item on source side
            int sourceIdA = SourceAdapter.AddWorkItem("Bug", "A", "A");
            int sourceIdB = SourceAdapter.AddWorkItem("Bug", "B", "B");

            RunAndNoValidate();
            int targetId1 = QueryTargetWorkItemID(sourceIdA);
            int targetId2 = QueryTargetWorkItemID(sourceIdB);

            TfsSourceAdapter.AddParentChildLink(sourceIdB, sourceIdA);
            TfsTargetAdapter.AddParentChildLink(targetId1, targetId2);

            RunAndNoValidate(true);

            // verify the expected conflict          
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Guid TFSCyclicLinkConflictTypeReferenceName = new Guid("BF1277E9-A218-4a2d-8C3C-A9501D30ECD5");

            // Workaround for Bug 529648:  Accept 1 or more conflicts.  We should only generate 1 conflict, but for Two-way scenarios we generate 2.
            Assert.IsTrue(conflicts.Count > 0, "There should be 1 or more conflicts");

            foreach (RTConflict conflict in conflicts)
            {
                Assert.IsTrue(TFSCyclicLinkConflictTypeReferenceName.Equals(conflict.ConflictTypeReference.Value.ReferenceName),
                    "Conflict should be a TFS cyclic link reference conflict");
            }

            // fix the hierarchy on target, delete b->a ==> a b
            TfsTargetAdapter.DeleteParentChildLink(targetId1, targetId2);

            foreach (RTConflict conflict in conflicts)
            {
                // resolve the conflict by retrying
                conflictResolver.TryResolveConflict(conflict, new ManualConflictResolutionAction(), "/");

                // restart the session
                RunAndNoValidate(true);
            }

            conflicts = conflictResolver.GetConflicts();
            if (TestEnvironment.MigrationTestType == MigrationTestType.OneWay)
            {
                Assert.IsTrue(conflicts.Count == 0, "There should be no conflict");
            }
            else
            {
                // In two-way cases, migration back from target to source will still generate a conflict
                // because we have not deleted a->b on the source
                Assert.IsTrue(conflicts.Count == 1, "There should be one conflict");
            }

            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(sourceIdA));
            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(sourceIdB));
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(targetId1));
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(targetId2));

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });
        }

        ///<summary>
        /// Cyclic Link, both one way and two way
        ///</summary>
        [TestMethod(), Priority(1), Owner("wlennon")]
        [Description("Cyclic Link")]
        public void Link_CyclicLinkTest()
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
            int sourceIdA = SourceAdapter.AddWorkItem("Bug", "A", "A");
            int sourceIdB = SourceAdapter.AddWorkItem("Bug", "B", "B");
            int sourceIdC = SourceAdapter.AddWorkItem("Bug", "C", "C");
            int sourceIdD = SourceAdapter.AddWorkItem("Bug", "D", "D");

            RunAndNoValidate();

            // add b->c
            TfsSourceAdapter.AddParentChildLink(sourceIdC, sourceIdB);
            // add a->b
            TfsSourceAdapter.AddParentChildLink(sourceIdB, sourceIdA);

            // after sync, 
            // Source: a->b->c d
            // Target: a->b->c d
            RunAndNoValidate(true);

            int targetIdA = QueryTargetWorkItemID(sourceIdA);
            int targetIdB = QueryTargetWorkItemID(sourceIdB);
            int targetIdC = QueryTargetWorkItemID(sourceIdC);
            int targetIdD = QueryTargetWorkItemID(sourceIdD);

            // Source: delete b->c, add c->a, now source is c->a->b d
            TfsSourceAdapter.DeleteParentChildLink(sourceIdC, sourceIdB);
            TfsSourceAdapter.AddParentChildLink(sourceIdA, sourceIdC);

            // Target: delete b->c, add b->d , d->c, now target is a->b->d->c
            TfsTargetAdapter.DeleteParentChildLink(targetIdC, targetIdB);
            TfsTargetAdapter.AddParentChildLink(targetIdD, targetIdB);
            TfsTargetAdapter.AddParentChildLink(targetIdC, targetIdD);

            // c->a from source will create cyclic link on target
            RunAndNoValidate(true);

            // verify the expected conflict          
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Guid TFSCyclicLinkConflictTypeReferenceName = new Guid("BF1277E9-A218-4a2d-8C3C-A9501D30ECD5");

            // Workaround for Bug 529648:  Accept 1 or more conflicts.  We should only generate 1 conflict, but for Two-way scenarios we generate 2.
            Assert.IsTrue(conflicts.Count > 0, "There should be 1 or more conflicts");

            foreach (RTConflict conflict in conflicts)
            {
                Assert.IsTrue(TFSCyclicLinkConflictTypeReferenceName.Equals(conflict.ConflictTypeReference.Value.ReferenceName),
                    "Conflict should be a TFS cyclic link reference conflict");
            }

            // fix the hierarchy on target, delete d->c => a->b->d c
            TfsTargetAdapter.DeleteParentChildLink(targetIdC, targetIdD);

            foreach (RTConflict rtConflict in conflicts)
            {
                // resolve the conflict by retrying
                conflictResolver.TryResolveConflict(rtConflict, new ManualConflictResolutionAction(), "/");

                // restart the migration tool, after sync,
                RunAndNoValidate(true);
            }

            conflicts = conflictResolver.GetConflicts();
            Assert.IsTrue(conflicts.Count == 0, "There should be no conflict");

            // hiearchy should be c->a->b->d
            Assert.AreEqual(2, TfsTargetAdapter.GetRelatedLinkCount(targetIdA));
            Assert.AreEqual(2, TfsTargetAdapter.GetRelatedLinkCount(targetIdB));
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(targetIdC));
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(targetIdD));

            if (TestEnvironment.MigrationTestType == MigrationTestType.OneWay)
            {
                // for one-way migration the source should be out of sync with the target now
                Assert.AreEqual(2, TfsSourceAdapter.GetRelatedLinkCount(sourceIdA));
                Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(sourceIdB));
                Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(sourceIdC));
                Assert.AreEqual(0, TfsSourceAdapter.GetRelatedLinkCount(sourceIdD));
            }
            else
            {
                // Two-way sync should be in sync with target now
                // TODO: why does this work for TwoWayLeft but not TwoWayRight?
                Assert.AreEqual(2, TfsSourceAdapter.GetRelatedLinkCount(sourceIdA));
                Assert.AreEqual(2, TfsSourceAdapter.GetRelatedLinkCount(sourceIdB));
                Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(sourceIdC));
                Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(sourceIdD));

                // verify sync result 
                WitDiffResult result = GetDiffResult();

                // ignore Area/Iteration path mismatches
                VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });
            }
        }

        ///<summary>
        /// Link_TwoParentsTest
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Migrating link changes from source would result in an invalid topology (two parents)")]
        public void Link_TwoParentsTest()
        {
            // Legend: x->y (y is a parent of x)
            // T1: Source: c->b->a
            // T2: Sync
            // T3: Target: c->b->a
            // T4: Source: delete c->b, add a->c ==> b->a->c
            //     Target: delete b->a, add a->b ==> a->b, c->b (b has two children)
            // T5: Sync

            // add a work item on source side
            int sourceIdA = SourceAdapter.AddWorkItem("Bug", "A", "A");
            int sourceIdB = SourceAdapter.AddWorkItem("Bug", "B", "B");
            int sourceIdC = SourceAdapter.AddWorkItem("Bug", "C", "C");

            RunAndNoValidate();

            // T1-T3: c->b->a
            TfsSourceAdapter.AddParentChildLink(sourceIdB, sourceIdC);
            TfsSourceAdapter.AddParentChildLink(sourceIdA, sourceIdB);
            RunAndNoValidate(true);

            // T4: 
            // Source: delete c->b, add a->c ==> b->a->c (a is the child of c)
            TfsSourceAdapter.DeleteParentChildLink(sourceIdB, sourceIdC);
            TfsSourceAdapter.AddParentChildLink(sourceIdC, sourceIdA);

            // Target: delete b->a, add a->b ==> a->b, c->b (a is the child of b) 
            int targetId1 = QueryTargetWorkItemID(sourceIdA);
            int targetId2 = QueryTargetWorkItemID(sourceIdB);
            int targetIdC = QueryTargetWorkItemID(sourceIdC);
            TfsTargetAdapter.DeleteParentChildLink(targetId1, targetId2);
            TfsTargetAdapter.AddParentChildLink(targetId2, targetId1);

            // after sync:
            // source: b->a->c
            // target: a->b (adding a->c is conflicted)
            RunAndNoValidate(true);

            // verify the expected conflict
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Guid TFSMulitpleParentLinkConflictTypeReferenceName = new Guid("ADCE870C-33C0-46bc-9350-31660A463F9A");

            // Workaround for Bug 529648:  Accept 1 or more conflicts.  We should only generate 1 conflict, but for Two-way scenarios we generate 2.
            Assert.IsTrue(conflicts.Count > 0, "There should be 1 or more conflicts");

            foreach (RTConflict conflict in conflicts)
            {
                Assert.IsTrue(TFSMulitpleParentLinkConflictTypeReferenceName.Equals(conflict.ConflictTypeReference.Value.ReferenceName),
                    "It should be a TFSMultipleParentLinkConflict");
            }

            // fix the hierarchy on target, delete a->b
            // target: a, b, c
            TfsTargetAdapter.DeleteParentChildLink(targetId2, targetId1);

            foreach (RTConflict conflict in conflicts)
            {
                // resolve the conflict(s) by taking source side changes
                conflictResolver.TryResolveConflict(conflict, new ManualConflictResolutionAction(), "/");
            }

            // restart the migration tool, after sync
            // source: b->a->c
            // target: a->c
            RunAndNoValidate(true);

            conflicts = conflictResolver.GetConflicts();

            if (TestEnvironment.MigrationTestType == MigrationTestType.OneWay)
            {
                Assert.IsTrue(conflicts.Count == 0, "There should be no conflict");

                // One way migration should have source/target out of sync
                Assert.AreEqual(2, TfsSourceAdapter.GetRelatedLinkCount(sourceIdA));
                Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(sourceIdB));
                Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(sourceIdC));
            }
            else
            {
                // In two-way case, add a->b on Target at T4 above causes multi-parent conflict
                // when migrated back to source because a->c already exists on source.
                Assert.IsTrue(conflicts.Count == 1, "There should be one conflict");

                Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(sourceIdA));
                Assert.AreEqual(0, TfsSourceAdapter.GetRelatedLinkCount(sourceIdB));
                Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(sourceIdC));
            }
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(targetId1));
            Assert.AreEqual(0, TfsTargetAdapter.GetRelatedLinkCount(targetId2));
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(targetIdC));
        }

        ///<summary>
        /// Link_TwoParentsTest
        ///</summary>
        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("Migrating link changes from source should not result in an invalid topology (two parents)")]
        public void Link_TwoParentsInHistoryTest()
        {
            // Legend: x->y (y is a parent of x)
            // T1: Source: b->c
            // T2: Sync
            // T3: Target: b->c
            // T4: Source: delete b->c, add b->a
            // T5: Sync

            // add a work item on source side
            int sourceIdA = SourceAdapter.AddWorkItem("Bug", "A", "A");
            int sourceIdB = SourceAdapter.AddWorkItem("Bug", "B", "B");
            int sourceIdC = SourceAdapter.AddWorkItem("Bug", "C", "C");

            RunAndNoValidate();

            // T1
            TfsSourceAdapter.AddParentChildLink(sourceIdC, sourceIdB);
            RunAndNoValidate(true);

            // T4: 
            // Source: delete c->b, add a->c ==> b->a->c (a is the child of c)
            TfsSourceAdapter.DeleteParentChildLink(sourceIdC, sourceIdB);
            TfsSourceAdapter.AddParentChildLink(sourceIdA, sourceIdB);

            RunAndNoValidate(true);

            // verify the expected conflict
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflict");

            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(sourceIdA));
            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(sourceIdB));
            Assert.AreEqual(0, TfsSourceAdapter.GetRelatedLinkCount(sourceIdC));

            // Target: delete b->a, add a->b ==> a->b, c->b (a is the child of b) 
            int targetId1 = QueryTargetWorkItemID(sourceIdA);
            int targetId2 = QueryTargetWorkItemID(sourceIdB);
            int targetIdC = QueryTargetWorkItemID(sourceIdC);

            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(targetId1));
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(targetId2));
            Assert.AreEqual(0, TfsTargetAdapter.GetRelatedLinkCount(targetIdC));
        }

        ///<summary>
        /// LinkAddAddDeleteLink
        ///</summary>
        [TestMethod(), Priority(1), Owner("wlennon")]
        [Description("Link Add Link 1, Add Link 2, Delete Link1")]
        public void LinkAddAddDeleteLinkTest()
        {
            // add a work item on source side
            int sourceIdA = SourceAdapter.AddWorkItem("Bug", "A", "A");
            int sourceIdB = SourceAdapter.AddWorkItem("Task", "B", "B");
            int sourceIdC = SourceAdapter.AddWorkItem("Scenario", "C", "C");

            RunAndNoValidate();

            // link A-B and A-C
            TfsSourceAdapter.AddRelatedWorkItemLink(sourceIdA, sourceIdB);

            WITChangeAction action = new WITChangeAction() { Title = "added AB link", };
            TfsSourceAdapter.UpdateWorkItem(sourceIdA, action);
            TfsSourceAdapter.UpdateWorkItem(sourceIdB, action);

            TfsSourceAdapter.AddRelatedWorkItemLink(sourceIdA, sourceIdC);

            action = new WITChangeAction() { Title = "added AC link", };
            TfsSourceAdapter.UpdateWorkItem(sourceIdA, action);
            TfsSourceAdapter.UpdateWorkItem(sourceIdC, action);

            // delete A-B link
            TfsSourceAdapter.DeleteRelatedWorkItemLink(sourceIdA, sourceIdB);

            action = new WITChangeAction() { Title = "deleted AB link", };
            TfsSourceAdapter.UpdateWorkItem(sourceIdA, action);
            TfsSourceAdapter.UpdateWorkItem(sourceIdB, action);

            RunAndNoValidate(true);

            // verify no conflicts
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflict");

            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(sourceIdA));
            Assert.AreEqual(0, TfsSourceAdapter.GetRelatedLinkCount(sourceIdB));
            Assert.AreEqual(1, TfsSourceAdapter.GetRelatedLinkCount(sourceIdC));

            int targetIdA = QueryTargetWorkItemID(sourceIdA);
            int targetIdB = QueryTargetWorkItemID(sourceIdB);
            int targetIdC = QueryTargetWorkItemID(sourceIdC);

            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(targetIdA));
            Assert.AreEqual(0, TfsTargetAdapter.GetRelatedLinkCount(targetIdB));
            Assert.AreEqual(1, TfsTargetAdapter.GetRelatedLinkCount(targetIdC));
        }
    }
}
