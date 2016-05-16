// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTestLibrary.Conflict;
using MigrationTestLibrary;
using Microsoft.TeamFoundation.Migration.BusinessModel.WIT;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit.ServerDiff;

namespace TfsWitTest
{
    /// <summary>
    /// WIT UserMap tests
    /// </summary>
    [TestClass]
    public class WITUserMapTest : TfsWITTestCaseBase
    {
        private string m_sourceUser;
        public string SourceUser
        {
            get
            {
                return m_sourceUser;
            }

            set
            {
                m_sourceUser = value;
            }
        }

        public string TargetUser
        {
            get
            {
                return "TestTargetUser1";
            }
        }

        ///<summary>
        /// Test user map
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Basic FieldMap test")]
        public void Mapping_UserMapTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_DisableContextSync);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_EnableBypassRulesOnTarget);
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(SetUserValueMap);

            // add a work item on source side
            string title = string.Format("{0} {1}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"));

            int workitemId = SourceAdapter.AddWorkItem("Bug", title, "description1");
            string srcAssignedTo = SourceAdapter.GetFieldValue(workitemId, FIELD_ASSIGNEDTO);

            SourceUser = SourceAdapter.GetFieldValue(workitemId, FIELD_ASSIGNEDTO);

            RunAndNoValidate();

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify sync result excluding expected mismatches
            WitDiffResult result = GetDiffResult();
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH, FIELD_ASSIGNEDTO});

            // verify the user mapping
            int mirroredId = QueryMirroredWorkItemID(workitemId);
            string srcUser = SourceAdapter.GetFieldValue(workitemId, FIELD_ASSIGNEDTO);
            string tarUser = TargetAdapter.GetFieldValue(mirroredId, FIELD_ASSIGNEDTO);
            Assert.AreEqual(srcUser, SourceUser);
            Assert.AreEqual(tarUser, TargetUser);
        }

        public void SetUserValueMap(Configuration config)
        {
            // User Map
            Value v1 = new Value();
            v1.LeftValue = SourceUser;
            v1.RightValue = TargetUser;
            ValueMap vMap = new ValueMap();
            vMap.name = "UserMap";
            vMap.Value.Add(v1);

            // Map field
            MappedField mField1 = new MappedField();
            mField1.LeftName = FIELD_ASSIGNEDTO;
            mField1.RightName = FIELD_ASSIGNEDTO;
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
    }
}
