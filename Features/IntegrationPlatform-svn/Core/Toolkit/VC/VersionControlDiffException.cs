// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    [Serializable]
    public class VersionControlDiffException : MigrationException
    {
        public VersionControlDiffException()
            : base(MigrationToolkitResources.DefaultUnresolvableConflictExceptionMessage)
        {
        }

        public VersionControlDiffException(string message)
            : base(message)
        {
        }

        public VersionControlDiffException(Exception innerException)
            : base(MigrationToolkitResources.DefaultUnresolvableConflictExceptionMessage, innerException)
        {
        }

        public VersionControlDiffException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected VersionControlDiffException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
