// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.Framework.Client;
using System.Globalization;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2010UserIdLookupAddin
{
    class TfsCore
    {
        private IIdentityManagementService m_identityManagement = null;
        private ISecurityService m_securityService = null;
        private IAuthorizationService m_authorizationService = null;
        private ITeamFoundationServiceProvider m_server = null;
        private bool m_adSearchOnly = false;
        /*
         * obsoleting the usage of m_userIdCache to by-pass the TFS user idenitity scalability issue
         * 
         private TFSUserIdCache m_userIdCache = null;
         * 
         */

        private Dictionary<String, SecurityNamespace> s_securityNamespaces =
                new Dictionary<String, SecurityNamespace>(TFStringComparer.OrdinalIgnoreCase);

        public TfsCore()
        {
            m_adSearchOnly = true;
        }

        public TfsCore(string serverUrl)
        {
            m_server = new PostBeta2TeamFoundationServiceProvider(serverUrl);
            InitializeProxies(m_server);
        }

        public Guid InstanceId
        {
            get
            { 
                return m_server.InstanceId; 
            }
        }

        private void InitializeProxies(IServiceProvider server)
        {
            m_identityManagement = server.GetService(typeof(IIdentityManagementService)) as IIdentityManagementService;
            m_securityService = server.GetService(typeof(ISecurityService)) as ISecurityService;
            m_authorizationService = server.GetService(typeof(IAuthorizationService)) as IAuthorizationService;

            /*
             * obsoleting the usage of m_userIdCache to by-pass the TFS user idenitity scalability issue
             * 
            IGroupSecurityService gss = server.GetService(typeof(IGroupSecurityService)) as  IGroupSecurityService;
            if (null != gss)
            {
                m_userIdCache = new TFSUserIdCache(gss);
            }
             * 
             */
        }

        private TeamFoundationIdentity ResolveIdentity(
            string id, 
            MembershipQuery queryMembership, 
            ReadIdentityOptions readIdentityOptions,
            IdentitySearchFactor searchFactor)
        {
            TraceManager.TraceInformation("Resolving Identity: {0}", id);
            TeamFoundationIdentity i = ResolveIdentityInternal(id, queryMembership, readIdentityOptions, searchFactor);
            if (i == null)
            {
                throw new IdentityUnresolvedException(
                    string.Format("Identity '{0}' cannot be resolved", id ?? string.Empty));
            }

            IdentityType type = Identity.GetType(i.Descriptor.IdentityType, i.IsContainer);
            if (type == IdentityType.InvalidIdentity)
            {
                throw new IdentityUnresolvedException(
                    string.Format("Searching '{0}' returned invalid identity.", id ?? string.Empty));
            }

            return i;
        }

        private TeamFoundationIdentity ResolveIdentityInternal(
            string id, 
            MembershipQuery queryMembership, 
            ReadIdentityOptions readIdentityOptions,
            IdentitySearchFactor searchFactor)
        {
            id = id.Trim();
            try
            {
                TeamFoundationIdentity resolvedId = m_identityManagement.ReadIdentity(searchFactor, id, queryMembership, readIdentityOptions);
                if (null == resolvedId)
                {
                    resolvedId = ADSearchAssistResolveIdentityInternal(id, queryMembership, readIdentityOptions);
                }
                return resolvedId;
            }
            catch (System.NotSupportedException notSupportedEx)
            {
                if (notSupportedEx.Message.Contains(searchFactor.ToString())
                    && searchFactor == IdentitySearchFactor.DisplayName)
                {
                    return ADSearchAssistResolveIdentityInternal(id, queryMembership, readIdentityOptions);
                }
                else
                {
                    throw;
                }
            }
        }

        private TeamFoundationIdentity ADSearchAssistResolveIdentityInternal(
            string id, 
            MembershipQuery queryMembership, 
            ReadIdentityOptions readIdentityOptions)
        {
            /*
             * obsoleting the usage of m_userIdCache to by-pass the TFS user idenitity scalability issue
             * 
            if (null == m_userIdCache)
            {
                return null;
            }
            string accountName = m_userIdCache.FindAccountName(id);
             * 
             */
            string accountName = ADUserSearcher.GetAccountName(id);
            if (string.IsNullOrEmpty(accountName))
            {
                return null;
            }
            else
            {
                return m_identityManagement.ReadIdentity(IdentitySearchFactor.AccountName, accountName, queryMembership, readIdentityOptions);
            }
        }

        internal bool TryLookup(
            RichIdentity richIdentity,
            IdentityLookupContext context)
        {
            try
            {
                if (!string.IsNullOrEmpty(richIdentity.DisplayName))
                {
                    if (!m_adSearchOnly)
                    {
                        // "Jeff Smith"
                        TeamFoundationIdentity id = ResolveIdentity(richIdentity.DisplayName,
                                                                    MembershipQuery.None,
                                                                    ReadIdentityOptions.None,
                                                                    IdentitySearchFactor.DisplayName);

                        UpdateRichIdentity(richIdentity, id);
                        return true;
                    }
                    else
                    {
                        return ADUserSearcher.TryUpdateAccountDetails(richIdentity.DisplayName, richIdentity);
                    }
                }
                else if (!string.IsNullOrEmpty(richIdentity.Alias) && !string.IsNullOrEmpty(richIdentity.Domain))
                {
                    if (!m_adSearchOnly)
                    {
                        // "Fabrikam\jeffsmith"
                        string accountName = richIdentity.Domain + @"\" + richIdentity.Alias;
                        TeamFoundationIdentity id = ResolveIdentity(accountName,
                                                                    MembershipQuery.None,
                                                                    ReadIdentityOptions.None,
                                                                    IdentitySearchFactor.AccountName);
                        UpdateRichIdentity(richIdentity, id);
                        return true;
                    }
                }
                else if (!string.IsNullOrEmpty(richIdentity.Alias))
                {
                    if (!m_adSearchOnly)
                    {
                        TeamFoundationIdentity id = ResolveIdentity(richIdentity.Alias,
                                                                    MembershipQuery.None,
                                                                    ReadIdentityOptions.None,
                                                                    IdentitySearchFactor.AccountName);
                        UpdateRichIdentity(richIdentity, id);
                        return true;
                    }
                }
                
                return false;
            }
            catch (IdentityUnresolvedException unrslvEx)
            {
                TraceManager.TraceInformation(unrslvEx.Message);
                return false;
            }
            catch (IllegalIdentityException illglIdEx)
            {
                TraceManager.TraceInformation(illglIdEx.Message);
                return false;
            }
        }

        private void UpdateRichIdentity(RichIdentity richIdentity, TeamFoundationIdentity id)
        {
            const string Unknown = "";

            richIdentity.UniqueId = id.Descriptor.Identifier;
            richIdentity.Alias = id.GetAttribute(IdentityAttributeTags.AccountName, Unknown);
            richIdentity.DisplayName = id.DisplayName;
            richIdentity.DistinguishedName = id.GetAttribute(IdentityAttributeTags.DistinguishedName, Unknown);
            richIdentity.Domain = id.GetAttribute(IdentityAttributeTags.Domain, Unknown);

            if (id.GetAttribute(IdentityAttributeTags.MailAddress, "Unknown").Length != 0)
            {
                richIdentity.EmailAddress = id.GetAttribute(IdentityAttributeTags.MailAddress, Unknown);
            }

            TraceManager.TraceInformation(
                "Updated Identity (Unique Id: {0}; Alias: {1}; Display Name {2}; Distinguished Name: {3}; Domain: {4}",
                richIdentity.UniqueId ?? Unknown, richIdentity.Alias ?? Unknown, richIdentity.DisplayName ?? Unknown,
                richIdentity.DistinguishedName ?? Unknown, richIdentity.Domain ?? Unknown);
        }
    }
}
