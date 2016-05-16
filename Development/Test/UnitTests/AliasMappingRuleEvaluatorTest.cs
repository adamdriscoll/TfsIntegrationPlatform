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
    ///This is a test class for AliasMappingRuleEvaluatorTest and is intended
    ///to contain all AliasMappingRuleEvaluatorTest Unit Tests
    ///</summary>
    [TestClass()]
    public class AliasMappingRuleEvaluatorTest
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
            new ValuePair("user2", "user2_target"),
            new ValuePair("*", "default"),
            new ValuePair("user1", "user1_target"),
            new ValuePair("user3", "*") };

        ValuePair[] m_rightToLeftMappings = new ValuePair[] {
            new ValuePair("user2_target_on_left", "user2"),
            new ValuePair("user1_target_on_left", "user1") };

        ValuePair[] m_twoWayMappings = new ValuePair[] {
            new ValuePair("admin", "admin_target"),
            new ValuePair("*", "*") };

        ValuePair[] m_ignoreMappings = new ValuePair[] {
            new ValuePair("ignore", "ignore")
        };

        NotifyingCollection<AliasMappings> m_aliasMappings = new NotifyingCollection<AliasMappings>();
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

        // Use TestInitialize to run code before running each test
        [TestInitialize()]
        public void MyTestInitialize()
        {
            var mappingsCollection = new AliasMappings();
            mappingsCollection.DirectionOfMapping = MappingDirectionEnum.LeftToRight;
            foreach (var mapping in m_leftToRigthMappings)
            {
                AliasMapping mappingRule = new AliasMapping();
                mappingRule.Left = mapping.Left;
                mappingRule.Right = mapping.Right;
                mappingRule.MappingRule = MappingRules.SimpleReplacement;
                mappingsCollection.AliasMapping.Add(mappingRule);
            }
            m_aliasMappings.Add(mappingsCollection);

            mappingsCollection = new AliasMappings();
            mappingsCollection.DirectionOfMapping = MappingDirectionEnum.RightToLeft;
            foreach (var mapping in m_rightToLeftMappings)
            {
                AliasMapping mappingRule = new AliasMapping();
                mappingRule.Left = mapping.Left;
                mappingRule.Right = mapping.Right;
                mappingRule.MappingRule = MappingRules.SimpleReplacement;
                mappingsCollection.AliasMapping.Add(mappingRule);
            }
            m_aliasMappings.Add(mappingsCollection);

            mappingsCollection = new AliasMappings();
            mappingsCollection.DirectionOfMapping = MappingDirectionEnum.TwoWay;
            foreach (var mapping in m_twoWayMappings)
            {
                AliasMapping mappingRule = new AliasMapping();
                mappingRule.Left = mapping.Left;
                mappingRule.Right = mapping.Right;
                mappingRule.MappingRule = MappingRules.SimpleReplacement;
                mappingsCollection.AliasMapping.Add(mappingRule);
            }

            foreach (var mapping in m_ignoreMappings)
            {
                AliasMapping mappingRule = new AliasMapping();
                mappingRule.Left = mapping.Left;
                mappingRule.Right = mapping.Right;
                mappingRule.MappingRule = MappingRules.Ignore;
                mappingsCollection.AliasMapping.Add(mappingRule);
            }

            m_aliasMappings.Add(mappingsCollection);

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
        public void AliasMappingRuleEvaluator_TryMapUserIdentityTest()
        {
            AliasMappingRuleEvaluator target = new AliasMappingRuleEvaluator(m_aliasMappings);

            RichIdentity sourceUserIdentity = new RichIdentity();
            sourceUserIdentity.Alias = "user2";
            sourceUserIdentity.Domain = "microsoft";

            IdentityLookupContext context = new IdentityLookupContext(Guid.Empty, Guid.Empty);
            context.MappingDirection = MappingDirectionEnum.LeftToRight;

            RichIdentity mappedUserIdentity = new RichIdentity();
            bool expected = true;
            bool actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(mappedUserIdentity.Alias, "user2_target");
            Assert.AreEqual(mappedUserIdentity.Domain, string.Empty);
            Assert.AreEqual(expected, actual);

            sourceUserIdentity.Alias = "random user";
            mappedUserIdentity = new RichIdentity();
            actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(mappedUserIdentity.Alias, "default");
            Assert.AreEqual(mappedUserIdentity.Domain, string.Empty);
            Assert.AreEqual(expected, actual);

            sourceUserIdentity.Alias = "user3";
            mappedUserIdentity = new RichIdentity();
            actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(mappedUserIdentity.Alias, "user3");
            Assert.AreEqual(mappedUserIdentity.Domain, string.Empty);
            Assert.AreEqual(expected, actual);

            sourceUserIdentity.Alias = "user2_target_on_left";
            mappedUserIdentity = new RichIdentity();
            actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(mappedUserIdentity.Alias, "default");
            Assert.AreEqual(mappedUserIdentity.Domain, string.Empty);
            Assert.AreEqual(expected, actual);

            sourceUserIdentity.Alias = "admin";
            mappedUserIdentity = new RichIdentity();
            actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(mappedUserIdentity.Alias, "default");
            Assert.AreEqual(mappedUserIdentity.Domain, string.Empty);
            Assert.AreEqual(expected, actual);

            // switch mapping direction
            context.MappingDirection = MappingDirectionEnum.RightToLeft;
            
            sourceUserIdentity.Alias = "user2";
            mappedUserIdentity = new RichIdentity();
            actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(mappedUserIdentity.Alias, "user2_target_on_left");
            Assert.AreEqual(mappedUserIdentity.Domain, string.Empty);
            Assert.AreEqual(expected, actual);

            sourceUserIdentity.Alias = "random user";
            mappedUserIdentity = new RichIdentity();
            actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(mappedUserIdentity.Alias, "random user");
            Assert.AreEqual(mappedUserIdentity.Domain, string.Empty);
            Assert.AreEqual(expected, actual);

            sourceUserIdentity.Alias = "ignore";
            mappedUserIdentity = new RichIdentity();
            actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(mappedUserIdentity.Alias, string.Empty);
            Assert.AreEqual(mappedUserIdentity.Domain, string.Empty);
            Assert.AreEqual(expected, actual);
        }
    }
}
