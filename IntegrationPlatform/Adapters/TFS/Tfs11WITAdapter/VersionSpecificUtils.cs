// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
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
        internal const string AdapterGuid = "B84B30DD-1496-462A-BD9D-5A078A617779";
        internal const string AdapterName = "TFS 11 Migration WIT Provider";
        internal const string AdapterVersion = "1.0.0.0";

        internal static void CheckBypassRulePermission(TfsTeamProjectCollection tfs)
        {
            IIdentityManagementService identityService = (IIdentityManagementService)tfs.GetService(typeof(IIdentityManagementService));
            TeamFoundationIdentity serviceAccountIdentity = identityService.ReadIdentity(GroupWellKnownDescriptors.ServiceUsersGroup, MembershipQuery.None, ReadIdentityOptions.None);

            TeamFoundationIdentity authenticatedUser;
            tfs.GetAuthenticatedIdentity(out authenticatedUser);
            if (null == authenticatedUser)
            {
                return;
            }

            if (!identityService.IsMember(serviceAccountIdentity.Descriptor, authenticatedUser.Descriptor))
            {
                return;
                throw new PermissionException(
                    string.Format(TfsWITAdapterResources.UserNotInServiceAccountGroup, authenticatedUser.DisplayName, tfs.Uri.ToString()),
                    authenticatedUser.DisplayName, string.Empty, serviceAccountIdentity.DisplayName);
            }
            TraceManager.TraceInformation("BypassRulePermission verified for user '{0}'", authenticatedUser.DisplayName);
        }
    }
}
