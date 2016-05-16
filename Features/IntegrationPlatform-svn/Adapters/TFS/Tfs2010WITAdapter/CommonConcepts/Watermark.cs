// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    /// <summary>
    /// Watermark class; describes a single revision of a work item.
    /// </summary>
    public class Watermark
    {
        private string m_id;                                // Id of a work item
        private int m_rev;                                  // Revision of a work item

        /// <summary>
        /// Watermark class.
        /// </summary>
        /// <param name="id">Work item id</param>
        /// <param name="rev">Work item revision</param>
        public Watermark(
            string id,
            int rev)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }
            if (rev < 0)
            {
                throw new ArgumentException("rev");
            }
            m_id = id;
            m_rev = rev;
        }

        /// <summary>
        /// Gets id of a work item.
        /// </summary>
        public string Id { get { return m_id; } }

        /// <summary>
        /// Gets revision of a work item.
        /// </summary>
        public int Revision { get { return m_rev; } internal set { m_rev = value; } }
    }
}
