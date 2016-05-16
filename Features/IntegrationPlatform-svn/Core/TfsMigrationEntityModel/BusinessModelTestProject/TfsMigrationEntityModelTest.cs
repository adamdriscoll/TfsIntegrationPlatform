// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Xml.Serialization;

using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;
using System.IO;

namespace BusinessModelTestProject
{
    
    
    /// <summary>
    ///This is a test class for TfsMigrationConsolidatedDBEntitiesTest and is intended
    ///to contain all TfsMigrationConsolidatedDBEntitiesTest Unit Tests
    ///</summary>
    [TestClass()]
    public class TfsMigrationEntityModelTest
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
        ///A test for deserialize a configuration xml doc to Business Model OM objects
        ///</summary>
        [TestMethod()]
        public void ConfigurationBusinessModelDeserializationTest()
        {
            string inputConfigXmlFile =
                @"\tmp\SampleNewConfigFile.xml";

            FileStream xmlDoc = new FileStream(inputConfigXmlFile, FileMode.Open);

            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
            Configuration config = serializer.Deserialize(xmlDoc) as Configuration;

            if (null != config)
            {
                Assert.AreEqual(config.FriendlyName, "Orcas To Orcas Sync");
                Assert.AreEqual(config.UniqueId, "{76ED5F46-E6AD-42e3-8E1B-4AD2F1515043}");
            }
            else
            {
                Assert.Fail("deserialization failed");
            }
        }

        [TestMethod()]
        public void EntityModelLoadingTest()
        {
            string inputConfigXmlFile =
                @"\tmp\SampleNewConfigFile.xml";

            FileStream xmlDoc = new FileStream(inputConfigXmlFile, FileMode.Open);

            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
            Configuration config = serializer.Deserialize(xmlDoc) as Configuration;

            if (null != config)
            {
                config.Save();
            }
            else
            {
                Assert.Fail("deserialization failed");
            }
        }

    }
}
