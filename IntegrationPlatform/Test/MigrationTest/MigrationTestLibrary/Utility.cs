// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.IO;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace MigrationTestLibrary
{
    class Utility
    {
        private string m_configFileName;

        public Utility(string configFileName)
        {
            m_configFileName = configFileName;
        }

        public Configuration LoadConfiguration()
        {
            if (Config != null)
            {
                return Config;
            }

            try
            {
                using (FileStream fs = new FileStream(m_configFileName, FileMode.Open, FileAccess.Read))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
                    Config = serializer.Deserialize(fs) as Configuration;
                }
            }
            catch (Exception ex)
            {
                TraceManager.TraceError("Loading the configuration file {0} failed", m_configFileName);
                TraceManager.TraceException(ex);
            }
            return Config;
        }

        public SyncOrchestrator LoadSyncOrchestrator()
        {
            TraceManager.TraceInformation("Start loading sync orchestrator...");

            if (Config == null)
            {
                LoadConfiguration();
            }

            if (Config != null)
            {
                SessionGroupConfigurationManager configManager = new SessionGroupConfigurationManager(Config);
                configManager.TrySave(false);
                SyncOrchestrator = new SyncOrchestrator(Config);
            }
            
            TraceManager.TraceInformation("Finish loading sync orchestrator...");
            return SyncOrchestrator;
        }

        public Configuration Config
        {
            get;
            set;
        }

        public SyncOrchestrator SyncOrchestrator
        {
            get;
            set;
        }

        public static void PrintWarningMsg(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        public static void PrintErrorMsg(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        public static void PrintInfoMsg(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

    }
}
