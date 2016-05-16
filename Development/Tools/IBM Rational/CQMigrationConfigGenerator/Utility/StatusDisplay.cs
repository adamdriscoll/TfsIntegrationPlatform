// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// Base class for displaying migration status. Refer class summary for more details.

#region Using directives
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

#endregion


namespace Microsoft.TeamFoundation.Converters.Utility
{
    /// <remarks>
    /// Represents the Base class for the commandline parser. 
    /// </remarks>
    public abstract class StatusDisplay
    {

        /// <summary>
        /// Constructor with only the list of arguments to be parsed
        /// By default the escapesequence is assumed to be "/" and ':' to be the separating character
        /// </summary>
        protected StatusDisplay()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="displayFunction"></param>
        protected StatusDisplay(DisplayFunctionValue displayFunction)
        {
            m_displayFunction = displayFunction;
        }

        protected DisplayFunctionValue DisplayFunction
        {
            set { m_displayFunction = value; }
            get { return m_displayFunction; }
        }
        public void InitDisplay()
        {
            if (m_displayFunction == null)
            {
                //TODO: get resource string from some other resource manager hatteras converter should not be there 
                Logger.Write(LogSource.Common, System.Diagnostics.TraceLevel.Error, "Initializing display failed ::Display Function not set");
                throw new DisplayFunctionNotSetException();
            }
            displayEvent = new System.Threading.AutoResetEvent(false);
            timerDelegate = new TimerCallback(this.DisplayStatus);
            //TODO: put hardcoded value in resources
            DisplayTimer = new Timer(timerDelegate, displayEvent, 100, System.Threading.Timeout.Infinite);
        }
        private void DisplayStatus(Object stateInfo)
        {
            while (true)
            {
                AutoResetEvent autoEvent = (AutoResetEvent)stateInfo;
                DisplayFunction();
                //TODO: put hardcoded value in resources
                autoEvent.WaitOne(1000, false);//wait for 1 second
            }
        }
        public void RefreshDisplay()
        {
            displayEvent.Set();
        }

        private System.Threading.AutoResetEvent displayEvent;
        private TimerCallback timerDelegate;
        private Timer DisplayTimer;
        public delegate void DisplayFunctionValue();
        private DisplayFunctionValue m_displayFunction;
    }

    public class DisplayFunctionNotSetException : ApplicationException
    {
        const string ErrorString = "Display Function not set";//TODO: take this to seperate resource file
        const int ErrorCode = -9999;
        public DisplayFunctionNotSetException():base(ErrorString)
        {
            this.HResult = ErrorCode;
        }
        public DisplayFunctionNotSetException(string message):base(message)
        {
            this.HResult = ErrorCode;
        }
        public DisplayFunctionNotSetException(string message, int ErrorCode):base(message)
        {
            this.HResult = ErrorCode;
        }


    }


}
