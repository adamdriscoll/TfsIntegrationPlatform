// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement
{
    static class DefaultErrorRoutingPolicies
    {
        /// <summary>
        /// Registers the default errors such as OOM, NullReference, etc.
        /// </summary>
        public static void RegisterDefaultErrors(ErrorRegistrationService service)
        {
            #region instantly blocking errors
            //service.RegisterError(
            //    new ErrorSignatureOneLevelException(typeof(InitializationException)), 
            //    new MaxOccurrenceErrorRoutingPolicy(1));

            //service.RegisterError(
            //        new ErrorSignatureOneLevelException(typeof(AddinException)),
            //        new MaxOccurrenceErrorRoutingPolicy(1));

            service.RegisterError(
                    new ErrorSignatureOneLevelException(typeof(System.IO.IOException), MigrationToolkitResources.ErrReport_Exception_Message_NotEnoughSpaceOnDisk),
                    new MaxOccurrenceErrorRoutingPolicy(1));

            service.RegisterError(
                new ErrorSignatureOneLevelException(typeof(System.NotSupportedException), MigrationToolkitResources.ErrReport_Exception_Message_WorkspaceIsRequired),
                new MaxOccurrenceErrorRoutingPolicy(1));

            service.RegisterError(
                new ErrorSignatureOneLevelException("Microsoft.TeamFoundation.TeamFoundationServerUnauthorizedException"),
                new MaxOccurrenceErrorRoutingPolicy(1));
            #endregion

            #region less tolerable errors
            service.RegisterError(
                new ErrorSignatureOneLevelException(typeof(OutOfMemoryException)),
                new MaxOccurrenceErrorRoutingPolicy(MaxOccurrenceErrorRoutingPolicy.DefaultMaxOccurrence));

            service.RegisterError(
                new ErrorSignatureOneLevelException(typeof(NullReferenceException)),
                new MaxOccurrenceErrorRoutingPolicy(MaxOccurrenceErrorRoutingPolicy.DefaultMaxOccurrence));
            #endregion

            #region tolerable environmental errors
            service.RegisterError(
                    new ErrorSignatureOneLevelException(typeof(System.Data.EntityCommandExecutionException)),
                    new MaxOccurrenceErrorRoutingPolicy(MaxOccurrenceErrorRoutingPolicy.EnvironmentalErrorMaxOccurrence));

            service.RegisterError(
               new ErrorSignatureTwoLevelException(typeof(System.Data.EntityCommandExecutionException), string.Empty, typeof(System.Data.SqlClient.SqlException), 
                   MigrationToolkitResources.ErrReport_Exception_Message_Deadlock),
               new MaxOccurrenceErrorRoutingPolicy(MaxOccurrenceErrorRoutingPolicy.EnvironmentalErrorMaxOccurrence));

            service.RegisterError(
                new ErrorSignatureOneLevelException(typeof(System.Data.EntityException)),
                new MaxOccurrenceErrorRoutingPolicy(MaxOccurrenceErrorRoutingPolicy.EnvironmentalErrorMaxOccurrence));

            service.RegisterError(
                new ErrorSignatureTwoLevelException(typeof(System.Data.EntityException), string.Empty, typeof(System.Data.SqlClient.SqlException), 
                    MigrationToolkitResources.ErrReport_Exception_Message_Timeout),
                new MaxOccurrenceErrorRoutingPolicy(MaxOccurrenceErrorRoutingPolicy.EnvironmentalErrorMaxOccurrence));

            service.RegisterError(
                new ErrorSignatureOneLevelException(typeof(System.Data.SqlClient.SqlException)),
                new MaxOccurrenceErrorRoutingPolicy(MaxOccurrenceErrorRoutingPolicy.EnvironmentalErrorMaxOccurrence));

            service.RegisterError(
                new ErrorSignatureOneLevelException(typeof(System.Data.UpdateException)),
                new MaxOccurrenceErrorRoutingPolicy(MaxOccurrenceErrorRoutingPolicy.EnvironmentalErrorMaxOccurrence));

            service.RegisterError(
                new ErrorSignatureOneLevelException(typeof(System.Transactions.TransactionAbortedException)),
                new MaxOccurrenceErrorRoutingPolicy(MaxOccurrenceErrorRoutingPolicy.EnvironmentalErrorMaxOccurrence));

            service.RegisterError(
                new ErrorSignatureOneLevelException("Microsoft.TeamFoundation.Framework.Client.DatabaseConnectionException"),
                new MaxOccurrenceErrorRoutingPolicy(MaxOccurrenceErrorRoutingPolicy.EnvironmentalErrorMaxOccurrence));

            service.RegisterError(
                new ErrorSignatureOneLevelException("Microsoft.TeamFoundation.Framework.Client.DatabaseOperationTimeoutException"),
                new MaxOccurrenceErrorRoutingPolicy(MaxOccurrenceErrorRoutingPolicy.EnvironmentalErrorMaxOccurrence));

            service.RegisterError(
                new ErrorSignatureOneLevelException("Microsoft.TeamFoundation.TeamFoundationServerInvalidResponseException"),
                new MaxOccurrenceErrorRoutingPolicy(MaxOccurrenceErrorRoutingPolicy.EnvironmentalErrorMaxOccurrence));

            service.RegisterError(
                new ErrorSignatureOneLevelException("Microsoft.TeamFoundation.TeamFoundationServiceUnavailableException"),
                new MaxOccurrenceErrorRoutingPolicy(MaxOccurrenceErrorRoutingPolicy.EnvironmentalErrorMaxOccurrence));

            service.RegisterError(
                new ErrorSignatureOneLevelException("Microsoft.TeamFoundation.VersionControl.Client.RepositoryNotFoundException"),
                new MaxOccurrenceErrorRoutingPolicy(MaxOccurrenceErrorRoutingPolicy.EnvironmentalErrorMaxOccurrence));
            #endregion
        }

        /// <summary>
        /// Registers the wildcard error routing policies; must be called after all other
        /// errors are registered
        /// </summary>
        public static void RegisterImplicitDefaultErrors(ErrorRegistrationService service)
        {
            service.RegisterError(
                    new ErrorSignatureOneLevelException(ErrorSignatureBase.WildcardAny),
                    new MaxOccurrenceErrorRoutingPolicy(MaxOccurrenceErrorRoutingPolicy.DefaultMaxOccurrence));

            service.RegisterError(
                new ErrorSignatureTwoLevelException(ErrorSignatureBase.WildcardAny, ErrorSignatureBase.WildcardAny),
                new MaxOccurrenceErrorRoutingPolicy(MaxOccurrenceErrorRoutingPolicy.DefaultMaxOccurrence));
        }
    }
}
