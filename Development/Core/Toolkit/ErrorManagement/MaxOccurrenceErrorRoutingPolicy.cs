// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement
{
    /// <summary>
    /// This policy defines that when a maximum number of reoccurrence of the 
    /// same error (i.e. error signature matches) is reached, the error will
    /// be routed to the error reporting channels
    /// </summary>
    public class MaxOccurrenceErrorRoutingPolicy : ErrorRoutingPolicy
    {
        public const int DefaultMaxOccurrence = 60;
        public const int EnvironmentalErrorMaxOccurrence = 1000000;
        private readonly int m_maxOccurence;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="maxOccurrence"></param>
        public MaxOccurrenceErrorRoutingPolicy(int maxOccurrence)
        {
            m_maxOccurence = maxOccurrence;
            CurrentOccurenceCount = 0;
        }

        /// <summary>
        /// Gets the maximum occurrence of this error allowed before it is routed to the error reporting channels
        /// </summary>
        public int MaxOccurernce
        {
            get
            {
                return m_maxOccurence;
            }
        }

        /// <summary>
        /// Gets the current occurrence count of the corresponding error/exception
        /// </summary>
        public int CurrentOccurenceCount
        {
            get;
            private set;
        }

        /// <summary>
        /// Checks this policy and route the exception to the error-reporting channel if policy condition matches
        /// </summary>
        /// <param name="e"></param>
        /// <param name="channels"></param>
        /// <returns></returns>
        public override ErrorHandlingResult TryRouteError(
            Exception e, 
            ReadOnlyCollection<IErrorRoutingChannel> channels)
        {
            if (++CurrentOccurenceCount >= MaxOccurernce)
            {
                ResetCount();
                return base.TryRouteError(e, channels);
            }
            else
            {
                return new ErrorHandlingResult(ErrorHandlingResult.RoutingDecision.PolicyConditionIsNotMet);
            }
        }

        private void ResetCount()
        {
            CurrentOccurenceCount = 0;
        }
    }
}
