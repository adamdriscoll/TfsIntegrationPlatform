// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit.WCFServices
{
    /// <summary>
    /// WCF service endpoint for RuntimeTrace.
    /// </summary>
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class RuntimeTrace : IRuntimeTrace
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
    }
}
