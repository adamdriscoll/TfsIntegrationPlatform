// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement
{
    /// <summary>
    /// This class defines the error routing conditions that are used by the
    /// ErrorRoutingAlgorithm to determine if an error should be routed to the
    /// error reporting channels
    /// </summary>
    public abstract class ErrorRoutingPolicy
    {
        /// <summary>
        /// Default error-routing behavior - always route the error
        /// </summary>
        /// <param name="e"></param>
        /// <param name="channels"></param>
        /// <returns></returns>
        public virtual ErrorHandlingResult TryRouteError(Exception e, ReadOnlyCollection<IErrorRoutingChannel> channels)
        {
            foreach (IErrorRoutingChannel channel in channels)
            {
                channel.RouteError(e);
            }

            return new ErrorHandlingResult(ErrorHandlingResult.RoutingDecision.RoutedAsError);
        }
    }
}
