// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTestLibrary;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace TfsVCTest
{
    /// <summary>
    /// Test scenarios for basic operations
    /// </summary>
    [TestClass]
    public class BasicTest : TfsVCTestCaseBase
    {
        /// <summary>
        /// Test scenarios for basic operations
        /// </summary>
        [TestClass]
        public class UndeleteTest : TfsVCTestCaseBase
        {
            ///<summary>
            ///Scenario: Migrate an undelete of a folder without undelete the child
            ///Expected Result: Only the folder is undeleted
            ///</summary>
            [TestMethod(), Priority(1), Owner("peigu")]
            [Description("Migrate an undelete of a folder without undelete the child")]
            public void UndeleteParentButNotChildTest()
            {
                MigrationItemStrings file = new MigrationItemStrings("folder/file.txt", "folder/file.txt", TestEnvironment, true);
                MigrationItemStrings folder = new MigrationItemStrings("folder", "folder", TestEnvironment, true);

                SourceAdapter.AddFile(file.LocalPath);

                int deleteChangesetId = SourceAdapter.DeleteItem(folder.ServerPath);
                Item item = SourceTfsClient.GetChangeset(deleteChangesetId).Changes[0].Item;
                SourceWorkspace.Get();
                SourceWorkspace.PendUndelete(folder.ServerPath, item.DeletionId);
                SourceWorkspace.Undo(file.ServerPath);
                SourceWorkspace.CheckIn(SourceWorkspace.GetPendingChanges(), "Migration Test undelete");

                RunAndValidate();
            }
        }

        /// <summary>
        /// Test scenarios for user id lookups
        /// </summary>
        [TestClass]
        public class OwnerMappingTest : TfsVCTestCaseBase
        {
            ///<summary>
            ///Scenario: Migrate an undelete of a folder without undelete the child
            ///Expected Result: Only the folder is undeleted
            ///</summary>
            [TestMethod(), Priority(1), Owner("teyang")]
            [Description("Map the domain and alias of the owner")]
            public void DomainAliasMappingTest()
            {
                TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(TestEnvironment_ConfigureUserIdLookup);

                MigrationItemStrings file = new MigrationItemStrings("folder/file.txt", "folder/file.txt", TestEnvironment, true);
                MigrationItemStrings folder = new MigrationItemStrings("folder", "folder", TestEnvironment, true);

                SourceAdapter.AddFile(file.LocalPath);

                RunAndValidate();
            }

            void TestEnvironment_ConfigureUserIdLookup(Configuration config)
            {
                var aliasMapping = new AliasMapping();
                aliasMapping.Left = "*";
                aliasMapping.Right = "vseqa1";
                aliasMapping.MappingRule = MappingRules.SimpleReplacement;
                var aliasMappings = new AliasMappings();
                aliasMappings.AliasMapping.Add(aliasMapping);
                aliasMappings.DirectionOfMapping = MappingDirectionEnum.LeftToRight;
                config.SessionGroup.UserIdentityMappings.AliasMappings.Add(aliasMappings);

                var domainMapping = new DomainMapping();
                domainMapping.Left = "*";
                domainMapping.Right = "redmond";
                domainMapping.MappingRule = MappingRules.SimpleReplacement;
                var domainMappings = new DomainMappings();
                domainMappings.DomainMapping.Add(domainMapping);
                domainMappings.DirectionOfMapping = MappingDirectionEnum.LeftToRight;
                config.SessionGroup.UserIdentityMappings.DomainMappings.Add(domainMappings);

                config.SessionGroup.UserIdentityMappings.EnableValidation = false;
                config.SessionGroup.UserIdentityMappings.UserIdentityLookupAddins.UserIdentityLookupAddin.Add(
                    "CDDE6B6B-72FC-43b6-BBD1-B8A89A788C6F"); // Tfs2010UserIdLookupAddin

                foreach (var ms in config.SessionGroup.MigrationSources.MigrationSource)
                {
                    ms.Settings.DefaultUserIdProperty.UserIdPropertyName = UserIdPropertyNameEnum.DomainAlias;
                }
            }
        }
    }
}
