// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Server;

namespace Microsoft.TeamFoundation.Migration.Tfs2010UserIdLookupAddin
{
    class PostBeta2TeamFoundationServiceProvider : ITeamFoundationServiceProvider
    {
        private TfsTeamProjectCollection m_server = null;   // Team foundation server
        // private TeamFoundationServer m_server = null;
        

        public PostBeta2TeamFoundationServiceProvider(string serverUrl)
        {
            m_server = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(serverUrl));
            // m_server = new TeamFoundationServer(serverUrl); 
            m_server.EnsureAuthenticated();
        }

        #region IServiceProvider Members

        public object GetService(Type serviceType)
        {
            return m_server.GetService(serviceType);
        }

        #endregion

        #region ITeamFoundationServiceProvider Members

        public Guid InstanceId
        {
            get { return m_server.InstanceId; }
        }

        #endregion
    }
}
