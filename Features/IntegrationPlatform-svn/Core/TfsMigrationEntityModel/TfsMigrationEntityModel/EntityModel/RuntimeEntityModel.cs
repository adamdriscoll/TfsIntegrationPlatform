// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Data.EntityClient;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Transactions;

namespace Microsoft.TeamFoundation.Migration.EntityModel
{
    /// <summary>
    /// RuntimeEntityModel class
    /// </summary>
    public partial class RuntimeEntityModel
    {
        const string c_providerName = "System.Data.SqlClient";

        /// <summary>
        /// Factory method to create a new instance of the Context Object.
        /// </summary>
        /// <returns></returns>
        public static RuntimeEntityModel CreateInstance()
        {
            string entityConnectionString = RuntimeEntityModel.ConnectionStringFromConfig;

            RuntimeEntityModel contextObject;
            if (!string.IsNullOrEmpty(entityConnectionString))
            {
                contextObject = new RuntimeEntityModel(entityConnectionString);
            }
            else
            {
                contextObject = new RuntimeEntityModel();
            }

            contextObject.CommandTimeout = GlobalConfiguration.CommandTimeOutValue; // set defaul timeout to 15 minutes
            return contextObject;
        }

        /// <summary>
        /// Gets the Entity Framework connection string based on the setting in the
        /// global configuration file.
        /// </summary>
        public static string ConnectionStringFromConfig
        {
            get
            {
                string dbConnString = GlobalConfiguration.TfsMigrationDbConnectionString;
                if (string.IsNullOrEmpty(dbConnString))
                {
                    return string.Empty;
                }

                EntityConnectionStringBuilder entityBuilder =
                    new EntityConnectionStringBuilder();

                entityBuilder.Provider = c_providerName;
                entityBuilder.ProviderConnectionString = dbConnString;
                entityBuilder.Metadata =
                    @"res://*/EntityModel.RuntimeEntityModel.csdl|
                      res://*/EntityModel.RuntimeEntityModel.ssdl|
                      res://*/EntityModel.RuntimeEntityModel.msl";

                return entityBuilder.ToString();
            }
        }

        /// <summary>
        /// Try saving changes to the entity model.
        /// </summary>
        public void TrySaveChanges()
        {
            if (!CommandTimeout.HasValue)
            {
                CommandTimeout = 900;
                Trace.TraceInformation(
                    "Saving changes timed out - increasing timeout value to {0} seconds and retry.",
                    CommandTimeout.Value);
            }

            int numberOfRetries = 3;

            TransactionOptions transactionOption = new TransactionOptions();
            transactionOption.IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted;

            while (numberOfRetries > 0)
            {
                try
                {
                    using (TransactionScope scope = new TransactionScope(TransactionScopeOption.RequiresNew, transactionOption))
                    {
                        SaveChanges(false);
                        AcceptAllChanges();
                        scope.Complete();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning(ex.Message);
                    if ((!IsTimeoutException(ex)) || numberOfRetries <= 0)
                    {
                        throw;
                    }
                    CommandTimeout = CommandTimeout * 3;
                    Trace.TraceInformation(
                        "Saving changes timed out - increasing timeout value to {0} seconds and retry.",
                        CommandTimeout.Value);
                    --numberOfRetries;
                }
            }
            return;
        }

        private bool IsTimeoutException(Exception ex)
        {
            if (null != ex.InnerException
                && ex.InnerException is SqlException
                && ex.InnerException.Message.Contains("Timeout expired"))
            {
                return true;
            }

            if (ex is SqlException
                && ex.Message.Contains("Timeout expired"))
            {
                return true;
            }

            return false;
        }
    }
}
