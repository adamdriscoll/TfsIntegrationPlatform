// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter
{
    /// <summary>
    /// Server date time; used to represent a server default time in TFS update package.
    /// </summary>
    class TfsServerDateTime
    {
        /// <summary>
        /// Gets the value.
        /// </summary>
        public static TfsServerDateTime Value { get { return s_value; } }

        /// <summary>
        /// Constructor.
        /// </summary>
        private TfsServerDateTime()
        {
        }

        private static TfsServerDateTime s_value = new TfsServerDateTime(); // Static value
    }
}
