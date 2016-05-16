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
    public enum ReportingLevel
    { 
        WriteToWindowsEventLog = 0,
        BlockSessionGroup = 1,
        RaiseDebugAssertion = 2,
    }

    /// <summary>
    /// This class provides the mechanism to drive the process of policy-based error handling
    /// </summary>
    class ErrorRoutingAlgorithm
    {
        private ErrorRegistrationService m_errRegService;
        private BM.ReportingSettings m_reportingSettings;
        private List<IErrorRoutingChannel> m_routingChannels = new List<IErrorRoutingChannel>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="errRegService"></param>
        /// <param name="reportingSettings"></param>
        /// <param name="syncOrchestrator"></param>
        public ErrorRoutingAlgorithm(
            ErrorRegistrationService errRegService,
            BM.ReportingSettings reportingSettings,
            SyncOrchestrator syncOrchestrator)
        {
            m_errRegService = errRegService;
            m_reportingSettings = reportingSettings;

           // add default EventLog Channel
            m_routingChannels.Add(new EventLogChannel());
            m_routingChannels.Add(new TraceLogChannel());

            if (null != m_reportingSettings 
                && (reportingSettings.EnableDebugAssertion 
                    || reportingSettings.ReportingLevel >= (int)ReportingLevel.RaiseDebugAssertion))
            {
                // configured to add DebugAssertion Channel
                // todo: remove this when we want to cleanly rely on build flavor to enable
                // debug failure
                m_routingChannels.Add(new DebugAssertChannel());
            }

            bool blockingSessionEnabledByDefault =
                (null != m_reportingSettings && reportingSettings.ReportingLevel >= (int)ReportingLevel.BlockSessionGroup);
            
            m_routingChannels.Add(new BlockingSessionGroupChannel(syncOrchestrator, blockingSessionEnabledByDefault));
        }

        /// <summary>
        /// Try handling an exception
        /// </summary>
        /// <param name="e"></param>
        /// <param name="conflictManager"></param>
        /// <returns>The decision returned is either RoutedAsError or RaisedAsRuntimeConflict</returns>
        public ErrorHandlingResult TryHandleException(Exception e, ConflictManager conflictManager)
        {
            bool conflictManagementIsEnabled = (null != conflictManager);
            bool anErrorRouterIsFound = false;
            bool exceptionIsRoutedAsError = false;
            foreach (ErrorRouter errRouter in m_errRegService.RegisteredRouters)
            {
                ErrorHandlingResult rslt = errRouter.TryRouteError(e, this.m_routingChannels.AsReadOnly());
                switch (rslt.Decision)
                {
                    case ErrorHandlingResult.RoutingDecision.RaisedAsRuntimeConflict:
                        anErrorRouterIsFound = true;
                        exceptionIsRoutedAsError = true;
                        break;
                    case ErrorHandlingResult.RoutingDecision.PolicyConditionIsNotMet:
                        anErrorRouterIsFound = true;
                        break;
                    case ErrorHandlingResult.RoutingDecision.RoutedAsError:
                        return rslt;
                    default:
                        break;
                }

                if (anErrorRouterIsFound || exceptionIsRoutedAsError)
                {
                    break;
                }
            }

            if (exceptionIsRoutedAsError)
            {
                return new ErrorHandlingResult(ErrorHandlingResult.RoutingDecision.RoutedAsError);
            }

            if (conflictManagementIsEnabled)
            {
                MigrationConflict conflict = GenericConflictType.CreateConflict(e);
                List<MigrationAction> actions;
                conflictManager.TryResolveNewConflict(conflictManager.SourceId, conflict, out actions);
                return new ErrorHandlingResult(ErrorHandlingResult.RoutingDecision.RaisedAsRuntimeConflict);
            }

            else if (anErrorRouterIsFound)
            {
                // no conflict manager to log the runtime conflict
                throw new MissingErrorRouterException(
                    "Runtime error does not meet the error routine policy and there is no conflict manager to log a conflict.",
                    e);
            }
            else
            {
                // no conflict manager is present to raise the error as a runtime conflict 
                // - default to report as an error
                foreach (IErrorRoutingChannel channel in m_routingChannels)
                {
                    channel.RouteError(e);
                }
                return new ErrorHandlingResult(ErrorHandlingResult.RoutingDecision.RoutedAsError);
            }
        }
    }
}
