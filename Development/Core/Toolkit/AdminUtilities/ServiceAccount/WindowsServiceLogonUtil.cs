// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// A utility class that helps config the Windows Service logon account and password
    /// </summary>
    public static class WindowsServiceLogonUtil
    {
        /// <summary>
        /// Change the logon account and password for the named Windows Service
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="logonAccount"></param>
        /// <param name="logonPassword"></param>
        /// <exception cref="System.Management.ManagementException"></exception>
        public static void ChangeWindowsServiceLogon(
            string serviceName,
            string logonAccount,
            string logonPassword)
        {
            ManagementObject mgmtObj = new ManagementObject(string.Format("Win32_Service.Name='{0}'", serviceName));
            mgmtObj.InvokeMethod("Change", new object[] { null, null, null, null, null, null, logonAccount, logonPassword, null, null, null });
        }

        /// <summary>
        /// Updates the password of the current logon account for the named Windows Service
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="logonPassword"></param>
        /// <exception cref="System.Management.ManagementException"></exception>
        public static void UpdateWindowsServiceLogonPassword(
            string serviceName,
            string logonPassword)
        {
            ChangeWindowsServiceLogon(serviceName, null, logonPassword);
        }

        /// <summary>
        /// Checks if an account is the logon account of the named service
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        public static bool IsLogonAccountOfService(
            string serviceName,
            string account)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentNullException("serviceName");
            }
            if (string.IsNullOrEmpty(account))
            {
                throw new ArgumentNullException("account");
            }

            SelectQuery query = new SelectQuery(string.Format("select name, startname from Win32_Service where name='{0}'", serviceName));
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
            {
                foreach (ManagementObject service in searcher.Get())
                {
                    string currentServicelogonName = (string)service["startname"];
                    if (!string.Equals(account, currentServicelogonName, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
