// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading;
using System.Xml.Schema;
using Microsoft.TeamFoundation.Migration;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.WCFServices;

namespace MigrationConsole
{
    public class Program
    {
        static bool s_launchPersistedSessionGroups;
        static string s_configFilePath;
        static string s_minutesTimeout;
        static bool s_useWindowsService;
        static CommandSwitch s_commandSwitch = new CommandSwitch();
        static int s_WCFOperationTimeoutInSecs;

        static Guid s_sessionGroupUniqueId;
        static Guid s_sessionUniqueId;
        static bool s_launchSingleSession = false;
        static bool s_mergeBackSingleSession = false;

        const int DefaultWCFOperationTimeoutInSecs = 3600;

        static ServiceHost s_migrationServiceHost;
        static ServiceHost s_runtimeTraceHost;

        static TraceMessage s_traceMessage;
        static List<TraceWriterBase> s_traceWriters;
        
        static readonly string StartSingleSessionCmd = "StartStandaloneSession";
        static readonly string MergeSingleSessionCmd = "MergeStandaloneSession";

        public delegate void TraceMessage(string message);

        static Program()
        {
            s_launchPersistedSessionGroups = false;
            s_configFilePath = s_minutesTimeout = string.Empty;
            
            System.Configuration.Configuration config =
                ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);          

            var settingEntry = config.AppSettings.Settings["WCFOperationTimeoutInSecs"];
            if (null == settingEntry || !int.TryParse(settingEntry.Value, out s_WCFOperationTimeoutInSecs))
            {
                // default to 1 hour
                s_WCFOperationTimeoutInSecs = DefaultWCFOperationTimeoutInSecs;
            }

            s_useWindowsService = GlobalConfiguration.UseWindowsService;

            // init trace writers
            s_traceWriters = new List<TraceWriterBase>(2);
            s_traceWriters.Add(new ConsoleTraceWriter());

            if (!s_useWindowsService)
            {
                // in the case of windows service hosting
                // the trace file is written by the windows service process
                s_traceWriters.Add(new FileTraceWriter());
            }

            foreach (var traceWriter in s_traceWriters)
            {
                s_traceMessage += traceWriter.WriteLine;
            }
        }

        public static void Trace(string message)
        {
            if (null != s_traceMessage)
            {
                s_traceMessage(message);
            }
        }

        public static void Main(string[] args)
        {
            try
            {
                Trace("MigrationConsole started...");

                ProcessCommandLineOptions(args);

                TryLocalHostMigrationService();

                ListenToTraces();

                if (s_launchPersistedSessionGroups)
                {
                    LaunchActiveSessionGroupsInDB();
                }
                else if (s_launchSingleSession)
                {
                    LaunchSingleSessionInDB();
                }
                else if (s_mergeBackSingleSession)
                {
                    MergeBackSingleSession();
                }
                else
                {
                    LaunchSessionGroupFromConfigFile();
                }
            }
            catch (MigrationServiceEndpointNotFoundException)
            {
                if (s_useWindowsService)
                {
                    Trace(string.Format(
                        "The migration service does not appear to be hosted in the Windows Service '{0}'.",
                        Constants.TfsIntegrationServiceName));
                }
                else
                {
                    Trace("The migration service does not appear to be properly hosted.");
                }

                // todo trace level for this console app should be configurable
                // todo need to avoid sharing the trace listerners, which are dedicated to toolkit traces
                /*
                 * uncomment the following two lines for verbose trace
                 * 
                Trace("Please refer to the following exception details to diagnoze the problem:");
                Trace(typeof(EndpointNotFoundException).ToString() + ": " + e.InnerException.Message);
                 */
            }
            catch (NoActiveSessionGroupException)
            {
                Trace(MigrationConsoleResources.WarningNoActiveSessionGroups);
            }
            catch (ConfigurationSchemaViolationException configSchemaViolationEx)
            {
                Trace("Error - The configuration does not conform with the schema.");
                if (null != configSchemaViolationEx.ConfigurationValidationResult)
                {
                    Trace(configSchemaViolationEx.ConfigurationValidationResult.ToString());
                }
            }
            catch (ConfigurationBusinessRuleViolationException brViolationEx)
            {
                Trace("Error - The configuration does not pass the business rule evaluation.");
                if (null != brViolationEx.ConfigurationValidationResult)
                {
                    Trace(brViolationEx.ConfigurationValidationResult.ToString());
                }
            }
            catch (Exception ex)
            {
                Trace(ex.ToString());
                if (ex.InnerException != null)
                {
                    Trace(ex.InnerException.ToString());
                }
            }
            finally
            {
                foreach (var traceWriter in s_traceWriters)
                {
                    traceWriter.StopListening();
                }

                Thread.Sleep(2000);

                foreach (var traceWriter in s_traceWriters)
                {
                    traceWriter.TracerThread.Join();
                }

                if (!s_useWindowsService)
                {
                    Console.WriteLine("Closing migration and runtime service host...");
                    if (null != s_migrationServiceHost) s_migrationServiceHost.Close();
                    if (null != s_runtimeTraceHost) s_runtimeTraceHost.Close();
                }
                Trace("MigrationConsole completed...");
            }
        }

        static internal int WCFOperationTimeoutInSecs
        {
            get
            {
                return s_WCFOperationTimeoutInSecs;
            }
        }

