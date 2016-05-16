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
            int sourceId = SourceAdapter.AddWorkItem("Bug", "title", "description1");
            SourceUser = SourceAdapter.GetFieldValue(sourceId, FIELD_ASSIGNEDTO);

            RunAndNoValidate();

            // verify there's no conflicts raised           
            ConflictResolver conflictResolver = new ConflictResolver(Configuration);
            List<RTConflict> conflicts = conflictResolver.GetConflicts();
            Assert.AreEqual(0, conflicts.Count, "There should be no conflicts");

            // verify sync result excluding expected mismatches
            WitDiffResult result = GetDiffResult();
            VerifySyncResult(result, new List<string> { FIELD_ITERATION_PATH, FIELD_AREA_PATH, FIELD_ASSIGNEDTO });

            // verify the user mapping
            int targetId = QueryTargetWorkItemID(sourceId);
            string sourceUser = SourceAdapter.GetFieldValue(sourceId, FIELD_ASSIGNEDTO);
            string targetUser = TargetAdapter.GetFieldValue(targetId, FIELD_ASSIGNEDTO);
            Assert.AreEqual(sourceUser, SourceUser);
            Assert.AreEqual(targetUser, TargetUser);
        }

        public void SetUserValueMap(MigrationTestEnvironment env, Configuration config)
        {
            // User Map
            Value v1 = env.NewValue(SourceUser, TargetUser);

            ValueMap vMap = new ValueMap();
            vMap.name = "UserMap";
            vMap.Value.Add(v1);

            // Map field
            MappedField mField1 = env.NewMappedField(FIELD_ASSIGNEDTO,FIELD_ASSIGNEDTO);
            mField1.valueMap = vMap.name;

            // Map the rest of the fields using wild card
            MappedField defaultField = env.NewMappedField("*","*");

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
