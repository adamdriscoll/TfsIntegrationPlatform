// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Server;

namespace Microsoft.TeamFoundation.Migration.Tfs2010UserIdLookupAddin
{
    class TFSUserIdCache
    {
        IGroupSecurityService m_gss;

        DateTime m_lastSyncTime = DateTime.MinValue;
        const int GSSSyncFrequencyInDays = 1;

        Dictionary<string, string> m_dispNameToAccName = new Dictionary<string, string>();
        object m_cacheAccessLock = new object();

        public TFSUserIdCache(IGroupSecurityService gss)
        {
            m_gss = gss;
        }

        public string FindAccountName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
            {
                throw new ArgumentNullException("displayName");
            }

            lock (m_cacheAccessLock)
            {
                if (SyncToGSSNeeded)
                {
                    SyncToGSS();
                }

                if (!m_dispNameToAccName.ContainsKey(displayName))
                {
                    return string.Empty;
                }
                else
                {
                    return m_dispNameToAccName[displayName];
                }
            }
        }

        private bool SyncToGSSNeeded
        {
            get
            {
                TimeSpan syncLatency = DateTime.Now.Subtract(m_lastSyncTime);
                return syncLatency.Days >= GSSSyncFrequencyInDays;
            }
        }

        private void SyncToGSS()
        {           
            m_dispNameToAccName.Clear();

            Identity identity = m_gss.ReadIdentity(SearchFactor.EveryoneApplicationGroup, "Team Foundation Valid Users", QueryMembership.Expanded);
            Identity[] identities = m_gss.ReadIdentities(SearchFactor.Sid, identity.Members, QueryMembership.None);

            foreach (Identity id in identities)
            {
                if (id.Type == IdentityType.WindowsUser && !m_dispNameToAccName.ContainsKey(id.DisplayName))
                {
                    m_dispNameToAccName.Add(id.DisplayName, id.AccountName);
                }
            }           

            m_lastSyncTime = DateTime.Now;
        }
    }
}
