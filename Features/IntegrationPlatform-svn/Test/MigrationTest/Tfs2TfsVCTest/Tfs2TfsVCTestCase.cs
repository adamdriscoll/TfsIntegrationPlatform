// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.VersionControl.Client;
using MigrationTestLibrary;
using Tfs2008VCTCAdapter;
using TfsVCTest;

namespace Tfs2TfsVCTest
{
    public abstract class Tfs2TfsVCTestCase : TfsVCTestCaseBase
    {
        protected override string TestProjectName
        {
            get
            {
                return "Tfs2TfsVCTest";
            }
        }

        protected Workspace TargetWorkspace
        {
            get
            {
                return ((ITfsVCTestCaseAdapter)TargetAdapter).Workspace;
            }
        }

        protected void PendEditToRevertFile(Workspace ws, MigrationItemStrings file, int latestVersion, int revertToVersion)
        {
            ws.Get(new ChangesetVersionSpec(revertToVersion), GetOptions.Overwrite);
            // Do a fake get to update the local version.
            using (UpdateLocalVersionQueue q = new UpdateLocalVersionQueue(ws))
            {
                ItemSet set = ws.VersionControlServer.GetItems(file.ServerPath, new ChangesetVersionSpec(latestVersion), RecursionType.None);
                q.QueueUpdate(set.Items[0].ItemId, file.LocalPath, latestVersion);
                q.Flush();
            }
            ws.PendEdit(file.LocalPath);
        }
    }
}
