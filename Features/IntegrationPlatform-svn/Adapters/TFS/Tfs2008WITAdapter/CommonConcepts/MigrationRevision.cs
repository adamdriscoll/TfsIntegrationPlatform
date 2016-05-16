// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter
{
    /// <summary>
    /// Revision class; used to represent a single revision.
    /// </summary>
    public sealed class MigrationRevision
    {
        private string m_author;                            // Person who created that revision
        private DateTime m_utcChangeTime;                   // Time of creation
        private Collection<MigrationField> m_fields;        // Revision's fields
        private int m_revision;                             // Revision number (existing revisions only)
        private Watermark m_source;                         // Source revision (new revisions only)


        #region constructors
        /// <summary>
        /// Constructor. Initializes new revision.
        /// </summary>
        /// <param name="revision">Revision number</param>
        /// <param name="author">Person who created the revision</param>
        /// <param name="utcChangeTime">Time when revision was created</param>
        public MigrationRevision(
            int revision,
            string author,
            DateTime utcChangeTime)
        {
            if (revision < 0)
            {
                throw new ArgumentException("revision");
            }
            if (string.IsNullOrEmpty(author))
            {
                throw new ArgumentNullException("author");
            }

            m_revision = revision;
            m_author = author;
            m_utcChangeTime = utcChangeTime;
            m_fields = new Collection<MigrationField>();
        }

        /// <summary>
        /// Constructor for an empty revision, which is used as a baseline.
        /// </summary>
        internal MigrationRevision()
        {
            m_revision = -1;
            m_fields = new Collection<MigrationField>();
        }

        /// <summary>
        /// Copy constructor for creating revisions on the target side.
        /// </summary>
        /// <param name="sourceId">Id of the source work item</param>
        /// <param name="source">Source revision</param>
        internal MigrationRevision(
            string sourceId,
            MigrationRevision source)
        {
            Debug.Assert(source.m_source == null, "Copying an already copied revision!");
            Debug.Assert(source.m_revision >= 0, "Copying an invalid revision!");
            m_revision = -1;
            m_author = source.m_author;
            m_utcChangeTime = source.m_utcChangeTime;
            m_fields = new Collection<MigrationField>();
            m_source = new Watermark(sourceId, source.m_revision);
        }

        /// <summary>
        /// Constructor. Initializes new revision.  
        /// </summary>
        /// <param name="sourceId">Id of the source work item</param>
        /// <param name="author">Person who created the revision</param>
        internal MigrationRevision(string sourceId, string author)
        {
            m_source = new Watermark(sourceId, 0);
            m_revision = m_source.Revision = -1;
            m_author = author;
            m_utcChangeTime = DateTime.UtcNow;
            m_fields = new Collection<MigrationField>();
        }
        #endregion

        /// <summary>
        /// Gets the revision number.
        /// </summary>
        public int Revision { get { return m_revision; } }

        /// <summary>
        /// Gets name of the person created the revision.
        /// </summary>
        public string Author { get { return m_author; } internal set { m_author = value; } }

        /// <summary>
        /// Gets time when revision was created.
        /// </summary>
        public DateTime UtcChangeTime { get { return m_utcChangeTime; } }

        /// <summary>
        /// Gets all fields in the revision.
        /// </summary>
        public Collection<MigrationField> Fields { get { return m_fields; } }

        /// <summary>
        /// Returns source revision.
        /// </summary>
        public Watermark Source { get { return m_source; } }
    }
}
