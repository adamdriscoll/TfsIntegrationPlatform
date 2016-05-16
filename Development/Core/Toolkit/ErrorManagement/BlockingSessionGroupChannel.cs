// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement
{
    class BlockingSessionGroupChannel : IErrorRoutingChannel
    {
        SyncOrchestrator m_syncOrchestrator;
        bool m_enabledByDefault;

        public BlockingSessionGroupChannel(SyncOrchestrator syncOrchestrator, bool enabledByDefault)
        {
            Debug.Assert(syncOrchestrator != null, "syncOrchestrator is NULL");
            m_syncOrchestrator = syncOrchestrator;
            m_enabledByDefault = enabledByDefault;
        }

        #region IErrorRoutingChannel Members

        public void RouteError(Exception e)
        {
            if (m_enabledByDefault || e is InitializationException || e is AddinException)
            {
                m_syncOrchestrator.Stop();
            }
        }

        #endregion
    }
}
