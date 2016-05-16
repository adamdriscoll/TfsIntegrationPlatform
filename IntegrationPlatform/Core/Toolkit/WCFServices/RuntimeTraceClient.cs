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
    /// WCF client endpoint for RuntimeTrace.
    /// </summary>
    public class RuntimeTraceClient : IRuntimeTrace
    {
        private CustomConfigChannelFactory<IRuntimeTrace> m_channelFactory =
            new CustomConfigChannelFactory<IRuntimeTrace>();

        private IRuntimeTrace m_channel;
        private IRuntimeTrace Channel
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

        #region IRuntimeTrace Members

        /// <summary>
        /// Gets lines of trace messages.
        /// </summary>
        /// <returns></returns>
        public string[] GetTraceMessages()
        {
            try
            {
                return Channel.GetTraceMessages();
            }
            catch (CommunicationException)
            {
                RecreateChannel();
                return Channel.GetTraceMessages();
            }
        }

        #endregion
    }
}
