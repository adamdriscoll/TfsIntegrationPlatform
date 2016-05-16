// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.BusinessModel.WIT;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ServerDiff;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTestLibrary;
using MigrationTestLibrary.Conflict;
using BM = Microsoft.TeamFoundation.Migration.BusinessModel;
using System.Diagnostics;

namespace TfsWitTest.Conflict
{
    /// <summary>
    /// WIT conflict test cases
    /// </summary>
    [TestClass]
    public class WITConflictTest : TfsWITTestCaseBase
    {
        private const string InvalidFieldRefName = "TfsWitTest.Conflict.InvalidFieldName";
        private ConflictResolver m_conflictResolver;

        public const string SOURCE_R1_DESC = "SOURCE: Description A1";
        public const string SOURCE_R2_TITLE = "SOURCE: Title B";
        public const string TARGET_R1_DESC = "TARGET: Description A2";

        ///<summary>
        /// resolve edit/edit conflict by taking source side changes
        /// See EditEditConflictScenario function for setup steps 1-5
        /// 6. User resolves edit/edit conflict by taking source side
        /// 7. Start Bi-directional migration (2nd round)
        ///    Pipeline flow S->T: Migrate S:A1 and S:B
        ///    Pipeline flow T->S: Skip T:A2 
        /// 8. Once migration is complete, two work items will have the following revision history
        ///    Source: A1,B ==> 
        ///    Target: A2   ==> A1',B'
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("single Edit/Edit conflict in bi-directional work flow")]
        public void EditEditConflictTakeSourceTest()
        {
            int sourceId;
            int targetId;
            EditEditConflictScenario(out sourceId, out targetId);

            string srcTitle = SourceAdapter.GetFieldValue(sourceId, FIELD_TITLE);
            string srcDesc = SourceAdapter.GetFieldValue(sourceId, FIELD_DESCRIPTION);
            string tarTitle = TargetAdapter.GetFieldValue(targetId, FIELD_TITLE);
            string tarDesc = TargetAdapter.GetFieldValue(targetId, FIELD_DESCRIPTION);
            Assert.AreNotEqual(srcTitle, tarTitle, "Title should not match due to conflict");
            Assert.AreNotEqual(srcDesc, tarDesc, "Description should not match due to conflict");

            // verify we have edit/edit conflict
            List<RTConflict> conflicts = m_conflictResolver.GetConflicts();
            Assert.IsTrue(conflicts.Count >= 1, "There should be edit/edit conflict");
            WITEditEditConflictType contentConflict = new WITEditEditConflictType();
            Assert.IsTrue(contentConflict.ReferenceName.Equals(conflicts[0].ConflictTypeReference.Value.ReferenceName),
                "It should be WIT edit/edit conflict");

            // resolve the conflict by taking source side changes
            bool isResolved = false;
            if (TestEnvironment.MigrationTestType == MigrationTestType.TwoWayRight)
            {
                isResolved = m_conflictResolver.TryResolveConflict(conflicts[0], new WITEditEditConflictTakeTargetChangesAction(), "/" + targetId);
            }
            else
            {
                isResolved = m_conflictResolver.TryResolveConflict(conflicts[0], new WITEditEditConflictTakeSourceChangesAction(), "/" + sourceId);
            }
            Assert.IsTrue(isResolved, "Conflict resolution should succeed");

            // restart the migration tool
            RunAndNoValidate(true);

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches due to test environments
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });

            srcDesc = SourceAdapter.GetFieldValue(sourceId, FIELD_DESCRIPTION);
            tarDesc = TargetAdapter.GetFieldValue(targetId, FIELD_DESCRIPTION);
            Assert.AreEqual(srcDesc, SOURCE_R1_DESC);
            Assert.AreEqual(tarDesc, SOURCE_R1_DESC);
        }

        ///<summary>
        /// resolve edit/edit conflict by taking source side changes
        /// See EditEditConflictScenario function for setup steps 1-5
        /// *5.5 Before user resolves the conflict, more changes on source side is introduced and the session picks them up
        ///    (The expected correct behavior should be that the changes are blocked)
        /// 6. User resolves edit/edit conflict by taking source side
        /// 7. Start Bi-directional migration (2nd round)
        ///    Pipeline flow S->T: Migrate S:A1 and S:B
        ///    Pipeline flow T->S: Skip T:A2 
        /// 8. Once migration is complete, two work items will have the following revision history
        ///    Source: A1,B ==> 
        ///    Target: A2   ==> A1',B'
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("single Edit/Edit conflict in bi-directional work flow")]
        public void EditEditConflictTakeTargetAfterMoreDeltaFromSourceTest()
        {
            int sourceId;
            int targetId;
            EditEditConflictScenario(out sourceId, out targetId);

            string srcTitle = SourceAdapter.GetFieldValue(sourceId, FIELD_TITLE);
            string srcDesc = SourceAdapter.GetFieldValue(sourceId, FIELD_DESCRIPTION);
            string tarTitle = TargetAdapter.GetFieldValue(targetId, FIELD_TITLE);
            string tarDesc = TargetAdapter.GetFieldValue(targetId, FIELD_DESCRIPTION);
            Assert.AreNotEqual(srcTitle, tarTitle, "Title should not match due to conflict");
            Assert.AreNotEqual(srcDesc, tarDesc, "Description should not match due to conflict");

            Trace.WriteLine("---------------------------------");
            Trace.WriteLine("verify we have edit/edit conflict");
            Trace.WriteLine("---------------------------------");
            List<RTConflict> conflicts = m_conflictResolver.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be one edit/edit conflict");

            WITEditEditConflictType contentConflict = new WITEditEditConflictType();
            Assert.IsTrue(contentConflict.ReferenceName.Equals(conflicts[0].ConflictTypeReference.Value.ReferenceName),
                "It should be WIT edit/edit conflict");

            // ---- start *5.5
            Trace.WriteLine("---------------------------------");
            Trace.WriteLine("update work items on both sides");
            Trace.WriteLine("---------------------------------");
            WITChangeAction action1 = new WITChangeAction();
            WITChangeAction action2 = new WITChangeAction();

            const string SourceLastDesc = "New Description 5.5.1";
            action1.Description = SourceLastDesc;
            SourceAdapter.UpdateWorkItem(sourceId, action1);

            const string TargetLastTitle = "New Title 5.5.2";
            action2.Title = TargetLastTitle;
            TargetAdapter.UpdateWorkItem(targetId, action2);

            Trace.WriteLine("---------------------------------");
            Trace.WriteLine("sync again");
            Trace.WriteLine("---------------------------------");
            // 1 edit/edit conflict with chained conflicts
            RunAndNoValidate(true);

            // ----- end *5.5

            Trace.WriteLine("---------------------------------");
            Trace.WriteLine("resolve the conflict by taking target side changes");
            Trace.WriteLine("---------------------------------");
            bool isResolved = false;
            if (TestEnvironment.MigrationTestType == MigrationTestType.TwoWayRight)
            {
                isResolved = m_conflictResolver.TryResolveConflict(conflicts[0], new WITEditEditConflictTakeSourceChangesAction(), "/" + targetId);
            }
            else
            {
                isResolved = m_conflictResolver.TryResolveConflict(conflicts[0], new WITEditEditConflictTakeTargetChangesAction(), "/" + sourceId);
            }
            Assert.IsTrue(isResolved, "Conflict resolution should succeed");
            conflicts = m_conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            Trace.WriteLine("---------------------------------");
            Trace.WriteLine("sync again");
            Trace.WriteLine("---------------------------------");
            RunAndNoValidate(true);

            srcDesc = SourceAdapter.GetFieldValue(sourceId, FIELD_DESCRIPTION);
            tarDesc = TargetAdapter.GetFieldValue(targetId, FIELD_DESCRIPTION);
            srcTitle = SourceAdapter.GetFieldValue(sourceId, FIELD_TITLE);
            tarTitle = TargetAdapter.GetFieldValue(targetId, FIELD_TITLE);

            if (TestEnvironment.MigrationTestType == MigrationTestType.OneWay)
            {
                // For one-way we took the target changes, but those changes should not migrate back to source
                Assert.AreEqual(SourceLastDesc, srcDesc);
                Assert.AreEqual(SourceLastDesc, tarDesc);

                Assert.IsTrue(srcTitle.EndsWith(SOURCE_R2_TITLE));
                Assert.IsTrue(tarTitle.EndsWith(SOURCE_R2_TITLE));
            }
            else
            {
                Assert.AreEqual(TARGET_R1_DESC, srcDesc);
                Assert.AreEqual(TARGET_R1_DESC, tarDesc);
                Assert.IsTrue(srcTitle.EndsWith(TargetLastTitle));
                Assert.IsTrue(tarTitle.EndsWith(TargetLastTitle));
            }

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches due to test environments
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });
        }

        ///<summary>
        /// resolve edit/edit conflict by taking target side changes
        /// 
        /// See EditEditConflictScenario function for setup steps 1-5
        /// 6. User resolves edit/edit conflict by taking target side
        /// 7. Start Bi-directional migration (2nd round)
        ///    Pipeline flow S->T: Skip S:A1 and Migrate S:B
        ///    Pipeline flow T->S: Migrate T:A2 
        /// 8. Once migration is complete, two work items will have the following revision history
        ///    Source: A1,B ==> A2'
        ///    Target: A2   ==> B'
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("single Edit/Edit conflict in bi-directional work flow")]
        public void EditEditConflictTakeTargetTest()
        {
            int sourceId;
            int targetId;
            EditEditConflictScenario(out sourceId, out targetId);

            string srcTitle = SourceAdapter.GetFieldValue(sourceId, FIELD_TITLE);
            string srcDesc = SourceAdapter.GetFieldValue(sourceId, FIELD_DESCRIPTION);
            string tarTitle = TargetAdapter.GetFieldValue(targetId, FIELD_TITLE);
            string tarDesc = TargetAdapter.GetFieldValue(targetId, FIELD_DESCRIPTION);
            Assert.AreNotEqual(srcTitle, tarTitle, "Title should not match due to conflict");
            Assert.AreNotEqual(srcDesc, tarDesc, "Description should not match due to conflict");

            // verify we have edit/edit conflict
            List<RTConflict> conflicts = m_conflictResolver.GetConflicts();
            Assert.IsTrue(conflicts.Count >= 1, "There should be edit/edit conflict");
            WITEditEditConflictType contentConflict = new WITEditEditConflictType();
            Assert.IsTrue(contentConflict.ReferenceName.Equals(conflicts[0].ConflictTypeReference.Value.ReferenceName),
                "It should be WIT edit/edit conflict");

            // resolve the conflict by taking target side changes
            bool isResolved = false;
            if (TestEnvironment.MigrationTestType == MigrationTestType.TwoWayRight)
            {
                isResolved = m_conflictResolver.TryResolveConflict(conflicts[0], new WITEditEditConflictTakeSourceChangesAction(), "/" + targetId);
            }
            else
            {
                isResolved = m_conflictResolver.TryResolveConflict(conflicts[0], new WITEditEditConflictTakeTargetChangesAction(), "/" + sourceId);
            }
            Assert.IsTrue(isResolved, "Conflict resolution should succeed");

            // restart the migration tool
            RunAndNoValidate(true);

            srcDesc = SourceAdapter.GetFieldValue(sourceId, FIELD_DESCRIPTION);
            tarDesc = TargetAdapter.GetFieldValue(targetId, FIELD_DESCRIPTION);

            Assert.AreEqual(TARGET_R1_DESC, tarDesc);

            if (TestEnvironment.MigrationTestType == MigrationTestType.OneWay)
            {
                // VerifySyncResult does not apply to Oneway since we expect differences when we
                // resolve conflicts by taking Target changes

                // Source does not get updated on one-way migration
                Assert.AreEqual(SOURCE_R1_DESC, srcDesc);
            }
            else
            {
                // verify sync result 
                WitDiffResult result = GetDiffResult();

                // ignore Area/Iteration path mismatches due to test environments
                VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });

                Assert.AreEqual(TARGET_R1_DESC, srcDesc);
            }
        }

        ///<summary>
        /// resolve edit/edit conflict by ignoring conflicts it edits are on different fields
        /// 
        /// See EditEditConflictScenario function for setup steps 1-5
        /// 6. User resolves edit/edit conflict by ignoring conflicts it edits are on different fields
        /// 7. Conflict rosolution would fail because S:A1 and T:A1 change the same field (description)
        /// 8. Resolve it again by taking source side changes
        /// 9. Start Bi-directional migration (2nd round)
        ///    Pipeline flow S->T: Migrate S:A1 and S:B
        ///    Pipeline flow T->S: Skip T:A2 
        /// 10. Once migration is complete, two work items will have the following revision history
        ///    Source: A1,B ==> 
        ///    Target: A2   ==> A1',B'
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("single Edit/Edit conflict in bi-directional work flow")]
        public void EditEditConflictIgnoreByFieldChangeTest()
        {
            int sourceId;
            int targetId;
            EditEditConflictScenario(out sourceId, out targetId);

            string srcTitle = SourceAdapter.GetFieldValue(sourceId, FIELD_TITLE);
            string srcDesc = SourceAdapter.GetFieldValue(sourceId, FIELD_DESCRIPTION);
            string tarTitle = TargetAdapter.GetFieldValue(targetId, FIELD_TITLE);
            string tarDesc = TargetAdapter.GetFieldValue(targetId, FIELD_DESCRIPTION);
            Assert.AreNotEqual(srcTitle, tarTitle, "Title should not match due to conflict");
            Assert.AreNotEqual(srcDesc, tarDesc, "Description should not match due to conflict");

            // verify we have edit/edit conflict
            List<RTConflict> conflicts = m_conflictResolver.GetConflicts();
            Assert.IsTrue(conflicts.Count >= 1, "There should be edit/edit conflict");
            WITEditEditConflictType contentConflict = new WITEditEditConflictType();
            Assert.IsTrue(contentConflict.ReferenceName.Equals(conflicts[0].ConflictTypeReference.Value.ReferenceName),
                "It should be WIT edit/edit conflict");

            // resolve the conflict by ignoring conflicts it edits are on different fields
            bool isResolved = m_conflictResolver.TryResolveConflict(conflicts[0],
                new WITEditEditConflictIgnoreByFieldChangeAction(), "/" + sourceId);
            Assert.IsFalse(isResolved, "Conflict resolution should fail as both ends edited a same field");

            if (TestEnvironment.MigrationTestType == MigrationTestType.TwoWayRight)
            {
                isResolved = m_conflictResolver.TryResolveConflict(conflicts[0], new WITEditEditConflictTakeTargetChangesAction(), "/" + targetId);
            }
            else
            {
                isResolved = m_conflictResolver.TryResolveConflict(conflicts[0], new WITEditEditConflictTakeSourceChangesAction(), "/" + sourceId);
            }
            Assert.IsTrue(isResolved, "Conflict resolution should succeed");

            // restart the migration tool
            RunAndNoValidate(true);

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches due to test environments
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });

            srcDesc = SourceAdapter.GetFieldValue(sourceId, FIELD_DESCRIPTION);
            tarDesc = TargetAdapter.GetFieldValue(targetId, FIELD_DESCRIPTION);
            Assert.AreEqual(srcDesc, SOURCE_R1_DESC);
            Assert.AreEqual(tarDesc, SOURCE_R1_DESC);
        }

        [Ignore] // Manual test. Iteration path "source2\Test Path" should be created first to run this test.
        [TestMethod(), Priority(1), Owner(@"northamerica\teyang")]
        [Description("invalid field value conflict test")]
        public void InvalidFieldValueConflictBypassRuleEnabledTest()
        {
            // test invalid field value conflict bypass rule
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableCSSNodeCreationOnTarget);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_EnableBypassRulesOnTarget);

            // add a work item on source side
            int id = TfsSourceAdapter.AddWorkItem("Bug", "title", "description", @"source2\Test Path");

            // sync
            RunAndNoValidate();

            //// verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            //Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify we have edit/edit conflict
            //conflicts = conflictResolver.GetConflicts();
            Assert.IsTrue(conflicts.Count >= 1, "There should be invalid field value conflict");
            Assert.IsTrue(ConflictConstant.InvalidFieldValueConflictType.Equals(conflicts[0].ConflictTypeReference.Value.ReferenceName), "It should be invalid field value conflict");

            //// resolve the conflict and resume
            //conflictResolver.TryResolveConflict(conflicts[0], new WITEditEditConflictTakeSourceChangesAction(), "/" + id);

            //// restart the migration tool
            //RunAndNoValidate(true);

            //// verify sync result 
            //VerifySyncResult(true);
        }

        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("InvalidFieldConflictUseFieldMappTest")]
        public void InvalidFieldConflictUseFieldMapTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(SetInvalidFieldMap);

            // add a work item on source side
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title", "description1");

            WITChangeAction action1 = new WITChangeAction();

            action1.Priority = "3";
            SourceAdapter.UpdateWorkItem(workitemId, action1);

            // sync
            RunAndNoValidate();

            // verify we have InvalidField conflict
            m_conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = m_conflictResolver.GetConflicts();
            Assert.IsTrue(conflicts.Count >= 1, "There is no active conflict");
            Assert.IsTrue(ConflictConstant.InvalidFieldConflictType.Equals(conflicts[0].ConflictTypeReference.Value.ReferenceName),
                "The first active conflict is not a WIT invalid field conflict");

            Dictionary<string, string> dataFields = new Dictionary<string, string>();
            dataFields.Add("MapFrom", InvalidFieldRefName);
            dataFields.Add("MapTo", FIELD_DESCRIPTION);
            dataFields.Add(Constants.DATAKEY_UPDATED_CONFIGURATION_ID, "1");
            m_conflictResolver.TryResolveConflict(conflicts[0],
                ConflictConstant.InvalidFieldConflictUseFieldMapAction,
                conflicts[0].ScopeHint, dataFields);

            // restart the migration tool
            RunAndNoValidate(true);

            conflicts = m_conflictResolver.GetConflicts();
            Assert.IsTrue(conflicts.Count == 0, "There should be no conflict");

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches due to test environments
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });
        }

        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("InvalidFieldConflictUseFieldMapCaseInsensitiveTest")]
        public void InvalidFieldConflictUseFieldMapCaseInsensitiveTest()
        {
            // Same as InvalidFieldConflictUseFieldMappTest except testing
            // case-sensitivity of the resolution rule

            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(SetInvalidFieldMap);

            // add a work item on source side
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title", "description1");

            // sync
            RunAndNoValidate();

            // verify we have InvalidField conflict
            m_conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = m_conflictResolver.GetConflicts();
            Assert.IsTrue(conflicts.Count >= 1, "There is no active conflict");
            Assert.IsTrue(ConflictConstant.InvalidFieldConflictType.Equals(conflicts[0].ConflictTypeReference.Value.ReferenceName),
                "The first active conflict is not a WIT invalid field conflict");

            Dictionary<string, string> dataFields = new Dictionary<string, string>();
            dataFields.Add("MapFrom", InvalidFieldRefName.ToUpper()); // use upper case
            dataFields.Add("MapTo", FIELD_DESCRIPTION.ToLower());     // use lower case
            dataFields.Add(Constants.DATAKEY_UPDATED_CONFIGURATION_ID, "1");
            m_conflictResolver.TryResolveConflict(conflicts[0],
                ConflictConstant.InvalidFieldConflictUseFieldMapAction,
                conflicts[0].ScopeHint, dataFields);

            // restart the migration tool
            RunAndNoValidate(true);

            conflicts = m_conflictResolver.GetConflicts();
            Assert.IsTrue(conflicts.Count == 0, "There should be no conflict");

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches due to test environments
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH });
        }

        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("InvalidFieldValueConflict_MissingWildCardMap")]
        public void InvalidFieldValueConflict_MissingWildCardMapTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(SetFieldMap_NoWildCardMap);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableBypassRulesOnTarget);

            // add a work item on source side
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title", "description1");

            // sync
            RunAndNoValidate();

            // verify we have InvalidField conflict
            m_conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = m_conflictResolver.GetConflicts();
            Assert.IsTrue(conflicts.Count >= 1, "There is no active conflict");
            Assert.IsTrue(ConflictConstant.InvalidFieldValueConflictType.Equals(conflicts[0].ConflictTypeReference.Value.ReferenceName),
                "The first active conflict is not a WIT invalid field value conflict");

            // Users need to map * <-> * for the rest of fields to resolve this conflict
        }

        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("InvalidFieldValueConflict_ResolveByDroppingFieldTest")]
        public void InvalidFieldValueConflict_ResolveByDroppingFieldTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(SetInvalidFieldValueMap);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableBypassRulesOnTarget);

            // add a work item on source side
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title", "description1");

            WITChangeAction action1 = new WITChangeAction();
            action1.Priority = "1";
            SourceAdapter.UpdateWorkItem(workitemId, action1);

            // sync
            RunAndNoValidate();

            // verify we have InvalidField conflict
            m_conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = m_conflictResolver.GetConflicts();
            Assert.AreEqual(1, conflicts.Count, "There should be one active conflict");
            Assert.AreEqual(ConflictConstant.InvalidFieldValueConflictType, conflicts[0].ConflictTypeReference.Value.ReferenceName,
                "The first active conflict is not a WIT invalid field value conflict");

            Dictionary<string, string> dataFields = new Dictionary<string, string>();
            dataFields.Add("InvalidFieldReferenceName", FIELD_PRIORITY);
            dataFields.Add(Constants.DATAKEY_UPDATED_CONFIGURATION_ID, "1");

            var rslvRslt = m_conflictResolver.TryResolveConflict(
                conflicts[0], new Guid("3C8FE19D-3D02-4a19-BC5A-77640B0F5904"), "/", dataFields);
            Assert.IsTrue(rslvRslt, "Conflict resolution failed");

            // sync again
            RunAndNoValidate(true);

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches due to test environments
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH, "Priority" /*FIELD_PRIORITY teyang: verification uses field name rather than ref name*/ });
        }

        //public void CustomizeTargetFilterString(MigrationTestEnvironment env, Configuration config)
        //{
        // Replace target filter string as the default filter string would not 
        // work in edit/edit conflict test scenarios
        //FilterItem targetFilterItem = TestEnvironment.GetTargetFilterItem(config.SessionGroup.Sessions.Session[0].Filters.FilterPair[0]);
        //targetFilterItem.FilterString = string.Format("[System.ChangedDate] > '{0}'", TestStartTime);
        //}

        /// Single Edit/Edit conflict
        /// 
        /// 1. Source (S) and Target (T) are in sync
        /// 
        /// Both ends have changes
        /// 2. Source revisions: A1 -> B 
        /// 3. Target revisions: A2
        /// 
        /// 4. Start Bi-directional migration (1st round)
        /// 5. Tool generates 1 edit/edit conflict with chained conflicts
        ///    Raise edit/edit conflicts for S:A1 and T:A2
        ///    Raise chained conflicts for S:B
        ///    
        private void EditEditConflictScenario(out int sourceId, out int targetId)
        {
            // test edit/edit conflit in bi-directional work flow
            //TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(CustomizeTargetFilterString);

            // add a work item on source side
            sourceId = SourceAdapter.AddWorkItem("Bug", "title", "description1");

            // sync
            RunAndNoValidate();

            // verify there's no conflicts raised    
            m_conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = m_conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // update work items on both sides
            WITChangeAction sourceAction = new WITChangeAction();
            WITChangeAction targetAction = new WITChangeAction();
            targetId = QueryTargetWorkItemID(sourceId);

            sourceAction.Description = SOURCE_R1_DESC; // S:A1
            SourceAdapter.UpdateWorkItem(sourceId, sourceAction);
            sourceAction.Title = SOURCE_R2_TITLE; // S:B
            SourceAdapter.UpdateWorkItem(sourceId, sourceAction);

            targetAction.Description = TARGET_R1_DESC; // T:A2
            TargetAdapter.UpdateWorkItem(targetId, targetAction);

            // sync again 
            // 1 edit/edit conflict with chained conflicts
            RunAndNoValidate(true);
        }

        private void SetInvalidFieldMap(MigrationTestEnvironment env, Configuration config)
        {
            // Map Description to an invalide field
            MappedField mField1 = env.NewMappedField(FIELD_DESCRIPTION, InvalidFieldRefName);

            // Map the rest of the fields using wild card
            MappedField defaultField = env.NewMappedField("*", "*");

            FieldMap fieldMap = new FieldMap();
            fieldMap.name = "BugToBugFieldMap";
            fieldMap.MappedFields.MappedField.Add(defaultField);
            fieldMap.MappedFields.MappedField.Add(mField1);

            WorkItemTypeMappingElement typeMapping = new WorkItemTypeMappingElement();
            typeMapping.LeftWorkItemTypeName = "Bug";
            typeMapping.RightWorkItemTypeName = "Bug";
            typeMapping.fieldMap = fieldMap.name;

            WITSessionCustomSetting customSetting = new WITSessionCustomSetting();
            customSetting.WorkItemTypes.WorkItemType.Add(typeMapping);
            customSetting.FieldMaps.FieldMap.Add(fieldMap);

            SetWitSessionCustomSetting(config, customSetting);
        }

        private void SetInvalidFieldValueMap(MigrationTestEnvironment env, Configuration config)
        {
            ValueMap vMap = new ValueMap();
            vMap.name = "PriorityValues";
            var value = env.NewValue("*", "10000");
            vMap.Value.Add(value);

            MappedField mField1 = env.NewMappedField(FIELD_PRIORITY, FIELD_PRIORITY);
            mField1.valueMap = "PriorityValues";

            // Map the rest of the fields using wild card
            MappedField defaultField = env.NewMappedField("*", "*");

            FieldMap fieldMap = new FieldMap();
            fieldMap.name = "BugToBugFieldMap";
            fieldMap.MappedFields.MappedField.Add(defaultField);
            fieldMap.MappedFields.MappedField.Add(mField1);

            WorkItemTypeMappingElement typeMapping = new WorkItemTypeMappingElement();
            typeMapping.LeftWorkItemTypeName = "Bug";
            typeMapping.RightWorkItemTypeName = "Bug";
            typeMapping.fieldMap = fieldMap.name;

            WITSessionCustomSetting customSetting = new WITSessionCustomSetting();
            customSetting.WorkItemTypes.WorkItemType.Add(typeMapping);
            customSetting.FieldMaps.FieldMap.Add(fieldMap);
            customSetting.ValueMaps.ValueMap.Add(vMap);

            SetWitSessionCustomSetting(config, customSetting);
        }

        private void SetFieldMap_NoWildCardMap(MigrationTestEnvironment env, Configuration config)
        {
            // Map Description to an invalide field
            MappedField mField1 = env.NewMappedField(FIELD_DESCRIPTION, FIELD_DESCRIPTION);

            // The field map does not have a wild card mapping like below:
            // Map the rest of the fields using wild card
            //MappedField defaultField = env.NewMappedField("*","*");

            FieldMap fieldMap = new FieldMap();
            fieldMap.name = "BugToBugFieldMap";
            //fieldMap.MappedFields.MappedField.Add(defaultField);
            fieldMap.MappedFields.MappedField.Add(mField1);

            WorkItemTypeMappingElement typeMapping = new WorkItemTypeMappingElement();
            typeMapping.LeftWorkItemTypeName = "Bug";
            typeMapping.RightWorkItemTypeName = "Bug";
            typeMapping.fieldMap = fieldMap.name;

            WITSessionCustomSetting customSetting = new WITSessionCustomSetting();
            customSetting.WorkItemTypes.WorkItemType.Add(typeMapping);
            customSetting.FieldMaps.FieldMap.Add(fieldMap);

            SetWitSessionCustomSetting(config, customSetting);
        }

        /// <summary>
        /// Note: comment out [Ignore] attribute to enable debugging of this test method.
        /// 
        /// This is a manual test. To test, follow these steps:
        /// 1. Set a break point at the first line in MigrationEngine.Migration(...); and set a breakpoint at the second call
        ///    to RunAndNoValidate() in this test method
        /// 2. Identify an existing "Bug" on the target server and replace @@PLACE_HOLDER@@ with its Id
        /// 3. Start "Debugging" this test case and wait till the breakpoint is hit
        /// 4. Go to the migration DB, and delete the migration instruction for the first revision of the source work item
        ///    (that should be the first row of the migration instruction)
        /// 5. Continue 
        /// </summary>
        [Ignore] // Manual test. 
        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("HistoryNotFound_ResolveByUpdateConvHistory")]
        public void HistoryNotFound_ResolveByUpdateConvHistory()
        {
            this.TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_EnableBypassRulesOnTarget);
            this.TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);

            // add a work item on source side
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title", "description1");

            WITChangeAction action = new WITChangeAction();
            action.Priority = "3";
            SourceAdapter.UpdateWorkItem(workitemId, action);

            action = new WITChangeAction();
            action.Priority = "2";
            SourceAdapter.UpdateWorkItem(workitemId, action);

            // sync again and expect conflict
            RunAndNoValidate();

            // check conflicts
            m_conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = m_conflictResolver.GetConflicts();
            Assert.IsTrue(conflicts.Count >= 1, "There is no active conflict");
            Assert.IsTrue(ConflictConstant.WitHistoryNotFoundConflictType.Equals(conflicts[0].ConflictTypeReference.Value.ReferenceName),
                "The first active conflict is not a history-not-found conflict");

            Dictionary<string, string> dataFields = new Dictionary<string, string>();
            dataFields.Add("Source Item Id", workitemId.ToString());
            dataFields.Add("Source Revisions", "1");
            dataFields.Add("Target Item Id", "17526" /*"@@PLACE_HOLDER@@"*/);
            dataFields.Add("Target Revisions", "1");
            m_conflictResolver.TryResolveConflict(conflicts[0],
                ConflictConstant.HistoryNotFoundUpdateConversionHistoryAction,
                conflicts[0].ScopeHint, dataFields);


            RunAndNoValidate(true);
            m_conflictResolver = new ConflictResolver(Configuration);
            conflicts = m_conflictResolver.GetConflicts();
            Assert.IsTrue(conflicts.Count == 0, "There is unresolved conflict");
        }
    }
}
