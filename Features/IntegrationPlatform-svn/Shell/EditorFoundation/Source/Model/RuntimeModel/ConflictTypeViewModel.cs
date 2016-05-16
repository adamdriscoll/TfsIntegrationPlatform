// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    public class ConflictTypeViewModel
    {
        RTConflictType m_conflictType;

        public ConflictTypeViewModel(RTConflictType conflictType)
        {
            m_conflictType = conflictType;
        }

        #region Properties
        // Selected RTConflictType properties
        public int Id { get { return m_conflictType.Id; } }
        public string DescriptionDoc { get { return m_conflictType.DescriptionDoc; } }
        public string FriendlyName { get { return m_conflictType.FriendlyName; } }
        public Guid ReferenceName { get { return m_conflictType.ReferenceName; } }
    	#endregion
    }
}
