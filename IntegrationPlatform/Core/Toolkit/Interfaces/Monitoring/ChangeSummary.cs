// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// This structure summarizes the changes made on a server endpoint tree in some time interval
    /// </summary>
    public struct ChangeSummary
    {
        /// <summary>
        /// A count of the number of changes matching the criteria for the call that returned this ChangeSummary
        /// </summary>
        public int ChangeCount
        {
            get;
            set;
        }

        /// <summary>
        /// The date and time that the oldest change was modified
        /// (for those changes matching the criteria for the call that returned this ChangeSummary and counted in ChangeCount)
        /// The DateTime value should be a UTC date to account for possible time zone differences between the end points 
        /// as well as the migration tool host.
        /// </summary>
        public DateTime FirstChangeModifiedTimeUtc
        {
            get;
            set;
        }
    }
}
