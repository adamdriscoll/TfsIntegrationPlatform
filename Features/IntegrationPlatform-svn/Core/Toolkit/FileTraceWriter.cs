// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// FileTraceWriter reads trace from the WCF RuntimeTrace endpoint and writes the
    /// content to a log file.
    /// </summary>
    internal class FileTraceWriter : TraceWriterBase
    {
        private const string TraceFileSurfix = ".log";

        private string m_traceDirectory;
        private string m_traceFilePrefix;
        private int m_traceFilePartIndex = 0;
        private string m_traceFilePath;
        private object m_traceFileLock;

        /// <summary>
        /// Constructor
        /// </summary>
        public FileTraceWriter()
            :base()
        {
            m_traceFileLock = new object();
            InitTraceFile();
        }

        /// <summary>
        /// Name of the trace writer.
        /// </summary>
        public override string Name
        {
            get
            {
                return "FileTraceWriter";
            }
        }

        /// <summary>
        /// Write a line of the trace message to the log file.
        /// </summary>
        /// <param name="message"></param>
        public override void WriteLine(string message)
        {
            lock (m_traceFileLock)
            {
                if (IsCurrentLogFileOversized)
                {
                    m_traceFilePath = GetTraceFilePath(++m_traceFilePartIndex);
                }

                using (StreamWriter sw = File.AppendText(m_traceFilePath))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(String.Format("[{0}] {1} ", DateTime.Now, message));
                    sw.WriteLine(sb.ToString());
                }
            }
        }

        protected override void WriteTraceEntries(List<string> traceEntries)
        {
            lock (m_traceFileLock)
            {
                if (IsCurrentLogFileOversized)
                {
                    m_traceFilePath = GetTraceFilePath(++m_traceFilePartIndex);
                }

                using (StreamWriter sw = File.AppendText(m_traceFilePath))
                {
                    foreach (string line in traceEntries)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(String.Format("[{0}] {1} ", DateTime.Now, line));
                        sw.WriteLine(sb.ToString());
                    }
                }
            }
        }

        private bool IsCurrentLogFileOversized
        {
            get
            {
                if (!File.Exists(m_traceFilePath))
                {
                    return false;
                }
                else
                {
                    return CurrentLogFileSize >= GlobalConfiguration.MaxLogFileSizeInByte;
                }
            }
        }

        private long CurrentLogFileSize
        {
            get
            {
                return GetFileSizeInByte(m_traceFilePath);
            }
        }

        private long GetFileSizeInByte(string fileName)
        {
            FileInfo f = new FileInfo(fileName);
            return f.Length;
        }

        private void InitTraceFile()
        {
            string currProcName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

            m_traceFilePrefix = string.Format("_{0}_{1}",
                                           currProcName ?? "UnknownProcess",
                                           DateTime.Now.ToString("yyyy'-'MM'-'dd'_'HH'_'mm'_'ss"));
            m_traceFilePath = GetTraceFilePath(m_traceFilePartIndex);
            
            lock (m_traceFileLock)
            {
                if (!Directory.Exists(TraceDirectory))
                {
                    Directory.CreateDirectory(TraceDirectory);
                }

                if (!File.Exists(m_traceFilePath))
                {
                    using (StreamWriter sw = File.CreateText(m_traceFilePath))
                    {
                        sw.WriteLine();
                    }
                }
            }
        }

        private string GetTraceFilePath(int partIndex)
        {
            // check file size
            
            string logFile = string.Format("{0}_part_{1}{2}", 
                m_traceFilePrefix, partIndex.ToString(), TraceFileSurfix);

            return Path.Combine(TraceDirectory, logFile);
        }

        private string TraceDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(m_traceDirectory))
                {
                    m_traceDirectory = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        @"Microsoft\Team Foundation\TFS Integration Platform");

                    if (!Directory.Exists(m_traceDirectory))
                    {
                        Directory.CreateDirectory(m_traceDirectory);
                    }
                }

                return m_traceDirectory;
            }
        }
    }
}
