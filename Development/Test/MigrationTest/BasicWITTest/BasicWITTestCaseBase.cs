// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MigrationTestLibrary;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit.ServerDiff;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BasicWITTest
{
    public class BasicWITTestCaseBase : WITMigrationTestCaseBase
    {
        protected override string TestProjectName
        {
            get
            {
                return "BasicWITTest";
            }
        }

        internal override WitDiffResult VerifySyncResult()
        {
            ServerDiffEngine diff = new ServerDiffEngine(Guid.Empty, false, true, SessionTypeEnum.WorkItemTracking);
            WITDiffComparer witDiffComparer = new WITDiffComparer(diff);
            diff.RegisterDiffComparer(witDiffComparer);

            // Add additional fields for which different values should not cause a failure because
            // the tests are configured such that these will be different
            HashSet<string> fieldsToIgnore = new HashSet<string>();
            fieldsToIgnore.Add("System.AreaPath");
            fieldsToIgnore.Add("System.IterationPath");

            bool allContentsMatch = witDiffComparer.VerifyContentsMatch(null, null, fieldsToIgnore, fieldsToIgnore);

            witDiffComparer.DiffResult.LogDifferenceReport(diff);

            Assert.IsTrue(allContentsMatch);
            return witDiffComparer.DiffResult;
        }
    }
}
