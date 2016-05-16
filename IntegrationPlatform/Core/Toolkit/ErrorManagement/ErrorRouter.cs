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
    /// An ErrorRouter is a self-contained logic that knows how to match the signature of an incoming
    /// error (Exception only in the current version) and to try routing it to the configured error-reporting
    /// channels.
    /// </summary>
    internal class ErrorRouter
    {
        /// <summary>
        /// Gets the signature of the class of errors recognized by this router
        /// </summary>
        public ErrorSignatureBase Signature { get; private set; }

        /// <summary>
        /// Gets the RoutingPolicy for this router. 
        /// </summary>
        /// <remarks>
        /// RoutingPolicy can be NULL, in which case we will use the default rounting behavior,
        /// i.e. always recognize the matching error as a critical error
        /// </remarks>
        public ErrorRoutingPolicy RoutingPolicy { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="signature">The signature of the error</param>
        /// <param name="routingPolicy">The routing policy for this class of errors; if NULL, default policy 
        /// will be used, i.e. always report the error as critical and push to channels that are defined by
        /// the global configuration</param>
        public ErrorRouter(ErrorSignatureBase signature, ErrorRoutingPolicy routingPolicy)
        {
            Signature = signature;
            RoutingPolicy = routingPolicy;
        }

        /// <summary>
        /// Try routing an error.
        /// </summary>
        /// <param name="e">The exception to be handled.</param>
        /// <returns>The routing result</returns>
        public ErrorHandlingResult TryRouteError(
            Exception e, 
            ReadOnlyCollection<IErrorRoutingChannel> channels)
        {
            if (Signature.Matches(e))
            {
                return RoutingPolicy.TryRouteError(e, channels);
            }
            else
            {
                return new ErrorHandlingResult(ErrorHandlingResult.RoutingDecision.SignatureMismatch);
            }
        }
    }
}
