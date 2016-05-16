// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TfsVCTest;
using MigrationTestLibrary;
using Microsoft.TeamFoundation.VersionControl.Client;
using TfsWitTest;
using Microsoft.TeamFoundation.Migration.Toolkit.ServerDiff;

namespace PerfTest
{
    /// <summary>
    /// WITPerfTest
    /// </summary>
    [TestClass]
    public class WITPerfTest : TfsWITTestCaseBase
    {
        protected override string TestProjectName
        {
            get
            {
                return "WITPerfTest";
            }
        }

        ///<summary>
        /// Migrate many work items and verify paging works
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("WIT Paging test")]
        public void WITPagingTest()
        {
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

            // verify sync result 
            WitDiffResult result = GetDiffResult();

            // ignore Area/Iteration path mismatches due to test environments
            VerifySyncResult(result, new List<string> { "System.IterationPath", "System.AreaPath" });
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
