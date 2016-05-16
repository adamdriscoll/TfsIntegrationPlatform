// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts
{
    public class TFSModifyLockedWorkItemLinkConflict_ResolveByForceDelete : ResolutionAction
    {
        public override Guid ReferenceName
        {
            get { return new Guid("48D03A59-DBA2-47cd-8938-D7BBAD695B65"); }
        }

        public override string FriendlyName
        {
            get { return "Force to delete the locked link"; }
        }

        public override System.Collections.ObjectModel.ReadOnlyCollection<string> ActionDataKeys
        {
            get { return new List<string>().AsReadOnly(); }
        }
    }
}
