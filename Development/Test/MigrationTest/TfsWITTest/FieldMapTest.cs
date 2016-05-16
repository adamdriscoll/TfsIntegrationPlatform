// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.BusinessModel.WIT;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit.ServerDiff;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTestLibrary;
using MigrationTestLibrary.Conflict;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace TfsWitTest
{
    /// <summary>
    /// WIT FiledMap tests
    /// </summary>
    [TestClass]
    public class WITFieldMapTest : TfsWITTestCaseBase
    {
        const string AggregationFormat = "AggregatedFields:{0}:{1}";
        const string ConditionalValueMapDescription = "Conditional value map desc";

        /// <summary>
        /// Tests that when Reason and State are not mapped, WIT submission still passes with the data integrity check
        /// </summary>
        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("Basic FieldMap test")]
        public void Mapping_MissingStateReasonFieldMapTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_EnableBypassRulesOnTarget);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(SetMissingStateReasonFieldMap);

            // add a work item on source side
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title", "description1");

            RunAndNoValidate();

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify sync result excluding expected mismatches
            WitDiffResult result = GetDiffResult();
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH,
                FIELD_TITLE, FIELD_DESCRIPTION});
        }

        public void SetMissingStateReasonFieldMap(MigrationTestEnvironment env, Configuration config)
        {
            FieldMap fieldMap = new FieldMap();
            fieldMap.name = "BugToBugFieldMap";

            MappedField mField1 = env.NewMappedField(CoreFieldReferenceNames.State, CoreFieldReferenceNames.Title);

            MappedField mField2 = env.NewMappedField(CoreFieldReferenceNames.Reason, CoreFieldReferenceNames.Description);

            fieldMap.MappedFields.MappedField.Add(mField1);
            fieldMap.MappedFields.MappedField.Add(mField2);

            WorkItemTypeMappingElement typeMapping = new WorkItemTypeMappingElement();
            typeMapping.LeftWorkItemTypeName = "Bug";
            typeMapping.RightWorkItemTypeName = "Bug";
            typeMapping.fieldMap = fieldMap.name;

            WITSessionCustomSetting customSetting = new WITSessionCustomSetting();
            customSetting.WorkItemTypes.WorkItemType.Add(typeMapping);
            customSetting.FieldMaps.FieldMap.Add(fieldMap);

            SetWitSessionCustomSetting(config, customSetting);
        }


        ///<summary>
        /// Test FieldMap
        /// Map System.Title to System.Description
        /// Map System.Description to System.Title
        /// 
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Basic FieldMap test")]
        public void Mapping_FieldMapTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(SetFieldMap);

            // add a work item on source side
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title", "description1");

            RunAndNoValidate();

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify sync result excluding expected mismatches
            WitDiffResult result = GetDiffResult();
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH,
                FIELD_TITLE, FIELD_DESCRIPTION});

            // Title <-> Description
            int targetId = QueryTargetWorkItemID(workitemId);
            string srcTitle = SourceAdapter.GetFieldValue(workitemId, FIELD_TITLE);
            string srcDesc = SourceAdapter.GetFieldValue(workitemId, FIELD_DESCRIPTION);
            string tarTitle = TargetAdapter.GetFieldValue(targetId, FIELD_TITLE);
            string tarDesc = TargetAdapter.GetFieldValue(targetId, FIELD_DESCRIPTION);
            Assert.AreEqual(srcTitle, tarDesc);
            Assert.AreEqual(srcDesc, tarTitle);
        }

        ///<summary>
        /// Test FieldMap SourceField map to "" (i.e. dropping source field)
        /// Map System.Title to System.Title
        /// Map System.Description to ""
        /// 
        ///</summary>
        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("Basic FieldMap test")]
        public void Mapping_ExcludeFieldMapTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(SetExcludePriorityFieldMap);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_EnableBypassRulesOnTarget);

            // add a work item on source side
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title", "description1");

            RunAndNoValidate();

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify sync result excluding expected mismatches
            WitDiffResult result = GetDiffResult();
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH,
                FIELD_TITLE, FIELD_DESCRIPTION});

            int targetId = QueryTargetWorkItemID(workitemId);
            string srcTitle = SourceAdapter.GetFieldValue(workitemId, FIELD_TITLE);
            string srcDesc = SourceAdapter.GetFieldValue(workitemId, FIELD_DESCRIPTION);
            string tarTitle = TargetAdapter.GetFieldValue(targetId, FIELD_TITLE);
            string tarDesc = TargetAdapter.GetFieldValue(targetId, FIELD_DESCRIPTION);
            Assert.AreEqual(tarTitle, srcTitle);
            Assert.AreEqual(string.Empty, tarDesc);
        }

        ///<summary>
        /// Test ValueMap
        /// Map P2 <=> P1
        /// 
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Basic ValueMap test")]
        public void Mapping_ValueMapTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(SetPriorityValueMap);

            // add a work item on source side
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title", "description1");

            WITChangeAction action1 = new WITChangeAction();
            action1.Priority = "2";
            SourceAdapter.UpdateWorkItem(workitemId, action1);

            RunAndNoValidate();

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify sync result excluding expected mismatches
            WitDiffResult result = GetDiffResult();
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH,
                FIELD_PRIORITY});

            // P1 == P2
            int targetId = QueryTargetWorkItemID(workitemId);
            string sourcePriority = SourceAdapter.GetFieldValue(workitemId, FIELD_PRIORITY);
            string targetPriority = TargetAdapter.GetFieldValue(targetId, FIELD_PRIORITY);
            Assert.AreEqual(sourcePriority, "2");
            Assert.AreEqual(targetPriority, "1");
        }

        ///<summary>
        /// Test Conditional ValueMap
        /// Map P2 <=> P3 when Description == ConditionalValueMapDescription
        /// Map P2 <=> P1 in other cases
        /// 
        ///</summary>
        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("Conditional ValueMap with explicit source value test")]
        public void Mapping_ConditionalExplicitValueMapTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(SetConditionalExplicitPriorityValueMap);

            // add a work item on source side
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title", ConditionalValueMapDescription);

            WITChangeAction action1 = new WITChangeAction();
            action1.Priority = "2";
            SourceAdapter.UpdateWorkItem(workitemId, action1);

            RunAndNoValidate();

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify sync result excluding expected mismatches
            WitDiffResult result = GetDiffResult();
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH,
                FIELD_PRIORITY});

            // P1 == P2
            int targetId = QueryTargetWorkItemID(workitemId);
            string sourcePriority = SourceAdapter.GetFieldValue(workitemId, FIELD_PRIORITY);
            string targetPriority = TargetAdapter.GetFieldValue(targetId, FIELD_PRIORITY);
            Assert.AreEqual(sourcePriority, "2");
            Assert.AreEqual(targetPriority, "3");
        }

        ///<summary>
        /// Test Conditional ValueMap
        /// Map P2 <=> P3 when Description == ConditionalValueMapDescription
        /// Map P2 <=> P1 in other cases
        /// 
        ///</summary>
        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("Conditional ValueMap with explicit source value test")]
        public void Mapping_ConditionalExplicitValueMapWildCardinTargetValueTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(SetConditionalExplicitPriorityValueWithWildCardinTargetValueMap);

            // add a work item on source side
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title", ConditionalValueMapDescription);

            WITChangeAction action1 = new WITChangeAction();
            action1.Priority = "2";
            SourceAdapter.UpdateWorkItem(workitemId, action1);

            RunAndNoValidate();

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify sync result excluding expected mismatches
            WitDiffResult result = GetDiffResult();
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH,
                FIELD_PRIORITY});

            // P1 == P2
            int targetId = QueryTargetWorkItemID(workitemId);
            string sourcePriority = SourceAdapter.GetFieldValue(workitemId, FIELD_PRIORITY);
            string targetPriority = TargetAdapter.GetFieldValue(targetId, FIELD_PRIORITY);
            Assert.AreEqual(sourcePriority, "2");
            Assert.AreEqual(targetPriority, "2");
        }

        ///<summary>
        /// Test Conditional ValueMap
        /// Map * <=> P3 when Description == ConditionalValueMapDescription
        /// Map P2 <=> P1 in other cases
        /// 
        ///</summary>
        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("Conditional ValueMap with wildcard source value test")]
        public void Mapping_ConditionalWildcardValueMapTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(SetConditionalWildcardPriorityValueMap);

            // add a work item on source side
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title", ConditionalValueMapDescription);

            WITChangeAction action1 = new WITChangeAction();
            action1.Priority = "2";
            SourceAdapter.UpdateWorkItem(workitemId, action1);

            RunAndNoValidate();

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify sync result excluding expected mismatches
            WitDiffResult result = GetDiffResult();
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH,
                FIELD_PRIORITY});

            // P1 == P2
            int targetId = QueryTargetWorkItemID(workitemId);
            string sourcePriority = SourceAdapter.GetFieldValue(workitemId, FIELD_PRIORITY);
            string targetPriority = TargetAdapter.GetFieldValue(targetId, FIELD_PRIORITY);
            Assert.AreEqual(sourcePriority, "2");
            Assert.AreEqual(targetPriority, "3");
        }

        ///<summary>
        /// Test Conditional ValueMap
        /// Map P2 <=> P3 when Description == ConditionalValueMapDescription
        /// Map * <=> P1 when Description == ConditionalValueMapDescription
        /// Map P2 <=> P1 in other cases
        /// 
        ///</summary>
        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("Conditional ValueMap with wildcard source value test")]
        public void Mapping_ConditionalHybridValueMapTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(SetConditionalHybridPriorityValueMap);

            // add a work item on source side
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title", ConditionalValueMapDescription);

            WITChangeAction action1 = new WITChangeAction();
            action1.Priority = "2";
            SourceAdapter.UpdateWorkItem(workitemId, action1);

            RunAndNoValidate();

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify sync result excluding expected mismatches
            WitDiffResult result = GetDiffResult();
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH,
                FIELD_PRIORITY});

            // P1 == P2
            int targetId = QueryTargetWorkItemID(workitemId);
            string sourcePriority = SourceAdapter.GetFieldValue(workitemId, FIELD_PRIORITY);
            string targetPriority = TargetAdapter.GetFieldValue(targetId, FIELD_PRIORITY);
            Assert.AreEqual(sourcePriority, "2");
            Assert.AreEqual(targetPriority, "3");
        }

        ///<summary>
        /// Test ValueMap
        /// Aggregate Title and Description to Description on target
        /// 
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("AggregatedFieldTest")]
        public void Mapping_AggregatedFieldTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(SetAggregatedFieldMap);

            // add a work item on source side
            int workitemId = SourceAdapter.AddWorkItem("Bug", "title", "description1");

            RunAndNoValidate();

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // Title <-> Description
            int targetId = QueryTargetWorkItemID(workitemId);
            string srcTitle = SourceAdapter.GetFieldValue(workitemId, FIELD_TITLE);
            string srcRev = SourceAdapter.GetFieldValue(workitemId, "System.Rev");
            string tarDesc = TargetAdapter.GetFieldValue(targetId, FIELD_DESCRIPTION);
            Assert.AreEqual(tarDesc, string.Format(AggregationFormat, srcTitle, srcRev));
        }

        public void SetFieldMap(MigrationTestEnvironment env, Configuration config)
        {
            FieldMap fieldMap = new FieldMap();
            fieldMap.name = "BugToBugFieldMap";

            MappedField mField1 = env.NewMappedField(FIELD_TITLE, FIELD_DESCRIPTION);
            mField1.MapFromSide = env.GetSourceSideTypeEnum();

            MappedField mField2 = env.NewMappedField(FIELD_DESCRIPTION, FIELD_TITLE);
            mField2.MapFromSide = env.GetSourceSideTypeEnum();

            fieldMap.MappedFields.MappedField.Add(mField1);
            fieldMap.MappedFields.MappedField.Add(mField2);

            WorkItemTypeMappingElement typeMapping = new WorkItemTypeMappingElement();
            typeMapping.LeftWorkItemTypeName = "Bug";
            typeMapping.RightWorkItemTypeName = "Bug";
            typeMapping.fieldMap = fieldMap.name;

            WITSessionCustomSetting customSetting = new WITSessionCustomSetting();
            customSetting.WorkItemTypes.WorkItemType.Add(typeMapping);
            customSetting.FieldMaps.FieldMap.Add(fieldMap);

            SetWitSessionCustomSetting(config, customSetting);
        }

        public void SetExcludePriorityFieldMap(MigrationTestEnvironment env, Configuration config)
        {
            FieldMap fieldMap = new FieldMap();
            fieldMap.name = "BugToBugFieldMap";

            MappedField mField1 = env.NewMappedField(FIELD_TITLE, FIELD_TITLE);
            mField1.MapFromSide = env.GetSourceSideTypeEnum();

            MappedField mField2 = env.NewMappedField(FIELD_DESCRIPTION, String.Empty);
            mField2.MapFromSide = env.GetSourceSideTypeEnum();

            fieldMap.MappedFields.MappedField.Add(mField1);
            fieldMap.MappedFields.MappedField.Add(mField2);

            WorkItemTypeMappingElement typeMapping = new WorkItemTypeMappingElement();
            typeMapping.LeftWorkItemTypeName = "Bug";
            typeMapping.RightWorkItemTypeName = "Bug";
            typeMapping.fieldMap = fieldMap.name;

            WITSessionCustomSetting customSetting = new WITSessionCustomSetting();
            customSetting.WorkItemTypes.WorkItemType.Add(typeMapping);
            customSetting.FieldMaps.FieldMap.Add(fieldMap);

            SetWitSessionCustomSetting(config, customSetting);
        }

        public void SetPriorityValueMap(MigrationTestEnvironment env, Configuration config)
        {
            // Map P2 to P1
            Value v1 = env.NewValue("2", "1");

            ValueMap vMap = new ValueMap();
            vMap.name = "PriorityValueMap";
            vMap.Value.Add(v1);

            // Map field
            MappedField mField1 = env.NewMappedField(FIELD_PRIORITY, FIELD_PRIORITY);
            mField1.valueMap = vMap.name;

            // Map the rest of the fields using wild card
            MappedField defaultField = env.NewMappedField("*", "*");

            FieldMap fieldMap = new FieldMap();
            fieldMap.name = "BugToBugFieldMap";
            fieldMap.MappedFields.MappedField.Add(defaultField);
            fieldMap.MappedFields.MappedField.Add(mField1);

            // Map work item type
            WorkItemTypeMappingElement typeMapping = new WorkItemTypeMappingElement();
            typeMapping.LeftWorkItemTypeName = "Bug";
            typeMapping.RightWorkItemTypeName = "Bug";
            typeMapping.fieldMap = fieldMap.name;

            // Build WIT Session custom setting
            WITSessionCustomSetting customSetting = new WITSessionCustomSetting();
            customSetting.WorkItemTypes.WorkItemType.Add(typeMapping);
            customSetting.FieldMaps.FieldMap.Add(fieldMap);
            customSetting.ValueMaps.ValueMap.Add(vMap);

            SetWitSessionCustomSetting(config, customSetting);
        }

        public void SetConditionalExplicitPriorityValueMap(MigrationTestEnvironment env, Configuration config)
        {
            // Map P2 to P1
            Value v1 = env.NewValue("2", "1");

            Value v2 = env.NewValue("2", "3");
            v2.When.ConditionalSrcFieldName = "System.Description";
            v2.When.ConditionalSrcFieldValue = ConditionalValueMapDescription;

            ValueMap vMap = new ValueMap();
            vMap.name = "PriorityValueMap";
            vMap.Value.Add(v1);
            vMap.Value.Add(v2);

            // Map field
            MappedField mField1 = env.NewMappedField(FIELD_PRIORITY, FIELD_PRIORITY);
            mField1.valueMap = vMap.name;

            // Map the rest of the fields using wild card
            MappedField defaultField = env.NewMappedField("*", "*");

            FieldMap fieldMap = new FieldMap();
            fieldMap.name = "BugToBugFieldMap";
            fieldMap.MappedFields.MappedField.Add(defaultField);
            fieldMap.MappedFields.MappedField.Add(mField1);

            // Map work item type
            WorkItemTypeMappingElement typeMapping = new WorkItemTypeMappingElement();
            typeMapping.LeftWorkItemTypeName = "Bug";
            typeMapping.RightWorkItemTypeName = "Bug";
            typeMapping.fieldMap = fieldMap.name;

            // Build WIT Session custom setting
            WITSessionCustomSetting customSetting = new WITSessionCustomSetting();
            customSetting.WorkItemTypes.WorkItemType.Add(typeMapping);
            customSetting.FieldMaps.FieldMap.Add(fieldMap);
            customSetting.ValueMaps.ValueMap.Add(vMap);

            SetWitSessionCustomSetting(config, customSetting);
        }

        public void SetConditionalExplicitPriorityValueWithWildCardinTargetValueMap(MigrationTestEnvironment env, Configuration config)
        {
            // Map P2 to P1
            Value v1 = env.NewValue("2", "1");

            Value v2 = env.NewValue("2", "3");
            v2.When.ConditionalSrcFieldName = "System.Description";
            v2.When.ConditionalSrcFieldValue = ConditionalValueMapDescription + "other";

            Value v3 = env.NewValue("2", "*");
            v3.When.ConditionalSrcFieldName = "System.Description";
            v3.When.ConditionalSrcFieldValue = ConditionalValueMapDescription;

            ValueMap vMap = new ValueMap();
            vMap.name = "PriorityValueMap";
            vMap.Value.Add(v1);
            vMap.Value.Add(v2);
            vMap.Value.Add(v3);

            // Map field
            MappedField mField1 = env.NewMappedField(FIELD_PRIORITY, FIELD_PRIORITY);
            mField1.valueMap = vMap.name;

            // Map the rest of the fields using wild card
            MappedField defaultField = env.NewMappedField("*", "*");

            FieldMap fieldMap = new FieldMap();
            fieldMap.name = "BugToBugFieldMap";
            fieldMap.MappedFields.MappedField.Add(defaultField);
            fieldMap.MappedFields.MappedField.Add(mField1);

            // Map work item type
            WorkItemTypeMappingElement typeMapping = new WorkItemTypeMappingElement();
            typeMapping.LeftWorkItemTypeName = "Bug";
            typeMapping.RightWorkItemTypeName = "Bug";
            typeMapping.fieldMap = fieldMap.name;

            // Build WIT Session custom setting
            WITSessionCustomSetting customSetting = new WITSessionCustomSetting();
            customSetting.WorkItemTypes.WorkItemType.Add(typeMapping);
            customSetting.FieldMaps.FieldMap.Add(fieldMap);
            customSetting.ValueMaps.ValueMap.Add(vMap);

            SetWitSessionCustomSetting(config, customSetting);
        }

        /// <summary>
        /// P1->P2, *->P3
        /// </summary>
        /// <param name="env"></param>
        /// <param name="config"></param>

        public void SetConditionalWildcardPriorityValueMap(MigrationTestEnvironment env, Configuration config)
        {
            // Map P2 to P1
            Value v1 = env.NewValue("1", "2");

            Value v2 = env.NewValue("*", "3");
            v2.When.ConditionalSrcFieldName = "System.Description";
            v2.When.ConditionalSrcFieldValue = ConditionalValueMapDescription;

            ValueMap vMap = new ValueMap();
            vMap.name = "PriorityValueMap";
            vMap.Value.Add(v1);
            vMap.Value.Add(v2);

            // Map field
            MappedField mField1 = env.NewMappedField(FIELD_PRIORITY, FIELD_PRIORITY);
            mField1.valueMap = vMap.name;

            // Map the rest of the fields using wild card
            MappedField defaultField = env.NewMappedField("*", "*");

            FieldMap fieldMap = new FieldMap();
            fieldMap.name = "BugToBugFieldMap";
            fieldMap.MappedFields.MappedField.Add(defaultField);
            fieldMap.MappedFields.MappedField.Add(mField1);

            // Map work item type
            WorkItemTypeMappingElement typeMapping = new WorkItemTypeMappingElement();
            typeMapping.LeftWorkItemTypeName = "Bug";
            typeMapping.RightWorkItemTypeName = "Bug";
            typeMapping.fieldMap = fieldMap.name;

            // Build WIT Session custom setting
            WITSessionCustomSetting customSetting = new WITSessionCustomSetting();
            customSetting.WorkItemTypes.WorkItemType.Add(typeMapping);
            customSetting.FieldMaps.FieldMap.Add(fieldMap);
            customSetting.ValueMaps.ValueMap.Add(vMap);

            SetWitSessionCustomSetting(config, customSetting);
        }

        public void SetConditionalHybridPriorityValueMap(MigrationTestEnvironment env, Configuration config)
        {
            // Map P2 to P1
            Value v1 = env.NewValue("2", "1");

            Value v2 = env.NewValue("*", "1");
            v2.When.ConditionalSrcFieldName = "System.Description";
            v2.When.ConditionalSrcFieldValue = ConditionalValueMapDescription;

            Value v3 = env.NewValue("2", "3");
            v3.When.ConditionalSrcFieldName = "System.Description";
            v3.When.ConditionalSrcFieldValue = ConditionalValueMapDescription;

            ValueMap vMap = new ValueMap();
            vMap.name = "PriorityValueMap";
            vMap.Value.Add(v1);
            vMap.Value.Add(v2);
            vMap.Value.Add(v3);

            // Map field
            MappedField mField1 = env.NewMappedField(FIELD_PRIORITY, FIELD_PRIORITY);
            mField1.valueMap = vMap.name;

            // Map the rest of the fields using wild card
            MappedField defaultField = env.NewMappedField("*", "*");

            FieldMap fieldMap = new FieldMap();
            fieldMap.name = "BugToBugFieldMap";
            fieldMap.MappedFields.MappedField.Add(defaultField);
            fieldMap.MappedFields.MappedField.Add(mField1);

            // Map work item type
            WorkItemTypeMappingElement typeMapping = new WorkItemTypeMappingElement();
            typeMapping.LeftWorkItemTypeName = "Bug";
            typeMapping.RightWorkItemTypeName = "Bug";
            typeMapping.fieldMap = fieldMap.name;

            // Build WIT Session custom setting
            WITSessionCustomSetting customSetting = new WITSessionCustomSetting();
            customSetting.WorkItemTypes.WorkItemType.Add(typeMapping);
            customSetting.FieldMaps.FieldMap.Add(fieldMap);
            customSetting.ValueMaps.ValueMap.Add(vMap);

            SetWitSessionCustomSetting(config, customSetting);
        }

        public void SetAggregatedFieldMap(MigrationTestEnvironment env, Configuration config)
        {
            // Aggregated fields
            AggregatedFields aggregatedFields = new AggregatedFields();
            FieldsAggregationGroup group = new FieldsAggregationGroup();
            group.MapFromSide = env.GetSourceSideTypeEnum();
            group.TargetFieldName = FIELD_DESCRIPTION;
            group.Format = AggregationFormat;

            SourceField f0 = new SourceField();
            f0.Index = 0;
            f0.SourceFieldName = FIELD_TITLE;

            SourceField f1 = new SourceField();
            f1.Index = 1;
            f1.SourceFieldName = "System.Rev";

            group.SourceField.Add(f0);
            group.SourceField.Add(f1);

            aggregatedFields.FieldsAggregationGroup.Add(group);

            // construct FieldMap
            // Map all fields explictly using wildcard
            MappedField defaultField = env.NewMappedField("*", "*");

            FieldMap fieldMap = new FieldMap();
            fieldMap.name = "BugToBugFieldMap";
            fieldMap.MappedFields.MappedField.Add(defaultField);

            // TODO: Create another test case for aggreated fields feature
            // Construct configurations with conflicting field maps
            //MappedField mField1 = env.NewMappedField(FIELD_TITLE,FIELD_DESCRIPTION);

            //MappedField mField2 = env.NewMappedField(FIELD_DESCRIPTION,FIELD_TITLE);

            //fieldMap.MappedFields.MappedField.Add(mField1);
            //fieldMap.MappedFields.MappedField.Add(mField2);

            fieldMap.AggregatedFields = aggregatedFields;

            WorkItemTypeMappingElement typeMapping = new WorkItemTypeMappingElement();
            typeMapping.LeftWorkItemTypeName = "Bug";
            typeMapping.RightWorkItemTypeName = "Bug";
            typeMapping.fieldMap = fieldMap.name;

            WITSessionCustomSetting customSetting = new WITSessionCustomSetting();
            customSetting.WorkItemTypes.WorkItemType.Add(typeMapping);
            customSetting.FieldMaps.FieldMap.Add(fieldMap);

            SetWitSessionCustomSetting(config, customSetting);
        }
    }
}
