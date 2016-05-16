// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// Wrapper class for tracing. Refer class summary for more details.

//****************************************************************************
// WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING
// DO NOT USE DEBUG.ASSERT OR DEBUG.FAIL IN THIS FILE.  Use LogAssert member
// function instead. The Assert failure and Fail call are sent to trace
// listeners also. This may lead to deadlock.
//****************************************************************************

using System;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Text;

namespace Microsoft.TeamFoundation.Converters.Utility
{
    /// <summary>
    /// Wrapper class for tracing.
    ///     - Adds additional information to the trace: ThreadID, Time etc.
    ///     - Uses custom switch per source from .config file. 
    ///     - The "MinLevelForAllSource" switch can be used for logging all sources.
    ///     - The individual source's logging can be controlled using LogSources.
    ///     - By default tracing if OFF.
    ///     - Our build environment always sets the /d:TRACE so this class is
    ///       always enabled.
    ///     - The exceptions thrown by Logger are logged to System's
    ///       Application EventLog.
    ///     - We pass through exceptions thrown due to incorrect arguments.
    /// </summary>
    internal static class Logger
    {
        #region Static Constructor

        /// <summary>
        /// Static Constructor - uses LogSource to build switches array.
        /// </summary>
        static Logger()
        {
            string[] switches = Enum.GetNames(typeof(LogSource));
            m_switchSources = new TraceSwitch[switches.Length];
            m_logSourcesStr = new string[switches.Length];
            for (int i = 0; i < switches.Length; i++)
            {
                m_switchSources[i] = new TraceSwitch(switches[i], null);
                m_logSourcesStr[i] = switches[i] + ',';
            }
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Write a message if appropriate source tracing level is set. 
        /// </summary>
        /// <param name="source">The source to trace for</param>
        /// <param name="level">The level at which to write</param>
        /// <param name="message">The message to write</param>
        [Conditional("TRACE")]
        public static void Write(LogSource source, TraceLevel level, string message)
        {
            if (ShouldTrace(source, level))
            {
                WriteLine(source, m_traceLevelStr[(int)level], message);
            }
        }

        /// <summary>
        /// Write a formatted message if appropriate source tracing
        /// level is set. 
        /// </summary>
        /// <param name="source">The source to trace for</param>
        /// <param name="level">The level at which to write</param>
        /// <param name="format">The format of the message</param>
        /// <param name="args">The arguments for formatting the message</param>
        [Conditional("TRACE")]
        public static void Write(LogSource source, TraceLevel level, string format, params object[] args)
        {
            LogAssert(format != null);
            if (ShouldTrace(source, level))
            {
                WriteLine(source, m_traceLevelStr[(int)level],
                    string.Format(CultureInfo.InvariantCulture, format, args));
            }
        }

        /// <summary>
        /// Write a message if condition is true and
        /// appropriate source tracing level is set. 
        /// </summary>
        /// <param name="condition">Write message only if condition is true
        /// </param>
        /// <param name="source">The source to trace for</param>
        /// <param name="level">The level at which to write</param>
        /// <param name="message">The message to write</param>
        [Conditional("TRACE")]
        public static void WriteIf(bool condition, LogSource source, TraceLevel level, string message)
        {
            if (condition)
            {
                Write(source, level, message);
            }
        }

        /// <summary>
        /// Write a formatted message if condition is true and
        /// appropriate source tracing level is set. 
        /// </summary>
        /// <param name="condition">Write message only if condition is true
        /// </param>
        /// <param name="source">The source to trace for</param>
        /// <param name="level">The level at which to write</param>
        /// <param name="format">The format of the message</param>
        /// <param name="args">The arguments for formatting the message</param>
        [Conditional("TRACE")]
        public static void WriteIf(bool condition, LogSource source, TraceLevel level, string format,
                params object[] args)
        {
            if (condition)
            {
                Write(source, level, format, args);
            }
        }

        /// <summary>
        /// Write a exception if tracing for error is enabled
        /// </summary>
        /// <param name="source">The source to trace for</param>
        /// <param name="e">The exception to write</param>
        [Conditional("TRACE")]
        public static void WriteException(LogSource source, Exception e)
        {
            LogAssert(e != null);

            // Write only if tracing for error is enabled.
            // Done upfront to avoid perf hit.
            if (!ShouldTrace(source, TraceLevel.Error))
                return;

            // Prefix for each line
            string prefix = Environment.NewLine + '\t';

            // Format this exception
            StringBuilder message = new StringBuilder();
            message.Append(string.Format(CultureInfo.InvariantCulture,
                "Exception: {0}{1}Message: {2}{3}Stack Trace: {4}{5}Help Link: {6}",
                e.GetType(), prefix, e.Message, prefix,
                e.StackTrace, prefix, e.HelpLink));

            // If there is base exception, add that to message
            if (e.GetBaseException() != null)
            {
                message.Append(string.Format(CultureInfo.InvariantCulture,
                    "{0}BaseExceptionMessage: {1}",
                    prefix, e.GetBaseException().Message));
            }

            // If there is inner exception, add that to message
            if (e.InnerException != null)
            {
                // Format same as outer exception except
                // "InnerException" is prefixed to each line
                Exception inner = e.InnerException;
                prefix += "InnerException";
                message.Append(string.Format(CultureInfo.InvariantCulture,
                    "{0}: {1}{2} Message: {3}{4} Stack Trace: {5}{6} Help Link: {7}",
                    prefix, inner.GetType(), prefix, inner.Message, prefix,
                    inner.StackTrace, prefix, inner.HelpLink));

                if (inner.GetBaseException() != null)
                {
                    message.Append(string.Format(CultureInfo.InvariantCulture,
                        "{0}BaseExceptionMessage: {1}",
                        prefix, inner.GetBaseException().Message));
                }
            }

            // Append a new line
            message.Append(Environment.NewLine);

            // Write at error level
            WriteLine(source, m_traceLevelStr[(int)TraceLevel.Error], message.ToString());
        }

        /// <summary>
        /// Write a exception as warning without stack trace and other details.
        /// This keeps the IO to low level for non-critical exceptions.
        /// </summary>
        /// <param name="source">The source to trace for</param>
        /// <param name="e">The exception to write</param>
        [Conditional("TRACE")]
        public static void WriteExceptionAsWarning(LogSource source, Exception e)
        {
            LogAssert(e != null);
            Write(source, TraceLevel.Warning, string.Format(
                    CultureInfo.InvariantCulture, "Exception as warning: {0}", e.Message));
        }

        /// <summary>
        /// Write a perf message if tracing for performance is enabled
        /// </summary>
        /// <param name="source">The source to trace for</param>
        /// <param name="message">The message to write</param>
        [Conditional("PERF")]
        public static void WritePerf(LogSource source, string message)
        {
            WriteLine(source, "Perf,", message);
        }

        /// <summary>
        /// Write a formatted perf message if tracing for performance is enabled
        /// </summary>
        /// <param name="source">The source to trace for</param>
        /// <param name="format">The format of the message</param>
        /// <param name="args">The arguments for formatting the message</param>
        [Conditional("PERF")]
        public static void WritePerf(LogSource source, string format, params object[] args)
        {
            LogAssert(format != null);
            WritePerf(source, string.Format(CultureInfo.InvariantCulture, format, args));
        }

        /// <summary>
        /// Write a message of form "Entered: xxxMethod" if debug build and
        /// tracing for verbose information is enabled
        /// </summary>
        /// <param name="source">The source to trace for</param>
        /// <param name="parameters">The parameters passed to the method</param>
        [Conditional("DEBUG")]
        public static void EnteredMethod(LogSource source, params object[] parameters)
        {
            // Extra check to avoid calling perf intensive methods.
            if (ShouldTrace(source, TraceLevel.Verbose))
            {
                MethodBase callee = new StackFrame(1).GetMethod();
                Write(source, TraceLevel.Verbose, GetMethodSignature("Entered", callee));

                // If input parameters are passed, build the string to display
                if (parameters != null)
                {
                    StringBuilder sb = new StringBuilder("Input parameters are: ");
                    bool first = true;
                    foreach (object o in parameters)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            sb.Append(", ");
                        }

                        sb.Append(o);
                    }

                    Write(source, TraceLevel.Verbose, sb.ToString());
                }
            }
        }

