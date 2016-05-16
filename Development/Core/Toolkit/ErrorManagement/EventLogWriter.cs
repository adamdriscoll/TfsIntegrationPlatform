// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement
{
    /// <summary>
    /// This static class provides the basic functionalities to write to the platform-specific
    /// Window Event Log source.
    /// </summary>
    public static class EventLogWriter
    {
        const int MaxEventMessageLength = 1000;

        static EventLogSource s_eventLog = new EventLogSource(Constants.TfsIntegrationServiceName, Constants.TfsServiceEventLogName);

        public static void WriteError(string eventMessage)
        {
            s_eventLog.WriteError(eventMessage);
        }

        public static void WriteError(string eventMessage, int eventID)
        {
            s_eventLog.WriteError(eventMessage, eventID);
        }

        public static void WriteWarning(string eventMessage)
        {
            s_eventLog.WriteWarning(eventMessage);
        }

        public static void WriteWarning(string eventMessage, int eventID)
        {
            s_eventLog.WriteWarning(eventMessage, eventID);
        }

        public static void WriteInformation(string eventMessage)
        {
            s_eventLog.WriteInformation(eventMessage);
        }

        public static void WriteInformation(string eventMessage, int eventID)
        {
            s_eventLog.WriteInformation(eventMessage, eventID);
        }

        public static void WriteSuccessAudit(string eventMessage)
        {
            s_eventLog.WriteSuccessAudit(eventMessage);
        }

        public static void WriteSuccessAudit(string eventMessage, int eventID)
        {
            s_eventLog.WriteSuccessAudit(eventMessage, eventID);
        }

        public static void WriteFailureAudit(string eventMessage)
        {
            s_eventLog.WriteFailureAudit(eventMessage);
        }

        public static void WriteFailureAudit(string eventMessage, int eventID)
        {
            s_eventLog.WriteFailureAudit(eventMessage, eventID);
        }
    }
}
