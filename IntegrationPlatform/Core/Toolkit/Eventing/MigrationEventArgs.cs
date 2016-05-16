// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Base class for all migration events.
    /// </summary>
    public class MigrationEventArgs : EventArgs
    {
        private string m_description;                       // Description of the event
        private DateTime m_time;                            // Local time of the event
        private Exception m_exception;                      // Exception associated with the event

        /// <summary>
        /// Gets/sets description of the event.
        /// </summary>
        public string Description { get { return m_description; } set { m_description = value; } }

        /// <summary>
        /// Gets/sets time of the event.
        /// </summary>
        public DateTime Time { get { return m_time; } set { m_time = value; } }

        /// <summary>
        /// Gets/sets exception associated with the event.
        /// </summary>
        public Exception Exception { get { return m_exception; } set { m_exception = value; } }

        /// <summary>
        /// Default constructor
        /// </summary>
        public MigrationEventArgs()
            : this(null)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="description">Event's description</param>
        public MigrationEventArgs(
            string description)
            : this(description, null)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="description">Event's description</param>
        /// <param name="exception">Exception to be associated with the event</param>
        public MigrationEventArgs(
            string description,
            Exception exception)
        {
            m_description = description;
            m_exception = exception;
            m_time = DateTime.Now;
        }
    }

    public class VersionControlEventArgs : MigrationEventArgs
    {
        SystemType m_primarySystem;                 // Primary system of the migration
        ChangeGroup m_changeGroup;
        /// <summary>
        /// Creates a new event args.
        /// </summary>
        public VersionControlEventArgs(SystemType sourceSystem)
            : this(MigrationToolkitResources.DefaultVCEventString, sourceSystem)
        {
        }

        /// <summary>
        /// Creates a new event args with the specified changeGroup.
        /// </summary>
        public VersionControlEventArgs(ChangeGroup changeGroup, SystemType sourceSystem)
            : this(changeGroup, MigrationToolkitResources.DefaultVCEventString, sourceSystem)
        {
        }

        /// <summary>
        /// Creates a new event args with the specified description.
        /// </summary>
        /// <param name="description">The description of the event</param>
        /// <param name="sourceSystem">The primary system of this event</param>
        public VersionControlEventArgs(string description, SystemType sourceSystem)
            : this(description, null, sourceSystem)
        {
        }

        /// <summary>
        /// Creates a new event args with the specified description and exception.
        /// </summary>
        /// <param name="description">The description of the event</param>
        /// <param name="exception">Exception that should be associated with the event</param>
        /// <param name="sourceSystem">The primary system of this event</param>
        public VersionControlEventArgs(string description, Exception exception, SystemType sourceSystem)
            : base(description, exception)
        {
            m_primarySystem = sourceSystem;
        }

        /// <summary>
        /// Creates a new event args with the specified session and description.
        /// </summary>
        /// <param name="description">The description of the event</param>
        public VersionControlEventArgs(ChangeGroup changeGroup, string description, SystemType sourceSystem)
            : this(changeGroup, description, null, sourceSystem)
        {
        }

        /// <summary>
        /// Creates a new event args with the specified session and description.
        /// </summary>
        /// <param name="changeGroup"></param>
        /// <param name="description">The description of the event</param>
        public VersionControlEventArgs(ChangeGroup changeGroup, string description, Exception exception, SystemType sourceSystem)
            : base(description, exception)
        {
            m_changeGroup = changeGroup;
            m_primarySystem = sourceSystem;
        }

        /// <summary>
        /// The ChangeGroup instance that was being operated on at the time the event was fired.
        /// </summary>
        public ChangeGroup ChangeGroup
        {
            get
            {
                return m_changeGroup;
            }
        }

        /// <summary>
        /// Gets primary system used in the migration.
        /// </summary>
        public SystemType PrimarySystem
        {
            get
            {
                return m_primarySystem;
            }
        }
    }
}
