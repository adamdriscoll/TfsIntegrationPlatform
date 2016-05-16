// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace Microsoft.TeamFoundation.Migration.TfsIntegrationJobService
{
    class SizeLimitTextWriterTraceListener : TraceListener
    {
        private const string TraceFileSuffix = ".log";

        private string m_traceDirectory;
        private string m_traceFilePrefix;
        private int m_traceFilePartIndex = 0;
        private string m_traceFilePath;
        private object m_traceFileLock = new object();
        private StreamWriter m_fileStreamWriter;

        public SizeLimitTextWriterTraceListener()
        {
            InitTraceFile();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && null != m_fileStreamWriter)
            {
                DisposeTraceFileStream();
            }

            base.Dispose(disposing);
        }

        public override void Write(string message)
        {
            lock (m_traceFileLock)
            {
                if (IsCurrentLogFileOversized)
                {
                    CreateNewTracePart();
                }

                StringBuilder sb = new StringBuilder();
                sb.Append(String.Format("[{0}] {1} ", DateTime.Now, message));
                m_fileStreamWriter.Write(sb.ToString());
            }
        }

        public override void WriteLine(string message)
        {
            lock (m_traceFileLock)
            {
                if (IsCurrentLogFileOversized)
                {
                    CreateNewTracePart();
                }

                StringBuilder sb = new StringBuilder();
                sb.Append(String.Format("[{0}] {1} ", DateTime.Now, message));
                m_fileStreamWriter.WriteLine(sb.ToString());
            }
        }

        private void CreateNewTracePart()
        {
            // note that this method should always be called when m_traceFileLock is aquired
            m_traceFilePath = GetTraceFilePath(++m_traceFilePartIndex);
            if (m_fileStreamWriter != null)
            {
                DisposeTraceFileStream();
            }

            OpenTraceFileStream();
        }

        private void DisposeTraceFileStream()
        {
            m_fileStreamWriter.Flush();
            m_fileStreamWriter.Close();
            m_fileStreamWriter.Dispose();
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
                    return CurrentLogFileSize >= AppConfig.MaxLogFileSizeInByte;
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

                OpenTraceFileStream();
            }
        }

        private void OpenTraceFileStream()
        {
            if (!File.Exists(m_traceFilePath))
            {
                m_fileStreamWriter = File.CreateText(m_traceFilePath);
            }
            else
            {
                m_fileStreamWriter = File.AppendText(m_traceFilePath);
            }
            m_fileStreamWriter.AutoFlush = true;
        }

        private string GetTraceFilePath(int partIndex)
        {
            // check file size

            string logFile = string.Format("{0}_part_{1}{2}",
                m_traceFilePrefix, partIndex.ToString(), TraceFileSuffix);

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
                }

                if (!Directory.Exists(m_traceDirectory))
                {
                    Directory.CreateDirectory(m_traceDirectory);
                }

                return m_traceDirectory;
            }
        }
    }
}
