// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

#region Using directives

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

#endregion

namespace Microsoft.TeamFoundation.Converters.Utility
{

    public static class Display
    {
        // Variables to maintain Color
        private static ConsoleColor m_foregroundColor;
        private static ConsoleColor m_backgroundColor;
        private static ConsoleColor m_errorColor;
        private const string m_errorFileName = "ConverterErrors.txt";

        //Variables for error messages
        private static StreamWriter m_errorStream;

        //progress display thread
        private static Thread m_progressDisplayThread;
        private static ThreadStart m_threadStart;
        private static AutoResetEvent m_progressDisplayEvent;
        //bool to take control of progress display thread
        private static bool m_progressDisplayFlag;
        private static bool m_isInitialized;
        private static bool m_isProgressDisplayBusy;
        // progress Message strings. 
        private static string m_displayProgressIndicator;
        private static string m_progressMessage;
        //repeat count for progress message
        const int m_maxRepeatCount = 25;
        //private static string m_blockingErrorMessage;
        
        [SuppressMessage("Microsoft.Design","CA1034:NestedTypesShouldNotBeVisible", Justification="This delegate is used in the WIT tests, referenced as external exe")]
        public delegate void GetErrorsStatisticsFunction(out long numberOfErrors);
        private static GetErrorsStatisticsFunction m_getErrors;

        private static object m_displayLock = string.Empty;

        public static GetErrorsStatisticsFunction GetErrors
        {
            get { return m_getErrors; }
            set { m_getErrors = value; }
        }


        /// <summary>
        /// use for starting display . 
        /// call InitDisplay function only once in a program
        /// </summary>
        public static void InitDisplay(GetErrorsStatisticsFunction getErrorsFunction)
        {
            Debug.Assert(!m_isInitialized, "Display already initialized. Check your Implementation");

            lock (m_displayLock)
            {
                m_progressDisplayFlag = false;
                GetErrors = getErrorsFunction;
                //maintain difference withe previous display
                Console.WriteLine(string.Empty);
                m_backgroundColor = Console.BackgroundColor;
                m_foregroundColor = Console.ForegroundColor;
                m_errorColor = ConsoleColor.Yellow;
                m_displayProgressIndicator = CommonResource.DisplayProgressIndicator;
                // Init the error file
                initErrorFile();
                //Start the thread that calls Display
                m_progressDisplayEvent = new System.Threading.AutoResetEvent(false);
                m_threadStart = new ThreadStart(Display.DisplayProgress);
                m_progressDisplayThread = null;
                m_isInitialized = true;
            }
        }

        private static void initErrorFile()
        {
            lock (m_displayLock)
            {
                if (!m_isInitialized)
                {
                    try
                    {
                        m_errorStream = File.CreateText(m_errorFileName);
                        m_errorStream.AutoFlush = true;
                    }
                    catch (IOException e)// could not create error file.
                    {
                        Logger.WriteException(LogSource.Common, e);
                        throw new ConverterException(UtilityMethods.Format(
                            CommonResource.UnableToCreateErrorFile, m_errorFileName, e.Message), e);
                    }
                    catch (System.UnauthorizedAccessException e)// could not create error file.
                    {
                        Logger.WriteException(LogSource.Common, e);
                        throw new ConverterException(UtilityMethods.Format(
                            CommonResource.UnableToCreateErrorFile, m_errorFileName, e.Message), e);
                    }
                }
            }

        }

        /// <summary>
        /// new thread of display
        /// </summary>
        /// <param name="stateInfo"></param>
        private static void DisplayProgress()
        {
            Logger.EnteredMethod(LogSource.Common);

            Console.WriteLine(m_progressMessage);
            while (m_progressDisplayFlag && !ThreadManager.IsAborting)
            {
                lock (m_displayLock)
                {
                    Console.Write(m_displayProgressIndicator);
                }

                m_progressDisplayEvent.WaitOne(60000, true);//wait for 1 minutes
            }

            Console.WriteLine();


            Logger.ExitingMethod(LogSource.Common);
        }


        /// <summary>
        /// Start progress message
        /// </summary>
        /// <param name="message"></param>
        /// 
        internal static bool StartProgressDisplay(string message)
        {
            Debug.Assert(m_isInitialized, "Display not initialized. Check your Implementation");

            lock (m_displayLock)
            {
                if (!m_isProgressDisplayBusy)
                {
                    //Add code here
                    m_isProgressDisplayBusy = true;
                    m_progressMessage = message;
                    m_progressDisplayFlag = true;
                    m_progressDisplayEvent.Reset();
                    m_progressDisplayThread = new Thread(m_threadStart);
                    m_progressDisplayThread.Name = "Display Thread";
                    m_progressDisplayThread.Start();
                    return true;
                }
                else
                {
                    DisplayMessage(message);
                }
            }

            return false;
        }

