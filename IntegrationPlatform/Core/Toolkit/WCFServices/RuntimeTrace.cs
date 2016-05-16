// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;
using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit.WCFServices
{
    /// <summary>
    /// WCF service endpoint for RuntimeTrace.
    /// </summary>
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class RuntimeTrace : IRuntimeTrace, IDisposable
    {
        private static RuntimeTrace s_instance;
        private static int MaxLineLength = 8000;
        private static string AbridgedNotes = "... (abridged)";
        private const int c_maxTraceEntries = 100;

        TextWriterTraceListener m_listener;
        private MemoryQueueStream m_blockingStream;

        /// <summary>
        /// Constructor.
        /// </summary>
        public RuntimeTrace()
        {
            // Note that a new RuntimeTrace object is created for each
            // consumer.  That means each caller has its own stream with 
            // its own state.
            m_blockingStream = new MemoryQueueStream();
            m_listener = new TextWriterTraceListener(m_blockingStream);
            Trace.Listeners.Add(m_listener);

            TraceManager.TraceInformation("Creating RuntimeTrace service with listener ID: {0}, stream ID: {1}, numListeners: {2}", 
                (m_listener != null) ? m_listener.GetHashCode().ToString() : "none",
                (m_blockingStream != null) ? m_blockingStream.GetHashCode().ToString() : "none",
                Trace.Listeners.Count);
        }

        /// <summary>
        /// Gets lines of trace messages.
        /// </summary>
        /// <returns>Lines of trace messages in an array of strings.</returns>
        public string[] GetTraceMessages()
        {
            List<string> traceLogEntries = new List<string>(c_maxTraceEntries);

            m_listener.Flush();

            using (StreamReader sr = new StreamReader(m_blockingStream))
            {
                int numLines = 0;
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Length >= MaxLineLength)
                    {
                        line = line.Substring(0, MaxLineLength - AbridgedNotes.Length) + AbridgedNotes;
                    }
                    traceLogEntries.Add(line);

                    if (++numLines >= c_maxTraceEntries)
                    {
                        break;
                    }
                }
            }
            
            return traceLogEntries.ToArray();
        }

        /// <summary>
        /// Gets a singleton object of this class.
        /// </summary>
        /// <returns></returns>
        public static object GetInstance()
        {
            if (s_instance == null)
            {
                TraceManager.TraceInformation("RuntimeTrace: GetInstance: Creating new RuntimeTrace instance");
                s_instance = new RuntimeTrace();
            }
            return s_instance;
        }

        #region IDisposable Members

        public void Dispose()
        {
            string listenerId = (m_listener != null) ? m_listener.GetHashCode().ToString() : "none";
            string streamId = (m_blockingStream != null) ? m_blockingStream.GetHashCode().ToString() : "none";
            int numListenersBeforeRemoval = Trace.Listeners.Count;
            
            if (m_listener != null)
            {
                Trace.Listeners.Remove(m_listener);
                m_listener = null;
            }

            int numListenersAfterRemoval = Trace.Listeners.Count;

            TraceManager.TraceInformation("Disposing RuntimeTrace service with listener ID: {0}, stream ID: {1}, numListeners (before removal): {2}, numListeners (after removal): {3} ",
                listenerId,
                streamId,
                numListenersBeforeRemoval,
                numListenersAfterRemoval);
        }

        #endregion
    }
}
