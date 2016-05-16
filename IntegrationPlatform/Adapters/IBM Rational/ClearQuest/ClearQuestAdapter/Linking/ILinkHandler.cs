// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClearQuestOleServer;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.Linking
{
    public interface ILinkHandler
    {
        void ExtractLinkChangeActions(Session session, OAdEntity hostRecord, List<LinkChangeGroup> linkChangeGroups);
        bool Update(ClearQuestMigrationContext migrationContext, Session session, OAdEntity hostRecord, LinkChangeAction linkChangeAction);
    }
}
