// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.TeamFoundation.Migration.Toolkit.WCFServices;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// TraceWriterBase is the base class of all WCF RuntimeTrace trace listeners.
    /// </summary>
    public abstract class TraceWriterBase
    {
        protected IRuntimeTrace m_pipeProxy;
        protected Thread m_workerThread;

        private bool m_stop;
        private object m_stopLock;

        /// <summary>
        /// Constructor.
        /// </summary>
        public TraceWriterBase()
        {
            InitializeProxy();

            m_workerThread = new Thread(Run);
            m_workerThread.IsBackground = true;
            m_workerThread.Name = Name;

            m_stop = false;
            m_stopLock = new object();
        }

        /// <summary>
        /// The thread where this trace writer executes.
        /// </summary>
        public Thread TracerThread
        {
            get
            {
                return m_workerThread;
            }
        }

        /// <summary>
        /// Starts listening to the trace.
        /// </summary>
        public virtual void StartListening()
        {
            m_workerThread.Start();
        }

        /// <summary>
        /// Stops listening to the trace.
        /// </summary>
        public virtual void StopListening()
        {
            StopRequested = true;
        }

        protected bool StopRequested
        {
            get
            {
                lock (m_stopLock)
                {
                    return m_stop;
                }
            }
            set
            {
                lock (m_stopLock)
                {
                    m_stop = value;
                }
            }
        }

        /// <summary>
        /// Writes a line of the trace.
        /// </summary>
        /// <param name="message"></param>
        public abstract void WriteLine(string message);

        /// <summary>
        /// The name of this trace writer.
        /// </summary>
        public abstract string Name { get; }

        protected abstract void WriteTraceEntries(List<string> traceEntries);
        
        protected void InitializeProxy()
        {
            m_pipeProxy = new RuntimeTraceClient();
        }

        private void Run()
        {
            List<string> traceEntries = new List<string>();

            while (true)
            {
                try
                {
                    traceEntries.Clear();
                    traceEntries.AddRange(m_pipeProxy.GetTraceMessages());

                    WriteTraceEntries(traceEntries);
                }
                catch (Exception)
                {
                    // Ugly, but this approach lets us start the test app before the endpoint is ready to service requests.
                    InitializeProxy();
                }

                if (StopRequested)
                {
                    return;
                }

                Thread.Sleep(1000);
            }
        }
    }
}
