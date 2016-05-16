// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.IO;
using MigrationTestLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit.ServerDiff;

namespace BasicVCTest
{
    public class BasicVCTestCaseBase : VCMigrationTestCaseBase
    {
        protected override string TestProjectName
        {
            get
            {
                return "BasicVCTest";
            }
        }

        protected override void InitializeTestCase()
        {
        }

        protected override void VerifyMigration(bool AddOnBranchSourceNotFound)
        {
            Trace.TraceInformation("==================== VCServerDiff BEGIN ====================");
            Guid sessionGuid = new Guid(base.VCSession.SessionUniqueId);
            ServerDiffEngine diff = new ServerDiffEngine(sessionGuid, false, true, SessionTypeEnum.VersionControl);
            VCDiffComparer diffComparer = new VCDiffComparer(diff);
            diff.RegisterDiffComparer(diffComparer);

            Assert.IsTrue(diff.VerifyContentsMatch(null, null), "The latest content is different");

            Trace.TraceInformation("==================== VCServerDiff END ====================");
        }
    }
}
