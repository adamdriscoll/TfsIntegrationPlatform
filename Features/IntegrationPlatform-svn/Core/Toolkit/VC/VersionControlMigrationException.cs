// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    [Serializable]
    public class VersionControlMigrationException : MigrationException
    {
        public VersionControlMigrationException()
            : base(MigrationToolkitResources.DefaultUnresolvableConflictExceptionMessage)
        {
        }

        public VersionControlMigrationException(string message)
            : base(message)
        {
        }

        public VersionControlMigrationException(string message, bool canRetry)
            : base(message)
        {
            m_canRetry = canRetry;
        }

        public VersionControlMigrationException(Exception innerException)
            : base(MigrationToolkitResources.DefaultUnresolvableConflictExceptionMessage, innerException)
        {
        }

        public VersionControlMigrationException(Exception innerException, bool canRetry)
            : base(MigrationToolkitResources.DefaultUnresolvableConflictExceptionMessage, innerException)
        {
            m_canRetry = canRetry;
        }

        public VersionControlMigrationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public VersionControlMigrationException(string message, Exception innerException, bool canRetry)
            : base(message, innerException)
        {
            m_canRetry = canRetry;
        }

        protected VersionControlMigrationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public bool CanRetry
        {
            get
            {
                return m_canRetry;
            }
        }
        [NonSerializedAttribute]
        bool m_canRetry;
    }

    [Serializable]
    public class MappingNotFoundException : VersionControlMigrationException
    {
        public MappingNotFoundException()
            : this(MigrationToolkitResources.VCMappingMissingUnknownItem)
        {
            Debug.Assert(false, "we should not use this exception ctor");
        }

        public MappingNotFoundException(string message)
            : this(message, null)
        {
        }

        public MappingNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected MappingNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

    }

    /// <summary>
    /// Exception type for the migration toolkit.  Can be used directly or as
    /// a base type.
    /// </summary>
    [Serializable]
    public class UnresolvableConflictException : VersionControlMigrationException
    {
        public UnresolvableConflictException()
            : base(MigrationToolkitResources.DefaultUnresolvableConflictExceptionMessage)
        {
        }

        public UnresolvableConflictException(string message)
            : base(message)
        {
        }

        public UnresolvableConflictException(Exception innerException)
            : base(MigrationToolkitResources.DefaultUnresolvableConflictExceptionMessage, innerException)
        {
        }

        public UnresolvableConflictException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected UnresolvableConflictException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }


    [Serializable]
    public class VCInvalidPathException : VersionControlMigrationException
    {
        public VCInvalidPathException()
            : base(MigrationToolkitResources.DefaultUnresolvableConflictExceptionMessage)
        {
        }

        public VCInvalidPathException(string message)
            : base(message)
        {
        }

        public VCInvalidPathException(Exception innerException)
            : base(MigrationToolkitResources.DefaultUnresolvableConflictExceptionMessage, innerException)
        {
        }

        public VCInvalidPathException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected VCInvalidPathException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    class MigrationAbortedException : VersionControlMigrationException
    {
        public MigrationAbortedException()
            : base(MigrationToolkitResources.MigrationProcessAborted)
        {
        }
    }
}
