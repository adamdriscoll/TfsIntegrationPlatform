// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace UnitTests
{
    
    
    /// <summary>
    ///This is a test class for IntegerRangeScopeInterpreterTest and is intended
    ///to contain all IntegerRangeScopeInterpreterTest Unit Tests
    ///</summary>
    [TestClass()]
    public class IntegerRangeScopeInterpreterTest
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
        ///A test for RuleScopeComparer
        ///</summary>
        [TestMethod()]
        public void RuleScopeComparerTest()
        {
            IntegerRangeScopeInterpreter target = new IntegerRangeScopeInterpreter(); // TODO: Initialize to an appropriate value
            IComparer<ConflictResolutionRule> actual;
            actual = target.RuleScopeComparer;
        }

        /// <summary>
        ///A test for IsInScope
        ///</summary>
        [TestMethod()]
        public void IsInScopeTest()
        {
            IntegerRangeScopeInterpreter target = new IntegerRangeScopeInterpreter(); 
            string scopeToCheck = "18"; 
            string scope = "1-20"; 
            Assert.IsTrue(target.IsInScope(scopeToCheck, scope));
        }

        /// <summary>
        ///A test for IsInScope
        ///</summary>
        [TestMethod()]
        public void IsInScopeExactMatchTest()
        {
            IntegerRangeScopeInterpreter target = new IntegerRangeScopeInterpreter();
            string scopeToCheck = "18";
            string scope = "18";
            Assert.IsTrue(target.IsInScope(scopeToCheck, scope));
        }

        /// <summary>
        ///A test for IsInScope
        ///</summary>
        [TestMethod()]
        public void IsInScopeInCompleteTest()
        {
            IntegerRangeScopeInterpreter target = new IntegerRangeScopeInterpreter();
            string scopeToCheck = "18";
            string scope = "12-";
            Assert.IsFalse(target.IsInScope(scopeToCheck, scope));
        }
    }
}