        /// <summary>
        /// Stop progress message
        /// </summary>
        internal static void StopProgressDisplay()
        {
            Debug.Assert(m_isInitialized, "Display not initialized. Check your Implementation");

            m_progressDisplayFlag = false;

            if (m_progressDisplayEvent != null)
            {
                m_progressDisplayEvent.Set();
            }

            if (m_progressDisplayThread != null)
            {
                while(!ThreadManager.IsAborting)
                {
                    // do not lock around this join as the progress thread also takes
                    // this lock (this results in a deadlock)
                    if (m_progressDisplayThread.Join(500))
                    {
                        break;
                    }
                }
            }

            if (!ThreadManager.IsAborting)
            {
                lock (m_displayLock)
                {
                    m_progressDisplayThread = null;
                    m_isProgressDisplayBusy = false;
                }
            }
        }


        /// <summary>
        /// Displays error
        /// </summary>
        /// <param name="error"></param>
        public static void DisplayError(string errorMessage)
        {
            Logger.EnteredMethod(LogSource.Common);
            bool restartProgressDisplay = false;

            if (m_isInitialized && !ThreadManager.IsAborting)
            {
                if (m_isProgressDisplayBusy)
                {
                    StopProgressDisplay();
                    restartProgressDisplay = true;
                }

                lock (m_displayLock)
                {
                    if (restartProgressDisplay)
                    {
                        Console.WriteLine();
                    }

                    long errors;
                    GetErrors(out errors);
                    string message = UtilityMethods.Format(
                        CommonResource.ErrorStart, errors, /*warnings, */errorMessage);
                    try
                    {
                        Console.ForegroundColor = m_errorColor;
                        Console.WriteLine(message);
                        Console.ForegroundColor = m_foregroundColor;

                        // write to error file.
                        if (m_errorStream != null)
                        {
                            m_errorStream.WriteLine(message);
                        }
                    }
                    catch (IOException e)// this should not come
                    {
                        Logger.WriteExceptionAsWarning(LogSource.Common, e);
                    }
                }

                if (restartProgressDisplay)
                {
                    StartProgressDisplay(m_progressMessage);
                }

            }

            Logger.ExitingMethod(LogSource.Common);
        }

        internal static void RaiseBlockingError(string errorMessage)
        {
            Logger.EnteredMethod(LogSource.Common);

            lock (m_displayLock)
            {
                if (m_isInitialized && !ThreadManager.IsAborting)
                {
                    try
                    {
                        // write to error file.
                        if (m_errorStream != null)
                        {
                            m_errorStream.WriteLine(errorMessage);
                        }
                    }
                    catch (IOException e)// this should not come
                    {
                        Logger.WriteExceptionAsWarning(LogSource.Common, e);
                    }

                    Console.ForegroundColor = m_errorColor;

                    //Start the thread that calls Display
                    StartProgressDisplay(errorMessage);
                }
                else
                {
                    UtilityMethods.DisplayOutput(errorMessage);
                }
            }

            Logger.ExitingMethod(LogSource.Common);
        }

        internal static void SetBlockingErrorResolved()
        {
            Logger.EnteredMethod(LogSource.Common);

            if (!ThreadManager.IsAborting)
            {
                if (m_isInitialized)
                {
                    // StopProgressDisplay method locks internally
                    StopProgressDisplay();
                    
                    lock (m_displayLock)
                    {
                        Console.ForegroundColor = m_foregroundColor;
                    }
                }
            }

            Logger.ExitingMethod(LogSource.Common);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        public static void DisplayMessage(string format, params object[] args)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }

            writeLine(string.Format(CultureInfo.InvariantCulture, format, args));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        public static void DisplayMessage(string message)
        {
            writeLine(message);
        }

        /// <summary>
        /// Displays a new line character
        /// </summary>
        public static void NewLine()
        {
            writeLine(string.Empty);
        }

        private static void writeLine(string message)
        {
            lock (m_displayLock)
            {
                Debug.Assert(!m_isProgressDisplayBusy, 
                    "Currently a progress Message is being displayed. It is not safe to use Display Message.");

                if (m_isInitialized)
                {
                    Console.WriteLine(message);
                }
            }
        }


        public static void EndDisplay()
        {
            Logger.EnteredMethod(LogSource.Common);

            lock (m_displayLock)
            {
                if (m_isInitialized)
                {                    
                    try
                    {
                        if (m_errorStream != null)
                        {
                            m_errorStream.Close();
                            m_errorStream = null;
                        }
                    }
                    catch (IOException e)
                    {
                        Logger.WriteExceptionAsWarning(LogSource.Common, e);
                    }
                }
            }

            StopProgressDisplay();

            lock(m_displayLock)
            {
                if(m_isInitialized)
                {
                    m_isInitialized = false;

                    // Console.CursorVisible = true;
                    Console.ForegroundColor = m_foregroundColor;
                    Console.BackgroundColor = m_backgroundColor;
                    Console.WriteLine();
                }
            }

            Logger.ExitingMethod(LogSource.Common);
        }
    }
}
