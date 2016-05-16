// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;


using Microsoft.TeamFoundation.Migration;
using System.Collections.Generic;

namespace BusinessModelTestProject
{
    
    
    /// <summary>
    ///This is a test class for TfsMigrationConsolidatedDBEntitiesTest and is intended
    ///to contain all TfsMigrationConsolidatedDBEntitiesTest Unit Tests
    ///</summary>
    [TestClass()]
    public class TfsMigrationConsolidatedDBEntitiesTest
    {
        const string providerRefName = "{8DEA9C85-1D26-4098-9A8E-207560130021}";

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


        [TestMethod()]
        public void SaveEventSinkSettingXmlTest()
        {
            TfsMigrationConsolidatedDBEntities context = new TfsMigrationConsolidatedDBEntities();

            DateTime creationTime = DateTime.Now.ToUniversalTime();
            EventSinkSetting eventSinkSetting = EventSinkSetting.CreateEventSinkSetting(0, "test event sink", creationTime);

            Provider p = context.FindProviderByReferenceName(new Guid(providerRefName));
            if (null == p)
            {
                p = Provider.CreateProvider(0, "provider 1", new Guid(providerRefName));
                context.AddToProviderSet(p);
            }

            string settingTxt = @"<setting>test setting</setting>";
            eventSinkSetting.SettingXml = settingTxt;

            eventSinkSetting.Provider = p;
            context.SaveChanges();

            var rslt = from s in context.EventSinkSettingSet
                       where s.Provider.ReferenceName == p.ReferenceName
                       select s;

            Assert.IsTrue(0 != rslt.Count<EventSinkSetting>());

            int matchedSetting = 0;
            foreach (var s in rslt)
            {
                if (string.Equals(s.SettingXml, settingTxt))
                {
                    ++matchedSetting;
                }
            }

            Assert.IsTrue(0 != matchedSetting);
        }

        /// <summary>
        ///A test for FindSessionGroupConfigBySessionGroupUniqueId
        ///</summary>
        [TestMethod()]
        public void FindSessionGroupConfigBySessionGroupUniqueIdTest()
        {
            TfsMigrationConsolidatedDBEntities target = new TfsMigrationConsolidatedDBEntities(); 
            Guid sessionGroupUniqueId = Guid.NewGuid(); // TODO: Initialize to an appropriate value

            SessionGroup group = 
                SessionGroup.CreateSessionGroup(0, sessionGroupUniqueId, "FindSessionGroupConfigBySessionGroupUniqueIdTest Group");
            target.AddToSessionGroupSet(group);

            Guid groupConfigUniqueId1 = Guid.NewGuid();
            SessionGroupConfig groupConfig1 =
                SessionGroupConfig.CreateSessionGroupConfig(0, DateTime.Now, 1, groupConfigUniqueId1, 1);
            groupConfig1.SessionGroup = group;

            Guid groupConfigUniqueId2 = Guid.NewGuid();
            SessionGroupConfig groupConfig2 =
                SessionGroupConfig.CreateSessionGroupConfig(0, DateTime.Now, 1, groupConfigUniqueId2, 1);
            groupConfig2.SessionGroup = group;

            Guid groupConfigUniqueId3 = Guid.NewGuid();
            SessionGroupConfig groupConfig3 =
                SessionGroupConfig.CreateSessionGroupConfig(0, DateTime.Now, 1, groupConfigUniqueId3, 1);
            groupConfig3.SessionGroup = group;

            target.SaveChanges();

            IEnumerable<SessionGroupConfig> actual;
            actual = target.FindSessionGroupConfigBySessionGroupUniqueId(sessionGroupUniqueId);
            Assert.AreEqual(actual.Count<SessionGroupConfig>(), 3);

            foreach (var sgc in actual)
            {
                Guid configUniqueId = sgc.UniqueId;
                Assert.IsTrue(
                    configUniqueId.Equals(groupConfigUniqueId1)
                    || configUniqueId.Equals(groupConfigUniqueId2)
                    || configUniqueId.Equals(groupConfigUniqueId3));
            }
        }
    }
}
