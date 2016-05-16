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
            string title = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId = SourceAdapter.AddWorkItem("Bug", title, "description1");

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
        /// duplicate attachment test
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Migrate a work item with duplicate attachments")]
        public void Attachment_DuplicateTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_EnableBypassRulesOnTarget);

            // add a work item on source side
            string title = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId = SourceAdapter.AddWorkItem("Bug", title, "description1");

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
            int mirroredId = QueryMirroredWorkItemID(workitemId);
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
            string title = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));
            int workitemId = SourceAdapter.AddWorkItem("Bug", title, "description1");

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
            int mirroredId = QueryMirroredWorkItemID(workitemId);
            Assert.AreEqual(TfsSourceAdapter.GetAttachmentCount(workitemId),
                TfsTargetAdapter.GetAttachmentCount(mirroredId), "Attachment counts should be same");
        }
    }
}
