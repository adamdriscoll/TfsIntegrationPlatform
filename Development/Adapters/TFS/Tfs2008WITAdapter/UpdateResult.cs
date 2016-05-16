// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter
{
    /// <summary>
    /// Update result class; describes result of updating a single work item.
    /// </summary>
    public sealed class UpdateResult
    {
        /// <summary>
        /// Constructor for successful updates.
        /// </summary>
        /// <param name="watermark">Updated revision</param>
        public UpdateResult(
            Watermark watermark)
        {
            if (watermark == null)
            {
                throw new ArgumentNullException("watermark");
            }
            m_watermark = watermark;
        }

        /// <summary>
        /// Constructor for failed updates.
        /// </summary>
        /// <param name="exception">Exception describing the failure</param>
        public UpdateResult(
            Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }
            m_exception = exception;
        }

        /// <summary>
        /// Returns revision created as a result of update.
        /// </summary>
        public Watermark Watermark { get { return m_watermark; } }

        /// <summary>
        /// Gets exception for failed updates.
        /// </summary>
        public Exception Exception { get { return m_exception; } }

        private Watermark m_watermark;                      // Updated revision
        private Exception m_exception;                      // Exception
    }
}
