// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Win32;

namespace InstallationCA
{
    public class CustomActions
    {
        private const string INTEGRATION_DBNAME = "Tfs_IntegrationPlatform";
        public const string TfsServiceEventLogName = "Application";

        #region Immediate Custom Actions
        [CustomAction]
        public static ActionResult SetDefaultValues(Session session)
        {

            string currentWorkspaceDir = GetCurrentWorkspaceRoot(session);

            if (!string.IsNullOrEmpty(currentWorkspaceDir))
            {
                session["WORKSPACEDIR"] = currentWorkspaceDir;
            }

            SqlConnection sqlConnection = GetCurrentDBConnectionInformation(session);

            if (sqlConnection != null)
            {
                session["DATABASESERVER"] = sqlConnection.DataSource;
                session["DATABASENAME"] = sqlConnection.Database;
            }
            else
            {
                // set the defaults unless they are already set (by silent install)
                if (String.IsNullOrEmpty(session["DATABASESERVER"]))
                {
                    session["DATABASESERVER"] = GetLocalSQLInstanceName();
                }

                if (String.IsNullOrEmpty(session["DATABASENAME"]))
                {
                    session["DATABASENAME"] = INTEGRATION_DBNAME;
                }
            }

            if (string.IsNullOrEmpty(session["SERVICEACCOUNTNAME"]))
            {
                session["SERVICEACCOUNTNAME"] = WindowsIdentity.GetCurrent().Name;
            }

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult CheckExistingDBVersion(Session session)
        {
            session["CACURRENTDBCHECK"] = "fail";
            session["EXISTINGDBVERSION"] = "DBNOTFOUND";
            session["LASTERROR"] = string.Empty;

            if (IntegrationDBExists(session))
            {
                if (!GetDBSchemaVersion(session))
                {
                    // Fail: DB schema version not found
                    return ActionResult.Success;
                }
            }

            if (string.IsNullOrEmpty(session["LASTERROR"]))
            {
                session["CACURRENTDBCHECK"] = "pass";
            }

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult CheckDBPermission(Session session)
        {
            string dbServer = session["DATABASESERVER"];
            string dbName = String.Format("Tfs_IntegrationSetup{0}", Guid.NewGuid().ToString("N"));
            string serviceAccountName = session["SERVICEACCOUNTNAME"];
            session["CACHECKDBPERMISSION"] = "fail";

            if (string.IsNullOrEmpty(dbServer))
            {
                session["LASTERROR"] = ResourceStrings.MissingDatabaseServer;
                return ActionResult.Success;
            }

            if (string.IsNullOrEmpty(dbName))
            {
                session["LASTERROR"] = ResourceStrings.MissingDatabaseName;
                return ActionResult.Success;
            }

            if (string.IsNullOrEmpty(serviceAccountName))
            {
                session["LASTERROR"] = ResourceStrings.MissingServiceAccountName;
                return ActionResult.Success;
            }

            // Create a temp DB for permission check
            if (CreateTempDB(session, dbServer, dbName))
            {
                session["CACHECKDBPERMISSION"] = "pass";
                DropDB(session, dbServer, dbName, serviceAccountName);
            }

            return ActionResult.Success;
        }
        #endregion

        #region Deferred Custom Actions
        // Deferred Custom Actions get no session parameters directly.  Parameters come through 
        // packed properties set in an immediate custom action.

        [CustomAction]
        public static ActionResult CreateEventSources(Session session)
        {
            try
            {
                string[] args = UnpackArgs(session, "CreateEventSources", 2);

                CreateEventSources(args);
            }
            catch (Exception e)
            {
                Debug.Assert(false, e.Message);
            }
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult DeleteEventSources(Session session)
        {
            try
            {
                string[] args = UnpackArgs(session, "DeleteEventSources", 2);

                DeleteEventSources(args);
            }
            catch (Exception e)
            {
                Debug.Assert(false, e.Message);
            }
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult StartWindowsServices(Session session)
        {
            try
            {
                string[] args = UnpackArgs(session, "StartWindowsServices", 3);
                string integrationServiceName = args[0];
                string integrationJobServiceName = args[1];
                string installDir = args[2];

                StartWindowsServices(integrationServiceName, integrationJobServiceName, installDir);
            }
            catch (Exception e)
            {
                Debug.Assert(false, e.Message);
            }
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult CreateWorkerProcessGroup(Session session)
        {
            try
            {
                string[] args = UnpackArgs(session, "CreateWorkerProcessGroup", 3);
                string groupName = args[0];
                string groupComment = args[1];
                string accountName = args[2];

                CreateWorkerProcessGroup(groupName, groupComment, accountName);
            }
            catch (Exception e)
            {
                Debug.Assert(false, e.Message);
            }
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult GrantFullControlRights(Session session)
        {
            try
            {
                string[] args = UnpackArgs(session, "GrantFullControlRights", 2);
                string groupName = args[0];
                string targetDir = args[1];

                GrantFullControlRights(groupName, targetDir);
            }
            catch (Exception e)
            {
                Debug.Assert(false, e.Message);
            }
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult DeleteWorkerProcessGroup(Session session)
        {
            try
            {
                string[] args = UnpackArgs(session, "DeleteWorkerProcessGroup", 1);
                string groupName = args[0];

                DeleteWorkerProcessGroup(groupName);
            }
            catch (Exception e)
            {
                Debug.Assert(false, e.Message);
            }
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult CreateDatabaseRole(Session session)
        {
            try
            {
                string[] args = UnpackArgs(session, "CreateDatabaseRole", 3);
                string dbServer = args[0];
                string dbName = args[1];
                string accountName = args[2];

                CreateDatabaseRole(dbServer, dbName, accountName);
            }
            catch (Exception e)
            {
                Debug.Assert(false, e.Message);
            }
            return ActionResult.Success;
        }
        #endregion

        #region Immediate Custom Action Private Methods
        private static bool IntegrationDBExists(Session session)
        {
            string dbServer = session["DATABASESERVER"];

            SqlConnectionStringBuilder connectionStringBuilder = new SqlConnectionStringBuilder();

            if (string.IsNullOrEmpty(dbServer))
            {
                session["LASTERROR"] = ResourceStrings.MissingDatabaseName;
                return false;
            }
            else
            {
                connectionStringBuilder.DataSource = dbServer;
            }

            connectionStringBuilder.IntegratedSecurity = true;
            connectionStringBuilder.MultipleActiveResultSets = true;
            connectionStringBuilder.InitialCatalog = "master";

            using (SqlConnection conn = new SqlConnection(connectionStringBuilder.ConnectionString))
            {
                try
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = string.Format("SELECT name FROM sys.databases where name = '{0}'", INTEGRATION_DBNAME);

                        if (cmd.ExecuteReader().HasRows)
                        {
                            return true;
                        }
                    }
                }
                catch (Exception e)
                {
                    session["LASTERROR"] = string.Format(ResourceStrings.DBConnectionError, dbServer, e.Message);
                }
            }

            return false;
        }

        private static bool GetDBSchemaVersion(Session session)
        {
            const string dbExtPropQuery = @"SELECT name, value FROM fn_listextendedproperty(default, default, default, default, default, default, default)";
            const string dbExtPropRefName = "ReferenceName";

            string dbServer = session["DATABASESERVER"];
            session["EXISTINGDBVERSION"] = "DBVERSIONNOTFOUND";

            SqlConnectionStringBuilder connectionStringBuilder = new SqlConnectionStringBuilder();

            if (string.IsNullOrEmpty(dbServer))
            {
                session["LASTERROR"] = ResourceStrings.MissingDatabaseName;
            }
            else
            {
                connectionStringBuilder.DataSource = dbServer;
            }

            connectionStringBuilder.IntegratedSecurity = true;
            connectionStringBuilder.MultipleActiveResultSets = true;
            connectionStringBuilder.InitialCatalog = INTEGRATION_DBNAME;

            SqlDataAdapter da = new SqlDataAdapter(dbExtPropQuery, connectionStringBuilder.ConnectionString);
            DataTable dt = new DataTable();
            da.Fill(dt);

            for (int i = 0; i < dt.Rows.Count; ++i)
            {
                if (dbExtPropRefName.Equals(dt.Rows[i][0].ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        Guid dbSchemaRefName = new Guid(dt.Rows[i][1].ToString());
                        session["EXISTINGDBVERSION"] = dbSchemaRefName.ToString().ToUpper();
                        return true;
                    }
                    catch (Exception e)
                    {
                        session["LASTERROR"] = string.Format(ResourceStrings.DBSchemaPropertyError, e.Message);
                    }
                }
            }

            session["LASTERROR"] = ResourceStrings.DBVersionNotFound;
            return false;
        }

        private static bool CreateTempDB(Session session, string dbServer, string dbName)
        {
            bool result = false;

            SqlConnectionStringBuilder connectionStringBuilder = new SqlConnectionStringBuilder();

            if (string.IsNullOrEmpty(dbServer))
            {
                session["LASTERROR"] = ResourceStrings.MissingDatabaseName;
                return result;
            }
            else
            {
                connectionStringBuilder.DataSource = dbServer;
            }

            connectionStringBuilder.IntegratedSecurity = true;
            connectionStringBuilder.MultipleActiveResultSets = true;
            connectionStringBuilder.InitialCatalog = "master";

            using (SqlConnection conn = new SqlConnection(connectionStringBuilder.ConnectionString))
            {
                try
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = string.Format("CREATE DATABASE {0}", dbName);
                        cmd.ExecuteNonQuery();
                    }

                    result = true;
                }
                catch (Exception e)
                {
                    session["LASTERROR"] = string.Format(ResourceStrings.DBPermissionFailure, dbServer, e.Message);
                }
            }

            return result;
        }

        private static bool DropDB(Session session, string dbServer, string dbName, string accountName)
        {
            SqlConnectionStringBuilder connSB = new SqlConnectionStringBuilder();
            connSB.IntegratedSecurity = true;
            connSB.DataSource = dbServer;
            connSB.InitialCatalog = "master";

            using (SqlConnection connection = new SqlConnection(connSB.ConnectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand cmd = connection.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        StringBuilder sb = new StringBuilder();
                        sb.AppendFormat("ALTER DATABASE {0} SET SINGLE_USER WITH ROLLBACK IMMEDIATE ", dbName);
                        sb.AppendFormat("DROP DATABASE {0}", dbName);
                        cmd.CommandText = sb.ToString();
                        cmd.ExecuteNonQuery();
                    }

                    return true;
                }
                catch (Exception e)
                {
                    session["LASTERROR"] = string.Format(ResourceStrings.ChangeDBOwnerError, accountName, dbName, e.Message);
                }
            }

            return false;
        }

        private static string GetLocalSQLInstanceName()
        {
            string instanceName = string.Empty;

            try
            {
                List<string> instanceNames = DetectSQLInstanceNames();
                if (instanceNames.Count >= 1)
                {
                    instanceName = instanceNames[0];
                }
            }
            catch (Exception)
            {
            }

            if (!string.IsNullOrEmpty(instanceName))
            {
                instanceName = string.Format("localhost\\{0}", instanceName);
            }

            return instanceName;
        }

        /// <summary>
        /// Detect all SQL Server instances on the machine.
        /// </summary>
        /// <returns></returns>
        private static List<string> DetectSQLInstanceNames()
        {
            List<string> instanceNames = new List<string>();
            string correctNamespace = GetCorrectWmiNameSpace();
            if (string.Equals(correctNamespace, string.Empty))
            {
                return instanceNames;
            }
            string query = string.Format("select * from SqlServiceAdvancedProperty where SQLServiceType = 1 and PropertyName = 'instanceID'");
            ManagementObjectSearcher getSqlEngine = new ManagementObjectSearcher(correctNamespace, query);
            if (getSqlEngine.Get().Count == 0)
            {
                return instanceNames;
            }
            string instanceName = string.Empty;
            string serviceName = string.Empty;
            foreach (ManagementObject sqlEngine in getSqlEngine.Get())
            {
                serviceName = sqlEngine["ServiceName"].ToString();
                instanceName = GetInstanceNameFromServiceName(serviceName);
                instanceNames.Add(instanceName);
            }
            return instanceNames;
        }

        /// <summary>
        /// Method returns the correct SQL namespace to use to detect SQL Server instances.
        /// </summary>
        /// <returns>namespace to use to detect SQL Server instances</returns>
        private static string GetCorrectWmiNameSpace()
        {
            String wmiNamespaceToUse = "root\\Microsoft\\sqlserver";
            List<string> namespaces = new List<string>();
            try
            {
                // Enumerate all WMI instances of
                // __namespace WMI class.
                ManagementClass nsClass =
                    new ManagementClass(
                    new ManagementScope(wmiNamespaceToUse),
                    new ManagementPath("__namespace"),
                    null);
                foreach (ManagementObject ns in
                    nsClass.GetInstances())
                {
                    namespaces.Add(ns["Name"].ToString());
                }
            }
            catch (ManagementException)
            {
            }
            if (namespaces.Count > 0)
            {
                if (namespaces.Contains("ComputerManagement10"))
                {
                    //use katmai+ namespace
                    wmiNamespaceToUse = wmiNamespaceToUse + "\\ComputerManagement10";
                }
                else if (namespaces.Contains("ComputerManagement"))
                {
                    //use yukon namespace
                    wmiNamespaceToUse = wmiNamespaceToUse + "\\ComputerManagement";
                }
                else
                {
                    wmiNamespaceToUse = string.Empty;
                }
            }
            else
            {
                wmiNamespaceToUse = string.Empty;
            }
            return wmiNamespaceToUse;
        }

        /// <summary>
        /// method extracts the instance name from the service name
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        private static string GetInstanceNameFromServiceName(string serviceName)
        {
            if (!string.IsNullOrEmpty(serviceName))
            {
                if (string.Equals(serviceName, "MSSQLSERVER", StringComparison.OrdinalIgnoreCase))
                {
                    return serviceName;
                }
                else
                {
                    return serviceName.Substring(serviceName.IndexOf('$') + 1, serviceName.Length - serviceName.IndexOf('$') - 1);
                }
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// If the MigrationToolServer.config file exists on the system, examine it and extract the
        /// configured workspace root.  If the user has changed the value, this logic will preserve it 
        /// during upgrade.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private static string GetCurrentWorkspaceRoot(Session session)
        {
            string workspaceRoot = string.Empty;
            try
            {
                string filename = session["MIGRATIONTOOLSERVERSFILE"];

                // If the property is not set, the file has not been installed on the system yet.
                if (!string.IsNullOrEmpty(filename))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(filename);

                    XmlElement rootNode = doc.DocumentElement;
                    XmlNode workspace = rootNode.SelectSingleNode(@"/configuration/appSettings/add[@key='WorkSpaceRoot']");
                    workspaceRoot = workspace.Attributes["value"].Value;
                }
            }
            catch (Exception e)
            {
                session["LASTERROR"] = e.Message;
            }

            return workspaceRoot;
        }

        /// <summary>
        /// If the MigrationToolServer.config file exists on the system, examine it and extract the
        /// configured DB connection information.  These values become the initial settings for the 
        /// DB connection info in the wizard if the UI is being used or they are simply preserved
        /// in the case of a silent install.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private static SqlConnection GetCurrentDBConnectionInformation(Session session)
        {
            SqlConnection sqlConnection = null;

            try
            {
                string filename = session["MIGRATIONTOOLSERVERSFILE"];

                // If the property is not set, the file has not been installed on the system yet.
                if (!string.IsNullOrEmpty(filename))
                {

                    XmlDocument doc = new XmlDocument();
                    doc.Load(filename);

                    XmlElement rootNode = doc.DocumentElement;
                    XmlNode connectionStringNode = rootNode.SelectSingleNode(@"/configuration/connectionStrings/add[@name='TfsMigrationDBConnection']");

                    if (connectionStringNode != null)
                    {
                        string connectionString = connectionStringNode.Attributes["connectionString"].Value;

                        if (!string.IsNullOrEmpty(connectionString))
                        {
                            sqlConnection = new SqlConnection(connectionString);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                session["LASTERROR"] = e.Message;
            }

            return sqlConnection;
        }
        #endregion

        #region Deferred Custom Action Private Methods
        private static string[] UnpackArgs(Session session, string methodName, int expectedArgCount)
        {
            string args = session["CustomActionData"];
            string[] argArray = args.Split(',');

            if (argArray.Length != expectedArgCount)
            {
                throw new Exception(string.Format(ResourceStrings.DeferredCustomActionParamError, methodName, args));
            }

            foreach (string arg in argArray)
            {
                if (string.IsNullOrEmpty(arg))
                {
                    throw new Exception(string.Format(ResourceStrings.NullOrEmptyParamError, methodName, args));
                }
            }

            return argArray;
        }

        private static void CreateEventSources(string[] args)
        {
            try
            {
                foreach (string arg in args)
                {
                    if (!EventLog.SourceExists(arg))
                    {
                        EventLog.CreateEventSource(arg, TfsServiceEventLogName);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format(ResourceStrings.UnhandledExceptionError, "CreateEventSources", e.Message));
            }
        }

        private static void DeleteEventSources(string[] args)
        {
            try
            {
                foreach (string arg in args)
                {
                    if (!EventLog.SourceExists(arg))
                    {
                        EventLog.DeleteEventSource(arg);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format(ResourceStrings.UnhandledExceptionError, "DeleteEventSources", e.Message));
            }
        }

        private static void StartWindowsServices(string integrationServiceName, string integrationJobServiceName, string installDir)
        {
            try
            {
                int timeoutMilliseconds = 30000;
                string[] serviceNames = new string[] { integrationServiceName, integrationJobServiceName };

                foreach (string serviceName in serviceNames)
                {
                    ServiceController service = new ServiceController(serviceName);
                    TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format(ResourceStrings.UnhandledExceptionError, "StartWindowsServices", e.Message));
            }
        }

        private static void CreateWorkerProcessGroup(string groupName, string groupComment, string accountName)
        {
            try
            {
                WindowsGroupUtil.CreateGroup(groupName, groupComment);
                WindowsGroupUtil.AddMemberToGroup(groupName, accountName);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format(ResourceStrings.UnhandledExceptionError, "CreateWorkerProcessGroup", e.Message));
            }
        }

        private static void GrantFullControlRights(string groupName, string targetDir)
        {
            try
            {
                WindowsGroupUtil.SetGroupAcl(groupName, targetDir, System.Security.AccessControl.FileSystemRights.FullControl);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format(ResourceStrings.UnhandledExceptionError, "GrantFullControlRights", e.Message));
            }
        }

        private static void DeleteWorkerProcessGroup(string groupName)
        {
            try
            {
                WindowsGroupUtil.DeleteGroup(groupName);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format(ResourceStrings.UnhandledExceptionError, "DeleteWorkerProcessGroup", e.Message));
            }
        }

        private static void CreateDatabaseRole(string dbServer, string dbName, string accountName)
        {
            try
            {
                SqlConnectionStringBuilder connectionStringBuilder = new SqlConnectionStringBuilder();
                connectionStringBuilder.DataSource = dbServer;
                connectionStringBuilder.IntegratedSecurity = true;
                connectionStringBuilder.MultipleActiveResultSets = true;
                connectionStringBuilder.InitialCatalog = dbName;

                DBRoleUtil.CreateTFSIPEXECRole(connectionStringBuilder.ConnectionString);
                DBRoleUtil.CreateWindowsLogin(connectionStringBuilder.ConnectionString, accountName);
                DBRoleUtil.AddAccountToTFSIPEXECRole(connectionStringBuilder.ConnectionString, accountName);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format(ResourceStrings.UnhandledExceptionError, "CreateDatabaseRole", e.Message));
            }
        }
        #endregion
    }
}
