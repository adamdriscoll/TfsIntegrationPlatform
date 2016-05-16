// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.MigrationItem
{
    public class InternalFieldSkipLogic : IFieldSkipAlgorithm
    {
        /*
         * CQConvertor code
         * [teyang] TODO: understand why the internal fields were chosen for skipping
         */
        private string[] m_internalFields = {
                        "id",
                        "old_id",
                        "old_internal_id",
                        "entitydb_id",
                        "entity_fielddefid",
                        "entitydef_name",
                        "entitydef_id",
                        "Resolution_Statetype",
                        "Note_Entry",
                        "dbid",
                        "is_active",
                        "version",
                        "lock_version",
                        "locked_by",
                        "ratl_mastership",
                        "history",
                        "ratl_keysite",
                        "is_duplicate",
                        "unduplicate_state",
                        "record_type"
                    };

        #region IFieldSkipAlgorithm Members

        public bool SkipField(string fieldName)
        {
            // return m_internalFields.Where(f => f.Equals(fieldName, StringComparison.Ordinal)).Count() > 0;
            // NOTE that unmapped fields are automatically dropped by the platform.
            return false;
        }

        #endregion
    }
}
