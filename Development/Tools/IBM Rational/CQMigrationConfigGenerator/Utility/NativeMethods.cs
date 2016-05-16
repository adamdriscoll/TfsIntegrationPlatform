// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// File with some utility functions useful for converters.

using System;
using System.Diagnostics;
using System.Text;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.TeamFoundation.Converters.Utility
{
    /// <summary>
    /// Class encapsulating all native methods used by converters
    /// </summary>
    internal static class ConverterNativeMethods
    {
        // A delegate type to be used as the handler routine 
        // for SetConsoleCtrlHandler.
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        // An enumerated type for the control messages
        // sent to the handler routine.
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        [DllImport("Kernel32", SetLastError=true)]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        /// <summary>
        /// This functions returns last win32 error. this error value is set only
        /// if calling function had DllImport attribute SetLastError=true
        /// </summary>
        /// <returns>last win32 error code</returns>
        public static int GetLastWin32Error()
        {
            return Marshal.GetLastWin32Error();
        }
    }

    internal static class LocalizedPasswordReader
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetStdHandle(IntPtr whichHandle);
        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr handle, out uint mode);
        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr handle, uint mode);

        static readonly IntPtr STD_INPUT_HANDLE = new IntPtr(-10); // -10 is for stdin, -11 for output and -12 for error
        static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1); // invalid handle constant
        const int ENABLE_LINE_INPUT = 2;
        const uint ENABLE_ECHO_INPUT = 4;

        /// <summary>
        /// Method to read password;
        /// </summary>
        /// <returns></returns>
        public static string ReadLine()
        {
            // Get the console handle
            IntPtr consoleHandle = GetStdHandle(STD_INPUT_HANDLE);
            if (consoleHandle == INVALID_HANDLE_VALUE) // If invalid handle is returned
            {
                Logger.Write(LogSource.Common, TraceLevel.Error, "Unable to open standard input");
                throw new ConverterException(CommonResource.StandardInputError);
            }

            // Store the old console mode.
            uint oldConsoleMode;
            if (GetConsoleMode(consoleHandle, out oldConsoleMode) == false)
            {
                Logger.Write(LogSource.Common, TraceLevel.Error, "Unable to get properties of standard input");
                throw new ConverterException(CommonResource.StandardInputError);
            }

            // Set the new console mode with these two properties turned off
            uint newConsoleMode = oldConsoleMode & ~(ENABLE_LINE_INPUT | ENABLE_ECHO_INPUT);
            if (SetConsoleMode(consoleHandle, newConsoleMode) == false)
            {
                Logger.Write(LogSource.Common, TraceLevel.Error, "Unable to set properties of standard input");
                throw new ConverterException(CommonResource.StandardInputError);
            }

            int inputChar;
            StringBuilder password = new StringBuilder();
            string echo = CommonResource.PasswordEchoChar;
            try
            {
                //Read till enter is entered
                while (true)
                {
                    inputChar = Console.Read();
                    if (inputChar == 13) // if end of line 
                    {
                        break;
                    }

                    if (inputChar == 8) // backspace
                    {
                        if (password.Length > 0) // Only if something is entered, remove it
                        {
                            password.Remove(password.Length - 1, 1);
                            Console.Write("\b");
                            Console.Write(" ");
                            Console.Write("\b");
                        }

                        continue;
                    }

                    password.Append((char)inputChar);
                    Console.Write(echo);
                }
            }
            finally
            {
                //Restore the console mode
                if (SetConsoleMode(consoleHandle, oldConsoleMode) == false)
                {
                    Logger.Write(LogSource.Common, TraceLevel.Error, "Unable to set properties of standard input");
                    throw new ConverterException(CommonResource.StandardInputError);
                }
            }

            // return password
            return (password.ToString());
        }
    }
}
