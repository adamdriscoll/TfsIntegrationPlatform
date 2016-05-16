// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.ComponentModel.Design;
using System.ServiceModel;
using Microsoft.TeamFoundation.Migration.Toolkit.WCFServices;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    public class MigrationServiceManager : IServiceProvider
    {
        private static MigrationServiceManager m_instance;
        private ServiceContainer m_serviceContainer;

        private ServiceHost m_migrationServiceHost;
        private ServiceHost m_runtimeTraceHost;
        private FileTraceWriter m_writer;

        public static MigrationServiceManager GetInstance()
        {
            if (m_instance == null)
            {
                m_instance = new MigrationServiceManager();
            }
            return m_instance;
        }

        protected MigrationServiceManager()
        {
            TryLoadMigrationService();

            m_serviceContainer = new ServiceContainer();

            IMigrationService migrationServiceProxy = new MigrationServiceClient();
            m_serviceContainer.AddService(typeof(IMigrationService), migrationServiceProxy);

            IRuntimeTrace runtimeTraceProxy = new RuntimeTraceClient();
            m_serviceContainer.AddService(typeof(IRuntimeTrace), runtimeTraceProxy);

            m_writer = new FileTraceWriter();
            m_writer.StartListening();
        }

        private void TryLoadMigrationService()
        {
            // Host MigrationService service locally
            if (!GlobalConfiguration.UseWindowsService)
            {
                try
                {
                    m_migrationServiceHost = new CustomConfigServiceHost(MigrationService.GetInstance());
                    m_migrationServiceHost.Open();
                }
                catch (AddressAlreadyInUseException)
                {
                    TraceManager.TraceWarning("Migration service has already been hosted - self-hosting is cancelled.");
                }

                try
                {
                    m_runtimeTraceHost = new CustomConfigServiceHost(typeof(RuntimeTrace));
                    m_runtimeTraceHost.Open();
                }
                catch (AddressAlreadyInUseException)
                {
                    TraceManager.TraceWarning("RuntimeTrace service has already been hosted - self-hosting is cancelled.");
                }
            }
        }

        #region IServiceProvider Members
     
   public object GetService(Type serviceType)
        {
            return m_serviceContainer.GetService(serviceType);
        }

        #endregion
    }
}