        private static void ListenToTraces()
        {
            foreach (var traceWriter in s_traceWriters)
            {
                traceWriter.StartListening();
            }
        }

        private static void TryLocalHostMigrationService()
        {
            // Host MigrationService service locally
            if (!s_useWindowsService)
            {
                try
                {
                    s_migrationServiceHost = new CustomConfigServiceHost(MigrationService.GetInstance());
                    s_migrationServiceHost.Open();
                }
                catch (AddressAlreadyInUseException)
                {
                    Trace("Migration service has already been hosted - self-hosting is cancelled.");
                }

                try
                {
                    s_runtimeTraceHost = new CustomConfigServiceHost(typeof(RuntimeTrace));
                    s_runtimeTraceHost.Open();
                }
                catch (AddressAlreadyInUseException)
                {
                    Trace("RuntimeTrace service has already been hosted - self-hosting is cancelled.");
                }
            }
        }

        private static void LaunchActiveSessionGroupsInDB()
        {
            MigrationApp app = new MigrationApp(s_commandSwitch);
            KickoffMigration(app);
        }

        private static void LaunchSessionGroupFromConfigFile()
        {
            Debug.Assert(!string.IsNullOrEmpty(s_configFilePath));

            MigrationApp app = new MigrationApp(s_configFilePath, s_commandSwitch);
            KickoffMigration(app);
        }

        private static void LaunchSingleSessionInDB()
        {
            MigrationApp app = new MigrationApp(s_commandSwitch);
            app.StartSingleSession = true;
            app.SingleSessionStartInfo = new SingleSessionStartInfo(s_sessionGroupUniqueId, s_sessionUniqueId);
            KickoffMigration(app);
        }
        
        private static void MergeBackSingleSession()
        {
            MigrationApp.MergebackStandAloneSessionInGroup(s_sessionGroupUniqueId, s_sessionUniqueId);
        }

        private static void KickoffMigration(MigrationApp app)
        {
            app.Start();
        }

        private static void ProcessCommandLineOptions(
            string[] args)
        {
            if (args.Length > 5)
            {
                PrintHelp();
                Environment.Exit(0);
            }

            List<string> processedArgs = new List<string>();

            // Pre-process args to strip off all switches. 
            foreach (string arg in args)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals("/IgnoreUnmappedPath", arg))
                {
                    s_commandSwitch.IgnoreUnmappedPath = true;
                    s_commandSwitch.IgnoreUnmappedPathReason = 
                        string.Format("Commandline switch /IgnoreUnmappedPath was used on {0}", DateTime.Now);
                }
                else
                {
                    processedArgs.Add(arg);
                }
            }

            args = processedArgs.ToArray();

            switch (args.Length)
            {
                case 0:
                    s_launchPersistedSessionGroups = true;
                    break;
                case 1:
                    if (StringComparer.OrdinalIgnoreCase.Equals("/h", args[0])
                        || StringComparer.OrdinalIgnoreCase.Equals("/help", args[0])
                        || StringComparer.OrdinalIgnoreCase.Equals("/?", args[0]))
                    {
                        PrintHelp();
                        Environment.Exit(0);
                    }
                    else
                    {
                        s_configFilePath = args[0];
                    }
                    break;
                case 2:
                    s_configFilePath = args[0];
                    s_minutesTimeout = args[1];
                    break;
                case 3:
                    if (args[0].Equals(StartSingleSessionCmd))
                    {
                        s_launchSingleSession = true;
                    }
                    else if (args[0].Equals(MergeSingleSessionCmd))
                    {
                        s_mergeBackSingleSession = true;
                    }
                    else
                    {
                        PrintHelp();
                        Environment.Exit(0);
                    }
                    try
                    {
                        s_sessionGroupUniqueId = new Guid(args[1]);
                        s_sessionUniqueId = new Guid(args[2]);                        
                    }
                    catch (Exception)
                    {
                        PrintHelp();
                        Environment.Exit(0);
                    }
                    break;
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("\tMigrationConsole.exe [configurationFileName [minutesTimeout]] [/IgnoreUnmappedPath]");
            Console.WriteLine("\tMigrationConsole.exe StartStandaloneSession SessionGroupUniqueId SessionUniqueId [/IgnoreUnmappedPath]");
            Console.WriteLine("\tMigrationConsole.exe MergeStandaloneSession SessionGroupUniqueId SessionUniqueId [/IgnoreUnmappedPath]");
        }
    }

    /// <summary>
    /// This class wrap all commandline switches.
    /// </summary>
    public class CommandSwitch
    {
        private bool m_ignoreUnmappedPath;

        public bool CommandSwitchSet { get; private set; }

        /// <summary>
        /// For VC session, ignore unmapped path. This means change Branch to Add, skip Merge change type and change Rename to Add.
        /// </summary>
        public bool IgnoreUnmappedPath {
            get
            {
                return m_ignoreUnmappedPath;
            }
            set
            {
                m_ignoreUnmappedPath = value;
                CommandSwitchSet = true;
            }
        }

        /// <summary>
        /// The reason code for setting the IgnoreUnmappedPath switch.
        /// </summary>
        public string IgnoreUnmappedPathReason { get; set; }

        /// <summary>
        /// Default constructor, reset all switches to default value and clear reason code.
        /// </summary>
        public CommandSwitch()
        {
            IgnoreUnmappedPath = false;
            IgnoreUnmappedPathReason = string.Empty;
            CommandSwitchSet = false;
        }
    }
}
