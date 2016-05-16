// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement
{
    /// <summary>
    /// This class represents the results of an error handling attempt
    /// </summary>
    public class ErrorHandlingResult
    {
        /// <summary>
        /// This enum enlists the choices of error routing decisions
        /// </summary>
        public enum RoutingDecision
        {
            SignatureMismatch,          // indicates the exception does not match the error signature
            PolicyConditionIsNotMet,    // indicates the exception matches the registered error signature but 
                                        //   does not meet the conditions specified in the policy
            RoutedAsError,              // indicates the exception is handled and routed as an error
            RaisedAsRuntimeConflict,    // indicates the exception has been raised as a runtime conflict
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="decision"></param>
        public ErrorHandlingResult(RoutingDecision decision)
        {
            Decision = decision;
        }

        /// <summary>
        /// Gets the decision represented in this result
        /// </summary>
        public RoutingDecision Decision
        {
            get;
            private set;
        }
    }
}
