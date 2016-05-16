// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Transactions;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public static class SessionVariables
    {
        public static string GetSessionVariable(string sessionId, string variableName)
        {
            using (SqlConnection conn = DataAccessManager.Current.GetSqlConnection())
            {
                object hwmObj = null;

                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Old_prc_LoadSessionVariable";

                    cmd.Parameters.Add("@SessionId", SqlDbType.NVarChar).Value = sessionId;
                    cmd.Parameters.Add("@Variable", SqlDbType.NVarChar).Value = variableName;

                    conn.Open();
                    hwmObj = cmd.ExecuteScalar();
                }

                if (hwmObj != null)
                {
                    return hwmObj.ToString();
                }
                else
                {
                    return null;
                }
            }
        }

        public static void SaveSessionVariable(string sessionId, string variableName, string value)
        {
            using (SqlConnection conn = DataAccessManager.Current.GetSqlConnection())
            {
                conn.Open();
                SaveSessionVariable(sessionId, variableName, value, conn);
            }
        }

        public static void SaveSessionVariable(string sessionId, string variableName, string value, SqlConnection conn)
        {
            if (conn == null)
            {
                throw new ArgumentNullException("conn");
            }

            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Old_prc_UpdateSessionVariable";

                cmd.Parameters.Add("@SessionId", SqlDbType.NVarChar).Value = sessionId;
                cmd.Parameters.Add("@Variable", SqlDbType.NVarChar).Value = variableName;
                cmd.Parameters.Add("@Value", SqlDbType.NVarChar).Value = value;

                int rowCount = cmd.ExecuteNonQuery();

                Debug.Assert(rowCount == 1);
            }
        }
    }
}
