// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit.WCFServices;

namespace Microsoft.TeamFoundation.Migration.Toolkit.WCFServices
{
    /// <summary>
    /// WCF ClietBase for the MigrationService
    /// </summary>
    public class MigrationServiceClient : IMigrationService
    {
        private CustomConfigChannelFactory<IMigrationService> m_channelFactory =
            new CustomConfigChannelFactory<IMigrationService>();

        private IMigrationService m_channel;
        private IMigrationService Channel
        {
            get
            {
                if (null == m_channel)
                {
                    RecreateChannel();
                }
                return m_channel;
            }
        }

        private void RecreateChannel()
        {
            m_channel = m_channelFactory.CreateChannel();
        }

        #region IMigrationService Members

        /// <summary>
        /// Starts a session group.
        /// </summary>
        /// <param name="sessionGroupUniqueId">The GUID used as the session group's Unique Id.</param>
        public void StartSessionGroup(Guid sessionGroupUniqueId)
        {
            try
            {
                Channel.StartSessionGroup(sessionGroupUniqueId);
            }
            catch (EndpointNotFoundException e)
            {
                throw new MigrationServiceEndpointNotFoundException(e.Message, e);
            }
            catch (CommunicationException)
            {
                RecreateChannel();
                Channel.StartSessionGroup(sessionGroupUniqueId);
            }
        }

        /// <summary>
        /// Stops a session group.
        /// </summary>
        /// <param name="sessionGroupUniqueId">The GUID used as the session group's Unique Id.</param>
        public void StopSessionGroup(Guid sessionGroupUniqueId)
        {
            try
            {
                Channel.StopSessionGroup(sessionGroupUniqueId);
            }
            catch (EndpointNotFoundException e)
            {
                throw new MigrationServiceEndpointNotFoundException(e.Message, e);
            }
            catch (CommunicationException)
            {
                RecreateChannel();
                Channel.StopSessionGroup(sessionGroupUniqueId);
            }
        }

        /// <summary>
        /// Pauses a session group.
        /// </summary>
        /// <param name="sessionGroupUniqueId">The GUID used as the session group's Unique Id.</param>
        public void PauseSessionGroup(Guid sessionGroupUniqueId)
        {
            try
            {
                Channel.PauseSessionGroup(sessionGroupUniqueId);
            }
            catch (EndpointNotFoundException e)
            {
                throw new MigrationServiceEndpointNotFoundException(e.Message, e);
            }
            catch (CommunicationException)
            {
                RecreateChannel();
                Channel.PauseSessionGroup(sessionGroupUniqueId);
            }
        }

        /// <summary>
        /// Resumes a session group after pause.
        /// </summary>
        /// <param name="sessionGroupUniqueId">The GUID used as the session group's Unique Id.</param>
        public void ResumeSessionGroup(Guid sessionGroupUniqueId)
        {
            try
            {
                Channel.ResumeSessionGroup(sessionGroupUniqueId);
            }
            catch (EndpointNotFoundException e)
            {
                throw new MigrationServiceEndpointNotFoundException(e.Message, e);
            }
            catch (CommunicationException)
            {
                RecreateChannel();
                Channel.ResumeSessionGroup(sessionGroupUniqueId);
            }
        }

        /// <summary>
        /// Gets the Unique Ids of all the running session groups.
        /// </summary>
        /// <returns></returns>
        public List<Guid> GetRunningSessionGroups()
        {
            try
            {
                return Channel.GetRunningSessionGroups();
            }
            catch (EndpointNotFoundException e)
            {
                throw new MigrationServiceEndpointNotFoundException(e.Message, e);
            }
            catch (CommunicationException)
            {
                RecreateChannel();
                return Channel.GetRunningSessionGroups();
            }
        }

        /// <summary>
        /// Starts a particular session of the session group.
        /// </summary>
        /// <param name="sessionGroupUniqueId">The GUID used as the session group's Unique Id.</param>
        /// <param name="sessionUniqueId">The GUID used as the session's Unique Id.</param>
        public void StartSingleSessionInSessionGroup(Guid sessionGroupUniqueId, Guid sessionUniqueId)
        {
            try
            {
                Channel.StartSingleSessionInSessionGroup(sessionGroupUniqueId, sessionUniqueId);
            }
            catch (EndpointNotFoundException e)
            {
                throw new MigrationServiceEndpointNotFoundException(e.Message, e);
            }
            catch (CommunicationException)
            {
                RecreateChannel();
                Channel.StartSingleSessionInSessionGroup(sessionGroupUniqueId, sessionUniqueId);
            }
        }

        /// <summary>
        /// Gets a boolean to tell if a session group is running or not.
        /// </summary>
        /// <param name="sessionGroupUniqueId"></param>
        /// <returns></returns>
        public SessionGroupInitializationStatus GetSessionGroupInitializationStatus(Guid sessionGroupUniqueId)
        {
            try
            {
                return Channel.GetSessionGroupInitializationStatus(sessionGroupUniqueId);
            }
            catch (EndpointNotFoundException e)
            {
                throw new MigrationServiceEndpointNotFoundException(e.Message, e);
            }
            catch (CommunicationException)
            {
                RecreateChannel();
                return Channel.GetSessionGroupInitializationStatus(sessionGroupUniqueId);
            }
        }

        #endregion
    }
}
