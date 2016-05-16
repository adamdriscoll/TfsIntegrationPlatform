// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Data.EntityClient;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Transactions;

namespace Microsoft.TeamFoundation.Migration.EntityModel
{
    /// <summary>
    /// TfsMigrationConsolidatedDBEntities class
    /// </summary>
    public partial class TfsMigrationConsolidatedDBEntities
    {
        const string c_providerName = "System.Data.SqlClient";

        /// <summary>
        /// Factory method to get an instance of the Context Object.
        /// </summary>
        /// <returns></returns>
        public static TfsMigrationConsolidatedDBEntities CreateInstance()
        {
            string entityConnectionString = TfsMigrationConsolidatedDBEntities.ConnectionStringFromConfig;

            TfsMigrationConsolidatedDBEntities contextObject;
            if (!string.IsNullOrEmpty(entityConnectionString))
            {
                contextObject = new TfsMigrationConsolidatedDBEntities(entityConnectionString);
            }
            else
            {
                contextObject = new TfsMigrationConsolidatedDBEntities();
            }

            contextObject.CommandTimeout = GlobalConfiguration.CommandTimeOutValue; // set defaul timeout to 15 minutes
            return contextObject;
        }

        /// <summary>
        /// Gets the Entity Framework connection string based on the settings in the 
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
                    @"res://*/EntityModel.ConfigurationEntityModel.csdl|
                      res://*/EntityModel.ConfigurationEntityModel.ssdl|
                      res://*/EntityModel.ConfigurationEntityModel.msl";

                return entityBuilder.ToString();
            }
        }

        /// <summary>
        /// Get the Session Group configurations based on the group's unique Id.
        /// </summary>
        /// <param name="sessionGroupUniqueId"></param>
        /// <returns></returns>
        public IEnumerable<SessionGroupConfig> FindSessionGroupConfigBySessionGroupUniqueId(Guid sessionGroupUniqueId)
        {
            var sessionGroupConfigSet =
                from sgc in this.SessionGroupConfigSet
                where sgc.SessionGroup.GroupUniqueId.Equals(sessionGroupUniqueId)
                select sgc;

            return sessionGroupConfigSet;
        }

        /// <summary>
        /// Find a Provider by its reference name
        /// </summary>
        /// <param name="referenceName"></param>
        /// <returns></returns>
        public Provider FindProviderByReferenceName(Guid referenceName)
        {
            var providerSet = from p in this.ProviderSet
                              where p.ReferenceName.Equals(referenceName)
                              select p;
           
            if (providerSet.Count() < 1)
            {
                return null;
            }

            return providerSet.First();
        }

        /// <summary>
        /// Find the Event Sink setting (deprecated)
        /// </summary>
        /// <param name="engineProviderReferenceName"></param>
        /// <returns></returns>
        public EventSinkSetting FindEventSinkSetting(Guid engineProviderReferenceName)
        {
            var providerSet = from e in this.EventSinkSettingSet
                              where e.Provider.ReferenceName.Equals(engineProviderReferenceName)
                              orderby e.CreationTime descending
                              select e;

            int numOfFoundProviders = providerSet.Count<EventSinkSetting>();
            if (numOfFoundProviders > 1)
            {
                throw new InconsistentDataException(string.Format(Resource.ErrorGenericDBInconsistency, typeof(EventSinkSetting).FullName));
            }

            if (numOfFoundProviders < 1)
            {
                return null;
            }

            return providerSet.First<EventSinkSetting>();
        }

        /// <summary>
        /// Find the migration source by its unique Id.
        /// </summary>
        /// <param name="internalUniqueId"></param>
        /// <returns></returns>
        public MigrationSource FindMigrationSourceByInternalUniqueId(Guid internalUniqueId)
        {
            var migrationSourceSet = from ms in this.MigrationSourceSet
                                     where ms.UniqueId == internalUniqueId
                                     select ms;

            if (migrationSourceSet.Count<MigrationSource>() > 1)
            {
                throw new InconsistentDataException(Resource.ErrorMultiMigrationSourceWithSameUniqueId);
            }

            if (migrationSourceSet.Count<MigrationSource>() < 1)
            {
                return null;
            }

            return migrationSourceSet.First<MigrationSource>();
        }

        /// <summary>
        /// Find the latest linking setting.
        /// </summary>
        /// <param name="settingXml"></param>
        /// <returns></returns>
        public LinkingSetting FindLastestLinkingSetting(string settingXml)
        {
            var linkingSettingQuery = from l in LinkingSettingSet
                                      where l.SettingXml.Equals(settingXml)
                                      select l;

            return (linkingSettingQuery.Count() < 1) ? null : linkingSettingQuery.First();
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
