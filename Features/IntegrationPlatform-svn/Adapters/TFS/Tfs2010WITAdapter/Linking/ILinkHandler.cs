// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Linking
{
    public interface ILinkHandler
    {
        void ExtractLinkChangeActions(TfsMigrationWorkItem source, List<LinkChangeGroup> linkChangeGroups, WorkItemLinkStore store);
        bool UpdateTfs(TfsUpdateDocument updateDoc, LinkChangeAction linkChangeAction);
    }
}