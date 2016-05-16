// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace InstallationCA
{
    /// <summary>
    /// A utility class that helps to create and configure the TFSIPEXEC role in the Tfs_IntegrationPlatform DB
    /// </summary>
    public static class DBRoleUtil
    {
        public enum AccountsResult
        {
            Success = 0,
            Fail = 1,
            Noop = 2,
            Skipped = 3,
        }

        /// <summary>
        /// Creates the TFSIPEXEC role and grants db_datareader, db_datawriter, EXECUTE rights to it in a named DB
        /// </summary>
        /// <param name="connString"></param>
        public static AccountsResult CreateTFSIPEXECRole(string connString)
        {
            return (AccountsResult)SqlHandler.ExecuteScalar<int>(connString, SqlCommands.CreateTFSIPEXECRole, null);
        }

        /// <summary>
        /// Creates the login and user in the named DB
        /// </summary>
        /// <param name="connString"></param>
        /// <param name="loginName"></param>
        public static AccountsResult CreateWindowsLogin(string connString, string loginName)
        {
            return (AccountsResult)SqlHandler.ExecuteScalar<int>(connString, SqlCommands.CreateWindowsLogin, 
                new SqlParameter[] { new SqlParameter("loginName", loginName) });
        }

        /// <summary>
        /// Adds an existing user to the TFSIPEXEC role of the named DB
        /// </summary>
        /// <param name="connString"></param>
        /// <param name="account"></param>
        public static AccountsResult AddAccountToTFSIPEXECRole(string connString, string account)
        {
            return (AccountsResult)SqlHandler.ExecuteScalar<int>(connString, SqlCommands.AddAccountToTFSIPEXECRole,
                new SqlParameter[] { new SqlParameter("account", account) });
        }

        /// <summary>
        /// Removes an existing user from the TFSIPEXEC role of the named DB
        /// </summary>
        /// <param name="connString"></param>
        /// <param name="account"></param>
        public static AccountsResult RemoveAccountFromTFSIPEXECRole(string connString, string account)
        {
            return (AccountsResult)SqlHandler.ExecuteScalar<int>(connString, SqlCommands.RemoveAccountFromTFSIPEXECRole,
                new SqlParameter[] { new SqlParameter("account", account) });
        }
    }
}
