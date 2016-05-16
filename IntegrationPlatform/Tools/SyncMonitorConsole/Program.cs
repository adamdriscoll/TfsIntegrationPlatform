// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
using System;
using Microsoft.TeamFoundation.Migration.Toolkit.SyncMonitor;

namespace SyncMonitorConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            // private const int c_defaultMonitoringInterval = 60;
            const int c_defaultPollInterval = 60;

            bool verbose = false;
            int pollInterval = c_defaultPollInterval;
            foreach (string arg in args)
            {
                if (arg.Equals("/?", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine(SyncMonitorConsoleResources.CommandLineUsage);
                    Environment.Exit(0);
                }

                if ((arg.Equals("/v", StringComparison.InvariantCultureIgnoreCase)
                    || (arg.Equals("/verbose", StringComparison.InvariantCultureIgnoreCase))))
                {
                    verbose = true;
                    continue;
                }
                
                if ((arg.StartsWith("/i=", StringComparison.InvariantCultureIgnoreCase)
                    || (arg.StartsWith("/internal=", StringComparison.InvariantCultureIgnoreCase))))
                {
                    try
                    {
                        int valuePos = arg.IndexOf('=') + 1;
                        pollInterval = int.Parse(arg.Substring(valuePos));
                    }
                    catch(Exception)
                    {
                        Console.WriteLine(SyncMonitorConsoleResources.CommandLineUsage);
                        Environment.Exit(1);
                    }
                    continue;
                }

                // Unexpected argument
                Console.WriteLine(SyncMonitorConsoleResources.CommandLineUsage);
                Environment.Exit(1);
            }

            MonitorWatcher watcher = new MonitorWatcher(false, verbose, pollInterval);
            watcher.Start();

            Console.WriteLine("Press ENTER to stop monitor.");
            Console.ReadLine();

            watcher.Stop();
        }
    }
}
