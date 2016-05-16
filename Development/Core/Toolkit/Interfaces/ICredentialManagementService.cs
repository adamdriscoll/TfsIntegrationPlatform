// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using WinCredentials;
using System.Collections.ObjectModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public interface ICredentialManagementService : INewworkCredentialManagementService
    {
        bool IsMigrationSourceConfiguredToUseStoredCredentials(Guid migrationSourceId);

        ReadOnlyCollection<NetworkCredential> GetCredentialsForMigrationSource(Guid migrationSourceId);
    }
}
