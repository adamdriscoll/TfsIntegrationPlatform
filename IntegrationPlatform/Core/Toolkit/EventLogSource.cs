// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// This class encapsulates the logic of writing to a name Window Event Log Source
    /// </summary>
    public class EventLogSource
    {
        private const int MaxEventMessageLength = 1000;

        private string EventLogSourceName { get; set; }
        private string EventLogName { get; set; }

        public EventLogSource(string eventLogSourceName, string eventLogName)
        {
            EventLogSourceName = eventLogSourceName;
            EventLogName = eventLogName;
        }

        public void WriteError(string eventMessage)
        {
            if (TryValidateSource())
            {
                TryWrite(EventLogEntryType.Error, eventMessage, 0);
            }
        }

        public void WriteError(string eventMessage, int eventID)
        {
            if (TryValidateSource())
            {
                TryWrite(EventLogEntryType.Error, eventMessage, eventID);
            }
        }

        public void WriteWarning(string eventMessage)
        {
            if (TryValidateSource())
            {
                TryWrite(EventLogEntryType.Warning, eventMessage, 0);
            }

        }

        public void WriteWarning(string eventMessage, int eventID)
        {
            if (TryValidateSource())
            {
                TryWrite(EventLogEntryType.Warning, eventMessage, eventID);
            }
        }

        public void WriteInformation(string eventMessage)
        {
            if (TryValidateSource())
            {
                TryWrite(EventLogEntryType.Information, eventMessage, 0);
            }
        }

        public void WriteInformation(string eventMessage, int eventID)
        {
            if (TryValidateSource())
            {
                TryWrite(EventLogEntryType.Information, eventMessage, eventID);
            }
        }

        public void WriteSuccessAudit(string eventMessage)
        {
            if (TryValidateSource())
            {
                TryWrite(EventLogEntryType.SuccessAudit, eventMessage, 0);
            }
        }

        public void WriteSuccessAudit(string eventMessage, int eventID)
        {
            if (TryValidateSource())
            {
                TryWrite(EventLogEntryType.SuccessAudit, eventMessage, eventID);
            }
        }

        public void WriteFailureAudit(string eventMessage)
        {
            if (TryValidateSource())
            {
                TryWrite(EventLogEntryType.FailureAudit, eventMessage, 0);
            }
        }

        public void WriteFailureAudit(string eventMessage, int eventID)
        {
            if (TryValidateSource())
            {
                TryWrite(EventLogEntryType.FailureAudit, eventMessage, eventID);
            }
        }

        private void TryWrite(EventLogEntryType type, string eventMessage, int eventID)
        {
            try
            {
                // The message string cannot be longer than 32766 bytes
                EventLog.WriteEntry(EventLogSourceName, NormalizeEventMessage(eventMessage), type, NormalizeEventID(eventID));
            }
            catch (Exception e)
            {
                TraceManager.TraceException(e);
            }
        }

        private string NormalizeEventMessage(string originalMessage)
        {
            if (originalMessage.Length > MaxEventMessageLength)
            {
                return originalMessage.Substring(0, MaxEventMessageLength);
            }
            else
            {
                return originalMessage;
            }
        }

        private int NormalizeEventID(int originalID)
        {
            if (originalID < 0 || originalID >= int.MaxValue)
            {
                return 0;
            }
            else
            {
                return originalID;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <remarks>If we don't have permission to validate the source, we will just assume that the source is there. We will 
        /// catch exceptions when we write to event log</remarks>
        private bool TryValidateSource()
        {
            try
            {
                if (!EventLog.SourceExists(EventLogSourceName))
                {
                    try
                    {
                        EventLog.CreateEventSource(EventLogSourceName, EventLogName);
                    }
                    catch (Exception e)
                    {
                        TraceManager.TraceException(e);
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                // cannot validate, assume valid
                // To search for an event source in Windows Vista, Windows XP Professional, or Windows Server 2003, administrative privileges are required
                return true;
            }
        }
    }
}
