// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.Exceptions;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.ConflictManagement
{
    public class ClearQuestMissingCQDllConflictType : ConflictType
    {
        public ClearQuestMissingCQDllConflictType()
            : base(new ClearQuestMissingCQDllConflictHandler())
        { }

        public static MigrationConflict CreateConflict(
            ClearQuestCOMDllNotFoundException ex)
        {
            return new MigrationConflict(
                new ClearQuestMissingCQDllConflictType(),
                MigrationConflict.Status.Unresolved,
                CreateConflictDetails(ex),
                CreateScopeHint(ex));
        }

        public override Guid ReferenceName
        {
            get { return new Guid("{C67A927D-AAE4-4a19-AFC9-034621BC04CB}"); }
        }

        public override string FriendlyName
        {
            get { return ClearQuestResource.ClearQuest_Conflict_MisingCQCom_Name; }
        }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            AddSupportedResolutionAction(new ManualConflictResolutionAction());
        }

        private static string CreateScopeHint(Exception ex)
        {
            Debug.Assert(null != ex, "ex is NULL");
            StringBuilder sb = new StringBuilder(BasicPathScopeInterpreter.PathSeparator);

            if (!string.IsNullOrEmpty(ex.Message))
            {
                sb.Append(ex.Message);
            }
            else
            {
                sb.Append(ex.ToString());
            }

            return sb.ToString();
        }

        private static string CreateConflictDetails(Exception ex)
        {
            return ex.ToString();
        }
    }
}