        /// <summary>
        /// Write a message of form "Exiting: xxxMethod" if debug build and
        /// tracing for verbose information is enabled
        /// </summary>
        /// <param name="source">The source to trace for</param>
        /// <param name="returnValue">The value returned by this method</param>
        [Conditional("DEBUG")]
        public static void ExitingMethod(LogSource source, object returnValue)
        {
            // Extra check to avoid calling perf intensive methods.
            if (ShouldTrace(source, TraceLevel.Verbose))
            {
                MethodBase callee = new StackFrame(1).GetMethod();
                Write(source, TraceLevel.Verbose,
                    string.Format(CultureInfo.InvariantCulture,
                    "{0} with value: {1}",
                    GetMethodSignature("Exiting", callee), returnValue));
            }
        }

        /// <summary>
        /// Write a message of form "Exiting: xxxMethod" if debug build and
        /// tracing for verbose information is enabled
        /// </summary>
        /// <param name="source">The source to trace for</param>
        [Conditional("DEBUG")]
        public static void ExitingMethod(LogSource source)
        {
            // Extra check to avoid calling perf intensive methods.
            if (ShouldTrace(source, TraceLevel.Verbose))
            {
                MethodBase callee = new StackFrame(1).GetMethod();
                Write(source, TraceLevel.Verbose, GetMethodSignature("Exiting", callee));
            }
        }

