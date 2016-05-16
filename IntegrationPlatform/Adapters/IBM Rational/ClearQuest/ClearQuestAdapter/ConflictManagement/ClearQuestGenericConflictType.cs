// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.ConflictManagement
{
    public class ClearQuestGenericConflictType : ConflictType
    {
        public ClearQuestGenericConflictType()
            : base(new ClearQuestGenericConflictHandler())
        { }

        public static MigrationConflict CreateConflict(Exception ex)
        {
            return new MigrationConflict(
                new ClearQuestGenericConflictType(),
                MigrationConflict.Status.Unresolved,
                CreateConflictDetails(ex),
                CreateScopeHint(ex));
        }

        public override Guid ReferenceName
        {
            get { return new Guid("{9B1CA19E-44D8-4fa4-8167-01097D0E10FA}"); }
        }

        public override string FriendlyName
        {
            get { return ClearQuestResource.ClearQuest_Conflict_GenericConflict_Name; }
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
