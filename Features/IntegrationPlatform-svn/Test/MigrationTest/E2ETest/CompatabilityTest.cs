// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTestLibrary;

namespace TfsVCTest
{
    /// <summary>
    /// Summary description for CompatabilityTest
    /// </summary>
    [TestClass]
    public class CompatabilityTest : TfsVCTestCaseBase
    {
        ///<summary>
        ///Scenario: Undelete a folder and edit a file underneath it
        //Expected Result: Mapped to the correct sequence of actions
        ///</summary>
        [TestMethod(), Priority(2), Owner("curtisp")]
        [Description("Undelete a folder and edit a file underneath it")]
        public void UndeleteEditTest()
        {
            MigrationItemStrings UnDeleteFolder = new MigrationItemStrings("UndeleteFolder/", null, TestEnvironment, true);
            MigrationItemStrings editFile = new MigrationItemStrings(UnDeleteFolder.Name + "file.txt", null, TestEnvironment, true);

            SourceAdapter.AddFile(editFile.LocalPath);

            int deleteChangeSetId = SourceAdapter.DeleteItem(UnDeleteFolder.ServerPath);

            PendUndelete(UnDeleteFolder.ServerPath, deleteChangeSetId);
            SourceAdapter.EditFile(editFile.LocalPath);

            RunAndValidate();
        }

        private void PendUndelete(string serverPath, int deleteChangeSetId)
        {
            Item item = SourceTfsClient.GetChangeset(deleteChangeSetId).Changes[0].Item;
            SourceWorkspace.Get();
            SourceWorkspace.PendUndelete(serverPath, item.DeletionId);
        }
    }
}
