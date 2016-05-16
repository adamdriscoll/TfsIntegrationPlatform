// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    class CQStoredCredentialManager : ICQLoginCredentialManager
    {
        public CQStoredCredentialManager(
            ICredentialManagementService credManagementService,
            MigrationSource migrationSourceConfig)
        {
            var credentials = 
                credManagementService.GetCredentialsForMigrationSource(new Guid(migrationSourceConfig.InternalUniqueId));
            if (null == credentials || credentials.Count == 0)
            {
                string credUri = (migrationSourceConfig.StoredCredential == null ?
                    string.Empty : migrationSourceConfig.StoredCredential.CredentialString);
                throw new InvalidOperationException(
                    string.Format(ClearQuestResource.ClearQuest_Error_CredntialMissing, credUri));
            }
            else
            {
                Debug.Assert(credentials.Count == 1, "more than 1 credentials are found");
                UserName = credentials[0].UserName;
                Password = credentials[0].Password;
            }
        }

        public string UserName
        {
            get;
            private set;
        }

        public string Password
        {
            get;
            private set;
        }
    }
}
