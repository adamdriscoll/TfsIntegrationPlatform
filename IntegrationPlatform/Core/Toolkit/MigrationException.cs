// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Exception type for the migration toolkit.  Can be used directly or as
    /// a base type.
    /// </summary>
    [Serializable]
    public class MigrationException : Exception
    {
        public MigrationException()
        {
        }

        public MigrationException(string message)
            : base(message)
        {
        }

        public MigrationException(string formatString, params object[] parameters)
            : base(string.Format(formatString, parameters))
        {
        }

        public MigrationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected MigrationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// Exception type for the migration toolkit. Can be used directly or as
    /// a base type.
    /// </summary>
    [Serializable]
    public class DBSchemaValidationException : MigrationException
    {
        public DBSchemaValidationException()
        {
        }

        public DBSchemaValidationException(string message)
            : base(message)
        {
        }

        public DBSchemaValidationException(string formatString, params object[] parameters)
            : base(formatString, parameters)
        {
        }

        public DBSchemaValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

    }

    /// <summary>
    /// Exception type for the migration toolkit.  Can be used directly or as
    /// a base type.
    /// </summary>
    [Serializable]
    public class InitializationException : MigrationException
    {
        public InitializationException()
        {
        }

        public InitializationException(string message)
            : base(message)
        {
        }

        public InitializationException(string formatString, params object[] parameters)
            : base(formatString, parameters)
        {
        }

        public InitializationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InitializationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// Exception type used when an Addin method throws an Exception
    /// </summary>
    [Serializable]
    public class AddinException : MigrationException
    {
        public AddinException()
        {
        }

        public AddinException(string message)
            : base(message)
        {
        }

        public AddinException(string formatString, params object[] parameters)
            : base(formatString, parameters)
        {
        }

        public AddinException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected AddinException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// Exception type for the migration toolkit.  Can be used directly or as
    /// a base type.
    /// </summary>
    [Serializable]
    public class ConflictManagementGeneralException : MigrationException
    {
        public ConflictManagementGeneralException()
        {
        }

        public ConflictManagementGeneralException(string message)
            : base(message)
        {
        }

        public ConflictManagementGeneralException(string formatString, params object[] parameters)
            : base(formatString, parameters)
        {
        }

        public ConflictManagementGeneralException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ConflictManagementGeneralException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// Exception type for the migration toolkit. Used when a conflict is created and we want to return control to the adapter level directly. 
    /// </summary>
    [Serializable]
    public class MigrationUnresolvedConflictException : MigrationException
    {
        MigrationConflict m_conflict;

        public MigrationConflict Conflict
        {
            get
            {
                return m_conflict;
            }
        }

        public MigrationUnresolvedConflictException()
        {
        }
        public MigrationUnresolvedConflictException(MigrationConflict conflict)
            : base(conflict.Description)
        {
            m_conflict = conflict;
        }

    }

    /// <summary>
    /// Exception type for the migration toolkit. Used when WIT translation service detects an untranslatable Work Item Type.
    /// </summary>
    [Serializable]
    public class UnmappedWorkItemTypeException : MigrationException
    {
        public UnmappedWorkItemTypeException()
        {
        }

        public UnmappedWorkItemTypeException(
            string sourceWorkItemType)
        {
            SourceWorkItemType = sourceWorkItemType;
        }

        public string SourceWorkItemType
        {
            get;
            private set;
        }
    }

    /// <summary>
    /// Exception type for the migration toolkit.  Can be used directly or as
    /// a base type.
    /// </summary>
    [Serializable]
    public class MigrationServiceEndpointNotFoundException : MigrationException
    {
        public MigrationServiceEndpointNotFoundException()
        {
        }

        public MigrationServiceEndpointNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception type for the migration toolkit.  Can be used directly or as
    /// a base type.
    /// </summary>
    [Serializable]
    public class InvalidMigrationSourceUriException : MigrationException
    {
        public string InvalidMigrationSourceUriString { get; set; }

        public InvalidMigrationSourceUriException()
        {
        }

        public InvalidMigrationSourceUriException(
            string invalidUriStr, Exception innerException)
            : base(string.Empty, innerException)
        {
            InvalidMigrationSourceUriString = invalidUriStr;
        }
    }

    /// <summary>
    /// Exception type for the migration toolkit.  Can be used directly or as
    /// a base type.
    /// </summary>
    [Serializable]
    public class MissingCredentialException : MigrationException
    {
        public Guid MigrationSourceId { get; set; }

        public MissingCredentialException()
        {
        }

        public MissingCredentialException(Guid migrationSourceId)
        {
            MigrationSourceId = migrationSourceId;
        }
    }

    /// <summary>
    /// Exception type for the migration toolkit. Can be used directly or as
    /// a base type.
    /// </summary>
    [Serializable]
    public class SessionGroupDeletionException : MigrationException
    {
        public SessionGroupDeletionException()
        {
        }

        public SessionGroupDeletionException(string message)
            : base(message)
        {
        }

        public SessionGroupDeletionException(string formatString, params object[] parameters)
            : base(formatString, parameters)
        {
        }

        public SessionGroupDeletionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

    }

    /// <summary>
    /// Exception type for the migration toolkit. Can be used directly or as
    /// a base type.
    /// </summary>
    [Serializable]
    public class MigrationSessionNotFoundException : MigrationException
    {
        public MigrationSessionNotFoundException()
        {
        }

        public MigrationSessionNotFoundException(string message)
            : base(message)
        {
        }

        public MigrationSessionNotFoundException(string formatString, params object[] parameters)
            : base(formatString, parameters)
        {
        }

        public MigrationSessionNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

    }

    /// <summary>
    /// Exception type used in TfsIntegrationAdmin command implementation
    /// </summary>
    public class ConfigNotExistInPackageException : Exception
    {
        public string PackageFile { get; private set; }

        public ConfigNotExistInPackageException(string packageFile)
        {
            PackageFile = packageFile;
        }
    }

    /// <summary>
    /// Exception type used in TfsIntegrationAdmin command implementation
    /// </summary>
    public class NonExistingSessionGroupUniqueIdException : Exception
    {
        public Guid NonExistingSessionGroupId
        {
            get;
            private set;
        }

        public NonExistingSessionGroupUniqueIdException(Guid nonExistingId)
        {
            NonExistingSessionGroupId = nonExistingId;
        }
    }
}
