// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TfsVCTest
{

    [TestClass]
    public class BranchTest : BranchTestCaseBase
    {
        [TestInitialize]
        public override void Initialize()
        {
            base.Initialize();
        }

        ///<summary>
        ///Scenario: Migrate a branch of a file
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(1), Owner("curtisp")]
        [Description("Migrate a branch of a file")]
        public void BranchFileTest()
        {
            BranchFileScenario();
        }

        ///<summary>
        ///Scenario: Migrate a branch of an empty folder
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Migrate a branch of an empty folder")]
        public void BranchEmptyFolderTest()
        {
            BranchEmptyFolderScenario();
        }

        ///<summary>
        ///Scenario: Migrate a branch of a populated folder
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(1), Owner("curtisp")]
        [Description("Migrate a branch of a populated folder")]
        public void BranchPopulatedFolderTest()
        {
            BranchPopulatedFolderScenario();
        }

        ///<summary>
        ///Scenario: Migrate a branch of a folder from a version before some of the items were added
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(4), Owner("curtisp")]
        [Description("Migrate a branch of a folder from a version before some of the items were added")]
        public void BranchPartailTest()
        {
            BranchParitalScenario();
        }

        ///<summary>
        ///Scenario: Migrate a branch of a cyclical rename
        ///Expected Result: Server histories are the same
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Migrate a merge of a cyclical rename")]
        public void BranchCyclicRenameTest()
        {
            BranchCyclicalRenameTest();
        }
    } 
}