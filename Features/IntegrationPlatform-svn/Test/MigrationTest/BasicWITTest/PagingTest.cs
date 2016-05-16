// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTestLibrary;

namespace BasicWITTest
{
    /// <summary>
    /// WIT Paging tests
    /// </summary>
    [Ignore]
    [TestClass]
    public class PagingTest : BasicWITTestCaseBase
    {
        ///<summary>
        /// Migrate many work items and verify paging works
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Paging test")]
        public void BasicPagingTest()
        {
            TestEnvironment.CustomActions += new MigrationTestEnvironment.Customize(ConfigCustomizer.CustomActions_EnableBypassRulesOnTarget);
            
            // Note the size of page is 50
            int pageSize = 50;

            // add 49 work items (page size - 1)
            BatchAddWorkItems(pageSize - 1);
            RunAndNoValidate(false);

            // add 50 work items
            BatchAddWorkItems(pageSize);
            RunAndNoValidate(true);

            // add 51 work items
            BatchAddWorkItems(pageSize + 1);
            RunAndNoValidate(true);
            
            // verify the sync result
            VerifySyncResult();
        }

        private void BatchAddWorkItems(int numWorkItems)
        {
            for (int i = 1; i <= numWorkItems; i++)
            {
                string title = string.Format("{0} {1} {2}", TestContext.TestName, DateTime.Now.ToString("HH'_'mm'_'ss"), i);
                int workitemId = SourceAdapter.AddWorkItem("Bug", title, "description" + i);
            }
        }
    }
}
