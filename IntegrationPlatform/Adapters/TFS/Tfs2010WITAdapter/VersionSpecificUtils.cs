// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    /// <summary>
    /// This class exists to isolate provider version specific dependencies so that the same body of
    /// code can be compiled to different targets.  See the File System adapter for another example
    /// of this pattern.
    /// </summary>
     
    class VersionSpecificUtils
    {
        internal const string AdapterGuid = "{04201D39-6E47-416f-98B2-07F0013F8455}";
        internal const string AdapterName = "TFS 2010 Migration WIT Provider";
        internal const string AdapterVersion = "1.0.0.0";

        internal static void CheckBypassRulePermission(TfsTeamProjectCollection tfs)
        {
            // Verify whether the user is in the service account group. Throw an exception if it is not.
            IGroupSecurityService gss = (IGroupSecurityService)tfs.GetService(typeof(IGroupSecurityService));
            Identity serviceAccountIdentity = gss.ReadIdentity(SearchFactor.ServiceApplicationGroup, null, QueryMembership.None);

            TeamFoundationIdentity authenticatedUser;
            tfs.GetAuthenticatedIdentity(out authenticatedUser);
            if (null == authenticatedUser)
            {
                return;
            }

            Identity authenticatedUserId = gss.Convert(authenticatedUser);

            if (!gss.IsMember(serviceAccountIdentity.Sid, authenticatedUserId.Sid))
            {
                throw new PermissionException(
                    string.Format(TfsWITAdapterResources.UserNotInServiceAccountGroup, authenticatedUser.DisplayName, tfs.Uri.ToString()),
                    authenticatedUserId.AccountName, authenticatedUserId.Domain, serviceAccountIdentity.DisplayName);
            }
            TraceManager.TraceInformation("BypassRulePermission verified for user '{0}'", authenticatedUser.DisplayName);
        }
    }
}
