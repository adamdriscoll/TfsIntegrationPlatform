// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    /// <summary>
    /// Generic WIT exception.
    /// </summary>
    [Serializable]
    public class WitMigrationException : MigrationException
    {
        public WitMigrationException()
            : base()
        {
        }

        public WitMigrationException(string message)
            : base(message)
        {
        }

        public WitMigrationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected WitMigrationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter=true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            base.GetObjectData(info, context);
            info.SetType(GetType());
        }
    }

    /// <summary>
    /// The exception occurs when the engine encounters an error it cannot handle.
    /// </summary>
    [Serializable]
    public class SynchronizationEngineException : WitMigrationException
    {
        public SynchronizationEngineException()
            : base()
        {
        }

        public SynchronizationEngineException(string message)
            : base(message)
        {
        }

        public SynchronizationEngineException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected SynchronizationEngineException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// Exception informing that metadata is out of sync.
    /// </summary>
    [Serializable]
    public class MetadataOutOfSyncException : WitMigrationException
    {
        public MetadataOutOfSyncException()
            : base()
        {
        }
        
        public MetadataOutOfSyncException(string message)
            : base(message)
        {
        }

        public MetadataOutOfSyncException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected MetadataOutOfSyncException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// Exception informing that the permission requirement is not met.
    /// </summary>
    [Serializable]
    public class PermissionException : WitMigrationException
    {
        public PermissionException()
            : base()
        {
        }

        public PermissionException(string message, string userAlias, string userDomain, string requiredGroupAccountDispName)
            : base(message)
        {
            UserAlias = userAlias;
            UserDomain = userDomain;
            RequiredGroupAccountDisplayName = requiredGroupAccountDispName;
        }

        public string UserAlias
        {
            get;
            set;
        }

        public string UserDomain
        {
            get;
            set;
        }

        public string RequiredGroupAccountDisplayName
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Exception informing that a field does not exist in the Work Item Type Definition.
    /// </summary>
    [Serializable]
    public class FieldNotExistException : WitMigrationException
    {
        public FieldNotExistException()
            : base()
        {
        }

        public FieldNotExistException(string message)
            : base(message)
        {
        }

        public FieldNotExistException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected FieldNotExistException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The exception occurs when a work item on the opposite side no longer exists.
    /// </summary>
    [Serializable]
    public class OrphanedWorkItemException : WitMigrationException
    {
        public OrphanedWorkItemException()
        {
        }

        public OrphanedWorkItemException(
            string msg)
            : base(msg)
        {
        }

        public OrphanedWorkItemException(
            string msg,
            Exception exception)
            : base(msg, exception)
        {
        }

        protected OrphanedWorkItemException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }

    /// <summary>
    /// The exception occurs when a work item on the opposite side no longer exists.
    /// </summary>
    [Serializable]
    public class HistoryNotFoundException : WitMigrationException
    {
        public HistoryNotFoundException()
        {
        }

        public HistoryNotFoundException(
            string sourceItemId)
        {
            SourceItemId = sourceItemId;
        }

        public string SourceItemId { get; set; }
    }

    /// <summary>
    /// Missing path exception.
    /// </summary>
    [Serializable]
    public class MissingPathException : WitMigrationException
    {
        private Node.TreeType m_nodeType;                   // Missing path's type
        private string m_path;                              // Missing path

        /// <summary>
        /// Returns type of the missing path.
        /// </summary>
        public Node.TreeType NodeType { get { return m_nodeType; } }

        /// <summary>
        /// Returns missing path.
        /// </summary>
        public string Path { get { return m_path; } }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="nodeType">Type of the missing path</param>
        /// <param name="path">Missing path</param>
        /// <param name="message">Message to be associated with the exception</param>
        public MissingPathException(
            Node.TreeType nodeType,
            string path,
            string message)
            : base(message)
        {
            m_nodeType = nodeType;
            m_path = path;
        }

        public MissingPathException()
        {
        }

        public MissingPathException(
            string message)
            : base(message)
        {
        }

        public MissingPathException(
            string message,
            Exception innerException)
            : base(message, innerException)
        {
        }

        protected MissingPathException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
