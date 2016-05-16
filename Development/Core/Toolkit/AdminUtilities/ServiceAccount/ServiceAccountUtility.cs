// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace Microsoft.TeamFoundation.Migration.Toolkit.AdminUtilities.ServiceAccount
{
    [Flags]
    public enum AccountValidationResult
    {
        Valid = 0x0,
        NotInTFSIPEXECRole = 0x1,
        NotInTFSIPEXECWorkProcessGroup = 0x2,
        NotTfsIntegrationServiceLogonAccount = 0x4,
        ValidationFailed = 0x8,
    }

    public enum ServiceType
    {
        TfsIntegrationService,
        TfsIntegrationJobService,
    }

    public static class ServiceAccountUtility
    {
        static Dictionary<ServiceType, string> s_serviceNames = new Dictionary<ServiceType, string>();
        static ServiceAccountUtility()
        {
            s_serviceNames[ServiceType.TfsIntegrationService] = Constants.TfsIntegrationServiceName;
            s_serviceNames[ServiceType.TfsIntegrationJobService] = Constants.TfsIntegrationJobServiceName;
        }

        public static AccountValidationResult CurrentAccountHasAllServiceAccountPermissions(
            ServiceType serviceType)
        {
            return HasAllServiceAccountPermissions(serviceType, WindowsIdentity.GetCurrent().Name, WindowsIdentity.GetCurrent().User);
        }

        public static AccountValidationResult HasAllServiceAccountPermissions(
            ServiceType serviceType,
            string account,
            SecurityIdentifier accountSid)
        {
            AccountValidationResult result = ServiceAccount.AccountValidationResult.Valid;

            try
            {
                if (!DBRoleUtil.IsAccountInTFSIPEXECRole(account))
                {
                    result |= ServiceAccount.AccountValidationResult.NotInTFSIPEXECRole;
                }

                if (!WindowsGroupUtil.IsMemberOfLocalGroup(Constants.TfsIntegrationExecWorkProcessGroupName, accountSid))
                {
                    result |= ServiceAccount.AccountValidationResult.NotInTFSIPEXECWorkProcessGroup;
                }

                if (GlobalConfiguration.UseWindowsService)
                {
                    string serviceName = s_serviceNames[serviceType];
                    if (!WindowsServiceLogonUtil.IsLogonAccountOfService(serviceName, account))
                    {
                        result |= ServiceAccount.AccountValidationResult.NotTfsIntegrationServiceLogonAccount;
                    }
                }
            }
            catch (InvalidConfigurationException e)
            {
                throw;
            }
            catch (Exception e)
            {
                TraceManager.TraceException(e);
                result |= ServiceAccount.AccountValidationResult.ValidationFailed;
            }

            return result;
        }
    }
}
