// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BM = Microsoft.TeamFoundation.Migration.BusinessModel;


namespace Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement
{
    /// <summary>
    /// The error manager is a service provided by the platform to route unhandled exceptions to configurable
    /// error reporting channels. It is guaranteed that each running session group have a singleton instance
    /// of this class. It is thread-safe among multiple session threads of the same session group to call
    /// TryHandleException (overloaded) after the session pipeline is initialized.
    /// </summary>
    public class ErrorManager
    {
        private static Dictionary<Guid, ErrorManager> s_singletonInstances = new Dictionary<Guid,ErrorManager>();
        private static object m_lockOnPerGroupSingletonInstances = new object();

        private object m_lockForMultiSessionAccess = new object();  // this lock is used for thread-safety among
                                                                    // sessions of the same session group
        private ErrorRegistrationService m_errRegService;
        private ErrorRoutingAlgorithm m_routingAlgorithm;
        private SyncOrchestrator m_syncOrchestrator;

        /// <summary>
        /// Create and initialize the singleton intance of ErrorManager
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="syncOrchestrator"></param>
        /// <returns></returns>
        public static ErrorManager CreateSingletonInstance(
            Guid sessionGroupUniqueId,
            BM.ErrorManagement configuration,
            SyncOrchestrator syncOrchestrator)
        {
            lock (m_lockOnPerGroupSingletonInstances)
            {
                var newInstance = new ErrorManager(configuration, syncOrchestrator);
                if (s_singletonInstances.ContainsKey(sessionGroupUniqueId))
                {
                    s_singletonInstances.Remove(sessionGroupUniqueId);
                }

                s_singletonInstances.Add(sessionGroupUniqueId, newInstance);
                return s_singletonInstances[sessionGroupUniqueId];
            }
        }

        /// <summary>
        /// Gets the singleton instance of ErrorManager. NULL may be returned in the instance
        /// has not been initialized yet.
        /// </summary>
        public static ErrorManager GetErrorManager(Guid sessionGroupId)
        {
            lock (m_lockOnPerGroupSingletonInstances)
            {
                if (s_singletonInstances.ContainsKey(sessionGroupId))
                {
                    return s_singletonInstances[sessionGroupId];
                }
                else
                {
                    return null;
                }
            }
        }

        internal ErrorManager(
            BM.ErrorManagement configuration,
            SyncOrchestrator syncOrchestrator)
        {
            Initialize(configuration, syncOrchestrator);
        }

        private void Initialize(
            BM.ErrorManagement configuration,
            SyncOrchestrator syncOrchestrator)
        {
            m_errRegService = new ErrorRegistrationService(configuration.ErrorRouters);
            m_routingAlgorithm = new ErrorRoutingAlgorithm(m_errRegService, configuration.ReportingSettings, syncOrchestrator);
            m_syncOrchestrator = syncOrchestrator;
        }

        /// <summary>
        /// Try handling the exception
        /// </summary>
        /// <param name="e">The exception to handle</param>
        /// <returns></returns>
        public ErrorHandlingResult TryHandleException(Exception e)
        {
            return TryHandleException(e, null);
        }

        /// <summary>
        /// Try handling the exception
        /// </summary>
        /// <param name="e">The exception to handle</param>
        /// <param name="conflictManager">In case the exception does not match registered error signatures, 
        /// use this conflict manager to raise a runtime error (conflict type)</param>
        /// <returns></returns>
        public ErrorHandlingResult TryHandleException(Exception e, ConflictManager conflictManager)
        {
            lock (m_lockForMultiSessionAccess)
            {
                if (e == null)
                {
                    // todo: alternative response can be to return a result to indicate the absense of exception to handle
                    throw new ArgumentNullException();
                }

                return m_routingAlgorithm.TryHandleException(e, conflictManager);
            }
        }

        /// <summary>
        /// Register an error signature with a custom routing policy
        /// </summary>
        /// <param name="signature"></param>
        /// <param name="routineRule"></param>
        public void RegisterError(
            ErrorSignatureBase signature,
            ErrorRoutingPolicy routineRule)
        {
            m_errRegService.RegisterError(signature, routineRule);
        }

        /// <summary>
        /// Register an error signature to use the default policy - max occurrence count is 1
        /// </summary>
        /// <param name="signature"></param>
        public void RegisterError(
            ErrorSignatureBase signature)
        {
            m_errRegService.RegisterError(signature, null);
        }

        internal void RegisterErrorsInConfigurationFile()
        {
            m_errRegService.RegisterErrorsInConfigurationFile();
        }
    }
}
