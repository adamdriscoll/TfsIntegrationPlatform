// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace MigrationTestLibrary
{
    public static class MigrationApp
    {
        static MigrationApp()
        {
            TraceManager.Toolkit.Level = TraceLevel.Verbose;
        }

        public static void Start(string configFileName)
        {
            Configuration config = null;

            try
            {
                using (FileStream fs = new FileStream(configFileName, FileMode.Open, FileAccess.Read))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
                    config = serializer.Deserialize(fs) as Configuration;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Failed to load the configuration file {0}", configFileName);
                Trace.TraceError(ex.ToString());
                throw ex;
            }

            Trace.TraceInformation("Loaded {0} file successfully", configFileName);

            if (config != null)
            {
                // persist to db
                try
                {
                    SessionGroupConfigurationManager configManager = new SessionGroupConfigurationManager(config);
                    configManager.TrySave(false);
                }
                catch (DuplicateConfigurationException)
                {
                    // the same configuration already exists
                    // swallow the exception
                }
                
                Guid sessionGroupGuid = config.SessionGroupUniqueId;
                config = null;

                // load the configuration from db (instead of the deserialized object from file)
                BusinessModelManager businessModelManager = new BusinessModelManager();
                config = businessModelManager.LoadConfiguration(sessionGroupGuid);

                SyncOrchestrator syncOrch = new SyncOrchestrator(config);
                syncOrch.ConstructPipelines();
                syncOrch.InitializePipelines();
                syncOrch.Start(Timeout.Infinite);
                syncOrch.BlockUntilAllSessionFinishes();
            }
        }
    }
}
