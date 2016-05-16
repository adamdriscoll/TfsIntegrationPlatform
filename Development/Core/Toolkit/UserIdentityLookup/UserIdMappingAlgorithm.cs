// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal class UserIdMappingAlgorithm
    {
        UserMappingRuleEvaluator m_userMappingAlg;
        DisplayNameMappingRuleEvaluator m_dispNameMappingAlg;
        AliasMappingRuleEvaluator m_aliasMappingAlg;
        DomainMappingRuleEvaluator m_domainMappingAlg;
        IdentityLookupContextManager m_lookupContextManager;

        UserIdentityMappings m_userIdentityMappings; // config backup to be used by copy constructor
        NotifyingCollection<Session> m_sessions;
        object m_configBackupLock = new object();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="userIdentityMappingsConfig"></param>
        public UserIdMappingAlgorithm(
            UserIdentityMappings userIdentityMappingsConfig,
            NotifyingCollection<Session> sessions)
        {
            Initialize(userIdentityMappingsConfig, sessions);            
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="cpy"></param>
        public UserIdMappingAlgorithm(
            UserIdMappingAlgorithm cpy)
        {
            lock (cpy.m_configBackupLock)
            {
                Initialize(cpy.m_userIdentityMappings, cpy.m_sessions);
            }
        }

        public bool TryMapUserIdentity(
            RichIdentity sourceUserIdentity, 
            IdentityLookupContext context,
            out RichIdentity mappedUserIdentity)
        {
            mappedUserIdentity = new RichIdentity();

            // 1. figure out the mapping direction in the identity's host session, e.g. left to right or the opposite
            if (!m_lookupContextManager.TrySetupContext(context))
            {
                TraceManager.TraceError(
                    "Identity look up failed - Migration Source '{0}' and '{1}' in the lookup context does not belong to the same session",
                    context.SourceMigrationSourceId.ToString(), context.TargetMigrationSourceId.ToString());
                return false;
            }

            bool mapped = false;

            // 2. use the most specific mapping: user identity mapping
            mapped |= m_userMappingAlg.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);

            // 3. then use the display name mapping
            mapped |= m_dispNameMappingAlg.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);

            // 4. and then use the alias and/or domain name mapping together
            bool aliasMappingRslt = m_aliasMappingAlg.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);
            bool domainMappingRslt = m_domainMappingAlg.TryMapUserIdentity(sourceUserIdentity, context, mappedUserIdentity);

            return (mapped | (aliasMappingRslt & domainMappingRslt));
        }

        private void Initialize(
            UserIdentityMappings userIdentityMappingsConfig,
            NotifyingCollection<Session> sessions)
        {
            m_userIdentityMappings = userIdentityMappingsConfig;
            m_sessions = sessions;

            // teyang_todo when no mapping is configured, default to: output = input ??
            m_userMappingAlg = new UserMappingRuleEvaluator(userIdentityMappingsConfig.UserMappings);
            m_dispNameMappingAlg = new DisplayNameMappingRuleEvaluator(userIdentityMappingsConfig.DisplayNameMappings);
            m_aliasMappingAlg = new AliasMappingRuleEvaluator(userIdentityMappingsConfig.AliasMappings);
            m_domainMappingAlg = new DomainMappingRuleEvaluator(userIdentityMappingsConfig.DomainMappings);

            m_lookupContextManager = new IdentityLookupContextManager(sessions);
        }
    }
}
