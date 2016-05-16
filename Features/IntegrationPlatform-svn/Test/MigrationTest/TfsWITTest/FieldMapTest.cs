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
            string title = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));

            int workitemId = SourceAdapter.AddWorkItem("Bug", title, "description1");

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

        public void SetMissingStateReasonFieldMap(Configuration config)
        {
            FieldMap fieldMap = new FieldMap();
            fieldMap.name = "BugToBugFieldMap";

            MappedField mField1 = new MappedField();
            mField1.LeftName = CoreFieldReferenceNames.State;
            mField1.RightName = CoreFieldReferenceNames.Title;
            mField1.MapFromSide = SourceSideTypeEnum.Left;

            MappedField mField2 = new MappedField();
            mField2.LeftName = CoreFieldReferenceNames.Reason;
            mField2.RightName = CoreFieldReferenceNames.Description;
            mField2.MapFromSide = SourceSideTypeEnum.Left;

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
            string title = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));

            int workitemId = SourceAdapter.AddWorkItem("Bug", title, "description1");

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
            int mirroredId = QueryMirroredWorkItemID(workitemId);
            string srcTitle = SourceAdapter.GetFieldValue(workitemId, FIELD_TITLE);
            string srcDesc  = SourceAdapter.GetFieldValue(workitemId, FIELD_DESCRIPTION);
            string tarTitle = TargetAdapter.GetFieldValue(mirroredId, FIELD_TITLE);
            string tarDesc  = TargetAdapter.GetFieldValue(mirroredId, FIELD_DESCRIPTION);
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
            string title = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));

            int workitemId = SourceAdapter.AddWorkItem("Bug", title, "description1");

            RunAndNoValidate();

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify sync result excluding expected mismatches
            WitDiffResult result = GetDiffResult();
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH,
                FIELD_TITLE, FIELD_DESCRIPTION});

            int mirroredId = QueryMirroredWorkItemID(workitemId);
            string srcTitle = SourceAdapter.GetFieldValue(workitemId, FIELD_TITLE);
            string srcDesc = SourceAdapter.GetFieldValue(workitemId, FIELD_DESCRIPTION);
            string tarTitle = TargetAdapter.GetFieldValue(mirroredId, FIELD_TITLE);
            string tarDesc = TargetAdapter.GetFieldValue(mirroredId, FIELD_DESCRIPTION);
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
            string title = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));

            int workitemId = SourceAdapter.AddWorkItem("Bug", title, "description1");

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
            int mirroredId = QueryMirroredWorkItemID(workitemId);
            string srcPri = SourceAdapter.GetFieldValue(workitemId, FIELD_PRIORITY);
            string tarPri = TargetAdapter.GetFieldValue(mirroredId, FIELD_PRIORITY);
            Assert.AreEqual(srcPri, "2");
            Assert.AreEqual(tarPri, "1");
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
            string title = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));

            int workitemId = SourceAdapter.AddWorkItem("Bug", title, ConditionalValueMapDescription);

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
            int mirroredId = QueryMirroredWorkItemID(workitemId);
            string srcPri = SourceAdapter.GetFieldValue(workitemId, FIELD_PRIORITY);
            string tarPri = TargetAdapter.GetFieldValue(mirroredId, FIELD_PRIORITY);
            Assert.AreEqual(srcPri, "2");
            Assert.AreEqual(tarPri, "3");
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
            string title = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));

            int workitemId = SourceAdapter.AddWorkItem("Bug", title, ConditionalValueMapDescription);

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
            int mirroredId = QueryMirroredWorkItemID(workitemId);
            string srcPri = SourceAdapter.GetFieldValue(workitemId, FIELD_PRIORITY);
            string tarPri = TargetAdapter.GetFieldValue(mirroredId, FIELD_PRIORITY);
            Assert.AreEqual(srcPri, "2");
            Assert.AreEqual(tarPri, "2");
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
            string title = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));

            int workitemId = SourceAdapter.AddWorkItem("Bug", title, ConditionalValueMapDescription);

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
            int mirroredId = QueryMirroredWorkItemID(workitemId);
            string srcPri = SourceAdapter.GetFieldValue(workitemId, FIELD_PRIORITY);
            string tarPri = TargetAdapter.GetFieldValue(mirroredId, FIELD_PRIORITY);
            Assert.AreEqual(srcPri, "2");
            Assert.AreEqual(tarPri, "3");
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
            string title = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));

            int workitemId = SourceAdapter.AddWorkItem("Bug", title, ConditionalValueMapDescription);

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
            int mirroredId = QueryMirroredWorkItemID(workitemId);
            string srcPri = SourceAdapter.GetFieldValue(workitemId, FIELD_PRIORITY);
            string tarPri = TargetAdapter.GetFieldValue(mirroredId, FIELD_PRIORITY);
            Assert.AreEqual(srcPri, "2");
            Assert.AreEqual(tarPri, "3");
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
            string title = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));

            int workitemId = SourceAdapter.AddWorkItem("Bug", title, "description1");

            RunAndNoValidate();

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // Title <-> Description
            int mirroredId = QueryMirroredWorkItemID(workitemId);
            string srcTitle = SourceAdapter.GetFieldValue(workitemId, FIELD_TITLE);
            string srcRev = SourceAdapter.GetFieldValue(workitemId, "System.Rev");
            string tarDesc  = TargetAdapter.GetFieldValue(mirroredId, FIELD_DESCRIPTION);
            Assert.AreEqual(tarDesc, string.Format(AggregationFormat, srcTitle, srcRev));
        }

        public void SetFieldMap(Configuration config)
        {
            FieldMap fieldMap = new FieldMap();
            fieldMap.name = "BugToBugFieldMap";

            MappedField mField1 = new MappedField();
            mField1.LeftName = FIELD_TITLE;
            mField1.RightName = FIELD_DESCRIPTION;
            mField1.MapFromSide = SourceSideTypeEnum.Left;

            MappedField mField2 = new MappedField();
            mField2.LeftName = FIELD_DESCRIPTION;
            mField2.RightName = FIELD_TITLE;
            mField2.MapFromSide = SourceSideTypeEnum.Left;

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

        public void SetExcludePriorityFieldMap(Configuration config)
        {
            FieldMap fieldMap = new FieldMap();
            fieldMap.name = "BugToBugFieldMap";

            MappedField mField1 = new MappedField();
            mField1.LeftName = FIELD_TITLE;
            mField1.RightName = FIELD_TITLE;
            mField1.MapFromSide = SourceSideTypeEnum.Left;

            MappedField mField2 = new MappedField();
            mField2.LeftName = FIELD_DESCRIPTION;
            mField2.RightName = string.Empty;
            mField2.MapFromSide = SourceSideTypeEnum.Left;

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

        public void SetPriorityValueMap(Configuration config)
        {
            // Map P2 to P1
            Value v1 = new Value();
            v1.LeftValue = "2";
            v1.RightValue = "1";
            ValueMap vMap = new ValueMap();
            vMap.name = "PriorityValueMap";
            vMap.Value.Add(v1);

            // Map field
            MappedField mField1 = new MappedField();
            mField1.LeftName = FIELD_PRIORITY;
            mField1.RightName = FIELD_PRIORITY;
            mField1.MapFromSide = SourceSideTypeEnum.Left;
            mField1.valueMap = vMap.name;

            // Map the rest of the fields using wild card
            MappedField defaultField = new MappedField();
            defaultField.LeftName = "*";
            defaultField.RightName = "*";
            defaultField.MapFromSide = SourceSideTypeEnum.Left;
            
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

        public void SetConditionalExplicitPriorityValueMap(Configuration config)
        {
            // Map P2 to P1
            Value v1 = new Value();
            v1.LeftValue = "2";
            v1.RightValue = "1";
            ValueMap vMap = new ValueMap();
            vMap.name = "PriorityValueMap";
            vMap.Value.Add(v1);

            Value v2 = new Value();
            v2.LeftValue = "2";
            v2.RightValue = "3";
            v2.When.ConditionalSrcFieldName = "System.Description";
            v2.When.ConditionalSrcFieldValue = ConditionalValueMapDescription;
            vMap.Value.Add(v2);

            // Map field
            MappedField mField1 = new MappedField();
            mField1.LeftName = FIELD_PRIORITY;
            mField1.RightName = FIELD_PRIORITY;
            mField1.MapFromSide = SourceSideTypeEnum.Left;
            mField1.valueMap = vMap.name;

            // Map the rest of the fields using wild card
            MappedField defaultField = new MappedField();
            defaultField.LeftName = "*";
            defaultField.RightName = "*";
            defaultField.MapFromSide = SourceSideTypeEnum.Left;

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

        public void SetConditionalExplicitPriorityValueWithWildCardinTargetValueMap(Configuration config)
        {
            // Map P2 to P1
            Value v1 = new Value();
            v1.LeftValue = "2";
            v1.RightValue = "1";
            ValueMap vMap = new ValueMap();
            vMap.name = "PriorityValueMap";
            vMap.Value.Add(v1);

            Value v2 = new Value();
            v2.LeftValue = "2";
            v2.RightValue = "3";
            v2.When.ConditionalSrcFieldName = "System.Description";
            v2.When.ConditionalSrcFieldValue = ConditionalValueMapDescription + "other";
            vMap.Value.Add(v2);

            Value v3 = new Value();
            v3.LeftValue = "2";
            v3.RightValue = "*";
            v3.When.ConditionalSrcFieldName = "System.Description";
            v3.When.ConditionalSrcFieldValue = ConditionalValueMapDescription;
            vMap.Value.Add(v3);

            // Map field
            MappedField mField1 = new MappedField();
            mField1.LeftName = FIELD_PRIORITY;
            mField1.RightName = FIELD_PRIORITY;
            mField1.MapFromSide = SourceSideTypeEnum.Left;
            mField1.valueMap = vMap.name;

            // Map the rest of the fields using wild card
            MappedField defaultField = new MappedField();
            defaultField.LeftName = "*";
            defaultField.RightName = "*";
            defaultField.MapFromSide = SourceSideTypeEnum.Left;

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

        public void SetConditionalWildcardPriorityValueMap(Configuration config)
        {
            // Map P2 to P1
            Value v1 = new Value();
            v1.LeftValue = "2";
            v1.RightValue = "1";
            ValueMap vMap = new ValueMap();
            vMap.name = "PriorityValueMap";
            vMap.Value.Add(v1);

            Value v2 = new Value();
            v2.LeftValue = "*";
            v2.RightValue = "3";
            v2.When.ConditionalSrcFieldName = "System.Description";
            v2.When.ConditionalSrcFieldValue = ConditionalValueMapDescription;
            vMap.Value.Add(v2);

            // Map field
            MappedField mField1 = new MappedField();
            mField1.LeftName = FIELD_PRIORITY;
            mField1.RightName = FIELD_PRIORITY;
            mField1.MapFromSide = SourceSideTypeEnum.Left;
            mField1.valueMap = vMap.name;

            // Map the rest of the fields using wild card
            MappedField defaultField = new MappedField();
            defaultField.LeftName = "*";
            defaultField.RightName = "*";
            defaultField.MapFromSide = SourceSideTypeEnum.Left;

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

        public void SetConditionalHybridPriorityValueMap(Configuration config)
        {
            // Map P2 to P1
            Value v1 = new Value();
            v1.LeftValue = "2";
            v1.RightValue = "1";
            ValueMap vMap = new ValueMap();
            vMap.name = "PriorityValueMap";
            vMap.Value.Add(v1);

            Value v2 = new Value();
            v2.LeftValue = "*";
            v2.RightValue = "1";
            v2.When.ConditionalSrcFieldName = "System.Description";
            v2.When.ConditionalSrcFieldValue = ConditionalValueMapDescription;
            vMap.Value.Add(v2);

            Value v3 = new Value();
            v3.LeftValue = "2";
            v3.RightValue = "3";
            v3.When.ConditionalSrcFieldName = "System.Description";
            v3.When.ConditionalSrcFieldValue = ConditionalValueMapDescription;
            vMap.Value.Add(v3);

            // Map field
            MappedField mField1 = new MappedField();
            mField1.LeftName = FIELD_PRIORITY;
            mField1.RightName = FIELD_PRIORITY;
            mField1.MapFromSide = SourceSideTypeEnum.Left;
            mField1.valueMap = vMap.name;

            // Map the rest of the fields using wild card
            MappedField defaultField = new MappedField();
            defaultField.LeftName = "*";
            defaultField.RightName = "*";
            defaultField.MapFromSide = SourceSideTypeEnum.Left;

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

        public void SetAggregatedFieldMap(Configuration config)
        {
            // Aggregated fields
            AggregatedFields aggregatedFields = new AggregatedFields();
            FieldsAggregationGroup group = new FieldsAggregationGroup();
            group.MapFromSide = SourceSideTypeEnum.Left;
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
            MappedField defaultField = new MappedField();
            defaultField.LeftName = "*";
            defaultField.RightName = "*";
            defaultField.MapFromSide = SourceSideTypeEnum.Left;

            FieldMap fieldMap = new FieldMap();
            fieldMap.name = "BugToBugFieldMap";
            fieldMap.MappedFields.MappedField.Add(defaultField);

            // TODO: Create another test case for aggreated fields feature
            // Construct configurations with conflicting field maps
            //MappedField mField1 = new MappedField();
            //mField1.LeftName = FIELD_TITLE;
            //mField1.RightName = FIELD_DESCRIPTION;
            //mField1.MapFromSide = SourceSideTypeEnum.Left;

            //MappedField mField2 = new MappedField();
            //mField2.LeftName = FIELD_DESCRIPTION;
            //mField2.RightName = FIELD_TITLE;
            //mField2.MapFromSide = SourceSideTypeEnum.Left;

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
