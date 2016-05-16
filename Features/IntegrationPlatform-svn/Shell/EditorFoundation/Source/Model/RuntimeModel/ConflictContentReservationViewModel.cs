// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    public class ConflictContentReservationViewModel
    {
        RTConflictContentReservation m_conflictContentReservation;

        public ConflictContentReservationViewModel(RTConflictContentReservation conflictContentReservation)
        {
            m_conflictContentReservation = conflictContentReservation;
        }

        #region Properties
        // Selected RTConflictContentReservation properties
        public string Content { get { return m_conflictContentReservation.Content; } }
        public long ItemId { get { return m_conflictContentReservation.ItemId; } }
        #endregion
    }
}
