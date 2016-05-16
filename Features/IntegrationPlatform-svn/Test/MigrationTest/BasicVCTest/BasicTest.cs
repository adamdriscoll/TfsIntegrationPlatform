// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTestLibrary;

namespace BasicVCTest
{
    /// <summary>
    /// Basic test cases
    /// </summary>
    [TestClass]
    public class BasicTest : BasicVCTestCaseBase
    {
        ///<summary>
        ///Scenario: Migrate an added file
        ///Expected Result: The file is migrated sucessfully
        ///</summary>
        [TestMethod(), Priority(1), Owner("hykwon")]
        [Description("Migrate an added file")]
        public void AddTest()
        {
            MigrationItemStrings file = new MigrationItemStrings("file.txt", "FILE.txt", TestEnvironment, true);
            SourceAdapter.AddFile(file.LocalPath);

            RunAndValidate();
        }
    }
}
