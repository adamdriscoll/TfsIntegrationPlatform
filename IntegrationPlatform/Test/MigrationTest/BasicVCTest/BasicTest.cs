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
        [Description("Add, edit, delete and migrate")]
        public void AddEditDeleteTest()
        {
            MigrationItemStrings file = new MigrationItemStrings("file.txt", "FILE.txt", TestEnvironment, true);
            MigrationItemStrings file2 = new MigrationItemStrings("file2.bat", "fIlE2.bat", TestEnvironment, true);
            MigrationItemStrings file3 = new MigrationItemStrings("file3.cmd", "file3.cmd", TestEnvironment, true);

            SourceAdapter.AddFile(file.LocalPath);
            SourceAdapter.AddFile(file2.LocalPath);
            SourceAdapter.AddFile(file3.LocalPath);
            
            SourceAdapter.EditFile(file2.LocalPath);
            
            SourceAdapter.DeleteItem(file3.LocalPath);
            RunAndValidate();
        }
    }
}
