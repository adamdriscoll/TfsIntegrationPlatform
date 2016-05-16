// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTestLibrary;
using MigrationTestLibrary.Conflict;
using Microsoft.TeamFoundation.Migration.Toolkit.ServerDiff;
using Microsoft.TeamFoundation.Migration.BusinessModel.WIT;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration;
using System.Diagnostics;

namespace TfsWitTest
{
    /// <summary>
    /// WIT custom setting configuration tests
    /// </summary>
    [TestClass]
    public class WitCustomConfigurationTests : WitConfigurationTestCaseBase
    {
        const string AggregationFormat = "AggregatedFields:{0}:{1}";
        
        ///<summary>
        /// Test Field Map configuration (valueMap="") 
        /// 
        ///</summary>
        ///<remarks>Configuration validation is added and expected to pass</remarks>
        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("Empty ValueMap in field mapping element is valid")]
        public void Config_EmptyValueMapNameTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(SetEmptyValueMapNameInFieldMap);
            RunAndNoValidate();
        }

        private void SetEmptyValueMapNameInFieldMap(Configuration config)
        {
            // Map field
            MappedField mField1 = new MappedField();
            mField1.LeftName = FIELD_PRIORITY;
            mField1.RightName = FIELD_PRIORITY;
            mField1.MapFromSide = SourceSideTypeEnum.Left;
            mField1.valueMap = string.Empty; // set value map name to be empty string

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

            SetWitSessionCustomSetting(config, customSetting);
        }

        ///<summary>
        /// Test Field Map configuration
        /// 
        ///</summary>
        ///<remarks>Configuration validation is added and expected to pass</remarks>
        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("No two field maps map to the same target field")]
        public void Config_NoTwoFieldMapToSameTargetFieldTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(TestEnvironment_InsertTwoFieldMapToSameTargetInMappedField);

            try
            {
                RunAndNoValidate();
            }
            catch (ConfigurationBusinessRuleViolationException ex)
            {
                string msg = ex.ConfigurationValidationResult.ToString();
                Trace.WriteLine(msg);
                return;
            }

            Assert.Fail("No business rule validation failure was reported.");
        }

        void TestEnvironment_InsertTwoFieldMapToSameTargetInMappedField(Configuration config)
        {
            // Map field
            MappedField mField1 = new MappedField();
            mField1.LeftName = FIELD_PRIORITY;
            mField1.RightName = FIELD_PRIORITY;
            mField1.MapFromSide = SourceSideTypeEnum.Left;
            mField1.valueMap = string.Empty; // set value map name to be empty string

            // Map to SAME field again
            MappedField defaultField = new MappedField();
            defaultField.LeftName = FIELD_AREA_PATH;
            defaultField.RightName = FIELD_PRIORITY;
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

            SetWitSessionCustomSetting(config, customSetting);
        }

        ///<summary>
        /// Test Field Map configuration
        /// 
        ///</summary>
        ///<remarks>Configuration validation is added and expected to pass</remarks>
        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("Identical source fields cannot appear twice in MappedFields")]
        public void Config_SameSourceFieldCannotMapTwiceTestLeft()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(TestEnvironment_InsertTwoFieldMapFromSameSourceInMappedField);

            try
            {
                RunAndNoValidate();
            }
            catch (ConfigurationBusinessRuleViolationException ex)
            {
                string msg = ex.ConfigurationValidationResult.ToString();
                Trace.WriteLine(msg);
                return;
            }

            Assert.Fail("No business rule validation failure was reported.");
        }

        void TestEnvironment_InsertTwoFieldMapFromSameSourceInMappedField(Configuration config)
        {
            // Map field
            MappedField mField1 = new MappedField();
            mField1.LeftName = FIELD_PRIORITY;
            mField1.RightName = FIELD_PRIORITY;
            mField1.MapFromSide = SourceSideTypeEnum.Left;
            mField1.valueMap = string.Empty; // set value map name to be empty string

            // Map to SAME field again
            MappedField defaultField = new MappedField();
            defaultField.LeftName = FIELD_PRIORITY;
            defaultField.RightName = FIELD_AREA_PATH;
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

            SetWitSessionCustomSetting(config, customSetting);
        }

        ///<summary>
        /// Test Field Map configuration
        /// 
        ///</summary>
        ///<remarks>Configuration validation is added and expected to pass</remarks>
        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("Identical source fields cannot appear twice in MappedFields")]
        public void Config_SameSourceFieldCannotMapTwiceTestRight()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(TestEnvironment_InsertTwoFieldMapFromSameSourceInMappedFieldRight);

            try
            {
                RunAndNoValidate();
            }
            catch (ConfigurationBusinessRuleViolationException ex)
            {
                string msg = ex.ConfigurationValidationResult.ToString();
                Trace.WriteLine(msg);
                return;
            }

            Assert.Fail("No business rule validation failure was reported.");
        }

        void TestEnvironment_InsertTwoFieldMapFromSameSourceInMappedFieldRight(Configuration config)
        {
            // Map field
            MappedField mField1 = new MappedField();
            mField1.LeftName = FIELD_PRIORITY;
            mField1.RightName = FIELD_PRIORITY;
            mField1.MapFromSide = SourceSideTypeEnum.Right;
            mField1.valueMap = string.Empty; // set value map name to be empty string

            // Map to SAME field again
            MappedField defaultField = new MappedField();
            defaultField.LeftName = FIELD_PRIORITY;
            defaultField.RightName = FIELD_AREA_PATH;
            defaultField.MapFromSide = SourceSideTypeEnum.Right;

            FieldMap fieldMap = new FieldMap();
            fieldMap.name = "BugToBugFieldMap";
            fieldMap.MappedFields.MappedField.Add(defaultField);
            fieldMap.MappedFields.MappedField.Add(mField1);

            // Map work item type
            WorkItemTypeMappingElement typeMapping = new WorkItemTypeMappingElement();
            typeMapping.LeftWorkItemTypeName = "Bug";
            typeMapping.RightWorkItemTypeName = "Bug";
            typeMapping.fieldMap = fieldMap.name;

            //=================== another work item type mapping
            MappedField mField1_1 = new MappedField();
            mField1_1.LeftName = FIELD_PRIORITY;
            mField1_1.RightName = FIELD_PRIORITY;
            mField1_1.MapFromSide = SourceSideTypeEnum.Right;
            mField1_1.valueMap = string.Empty; // set value map name to be empty string

            // Map to SAME field again
            MappedField defaultField_1 = new MappedField();
            defaultField_1.LeftName = FIELD_PRIORITY;
            defaultField_1.RightName = FIELD_AREA_PATH;
            defaultField_1.MapFromSide = SourceSideTypeEnum.Right;

            FieldMap fieldMap_1 = new FieldMap();
            fieldMap_1.name = "BugToBugFieldMap_1";
            fieldMap_1.MappedFields.MappedField.Add(defaultField_1);
            fieldMap_1.MappedFields.MappedField.Add(mField1_1);

            // Map work item type
            WorkItemTypeMappingElement typeMapping_1 = new WorkItemTypeMappingElement();
            typeMapping_1.LeftWorkItemTypeName = "Bug_1";
            typeMapping_1.RightWorkItemTypeName = "Bug_1";
            typeMapping_1.fieldMap = fieldMap_1.name;


            // Build WIT Session custom setting
            WITSessionCustomSetting customSetting = new WITSessionCustomSetting();
            customSetting.WorkItemTypes.WorkItemType.Add(typeMapping);
            customSetting.WorkItemTypes.WorkItemType.Add(typeMapping_1);
            customSetting.FieldMaps.FieldMap.Add(fieldMap);
            customSetting.FieldMaps.FieldMap.Add(fieldMap_1);

            SetWitSessionCustomSetting(config, customSetting);
        }

        ///<summary>
        /// Test Field Map configuration
        /// 
        ///</summary>
        ///<remarks>Configuration validation is added and expected to pass</remarks>
        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("The aggregated source fields should be properly indexed in the WIT field mapping configuration.")]
        public void Config_AggregatedSourceFieldsIndexTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(TestEnvironment_InsertWronglyIndexedAggrSrcField);
            
            try
            {
                RunAndNoValidate();
            }
            catch (ConfigurationBusinessRuleViolationException ex)
            {
                string msg = ex.ConfigurationValidationResult.ToString();
                Trace.WriteLine(msg);
                return;
            }

            Assert.Fail("No business rule validation failure was reported.");
        }

        void TestEnvironment_InsertWronglyIndexedAggrSrcField(Configuration config)
        {
            // Aggregated fields
            AggregatedFields aggregatedFields = new AggregatedFields();
            FieldsAggregationGroup group = new FieldsAggregationGroup();
            group.MapFromSide = SourceSideTypeEnum.Left;
            group.TargetFieldName = FIELD_DESCRIPTION;
            group.Format = AggregationFormat;

            // NOTE: both source fields are assigned with indice 10
            SourceField f0 = new SourceField();
            f0.Index = 10;
            f0.SourceFieldName = FIELD_TITLE;

            SourceField f1 = new SourceField();
            f1.Index = 10;
            f1.SourceFieldName = FIELD_DESCRIPTION;

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

        ///<summary>
        /// Test Guid string normalization
        /// 
        ///</summary>
        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("The toolkit should normalize Guid strings in the configuration so that case-sensitive XSD Guid reference constraint is always met.")]
        public void Config_GuidStringCaseNormalizationTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(TestEnvironment_InvalidateGuidStringCase);
            RunAndNoValidate();
        }

        ///<summary>
        /// Test Guid string normalization
        /// 
        ///</summary>
        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("The configuration friendly name is too long; validation engine should detect this")]
        public void Config_ConfigFriendlyNameStringLengthValidationTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(TestEnvironment_SetTooLongConfigFriendlyName);

            try
            {
                RunAndNoValidate();
            }
            catch (ConfigurationSchemaViolationException ex)
            {
                string msg = ex.ConfigurationValidationResult.ToString();
                Trace.WriteLine(msg);
                return;
            }

            Assert.Fail("No schema validation failure was reported.");
        }

        void TestEnvironment_SetTooLongConfigFriendlyName(Configuration config)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 301; ++i)
            {
                sb.Append("a");
            }
            config.FriendlyName = sb.ToString();
        }

        ///<summary>
        /// Test all filters are 'neglected' config validation
        /// 
        ///</summary>
        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("At least one pair of filter string should not be 'neglected'")]
        public void Config_AllNeglectFilterStringTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(TestEnvironment_MarkAllFilterStringNeglect);

            try
            {
                RunAndNoValidate();
            }
            catch (ConfigurationBusinessRuleViolationException ex)
            {
                string msg = ex.ConfigurationValidationResult.ToString();
                Trace.WriteLine(msg);
                return;
            }

            Assert.Fail("No business rule validation failure was reported.");
        }

        private void TestEnvironment_MarkAllFilterStringNeglect(Configuration config)
        {
            Debug.Assert(config.Providers.Provider.Count > 0, "No provider is present in the configuration.");
            Session sessionConfig = config.SessionGroup.Sessions.Session[0];

            if (sessionConfig.Filters.FilterPair.Count <= 1)
            {
                
                for (int i = 0; i < 3; ++i)
                {
                    FilterPair p = new FilterPair();
                    p.Neglect = true;
                    FilterItem lItem = new FilterItem();
                    lItem.FilterString = "test" + i.ToString();
                    lItem.MigrationSourceUniqueId = sessionConfig.LeftMigrationSourceUniqueId;
                    FilterItem rItem = new FilterItem();
                    lItem.FilterString = "test" + i.ToString();
                    lItem.MigrationSourceUniqueId = sessionConfig.RightMigrationSourceUniqueId;
                    p.FilterItem.Add(lItem);
                    p.FilterItem.Add(rItem);
                }
            }

            foreach (var filterPair in sessionConfig.Filters.FilterPair)
            {
                filterPair.Neglect = true;
            }
        }

        ///<summary>
        /// Test Guid string normalization
        /// 
        ///</summary>
        [TestMethod(), Priority(1), Owner("teyang")]
        [Description("At least one pair of filter string should not be 'neglected'")]
        public void Config_FieldMapToEmpty()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(TestEnvironment_FieldMapToEmpty);

            try
            {
                RunAndNoValidate();
            }
            catch (Exception e)
            {
                Assert.IsFalse(e is ConfigurationBusinessRuleViolationException, "Business rule evaluation failed");
                Assert.IsFalse(e is ConfigurationSchemaViolationException, "Config schema validation failed");
            }
        }

        private void TestEnvironment_FieldMapToEmpty(Configuration config)
        {
            // Map field
            MappedField mField1 = new MappedField();
            mField1.LeftName = FIELD_PRIORITY;
            mField1.RightName = string.Empty;
            mField1.MapFromSide = SourceSideTypeEnum.Left;
            mField1.valueMap = string.Empty; // set value map name to be empty string

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

            SetWitSessionCustomSetting(config, customSetting);
        }

        private void TestEnvironment_InvalidateGuidStringCase(Configuration config)
        {
            Debug.Assert(config.Providers.Provider.Count > 0, "No provider is present in the configuration.");
            ProviderElement p = config.Providers.Provider.First();

            Debug.Assert(p.ReferenceName.Length > 0, "First provider has empty reference name.");

            p.ReferenceName = ChangeCase(p.ReferenceName);
        }

        private string ChangeCase(string guidStr)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < guidStr.Length; ++i)
            {
                char c = guidStr[i];

                if (Char.IsLetter(c))
                {
                    if (Char.IsLower(c))
                    {
                        c = Char.ToUpper(c);
                    }
                    else
                    {
                        c = Char.ToLower(c);
                    }
                }

                sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
