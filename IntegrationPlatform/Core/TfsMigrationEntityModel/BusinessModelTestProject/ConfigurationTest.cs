// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System;
using System.Xml.Serialization;
using System.Xml;
namespace BusinessModelTestProject
{
    
    
    /// <summary>
    ///This is a test class for ConfigurationTest and is intended
    ///to contain all ConfigurationTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ConfigurationTest
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

        readonly string inputFile = @"\tmp\SampleNewConfigFile.xml";

        /// <summary>
        ///A test for TrySave
        ///</summary>
        [TestMethod()]
        public void TrySaveTest()
        {
            string newConfigId = Guid.NewGuid().ToString();

            XmlDocument configDoc = new XmlDocument();
            configDoc.Load(inputFile);
            configDoc.DocumentElement.Attributes["UniqueId"].Value = newConfigId;
            configDoc.Save(inputFile);

            Configuration target = null;
            using (FileStream fs = new FileStream(inputFile, FileMode.Open))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
                    target = serializer.Deserialize(fs) as Configuration;
                }
            
            
            target.TrySave();

        }
    }
}
