// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Support class for tracing in the migration toolkit and within the migration application
    /// </summary>
    public static class TraceManager
    {
        /// <summary>
        /// Class used to control scope.  Returned by the call get StartLogicalOperation,
        /// this class will add and remove scope dependent on it's lifetime.
        /// </summary>
        private class LogicalOperation : IDisposable
        {
            /// <summary>
            /// enters a new scope with the provided scope id
            /// </summary>
            /// <param name="id">The scope id</param>
            internal LogicalOperation(object id)
            {
                m_id = id;

                Trace.CorrelationManager.StartLogicalOperation(m_id);
                m_sw = new Stopwatch();
                m_sw.Start();
                TraceManager.WriteLine("Starting");
            }

            #region IDisposable Members

            /// <summary>
            /// leaves the current scope with the provided scope id
            /// </summary>
            public void Dispose()
            {
                m_sw.Stop();
                TraceManager.WriteLine(TraceManager.Engine,
                    "Stopping (overall: {0})", m_sw.Elapsed);
                Trace.CorrelationManager.StopLogicalOperation();
            }

            #endregion

            object m_id;
            Stopwatch m_sw;
        }

        /// <summary>
        /// Returns an IDisposable object whose lifetime controls a new tracing scope.
        /// </summary>
        /// <param name="id">The scope id</param>
        /// <returns>The tracing scope object</returns>
        public static IDisposable StartLogicalOperation(object id)
        {
            return new LogicalOperation(id);
        }

        /// <summary>
        /// Sends an information level trace message to all trace listeners.
        /// </summary>
        /// <param name="traceMessage">The message to send</param>
        public static void TraceInformation(string traceMessage)
        {
            switch (TraceManager.Toolkit.Level)
            {
                case TraceLevel.Info:
                case TraceLevel.Verbose:
                    Trace.TraceInformation(TraceFormat(traceMessage));
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Sends an information level formatted message to all trace listeners
        /// </summary>
        /// <param name="format">The message format</param>
        /// <param name="args">The format arguments</param>
        public static void TraceInformation(string format, params object[] args)
        {
            switch (TraceManager.Toolkit.Level)
            {
                case TraceLevel.Info:
                case TraceLevel.Verbose:
                    Trace.TraceInformation(TraceFormat(format, args));
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Sends a verbose level trace message to all trace listeners.
        /// </summary>
        /// <param name="traceMessage">The message to send</param>
        public static void TraceVerbose(string traceMessage)
        {
            switch (TraceManager.Toolkit.Level)
            {
                case TraceLevel.Verbose:
                    Trace.TraceInformation(TraceFormat(traceMessage));
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Sends a verbose level formatted message to all trace listeners
        /// </summary>
        /// <param name="format">The message format</param>
        /// <param name="args">The format arguments</param>
        public static void TraceVerbose(string format, params object[] args)
        {
            switch (TraceManager.Toolkit.Level)
            {
                case TraceLevel.Verbose:
                    Trace.TraceInformation(TraceFormat(format, args));
                    break;
                default:
                    break;
            }
        }

        public static void TraceException(Exception exception)
        {
            if (exception == null)
            {
                return;
            }
            TraceWarning(exception.Message);
            TraceInformation(exception.ToString());
        }

        /// <summary>
        /// Sends a warning level trace message to all trace listeners.
        /// </summary>
        /// <param name="traceMessage">The message to send</param>
        public static void TraceWarning(string traceMessage)
        {
            if (WarningsAsErrors)
            {
                TraceError(traceMessage);
            }
            else
            {
                switch (TraceManager.Toolkit.Level)
                {
                    case TraceLevel.Warning:
                    case TraceLevel.Info:
                    case TraceLevel.Verbose:
                        Trace.TraceWarning(TraceFormat(traceMessage));
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Sends a warning level formatted message to all trace listeners
        /// </summary>
        /// <param name="format">The message format</param>
        /// <param name="args">The format arguments</param>
        public static void TraceWarning(string format, params object[] args)
        {
            if (WarningsAsErrors)
            {
                TraceError(format, args);
            }
            else
            {
                switch (TraceManager.Toolkit.Level)
                {
                    case TraceLevel.Warning:
                    case TraceLevel.Info:
                    case TraceLevel.Verbose:
                        Trace.TraceWarning(TraceFormat(format, args));
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Sends an error level trace message to all trace listeners.
        /// </summary>
        /// <param name="traceMessage">The message to send</param>
        public static void TraceError(string traceMessage)
        {
            string message = TraceFormat(traceMessage);
            switch (TraceManager.Toolkit.Level)
            {
                case TraceLevel.Error:
                case TraceLevel.Warning:
                case TraceLevel.Info:
                case TraceLevel.Verbose:
                    Trace.TraceError(message);
                    break;
                default:
                    break;
            }

            if (AbortOnError)
            {
                throw new MigrationException(message);
            }
        }

        /// <summary>
        /// Sends an error level formatted message to all trace listeners
        /// </summary>
        /// <param name="format">The message format</param>
        /// <param name="args">The format arguments</param>
        public static void TraceError(string format, params object[] args)
        {
            switch (TraceManager.Toolkit.Level)
            {
                case TraceLevel.Error:
                case TraceLevel.Warning:
                case TraceLevel.Info:
                case TraceLevel.Verbose:
                    Trace.TraceError(TraceFormat(format, args));
                    break;
                default:
                    break;
            }

            if (AbortOnError)
            {
                throw new MigrationException(string.Format(MigrationToolkitResources.Culture, format, args));
            }
        }

        /// <summary>
        /// Sends an error level formatted message to all trace listeners
        /// </summary>
        public static void TraceError(Exception exception, bool forceThrow)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }

            switch (TraceManager.Toolkit.Level)
            {
                case TraceLevel.Error:
                case TraceLevel.Warning:
                case TraceLevel.Info:
                case TraceLevel.Verbose:
                    Trace.TraceError(TraceFormat(exception.ToString()));
                    break;
                default:
                    break;
            }

            if (AbortOnError || forceThrow)
            {
                throw new MigrationException(exception.Message, exception);
            }
        }

        /// <summary>
        /// If ShowMethodDetails is true writes the current method name and the
        /// provided arguments to all trace listeners.
        /// </summary>
        /// <param name="args">The arguments passed to the current method</param>
        public static void EnterMethod(params object[] args)
        {
            if (ShowMethodDetails)
            {
                StackFrame frame = new StackFrame(1);
                MethodBase mi = frame.GetMethod();
                ParameterInfo[] parameters = mi.GetParameters();

                int inParamCount = 0;
                foreach (ParameterInfo pi in parameters)
                {
                    if (!pi.IsOut)
                    {
                        inParamCount++;
                    }
                }

                StringBuilder argString = new StringBuilder();

                if (args != null)
                {
                    if (args.Length == inParamCount)
                    {
                        argString.AppendFormat("{0}.{1}(", mi.DeclaringType.FullName, mi.Name);
                        for (int i = 0; i < args.Length; i++)
                        {
                            string arg = (args[i] != null) ? args[i].ToString() : "null";
                            bool dquote = parameters[i].ParameterType == typeof(string);

                            if (dquote && args[i] != null)
                            {
                                argString.AppendFormat("\"{0}\"", arg);
                            }
                            else
                            {
                                bool squote = parameters[i].ParameterType == typeof(char);

                                if (squote)
                                {
                                    argString.AppendFormat("\'{0}\'", arg);
                                }
                                else
                                {
                                    argString.Append(arg);
                                }
                            }
                            if (i != args.Length - 1)
                            {
                                argString.Append(", ");
                            }
                        }
                        argString.Append(")");
                    }
                    else
                    {
                        Trace.TraceWarning(
                            "Numbers of arguments ({0}) did not match number of parameters ({1}).",
                            args.Length,
                            parameters.Length
                        );

                        argString.Append("<unknown>");
                    }
                }
                else
                {
                    Trace.TraceWarning("Null parameter passed to EnterMethod");
                    argString.Append("(null)");
                }

                WriteLine(argString.ToString());
            }
        }

        /// <summary>
        /// If ShowMethodDetails is true writes the current method name to all 
        /// trace listeners.
        /// </summary>
        public static void EnterMethod()
        {
            if (ShowMethodDetails)
            {
                StackFrame frame = new StackFrame(1);
                MethodBase mb = frame.GetMethod();

                StringBuilder argString = new StringBuilder();
                argString.AppendFormat("Entering: {0}.{1}(void)",
                    mb.DeclaringType.FullName, mb.Name);

                WriteLine(argString.ToString());
            }
        }

        /// <summary>
        /// if ShowMethodDetails is true writes the method exit and the
        /// return value of the method to all trace listeners.
        /// </summary>
        /// <param name="o">The method return value</param>
        public static void ExitMethod(object o)
        {
            if (ShowMethodDetails)
            {
                StackFrame frame = new StackFrame(1);
                MethodBase mb = frame.GetMethod();

                StringBuilder argString = new StringBuilder();
                argString.AppendFormat("{0}.{1} returned \"{2}\"",
                    mb.DeclaringType.FullName,
                    mb.Name,
                    (o != null) ? o.ToString() : "<null>");

                WriteLine(argString.ToString());
            }
        }

        /// <summary>
        /// if ShowMethodDetails is true writes the method exit to all trace listeners.
        /// </summary>
        public static void ExitMethod()
        {
            if (ShowMethodDetails)
            {
                StackFrame frame = new StackFrame(1);
                MethodBase mb = frame.GetMethod();

                StringBuilder argString = new StringBuilder();
                argString.AppendFormat("{0}.{1} returning",
                    mb.DeclaringType.FullName,
                    mb.Name);

                WriteLine(argString.ToString());
            }
        }

        /// <summary>
        /// Writes the trace message to all trace listeners if the trace switch
        /// level is Verbose.
        /// </summary>
        /// <param name="traceSwitch">The switch we are writing against</param>
        /// <param name="traceMessage">The message to write</param>
        public static void WriteLine(TraceSwitch traceSwitch, string traceMessage, params object[] args)
        {
            if (traceSwitch == null)
            {
                throw new ArgumentNullException("traceSwitch");
            }

            if (traceMessage == null)
            {
                throw new ArgumentNullException("format");
            }

            WriteLine(traceSwitch, string.Format(MigrationToolkitResources.Culture, traceMessage, args));
        }

        /// <summary>
        /// Writes the trace message to all trace listeners if the trace switch
        /// level is Verbose.
        /// </summary>
        /// <param name="traceSwitch">The switch we are writing against</param>
        /// <param name="traceMessage">The message to write</param>
        public static void WriteLine(TraceSwitch traceSwitch, string traceMessage)
        {
            if (traceSwitch == null)
            {
                TraceWarning("WriteLine received null traceSwitch tracing: {0}",
                    traceMessage);
            }
            else
            {
                if (traceSwitch.TraceVerbose)
                {
                    WriteLine(traceMessage);
                }
            }
        }

        private static void WriteLine(string traceMessage)
        {
            if (Trace.CorrelationManager.LogicalOperationStack.Count == 0)
            {
                Trace.WriteLine(
                        string.Format(CultureInfo.CurrentCulture,
                            s_formatNoStack,
                            Thread.CurrentThread.ManagedThreadId,
                            Process.GetCurrentProcess().Id,
                            DateTime.Now,
                            traceMessage
                        )
                );
            }
            else
            {
                Trace.WriteLine(
                    string.Format(CultureInfo.CurrentCulture,
                            s_formatWithStack,
                            Thread.CurrentThread.ManagedThreadId,
                            Process.GetCurrentProcess().Id,
                            DateTime.Now,
                            Trace.CorrelationManager.LogicalOperationStack.Peek().ToString(),
                            traceMessage)
                    );
            }
        }

        /// <summary>
        /// If true EnterMethod(...) will write the method name and parameters to
        /// all trace listeners.
        /// </summary>
        public static bool ShowMethodDetails
        {
            get
            {
                return (Toolkit.Level == TraceLevel.Verbose);
            }
        }

        /// <summary>
        /// The stock trace switch for the migration toolkit
        /// </summary>
        public static TraceSwitch Toolkit
        {
            get
            {
                return s_migrationToolkitSwitch;
            }
        }

        /// <summary>
        /// The stock trace switch for the migration engine.
        /// </summary>
        public static TraceSwitch Engine
        {
            get
            {
                return s_migrationEngineSwitch;
            }
        }

        public static bool WarningsAsErrors
        {
            get
            {
                loadConfig();
                return m_treatWarningsAsErrors;
            }
            set
            {
                m_treatWarningsAsErrors = value;
            }
        }

        public static bool AbortOnError
        {
            get
            {
                loadConfig();
                return m_abortOnError;
            }
            set
            {
                m_abortOnError = value;
            }
        }

        private static string TraceFormat(string traceMessage)
        {
            StringBuilder message = new StringBuilder();
            message.Append(Thread.CurrentThread.Name);
            message.Append(": ");
            message.Append(traceMessage);
            return message.ToString();
        }

        private static string TraceFormat(string format, params object[] args)
        {
            StringBuilder message = new StringBuilder();
            message.Append(Thread.CurrentThread.Name);
            message.Append(": ");
            message.AppendFormat(format, args);
            return message.ToString();
        }

        static void loadConfig()
        {
            if (!m_configLoaded)
            {
                lock (m_configLoadLocker)
                {
                    if (!m_configLoaded)
                    {
                        //ToDo WarningsAsErrors = MigrationConfiguration.Current.GetValue<bool>("WarningsAsErrors", false);
                        //ToDo AbortOnError = MigrationConfiguration.Current.GetValue<bool>("AbortOnError", false);
                        m_configLoaded = true;
                    }
                }
            }
        }

        static TraceSwitch s_migrationToolkitSwitch = GlobalConfiguration.TfsIntegrationPlatformTraceSwitch;

        static TraceSwitch s_migrationEngineSwitch = GlobalConfiguration.VCMigrationEngineTraceSwitch;

        private const string s_formatNoStack = "[{0}, {1}, {2}] {3}";
        private const string s_formatWithStack = "[{0}, {1}, {2}, {3}] {4}";

        static object m_configLoadLocker = new object();
        static bool m_configLoaded;

        static bool m_treatWarningsAsErrors;
        static bool m_abortOnError;
    }
}
