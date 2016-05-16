// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.TfsIntegrationJobService
{
    static class Program
    {
        /// <summary>
        /// The Trace Switch to control the trace level of the service and its hosted jobs.
        /// </summary>
        public const string TfsIntegrationJobServiceTraceSwitch = "TfsIntegrationJobsTraceSwitch";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ChangeWorkingDirToExeLocation();

            InitTraces();         

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
			{ 
				new TfsIntegrationJobService() 
			};
            ServiceBase.Run(ServicesToRun);
        }

        private static void InitTraces()
        {
            Trace.AutoFlush = true;

            Trace.Listeners.Add(new SizeLimitTextWriterTraceListener());
        }

        private static void ChangeWorkingDirToExeLocation()
        {
            Environment.CurrentDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
        }
    }
}