        #endregion

        #region Private Static Methods

        /// <summary>
        /// Auxillary method: logs the exception that is being ignored.
        /// </summary>
        /// <param name="e">The exception to log.</param>
        [Conditional("TRACE")]
        internal static void LogIgnoredException(Exception e)
        {
            LogAssert(e != null);
            try
            {
                // Write to the system Application log.
                string message = Assembly.GetExecutingAssembly().FullName
                    + ": " + e.ToString();

#if DEBUG
                Console.Error.WriteLine(message);
#endif

                EventLog.WriteEntry("Application", message,
                        EventLogEntryType.Error);
            }
            catch
            {
                // Ignore everything at this point including FxCop warning
            }
        }

        /// <summary>
        /// Our version of assert for Logger classes.
        /// The standard Debug.Assert will callback on this listener. This
        /// could lead to recursive calls. Hence the need for this.
        /// </summary>
        /// <param name="condition">Assert if condition is false</param>
        [Conditional("DEBUG")]
        internal static void LogAssert(bool condition)
        {
            if (!condition)
            {
                try
                {
                    // REVIEW:: GautamG: Can we do better than this?
                    StackFrame frame = new StackFrame(1, true);
                    string message = "Assertion failure at line: " +
                        frame.GetFileLineNumber() + " in Logger class";

                    Console.Error.WriteLine(message);
                }
                catch
                {
                    // Ignore everything at this point including FxCop warning
                }
            }
        }

