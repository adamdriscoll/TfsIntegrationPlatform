// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter.Properties;
using Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter.WIT;
using Microsoft.TeamFoundation.Migration.Shell.View;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement.WITBasicConflicts;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter
{
    public abstract class TfsCommonWITShellAdapter : TfsCommonShellAdapter
    {
        protected const string c_emptyTfsWitQuery = "[System.Id] = 0";

        private static List<IConflictTypeView> s_conflictTypes;

        static TfsCommonWITShellAdapter()
        {
            s_conflictTypes = new List<IConflictTypeView>();
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new WITEditEditConflictType().ReferenceName,
                FriendlyName = Resources.WITEditEditConflictTypeFriendlyName,
                Description = Resources.WITEditEditConflictTypeDescription,
                Type = typeof(WITFieldCollisionConflictTypeViewModel)
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new WITUnmappedWITConflictType().ReferenceName,
                FriendlyName = Resources.WITUnmappedWITConflictTypeFriendlyName,
                Description = Resources.WITUnmappedWITConflictTypeDescription,
                Type = typeof(UnmappedWorkItemTypeCustomControl)
            });
        }

        public override IEnumerable<IConflictTypeView> GetConflictTypeViews()
        {
            return base.GetConflictTypeViews().Concat(s_conflictTypes);
        }
    }
}
