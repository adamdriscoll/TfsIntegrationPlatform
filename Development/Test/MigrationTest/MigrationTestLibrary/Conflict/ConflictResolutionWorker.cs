// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace MigrationTestLibrary.Conflict
{
    public class ConflictResolutionEntry
    {
        public Guid Conflict { get; set; }
        public Guid Resolution { get; set; }
    }

    public class ConflictResolutionList : List<ConflictResolutionEntry>
    { }

    public class ConflictResolutionWorker
    {
        // constancts
        public const int POLLING_INTERVAL = 1000; // 1000 milliseconds

        public ConflictResolutionList ConflictResolutionList = new ConflictResolutionList();
        public ConflictResolver ConflictResolver { get; set; }
        public String Scope { get; set; }

        private Stopwatch Stopwatch { get; set; }
        private Thread Thread { get; set; }
        private bool AbortRequested { get; set; }

        public ConflictResolutionWorker()
        {
            Thread = new Thread(ThreadStart);
            Thread.Name = "ConflictResolutionWorker";
            Thread.IsBackground = true;
            TraceManager.TraceInformation("Starting ConflictResolutionWorker thread");
        }

        public void Start()
        {
            Stopwatch = Stopwatch.StartNew();
            Thread.Start();
        }

        public void Stop()
        {
            Stopwatch.Stop();
            Trace.TraceInformation("Stopping ConflictResolutionWorker thread after {0} seconds", Stopwatch.Elapsed.TotalSeconds);
            AbortRequested = true;
        }

        private void ThreadStart()
        {
            do
            {
                try
                {
                    TraceManager.TraceInformation("{0}: Sleeping {1} seconds ", Thread.Name, POLLING_INTERVAL / 1000);
                    Thread.Sleep(POLLING_INTERVAL);

                    foreach (RTConflict conflict in ConflictResolver.GetConflicts())
                    {
                        TraceManager.TraceInformation("Attempting to resolve {0}", conflict.ConflictTypeReference.Value.ReferenceName);
                        bool resolved = false;
                        foreach (ConflictResolutionEntry resolution in ConflictResolutionList)
                        {
                            if (resolution.Conflict.Equals(conflict.ConflictTypeReference.Value.ReferenceName))
                            {
                                string applicableScope = Scope;
                                if (applicableScope == null)
                                {
                                    applicableScope = conflict.ScopeHint;
                                }

                                ConflictResolver.TryResolveConflict(conflict, resolution.Resolution, applicableScope);

                                TraceManager.TraceInformation("Conflict {0} is resolved", conflict.ConflictTypeReference.Value.ReferenceName);
                                resolved = true;
                            }
                        }
                        if (!resolved)
                        {
                            TraceManager.TraceInformation("Failed to resolve conflict {0}", conflict.ConflictTypeReference.Value.ReferenceName);
                        }
                    }
                }
                catch (Exception e)
                {
                    Trace.TraceInformation("ConflictResolutionManager Caught Exception {0}", e);
                }
            } while (!AbortRequested);

            Trace.TraceInformation("Ending ConflictResolutionWorker thread.");
        }
    }
}
