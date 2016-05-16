// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Services
{
    /// <summary>
    /// Service that helps to look up user identity.
    /// </summary>
    /// <remarks>
    /// This service can be consumed by multiple session worker threads.
    /// </remarks>
    internal class UserIdentityLookupService : IUserIdentityLookupService
    {
        UserIdLookupProviderRegistry m_lookupProviderRegistry = new UserIdLookupProviderRegistry();
        UserIdMappingAlgorithmPool m_mappingAlgorithmPool;
        UserIdLookupConfiguration m_lookupConfiguration;
        readonly bool m_serviceIsConfigured;
        readonly AddinManagementService m_addinManagementService;

        internal UserIdentityLookupService(Configuration configuration, AddinManagementService addinManagementService)
        {
            UserIdentityMappings userIdentityMappingsConfig = configuration.SessionGroup.UserIdentityMappings;
            MigrationSourcesElement migrationSourcesConfig = configuration.SessionGroup.MigrationSources;

            m_mappingAlgorithmPool = new UserIdMappingAlgorithmPool(configuration.SessionGroup.UserIdentityMappings, 
                                                                    configuration.SessionGroup.Sessions.Session);
            m_lookupConfiguration = new UserIdLookupConfiguration(configuration.SessionGroup.UserIdentityMappings.EnableValidation, 
                                                                  configuration.SessionGroup.MigrationSources);

            m_serviceIsConfigured = (userIdentityMappingsConfig.UserMappings.Count != 0
                                    || userIdentityMappingsConfig.DisplayNameMappings.Count != 0
                                    || userIdentityMappingsConfig.AliasMappings.Count != 0
                                    || userIdentityMappingsConfig.DomainMappings.Count != 0
                                    );

            m_addinManagementService = addinManagementService;

            foreach (AddinElement addinElem in configuration.Addins.Addin)
            {
                Guid addinRefName = new Guid(addinElem.ReferenceName);
                IAddin addin = m_addinManagementService.GetAddin(addinRefName);
                if (null != addin)
                {
                    var userLookupProvider = addin.GetService(typeof(IUserIdentityLookupServiceProvider)) as IUserIdentityLookupServiceProvider;
                    if (null != userLookupProvider)
                    {
                        this.RegisterLookupServiceProvider(addin.ReferenceName, userLookupProvider);
                    }
                }
            }
        }

        public bool IsConfigured
        {
            get { return m_serviceIsConfigured; }
        }

        #region IUserIdentityLookupService Members

        /// <summary>
        /// Based on the context and the user Id mapping configuration, translate a source (original)
        /// user identity to a target (translated) user identity.
        /// </summary>
        /// <param name="originalUserIdentity">The original user identity</param>
        /// <param name="context"></param>
        /// <param name="translatedUserIdentity"></param>
        /// <returns>TRUE if lookup succeeded; FALSE otherwise.</returns>
        public bool TryLookup(
            RichIdentity originalUserIdentity, 
            IdentityLookupContext context, 
            out RichIdentity translatedUserIdentity)
        {
            if (null == originalUserIdentity)
            {
                throw new ArgumentNullException("originalUserIdentity");
            }

            if (null == context)
            {
                throw new ArgumentNullException("context");
            }

            translatedUserIdentity = originalUserIdentity;
            if (!m_serviceIsConfigured)
            {
                // when the service is not configured, i.e. <UserIdentityMappings> is missing or empty
                // we simply return the original user identity as the lookup result
                return true;
            }

            // 1. [optional] look up on source side to find the original user id and refresh its properties
            if (!LookupUserIdOnOneSide(originalUserIdentity, context, context.SourceMigrationSourceId))
            {
                TraceManager.TraceError(@"User '{0}, {1}\{2}' cannot be found on source system. ",
                    string.IsNullOrEmpty(originalUserIdentity.DisplayName) ? "Unknown DisplayName" : originalUserIdentity.DisplayName,
                    string.IsNullOrEmpty(originalUserIdentity.Domain) ? "Unknown Domain" : originalUserIdentity.Domain,
                    string.IsNullOrEmpty(originalUserIdentity.Alias) ? "Unknown Alias" : originalUserIdentity.Alias);
            }

            // 2. map original user id properties to target one's properties
            RichIdentity mappedUserId = null;
            if (TryMapUserIdentity(originalUserIdentity, context, out mappedUserId))
            {
                translatedUserIdentity = mappedUserId;
            }
            else
            {
                return false;
            }
            
            // 3. [optional] look up on target side to find the mapped user id and refresh its properties
            IdentityLookupContext reversedContext = context.Reverse();
            if (LookupUserIdOnOneSide(mappedUserId, reversedContext, context.TargetMigrationSourceId))
            {
                translatedUserIdentity = mappedUserId;
                return true;
            }
            else
            {
                TraceManager.TraceError(@"User '{0} {1}\{2}' cannot be found on target system. ",
                    string.IsNullOrEmpty(translatedUserIdentity.DisplayName) ? "Unknown Display" : translatedUserIdentity.DisplayName,
                    string.IsNullOrEmpty(translatedUserIdentity.Domain) ? "Unknown Domain" : translatedUserIdentity.Domain,
                    string.IsNullOrEmpty(translatedUserIdentity.Alias) ? "Unknown Alias" : translatedUserIdentity.Alias);
                return true;
            }
        }

        /// <summary>
        /// Registers a lookup service provider.
        /// </summary>
        /// <param name="referenceName"></param>
        /// <param name="provider"></param>
        public void RegisterLookupServiceProvider(
            Guid referenceName, 
            IUserIdentityLookupServiceProvider provider)
        {
            m_lookupProviderRegistry.RegisterLookupServiceProvider(referenceName, provider);
        }

        #endregion

        #region IUserIdentityFactory Members

        /// <summary>
        /// Creates a RichdIdentity instance.
        /// </summary>
        /// <returns></returns>
        public RichIdentity CreateUserIdentity()
        {
            return new RichIdentity();
        }

        #endregion

        /// <summary>
        /// Basic user identity property, e.g. Alias and email address, mapping based on the mapping configuration.
        /// </summary>
        /// <param name="originalUserIdentity"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private bool TryMapUserIdentity(
            RichIdentity originalUserIdentity,
            IdentityLookupContext context,
            out RichIdentity mappingResult)
        {
            UserIdMappingAlgorithm algorithm = m_mappingAlgorithmPool.GetAlgorithm();
            return algorithm.TryMapUserIdentity(originalUserIdentity, context, out mappingResult);
        }

        private bool LookupUserIdOnOneSide(RichIdentity userIdentity, IdentityLookupContext context, Guid ownerMigrationSourceId)
        {
            if (!m_lookupConfiguration.LookupIsEnabled)
            {
                return true;
            }

            bool lookupSucceeded = false;
            Guid[] applicableProviderRefNames;
            lookupSucceeded = m_lookupConfiguration.TryGetConfiguredProviders(ownerMigrationSourceId, out applicableProviderRefNames);

            if (lookupSucceeded)
            {
                if (applicableProviderRefNames.Length == 0)
                {
                    // no specific set of lookup service providers is configured to be used for the subject Migration Source
                    // we default to use all of them
                    applicableProviderRefNames = m_lookupProviderRegistry.GetAllProviderReferenceNames();
                }

                foreach (Guid providerRefName in applicableProviderRefNames)
                {
                    IUserIdentityLookupServiceProvider provider = m_lookupProviderRegistry.GetProvider(providerRefName);
                    if (null != provider)
                    {
                        lookupSucceeded = provider.TryLookup(userIdentity, context);
                        if (lookupSucceeded)
                        {
                            break;
                        }
                    }
                    else
                    {
                        TraceManager.TraceWarning("Warning: cannot find User Id Lookup Service Provider (Reference name: '{0}')",
                                                  providerRefName.ToString());
                    }
                }
            }

            return lookupSucceeded;
        }

        internal bool TryGetDefaultUserIdProperty(Guid migrationSourceId, out UserIdPropertyNameEnum defaultUserIdProperty)
        {
            return m_lookupConfiguration.TryGetDefaultUserIdPropertyName(migrationSourceId, out defaultUserIdProperty);
        }
    }
}
