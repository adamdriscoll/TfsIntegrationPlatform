// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Shell.ConflictManagement
{
    internal class ConflictChanges
    {
        public ConflictChanges()
        {
            NewConflicts = new List<RTConflict>();
            ResolvedConflictIds = new List<int>();
        }

        public List<RTConflict> NewConflicts { get; private set; }
        public List<int> ResolvedConflictIds { get; private set; }
    }
}
