// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.TfsFileSystemAdapter
{
    class TfsFileSystemMigrationProvider : IMigrationProvider
    {
        ConflictManager m_conflictManagementService;
        IServiceContainer m_migrationServiceContainer;
        ChangeGroupService m_changeGroupService;
        ConfigurationService m_configurationService;
        EventService m_eventService;
        HighWaterMark<long> m_changeGroupHighWaterMark;


        #region Interface method

        /// <summary>
        /// Perform the adapter-specific initialization
        /// </summary>
        public virtual void InitializeClient()
        {
            return;
        }

        /// <summary>
        /// Initialize method. 
        /// </summary>
        public void InitializeServices(IServiceContainer migrationServiceContainer)
        {
            m_migrationServiceContainer = migrationServiceContainer;
            m_changeGroupService = (ChangeGroupService)m_migrationServiceContainer.GetService(typeof(ChangeGroupService));
            m_changeGroupService.RegisterDefaultSourceSerializer(new TfsFileSystemMigrationItemSerializer());
            Debug.Assert(m_changeGroupService != null, "Change group service is not initialized");
            m_configurationService = (ConfigurationService)m_migrationServiceContainer.GetService(typeof(ConfigurationService));
            Debug.Assert(m_configurationService != null, "Configuration service is not initialized");
            m_eventService = (EventService)m_migrationServiceContainer.GetService(typeof(EventService));
            m_changeGroupHighWaterMark = new HighWaterMark<long>("LastChangeGroupMigratedHighWaterMark");
            m_configurationService.RegisterHighWaterMarkWithSession(m_changeGroupHighWaterMark);
        }

        /// <summary>
        /// Establish the context based on the context info from the side of the pipeline
        /// </summary>
        public void EstablishContext(ChangeGroupService sourceSystemChangeGroupService)
        {
        }

        /// <summary>
        /// Registers conflict types supported by the provider.
        /// </summary>
        /// <param name="conflictManager"></param>
        public virtual void RegisterConflictTypes(ConflictManager conflictManager)
        {
            if (null == conflictManager)
            {
                throw new ArgumentNullException("conflictManager");
            }
            m_conflictManagementService = conflictManager;

            m_conflictManagementService.RegisterConflictType(new GenericConflictType());
        }

        /// <summary>
        /// Process the change group. 
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public ConversionResult ProcessChangeGroup(ChangeGroup group)
        {
            throw new MigrationException(TfsFileSystemResources.CannotMigrateToFileSystem);
        }
        #endregion


        #region IServiceProvider implementation
        /// <summary>
        /// Gets the service object of the specified type. 
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public Object GetService(Type serviceType)
        {
            return (IServiceProvider)this;
        }
        #endregion

        #region IDisposable members
        public void Dispose()
        {
        }
        #endregion
    }
}