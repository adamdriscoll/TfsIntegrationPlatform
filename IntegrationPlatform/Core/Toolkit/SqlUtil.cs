// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public static class SqlUtil
    {
        private static readonly string dbExtPropQuery =
@"SELECT name, value 
  FROM fn_listextendedproperty(default, default, default, default, default, default, default)";


        public static bool DoesProcExist(string procName, SqlConnection conn)
        {
            if (conn == null)
            {
                throw new ArgumentNullException("conn");
            }

            string query = string.Format(
                CultureInfo.InvariantCulture,
                "SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N\'[dbo].[{0}]\') AND type in (N\'P\', N\'PC\')",
                procName);

            using (SqlCommand cmd = conn.CreateCommand())
            {
                if ((conn.State & ConnectionState.Open) == 0)
                {
                    conn.Open();
                }

                cmd.CommandText = query;
                cmd.CommandType = CommandType.Text;

                object result = (string)cmd.ExecuteScalar();

                return (result != null);
            }
        }

        public static void DropProc(string procName, SqlConnection conn)
        {
            if (conn == null)
            {
                throw new ArgumentNullException("conn");
            }

            if (DoesProcExist(procName, conn))
            {
                string query = string.Format(
                    CultureInfo.InvariantCulture,
                    "DROP PROCEDURE {0}",
                    procName);

                using (SqlCommand cmd = conn.CreateCommand())
                {
                    if ((conn.State & ConnectionState.Open) == 0)
                    {
                        conn.Open();
                    }

                    cmd.CommandText = query;
                    cmd.CommandType = CommandType.Text;

                    cmd.ExecuteNonQuery();
                }
            }
        }


        /// <summary>
        /// Loads a SQL script from a name resource stream and executes the SQL on the remote server
        /// </summary>
        /// <param name="resourceStream">The stream to load and Execute</param>
        public static void ExecuteNamedResource(string resourceStream, SqlConnection conn)
        {
            if (conn == null)
            {
                throw new ArgumentNullException("conn");
            }

            if (string.IsNullOrEmpty(resourceStream))
            {
                throw new ArgumentNullException("resourceStream");
            }

            using (Stream schemaStream = Assembly.GetCallingAssembly().GetManifestResourceStream(resourceStream))
            {
                Debug.Assert(schemaStream != null);

                using (StreamReader schemaReader = new StreamReader(schemaStream))
                {
                    while (!schemaReader.EndOfStream)
                    {

                        string sqlString;
                        StringBuilder sqlBuilder = new StringBuilder();

                        while (true)
                        {
                            string current = schemaReader.ReadLine();
                            if (current == null || string.Compare(current.Trim(), "GO", StringComparison.InvariantCultureIgnoreCase) == 0)
                            {
                                break;
                            }

                            sqlBuilder.AppendLine(current);
                        }

                        sqlString = sqlBuilder.ToString();

                        if (!string.IsNullOrEmpty(sqlString))
                        {
                            using (SqlCommand cmd = conn.CreateCommand())
                            {
                                if ((conn.State & ConnectionState.Open) == 0)
                                {
                                    conn.Open();
                                }

                                cmd.CommandText = sqlString;
                                cmd.CommandType = CommandType.Text;

                                try
                                {
                                    cmd.ExecuteNonQuery();
                                }
                                catch (SqlException se)
                                {
                                    throw new InitializationException(
                                        string.Format(MigrationToolkitResources.Culture,
                                        MigrationToolkitResources.ErrorExecutingNamedSQLResource,
                                        se.Message), se);
                                }
                            }
                        }
                    }
                }
            }
        }

        internal static bool DoesDbExist(SqlConnection conn, string database)
        {
            using (SqlCommand cmd = conn.CreateCommand())
            {
                // prefer this to a where clause as this is not prone to injection attacks
                cmd.CommandText = "SELECT name FROM sys.databases";
                cmd.CommandType = CommandType.Text;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string dbName = reader.GetString(0);
                        if (string.Compare(dbName, database, true, CultureInfo.CurrentCulture) == 0)
                        {
                            // the database already exists - return
                            return true;
                        }
                    }
                }
            }

            return false;
        }


        internal static void CreateDB(SqlConnection conn, string dbName)
        {
            // TODO_226442 : validate re: SQL injection on Database name
            string query = string.Format(
                CultureInfo.InvariantCulture,
                "CREATE DATABASE [{0}]",
                dbName);

            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = query;
                cmd.ExecuteNonQuery();
            }
        }

        internal static void DropDb(SqlConnection conn, string dbName)
        {
            // TODO_226442 : validate re: SQL injection on Database name
            string query = string.Format(
                CultureInfo.InvariantCulture,
                "DROP DATABASE [{0}]",
                dbName);

            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = query;
                cmd.ExecuteNonQuery();
            }
        }

        internal static void ValidateDBSchemaVersion(string connStr)
        {
            SqlDataAdapter da = new SqlDataAdapter(dbExtPropQuery, connStr);
            DataTable dt = new DataTable();
            da.Fill(dt);

            for (int i = 0; i < dt.Rows.Count; ++i)
            {
                if (Constants.DBExtProp_ReferenceName.Equals(dt.Rows[i][0].ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        Guid dbSchemaRefName = new Guid(dt.Rows[i][1].ToString());
                        if (!dbSchemaRefName.Equals(new Guid(Constants.DBExtProp_ReferenceNameGuidStr)))
                        {
                            throw new DBSchemaValidationException(MigrationToolkitResources.SQLMismatchingDBSchema,
                                                                 dbSchemaRefName.ToString(),
                                                                 Constants.FrameWorkVersion);
                        }

                        return;
                    }
                    catch (Exception ex)
                    {
                        if (ex is DBSchemaValidationException)
                        {
                            throw;
                        }

                        throw new DBSchemaValidationException(MigrationToolkitResources.SQLInvalidDBReferenceName, ex);
                    }
                }
            }

            throw new DBSchemaValidationException(MigrationToolkitResources.SQLInvalidDBReferenceName);
        }
    }
}
