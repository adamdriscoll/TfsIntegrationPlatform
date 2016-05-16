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
    ///This is a test class for UserMappingRuleEvaluatorTest and is intended
    ///to contain all UserMappingRuleEvaluatorTest Unit Tests
    ///</summary>
    [TestClass()]
    public class UserMappingRuleEvaluatorTest
    {


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
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
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
        public void UserMappingRuleEvaluator_TryMapUserIdentityTest()
        {
            NotifyingCollection<UserMappings> userMappings = new NotifyingCollection<UserMappings>();

            UserMappings mappings = new UserMappings();
            mappings.DirectionOfMapping = MappingDirectionEnum.LeftToRight;

            UserMapping mapping = new UserMapping();
            mapping.LeftUser = new User();
            mapping.LeftUser.Alias = "alias1";
            mapping.LeftUser.Domain = "domain1";
            mapping.RightUser = new User();
            mapping.RightUser.Alias = "alias1_target";
            mapping.RightUser.Domain = "domain1_target";

            mappings.UserMapping.Add(mapping);
            
            userMappings.Add(mappings);
            

            UserMappingRuleEvaluator target = new UserMappingRuleEvaluator(userMappings);
            RichIdentity sourceUserIdentity = new RichIdentity();
            sourceUserIdentity.Alias = "alias1";
            sourceUserIdentity.Domain = "domain1";
            sourceUserIdentity.DisplayName = "random";

            IdentityLookupContext context = new IdentityLookupContext(Guid.Empty, Guid.Empty);
            context.MappingDirection = MappingDirectionEnum.LeftToRight;

            RichIdentity mappedUserIdentity = new RichIdentity();
            bool expected = true;
            bool actual;
            actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(mappedUserIdentity.Alias, "alias1_target");
            Assert.AreEqual(mappedUserIdentity.Domain, "domain1_target");
            Assert.AreEqual(mappedUserIdentity.DisplayName, string.Empty);


            sourceUserIdentity.Alias = "alias2";
            mappedUserIdentity = new RichIdentity();
            actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreNotEqual(expected, actual);

            sourceUserIdentity.Alias = "alias1";
            sourceUserIdentity.Domain = "different_domain";
            mappedUserIdentity = new RichIdentity();
            actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreNotEqual(expected, actual);


            // add another rule
            mappings = new UserMappings();
            mappings.DirectionOfMapping = MappingDirectionEnum.RightToLeft;

            mapping = new UserMapping();
            mapping.LeftUser = new User();
            mapping.LeftUser.Alias = "default_alias";
            mapping.LeftUser.Domain = "default_domain";
            mapping.RightUser = new User();
            mapping.RightUser.Alias = "*";
            mapping.RightUser.Domain = "domain1";

            mappings.UserMapping.Add(mapping);
            userMappings.Add(mappings);

            target = new UserMappingRuleEvaluator(userMappings);

            // switch mapping direction
            context.MappingDirection = MappingDirectionEnum.RightToLeft;

            sourceUserIdentity.Alias = "random alias";
            sourceUserIdentity.Domain = "domain1";
            mappedUserIdentity = new RichIdentity();
            actual = target.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(mappedUserIdentity.Alias, "default_alias");
            Assert.AreEqual(mappedUserIdentity.Domain, "default_domain");
            Assert.AreEqual(mappedUserIdentity.DisplayName, string.Empty);
        }
    }
}
