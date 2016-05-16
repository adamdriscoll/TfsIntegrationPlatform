// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Shell.ConflictManagement
{
    static class Constants
    {
        public static Guid witGeneralConflictTypeRefName = new Guid("470F9617-FC96-4166-96EB-44CC2CF73A97");
        public static Guid fileAttachmentGeneralConflictTypeRefName = new Guid("5EC7F170-E36C-4EA2-96F8-69DECDE0279C");
        public static Guid chainOnConflictConflictTypeRefName = new Guid("F6BFB484-EE70-4ffc-AAB3-4F659B0CAF7F");
        public static Guid witInsufficientPermissionConflictTypeRefName = new Guid("CBF0B9F3-9A1B-4deb-ADD3-7FEC98604118");
        public static Guid workItemTypeNotExistConflictTypeRefName = new Guid("87F1BAF4-E9DC-4c5b-8E08-68C70AEFB6FB");
    }
}
