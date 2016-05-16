// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace InstallationCA
{
    public static class SqlHandler
    {
        public static T ExecuteScalar<T>(string connectionString, string sqlStatement, SqlParameter[] parameters)
        {
            using (SqlConnection connection = new SqlConnection(
               connectionString))
            {
                SqlCommand command = new SqlCommand(sqlStatement, connection);

                if (null != parameters)
                {
                    foreach (var p in parameters)
                    {
                        command.Parameters.Add(p);
                    }
                }
                command.Connection.Open();
                object result = command.ExecuteScalar();
                if (result == null)
                {
                    // todo
                    throw new InvalidOperationException();
                }

                return (T)result;
            }
        }
    }
}
