// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration;
using System;

namespace UnitTests
{
    
    
    /// <summary>
    ///This is a test class for DisplayNameMappingRuleEvaluatorTest and is intended
    ///to contain all DisplayNameMappingRuleEvaluatorTest Unit Tests
    ///</summary>
    [TestClass()]
    public class DisplayNameMappingRuleEvaluatorTest
    {
        struct ValuePair
        {
            public string Left;
            public string Right;

            public ValuePair(string left, string right)
            {
                Left = left;
                Right = right;
            }
        }

        ValuePair[] m_leftToRigthMappings = new ValuePair[] {
            new ValuePair("user2", "user2 target"),
            new ValuePair("*", "default"),
            new ValuePair("user1", "user1 target"),
            new ValuePair("user3", "*") };

        ValuePair[] m_rightToLeftMappings = new ValuePair[] {
            new ValuePair("user2_target_on left", "user2"),
            new ValuePair("user1_target_on left", "user1") };

        ValuePair[] m_twoWayMappings = new ValuePair[] {
            new ValuePair("admin", "admin target"),
            new ValuePair("*", "*") };

        NotifyingCollection<DisplayNameMappings> m_mappings = new NotifyingCollection<DisplayNameMappings>();
       

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //

        //Use TestInitialize to run code before running each test
        [TestInitialize()]
        public void MyTestInitialize()
        {
            var mappingsCollection = new DisplayNameMappings();
            mappingsCollection.DirectionOfMapping = MappingDirectionEnum.LeftToRight;
            foreach (var mapping in m_leftToRigthMappings)
            {
                DisplayNameMapping mappingRule = new DisplayNameMapping();
                mappingRule.Left = mapping.Left;
                mappingRule.Right = mapping.Right;
                mappingRule.MappingRule = MappingRules.SimpleReplacement;
                mappingsCollection.DisplayNameMapping.Add(mappingRule);
            }
            m_mappings.Add(mappingsCollection);

            mappingsCollection = new DisplayNameMappings();
            mappingsCollection.DirectionOfMapping = MappingDirectionEnum.RightToLeft;
            foreach (var mapping in m_rightToLeftMappings)
            {
                DisplayNameMapping mappingRule = new DisplayNameMapping();
                mappingRule.Left = mapping.Left;
                mappingRule.Right = mapping.Right;
                mappingRule.MappingRule = MappingRules.SimpleReplacement;
                mappingsCollection.DisplayNameMapping.Add(mappingRule);
            }
            m_mappings.Add(mappingsCollection);

            mappingsCollection = new DisplayNameMappings();
            mappingsCollection.DirectionOfMapping = MappingDirectionEnum.TwoWay;
            foreach (var mapping in m_twoWayMappings)
            {
                DisplayNameMapping mappingRule = new DisplayNameMapping();
                mappingRule.Left = mapping.Left;
                mappingRule.Right = mapping.Right;
                mappingRule.MappingRule = MappingRules.SimpleReplacement;
                mappingsCollection.DisplayNameMapping.Add(mappingRule);
            }
            m_mappings.Add(mappingsCollection);
        }
        
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for TryMapUserIdentity
        ///</summary>
        [TestMethod(), Priority(2), Owner("teyang")]
        public void DisplayNameMappingRuleEvaluator_TryMapUserIdentityTest()
        {
            DisplayNameMappingRuleEvaluator target = new DisplayNameMappingRuleEvaluator(m_mappings);

            RichIdentity sourceUserIdentity = new RichIdentity();
            sourceUserIdentity.DisplayName = "user2";
            sourceUserIdentity.Domain = "microsoft";

            IdentityLookupContext context = new IdentityLookupContext(Guid.Empty, Guid.Empty); // TODO: Initialize to an appropriate value
            context.MappingDirection = MappingDirectionEnum.LeftToRight;

            RichIdentity mappedUserIdentity = new RichIdentity();
            bool expected = true;
            bool actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(mappedUserIdentity.DisplayName, "user2 target");
            Assert.AreEqual(mappedUserIdentity.Domain, string.Empty);
            Assert.AreEqual(expected, actual);

            sourceUserIdentity.DisplayName = "random user";
            mappedUserIdentity = new RichIdentity();
            actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(mappedUserIdentity.DisplayName, "default");
            Assert.AreEqual(mappedUserIdentity.Domain, string.Empty);
            Assert.AreEqual(expected, actual);

            sourceUserIdentity.DisplayName = "user3";
            mappedUserIdentity = new RichIdentity();
            actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(mappedUserIdentity.DisplayName, "user3");
            Assert.AreEqual(mappedUserIdentity.Domain, string.Empty);
            Assert.AreEqual(expected, actual);

            sourceUserIdentity.DisplayName = "user2_target_on left";
            mappedUserIdentity = new RichIdentity();
            actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(mappedUserIdentity.DisplayName, "default");
            Assert.AreEqual(mappedUserIdentity.Domain, string.Empty);
            Assert.AreEqual(expected, actual);

            sourceUserIdentity.DisplayName = "admin";
            mappedUserIdentity = new RichIdentity();
            actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(mappedUserIdentity.DisplayName, "default");
            Assert.AreEqual(mappedUserIdentity.Domain, string.Empty);
            Assert.AreEqual(expected, actual);

            // switch mapping direction
            context.MappingDirection = MappingDirectionEnum.RightToLeft;

            sourceUserIdentity.DisplayName = "user2";
            mappedUserIdentity = new RichIdentity();
            actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(mappedUserIdentity.DisplayName, "user2_target_on left");
            Assert.AreEqual(mappedUserIdentity.Domain, string.Empty);
            Assert.AreEqual(expected, actual);

            sourceUserIdentity.DisplayName = "random user";
            mappedUserIdentity = new RichIdentity();
            actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(mappedUserIdentity.DisplayName, "random user");
            Assert.AreEqual(mappedUserIdentity.Domain, string.Empty);
            Assert.AreEqual(expected, actual);
       
        }
    }
}
