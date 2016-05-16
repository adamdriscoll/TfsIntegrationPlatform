// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTestLibrary;
using MigrationTestLibrary.Conflict;
using Microsoft.TeamFoundation.Migration.Toolkit.ServerDiff;

namespace TfsWitTest
{
    /// <summary>
    /// WIT basic tests
    /// </summary>
    [TestClass]
    public class AttachmentTest : TfsWITTestCaseBase
    {
        ///<summary>
        /// Migrate a work item with attachment
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Migrate a work item with attachment")]
        public void Attachment_BasicTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_EnableBypassRulesOnTarget);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);

            // add a work item on source side
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title", "description1");

            // update link comment

            WITAttachmentChangeAction action1 = new WITAttachmentChangeAction();
            action1.AddAttachment(new WITAttachment("attachment1.txt", "comment 1"));
            SourceAdapter.UpdateAttachment(workitemId, action1);

            RunAndNoValidate();

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches
            VerifySyncResult(result, new List<string> { "System.IterationPath", "System.AreaPath" });
        }


        ///<summary>
        /// Add 3 attachments and delete 1
        ///</summary>
        [TestMethod(), Priority(1), Owner("wlennon")]
        [Description("Add 3 attachments and delete 1")]
        public void AttachmentAddAddDeleteTest()
        {
            // add a work item on source side
            int sourceId = SourceAdapter.AddWorkItem("Bug", "title", "description1");

            // update link comment

            WITAttachmentChangeAction add1 = new WITAttachmentChangeAction();
            add1.AddAttachment(new WITAttachment("attachment1.txt", "comment 1"));
            SourceAdapter.UpdateAttachment(sourceId, add1);

            WITAttachmentChangeAction add2 = new WITAttachmentChangeAction();
            add2.AddAttachment(new WITAttachment("attachment2.txt", "comment 2"));
            SourceAdapter.UpdateAttachment(sourceId, add2);

            RunAndNoValidate(true);

            WITAttachmentChangeAction delete2 = new WITAttachmentChangeAction();
            delete2.DeleteAttachment(new WITAttachment("attachment2.txt", null));
            SourceAdapter.UpdateAttachment(sourceId, delete2);

            WITAttachmentChangeAction add3 = new WITAttachmentChangeAction();
            add3.AddAttachment(new WITAttachment("attachment3.txt", "comment 2"));
            SourceAdapter.UpdateAttachment(sourceId, add3);

            RunAndNoValidate(true);

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            int targetId = QueryTargetWorkItemID(sourceId);
            int sourceCount = TfsSourceAdapter.GetAttachmentCount(sourceId);
            int targetCount = TfsTargetAdapter.GetAttachmentCount(targetId);

            Assert.AreEqual(2, sourceCount, "source should have 2 attachments");
            Assert.AreEqual(2, targetCount, "target should have 2 attachments");

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches
            VerifySyncResult(result, new List<string> { "System.IterationPath", "System.AreaPath" });
        }

        ///<summary>
        /// duplicate attachment test
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Migrate a work item with duplicate attachments")]
        public void Attachment_DuplicateTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_EnableBypassRulesOnTarget);

            // add a work item on source side
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title", "description1");

            WITAttachmentChangeAction action1 = new WITAttachmentChangeAction();
            action1.AddAttachment(new WITAttachment("attachment1.txt", "comment 1"));
            SourceAdapter.UpdateAttachment(workitemId, action1);

            // Attach same name and comment
            WITAttachmentChangeAction action2 = new WITAttachmentChangeAction();
            action2.AddAttachment(new WITAttachment("attachment1.txt", "comment 1"));
            SourceAdapter.UpdateAttachment(workitemId, action2);

            RunAndNoValidate();

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches
            VerifySyncResult(result, new List<string> { "System.IterationPath", "System.AreaPath" });

            // TODO: Remove the following verification once ServerDiff supports attachment count diffing.
            int mirroredId = QueryTargetWorkItemID(workitemId);
            Assert.AreEqual(TfsSourceAdapter.GetAttachmentCount(workitemId),
                TfsTargetAdapter.GetAttachmentCount(mirroredId), "Attachment counts should be same");
        }

        ///<summary>
        /// delete attachment
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Delete an attachment")]
        public void Attachment_DeleteTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_EnableBypassRulesOnTarget);

            // add a work item on source side
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title", "description1");

            WITAttachmentChangeAction action1 = new WITAttachmentChangeAction();
            WITAttachment attachment1 = new WITAttachment("attachment1.txt", "comment 1");
            action1.AddAttachment(attachment1);
            SourceAdapter.UpdateAttachment(workitemId, action1);

            RunAndNoValidate();

            // delete the attachment
            WITAttachmentChangeAction action2 = new WITAttachmentChangeAction();
            action2.DeleteAttachment(attachment1);
            SourceAdapter.UpdateAttachment(workitemId, action2);

            WITChangeAction action3 = new WITChangeAction();
            action3.Description = "Description change by action 3";
            SourceAdapter.UpdateWorkItem(workitemId, action3);

            RunAndNoValidate(true);

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches
            VerifySyncResult(result, new List<string> { "System.IterationPath", "System.AreaPath" });

            // TODO: Remove the following verification once ServerDiff supports attachment count diffing.
            int mirroredId = QueryTargetWorkItemID(workitemId);
            Assert.AreEqual(TfsSourceAdapter.GetAttachmentCount(workitemId),
                TfsTargetAdapter.GetAttachmentCount(mirroredId), "Attachment counts should be same");
        }
    }
}
