// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Thread-safe user Id mapping algorithm pool
    /// </summary>
    internal class UserIdMappingAlgorithmPool
    {
        // Dictionary<ThreadId, UserIdentityMappingAlgorithms> 
        Dictionary<int, UserIdMappingAlgorithm> m_perThreadMappingAlgPool =
            new Dictionary<int, UserIdMappingAlgorithm>();
        object m_perThreadMappingAlgPoolLock = new object();

        UserIdMappingAlgorithm m_baseMappingAlgorithm;
        object m_baseMappingAlgorithmLock = new object();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="userIdentityMappingsConfig"></param>
        public UserIdMappingAlgorithmPool(
            UserIdentityMappings userIdentityMappingsConfig,
            NotifyingCollection<Session> sessions)
        {
            m_baseMappingAlgorithm = new UserIdMappingAlgorithm(userIdentityMappingsConfig, sessions);
        }

        /// <summary>
        /// Gets a thread-specific algorithm instance.
        /// </summary>
        /// <returns></returns>
        public UserIdMappingAlgorithm GetAlgorithm()
        {
            UserIdMappingAlgorithm algorithm = null;
            int threadId = Thread.CurrentThread.ManagedThreadId;

            lock (m_perThreadMappingAlgPoolLock)
            {
                if (m_perThreadMappingAlgPool.ContainsKey(threadId))
                {
                    // an algorithm instance is available for re-use in the current thread
                    algorithm = m_perThreadMappingAlgPool[threadId];
                }
            }

            if (algorithm == null)
            {
                lock (m_baseMappingAlgorithmLock)
                {
                    // create a new algorithm instance for this thread
                    algorithm = new UserIdMappingAlgorithm(m_baseMappingAlgorithm);
                }

                lock (m_perThreadMappingAlgPoolLock)
                {
                    // add the new instance to the pool for re-use
                    m_perThreadMappingAlgPool.Add(threadId, algorithm);
                }
            }

            return algorithm;
        }
    }
}
