// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit.WCFServices;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// This class provides a mechnaism to manage the configuration of a session group.
    /// </summary>
    public class SessionGroupConfigurationManager
    {
        /// <summary>
        /// Determines whether a configuration can be saved.
        /// </summary>
        /// <param name="config">The configuration to be saved</param>
        /// <returns>TRUE if the configuration can be saved; FALSE otherwise.</returns>
        public static bool CanEditAndSaveConfiguration(Configuration config)
        {
            if (null == config)
            {
                throw new ArgumentNullException("config");
            }

            SessionGroupStatus groupStatus = new SessionGroupStatus(config.SessionGroupUniqueId);
            return !(groupStatus.IsLoadedForExecution);
        }

        /// <summary>
        /// Determines whether the managed configuration can be edited and saved to storage.
        /// </summary>
        public bool CanEditAndSave
        {
            get
            {
                return CanEditAndSaveConfiguration(Configuration);
            }
        }

        /// <summary>
        /// Gets the configuration instance that is managed by this manager.
        /// </summary>
        Configuration Configuration
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="config">The configuration to be managed by the new manager instance.</param>
        public SessionGroupConfigurationManager(Configuration config)
        {
            if (null == config)
            {
                throw new ArgumentNullException("config");
            }
            Configuration = config;
        }

        /// <summary>
        /// Try saving the managed configuration
        /// </summary>
        /// <param name="withCanSaveCheck"></param>
        /// <returns>The Id of the saved configuration</returns>
        public int TrySave(bool withCanSaveCheck)
        {
            if (withCanSaveCheck)
            {
                return TrySave();
            }
            else
            {
                return SaveConfigWithoutCanSaveCheck();
            }
        }

        /// <summary>
        /// Try saving the managed configuration (always check whether the configuration can be saved or not)
        /// </summary>
        /// <returns>The Id of the saved configuration</returns>
        /// <exception cref="Microsoft.TeamFoundation.Migration.BusinessModel.DuplicateConfigurationException" />
        public int TrySave()
        {
            if (CanEditAndSave)
            {
                return SaveConfigWithoutCanSaveCheck();
            }
            else
            {
                var pipeProxy = new MigrationServiceClient();
                bool sessionWasRunningBeforeSavingConfig = false;
                try
                {
                    var runningGroups = pipeProxy.GetRunningSessionGroups();
                    sessionWasRunningBeforeSavingConfig = runningGroups.Contains(Configuration.SessionGroupUniqueId);

                    if (sessionWasRunningBeforeSavingConfig)
                    {
                        pipeProxy.StopSessionGroup(Configuration.SessionGroupUniqueId);

                        if (!WaitForSessionGroupToStop(pipeProxy, TimeSpan.TicksPerMinute * 10))
                        {
                            throw new SavingUnsavableConfigurationException(Configuration);
                        }
                    }
                }
                catch (MigrationServiceEndpointNotFoundException)
                {
                    // WCF service is not active - session group shouldn't be running either
                }

                int saveResult = SaveConfigWithoutCanSaveCheck();

                if (sessionWasRunningBeforeSavingConfig)
                {
                    try
                    {
                        pipeProxy.StartSessionGroup(Configuration.SessionGroupUniqueId);
                    }
                    catch (MigrationServiceEndpointNotFoundException)
                    {
                        TraceManager.TraceInformation("Cannot restart Session Group '{0}' after updating its configuration.", 
                            Configuration.SessionGroupUniqueId.ToString());
                    }
                }

                return saveResult;
            }
        }

        private bool WaitForSessionGroupToStop(MigrationServiceClient pipeProxy, long maxWaitTimeTicks)
        {
            DateTime startTime = DateTime.Now;

            do
            {
                if (!pipeProxy.GetRunningSessionGroups().Contains(Configuration.SessionGroupUniqueId))
                {
                    return true;
                }
            }
            while ((DateTime.Now.Subtract(startTime).Ticks <= maxWaitTimeTicks));

            return false;
        }

        private int SaveConfigWithoutCanSaveCheck()
        {
            if (null == Configuration.Manager)
            {
                Configuration.Manager = new BusinessModelManager();
                if (!Configuration.Manager.IsConfigurationPersisted(Configuration))
                {
                    return Configuration.Manager.SaveDetachedConfiguration(Configuration);
                }
                else
                {
                    Configuration.Manager = null;
                    throw new DuplicateConfigurationException(MigrationToolkitResources.ErrorMultiSessionGroupConfigWithSameId);
                }
            }
            else
            {
                if (!Configuration.Manager.IsConfigurationPersisted(Configuration))
                {
                    return Save();
                }
                else
                {
                    throw new DuplicateConfigurationException(
                        MigrationToolkitResources.ErrorMultiSessionGroupConfigWithSameId);
                }
            }
        }

        /// <summary>
        /// Save the changes to this configuration, usu. resulting in a new version of the config persisted in DB
        /// A new configuration UniqueId needs to be assigned to this instance, otherwise an exception will be thrown
        /// </summary>
        /// <returns>The Id of the saved configuration</returns>
        /// <exception cref="Microsoft.TeamFoundation.Migration.BusinessModel.DuplicateConfigurationException" />
        private int Save()
        {
            if (null != Configuration.Manager)
            {
                return Configuration.Manager.SaveChanges();
            }
            else
            {
                Configuration.Manager = new BusinessModelManager();
                return Configuration.Manager.SaveDetachedConfiguration(Configuration);
            }
        }
    }
}
