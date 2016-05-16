// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2008VCAdapter
{
    /// <summary>
    /// Base class for all batched item event args.  Defines the common properties for
    /// all batched item events.
    /// </summary>
    public class BatchedItemEventArgs : EventArgs
    {
        /// <summary>
        /// Creates the object instance and initializes the properties to the specified values.
        /// </summary>
        /// <param name="target">The batched item the error occurred on</param>
        /// <param name="exception">The exception, if any, associated with this event (may be null)</param>
        public BatchedItemEventArgs(BatchedItem target, Exception exception)
            : this(target, exception, null)
        {
            if (exception != null)
            {
                m_message = exception.Message;
            }
        }

        /// <summary>
        /// Creates the object instance and initializes the properties to the specified values.
        /// </summary>
        /// <param name="target">The batched item the error occurred on</param>
        /// <param name="exception">The exception, if any, associated with this event (may be null)</param>
        /// <param name="message">The message, if any, associated with this event (may be null)</param>
        public BatchedItemEventArgs(BatchedItem target, Exception exception, string message)
        {
            m_target = target;
            m_exception = exception;
            m_message = message;
        }

        /// <summary>
        /// The action occuring at the time of the event
        /// </summary>
        public BatchedItem Target
        {
            get
            {
                return m_target;
            }
        }

        /// <summary>
        /// The exception associated with the event.  May be null.
        /// </summary>
        public Exception Exception
        {
            get
            {
                return m_exception;
            }
        }

        /// <summary>
        /// The message associated with this event. May be null
        /// </summary>
        public string Message
        {
            get
            {
                return m_message;
            }
        }

        BatchedItem m_target;
        Exception m_exception;
        String m_message;
    }

    /// <summary>
    /// A multi-item batched event is an event associated with a multi-item action such as rename.
    /// </summary>
    public class BatchedMergeErrorEventArgs : BatchedItemEventArgs
    {
        public BatchedMergeErrorEventArgs(BatchedItem targetItem, GetStatus stat, Exception exception)
            : base(targetItem, exception)
        {
            m_stat = stat;
        }

        public GetStatus GetStatus
        {
            get
            {
                return m_stat;
            }
        }

        GetStatus m_stat;
    }
}
