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
    ///This is a test class for DomainMappingRuleEvaluatorTest and is intended
    ///to contain all DomainMappingRuleEvaluatorTest Unit Tests
    ///</summary>
    [TestClass()]
    public class DomainMappingRuleEvaluatorTest
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
            new ValuePair("domain2", "domain2_target"),
            new ValuePair("*", "default"),
            new ValuePair("domain1", "domain1_target"),
            new ValuePair("domain3", "*") };

        ValuePair[] m_rightToLeftMappings = new ValuePair[] {
            new ValuePair("domain2_target_on_left", "domain2"),
            new ValuePair("domain1_target_on_left", "domain1") };

        ValuePair[] m_twoWayMappings = new ValuePair[] {
            new ValuePair("admin_domain", "admin_domain_target"),
            new ValuePair("*", "*") };

        ValuePair[] m_ignoreMappings = new ValuePair[] {
            new ValuePair("ignore", "ignore")
        };

        NotifyingCollection<DomainMappings> m_aliasMappings = new NotifyingCollection<DomainMappings>();

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
            var mappingsCollection = new DomainMappings();
            mappingsCollection.DirectionOfMapping = MappingDirectionEnum.LeftToRight;
            foreach (var mapping in m_leftToRigthMappings)
            {
                DomainMapping mappingRule = new DomainMapping();
                mappingRule.Left = mapping.Left;
                mappingRule.Right = mapping.Right;
                mappingRule.MappingRule = MappingRules.SimpleReplacement;
                mappingsCollection.DomainMapping.Add(mappingRule);
            }
            m_aliasMappings.Add(mappingsCollection);

            mappingsCollection = new DomainMappings();
            mappingsCollection.DirectionOfMapping = MappingDirectionEnum.RightToLeft;
            foreach (var mapping in m_rightToLeftMappings)
            {
                DomainMapping mappingRule = new DomainMapping();
                mappingRule.Left = mapping.Left;
                mappingRule.Right = mapping.Right;
                mappingRule.MappingRule = MappingRules.SimpleReplacement;
                mappingsCollection.DomainMapping.Add(mappingRule);
            }
            m_aliasMappings.Add(mappingsCollection);

            mappingsCollection = new DomainMappings();
            mappingsCollection.DirectionOfMapping = MappingDirectionEnum.TwoWay;
            foreach (var mapping in m_twoWayMappings)
            {
                DomainMapping mappingRule = new DomainMapping();
                mappingRule.Left = mapping.Left;
                mappingRule.Right = mapping.Right;
                mappingRule.MappingRule = MappingRules.SimpleReplacement;
                mappingsCollection.DomainMapping.Add(mappingRule);
            }

            foreach (var mapping in m_ignoreMappings)
            {
                DomainMapping mappingRule = new DomainMapping();
                mappingRule.Left = mapping.Left;
                mappingRule.Right = mapping.Right;
                mappingRule.MappingRule = MappingRules.Ignore;
                mappingsCollection.DomainMapping.Add(mappingRule);
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
        public void DomainMappingRuleEvaluator_TryMapUserIdentityTest()
        {
            DomainMappingRuleEvaluator target = new DomainMappingRuleEvaluator(m_aliasMappings); 

            RichIdentity sourceUserIdentity = new RichIdentity();
            sourceUserIdentity.Domain = "domain2";
            sourceUserIdentity.Alias = "user";

            IdentityLookupContext context = new IdentityLookupContext(Guid.Empty, Guid.Empty); // TODO: Initialize to an appropriate value
            context.MappingDirection = MappingDirectionEnum.LeftToRight;

            RichIdentity mappedUserIdentity = new RichIdentity();
            bool expected = true;
            bool actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(mappedUserIdentity.Domain, "domain2_target");
            Assert.AreEqual(mappedUserIdentity.Alias, string.Empty);
            Assert.AreEqual(expected, actual);

            sourceUserIdentity.Domain = "random domain";
            mappedUserIdentity = new RichIdentity();
            actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(mappedUserIdentity.Domain, "default");
            Assert.AreEqual(mappedUserIdentity.Alias, string.Empty);
            Assert.AreEqual(expected, actual);

            sourceUserIdentity.Domain = "domain3";
            mappedUserIdentity = new RichIdentity();
            actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(mappedUserIdentity.Domain, "domain3");
            Assert.AreEqual(mappedUserIdentity.Alias, string.Empty);
            Assert.AreEqual(expected, actual);

            sourceUserIdentity.Domain = "domain2_target_on_left";
            mappedUserIdentity = new RichIdentity();
            actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(mappedUserIdentity.Domain, "default");
            Assert.AreEqual(mappedUserIdentity.Alias, string.Empty);
            Assert.AreEqual(expected, actual);

            sourceUserIdentity.Domain = "admin_domain";
            mappedUserIdentity = new RichIdentity();
            actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(mappedUserIdentity.Domain, "default");
            Assert.AreEqual(mappedUserIdentity.Alias, string.Empty);
            Assert.AreEqual(expected, actual);

            // switch mapping direction
            context.MappingDirection = MappingDirectionEnum.RightToLeft;

            sourceUserIdentity.Domain = "domain2";
            mappedUserIdentity = new RichIdentity();
            actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(mappedUserIdentity.Domain, "domain2_target_on_left");
            Assert.AreEqual(mappedUserIdentity.Alias, string.Empty);
            Assert.AreEqual(expected, actual);

            sourceUserIdentity.Domain = "random domain";
            mappedUserIdentity = new RichIdentity();
            actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(mappedUserIdentity.Domain, "random domain");
            Assert.AreEqual(mappedUserIdentity.Alias, string.Empty);
            Assert.AreEqual(expected, actual);

            sourceUserIdentity.Domain = "ignore";
            mappedUserIdentity = new RichIdentity();
            actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(mappedUserIdentity.Domain, string.Empty);
            Assert.AreEqual(mappedUserIdentity.Alias, string.Empty);
            Assert.AreEqual(expected, actual);
        }
    }
}