        /// <summary>
        /// The line becomes: 
        ///     [Component, MsgType, ThreadID, Date Time] message
        /// </summary>
        /// <param name="source">The source to trace for</param>
        /// <param name="msgType">The type of message</param>
        /// <param name="message">The message to write</param>
        [Conditional("TRACE")]
        private static void WriteLine(LogSource source, string msgType, string message)
        {
            try
            {
                // The message will have form:
                //      [ComponentName,   MsgType,  ID, DateTime               ] message...
                //       |<-16 chars->|   |<- 8->| |3|  |<-     23 chars    ->|
                //
                // For example,
                //      [Reporting,       Info,      1, 2004/07/23 15:36:40.614] This is an info
                string log = string.Format(
                    CultureInfo.InvariantCulture,
                    "[{0,-16} {1,-8} {2,3}, {3:yyyy}/{3:MM}/{3:dd} {3:HH}:{3:mm}:{3:ss}.{3:fff}] {4}",
                    m_logSourcesStr[(int)source],
                    msgType,
                    Thread.CurrentThread.ManagedThreadId,
                    DateTime.Now,
                    message);

                Trace.WriteLine(log);
            }
            // dont handle this exception.. not related to the logger context
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception e)
            {
                // This is the best we can do - ignore FxCop warning
                LogAssert(false);
                LogIgnoredException(e);
            }

        }

        /// <summary>
        /// Whether to trace this message or not?
        /// </summary>
        /// <param name="source">The source to trace for</param>
        /// <param name="level">The level of the message</param>
        /// <returns>True if message is to be traced</returns>
        private static bool ShouldTrace(LogSource source, TraceLevel level)
        {
            // Check if all source tracing at this level is enabled
            if (m_switchMinLevel.Level >= level)
                return true;

            // Check if tracing for the source at this level is enabled
            if (m_switchSources[(int)source].Level >= level)
                return true;

            return false;
        }

        /// <summary>
        /// Returns the method signature
        /// </summary>
        private static string GetMethodSignature(string prefix, MethodBase method)
        {
            return string.Format(CultureInfo.InvariantCulture, 
                "{0} {1} method of type {2}", prefix,
                method.ToString(), method.DeclaringType.FullName);
        }

        #endregion

        #region Private Static Variables

        /// <summary>
        /// The switch in configuration file for tracing all sources.
        /// </summary>
        private static readonly TraceSwitch m_switchMinLevel = new TraceSwitch("MinLevelForAllSource", null);

        /// <summary>
        /// The switch in configuration file for tracing this source.
        /// </summary>
        private static readonly TraceSwitch[] m_switchSources;

        /// <summary>
        /// The LogSource strings to avoid calling ToString() for each log.
        /// Have the strings with trailing comma.
        /// </summary>
        private static readonly string[] m_logSourcesStr;

        /// <summary>
        /// The TraceLevel strings to avoid calling ToString() for each log.
        /// Have the strings with trailing comma.
        /// </summary>
        private static readonly string[] m_traceLevelStr = {
            TraceLevel.Off.ToString() + ',',
            TraceLevel.Error.ToString() + ',',
            TraceLevel.Warning.ToString() + ',',
            TraceLevel.Info.ToString() + ',',
            TraceLevel.Verbose.ToString() + ',',
        };

        #endregion
    }

    /// <summary>
    /// The listener class for logging trace messages
    /// </summary>
    public sealed class LogListener : TextWriterTraceListener
    {
        #region Constructors

        /// <summary>
        /// Constructor - logs trace information into "calling assembly".log
        /// </summary>
        public LogListener()
        {
            // Generate log file name based on exe name
            string fileName = Environment.CurrentDirectory +
                Path.DirectorySeparatorChar +
                Assembly.GetExecutingAssembly().GetName().Name + ".log";
            Initialize(fileName);
        }

        /// <summary>
        /// Trace listener constructor.
        /// Collects trace information into fileName.
        /// </summary>
        public LogListener(string fileName)
        {
            Initialize(fileName);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialize the listener
        /// </summary>
        /// <param name="fileName">The name of the log file</param>
        [Conditional("TRACE")]
        private void Initialize(string fileName)
        {
            Logger.LogAssert(fileName != null && fileName.Length != 0);

            // Create new StreamWriter truncating any old logs
            Writer = new StreamWriter(fileName, false);
        }

        #endregion
    }
}
