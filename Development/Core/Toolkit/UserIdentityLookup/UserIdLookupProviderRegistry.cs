// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Provide thread-safe LookupServiceProvider registration service
    /// </summary>
    internal class UserIdLookupProviderRegistry
    {
        // Dictionary<ProviderReferenceNam, IUserIdentityLookupServiceProvider>
        Dictionary<Guid, IUserIdentityLookupServiceProvider> m_registeredServiceProviders =
            new Dictionary<Guid, IUserIdentityLookupServiceProvider>();
        object m_registeredServiceProvidersLock = new object();

        /// <summary>
        /// Registers a lookup service provider to the registry
        /// </summary>
        /// <param name="referenceName"></param>
        /// <param name="provider"></param>
        public void RegisterLookupServiceProvider(
            Guid referenceName, 
            IUserIdentityLookupServiceProvider provider)
        {
            lock (m_registeredServiceProvidersLock)
            {
                if (!m_registeredServiceProviders.ContainsKey(referenceName))
                {
                    m_registeredServiceProviders.Add(referenceName, provider);
                }
            }
        }

        /// <summary>
        /// Thread-safe gets a provider by its reference name
        /// </summary>
        /// <param name="referenceName"></param>
        /// <returns>The provider instance if it is registered; NULL otherwise</returns>
        public IUserIdentityLookupServiceProvider GetProvider(Guid referenceName)
        {
            IUserIdentityLookupServiceProvider provider = null;
            lock (m_registeredServiceProvidersLock)
            {
                if (m_registeredServiceProviders.ContainsKey(referenceName))
                {
                    provider = m_registeredServiceProviders[referenceName];
                }
            }

            return provider;
        }

        public Guid[] GetAllProviderReferenceNames()
        {
            lock (m_registeredServiceProvidersLock)
            {
                return m_registeredServiceProviders.Keys.ToArray();
            }
        }
    }
}
