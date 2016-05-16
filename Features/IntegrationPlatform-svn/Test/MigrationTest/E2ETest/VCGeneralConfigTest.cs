// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTestLibrary;
using Microsoft.TeamFoundation.Migration;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using System.Diagnostics;

namespace TfsVCTest
{
    /// <summary>
    /// Summary description for VCGeneralConfigTest
    /// </summary>
    [TestClass]
    public class VCGeneralConfigTest : TfsVCConfigTestCaseBase
    {
        [TestMethod(), Priority(2), Owner("teyang")]
        [Description("Provide empty filter string, and expect business rule evaluation to report the error.")]
        public void Config_EmptyFilterStringTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(TestEnvironment_SetEmptyFilterString);

            try
            {
                RunAndValidate();
            }
            catch (ConfigurationBusinessRuleViolationException e)
            {
                string msg = e.ConfigurationValidationResult.ToString();
                Trace.WriteLine(msg);
                return;
            }

            Assert.Fail("No business rule validation failure was reported.");
        }

        void TestEnvironment_SetEmptyFilterString(Configuration config)
        {
            foreach (Session s in config.SessionGroup.Sessions.Session)
            {
                if (s.SessionType == SessionTypeEnum.VersionControl)
                {
                    s.Filters.FilterPair.First().FilterItem.First().FilterString = string.Empty;
                    break;
                }
            }
        }
    }
}
