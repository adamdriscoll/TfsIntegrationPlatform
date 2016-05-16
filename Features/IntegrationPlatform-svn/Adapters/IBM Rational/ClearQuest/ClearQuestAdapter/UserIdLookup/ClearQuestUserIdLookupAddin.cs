// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.Exceptions;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.UserIdLookup
{
    class ClearQuestUserIdLookupAddin : IAddin, IUserIdentityLookupServiceProvider
    {
        // key-ed on MigrationSource Unique Id
        Dictionary<Guid, ClearQuestOleServer.Session> m_cqSessions = new Dictionary<Guid, ClearQuestOleServer.Session>();
        
        List<string> m_userNames;
        object m_userNamesLock = new object();


        #region IAddin Members

        public Guid ReferenceName
        {
            get { return new Guid("3ADB3920-4215-4566-A280-907609B0FD23"); ; }
        }

        public virtual string FriendlyName
        {
            get
            {
                return CQResource.CQUserIdLookupAddinFriendlyName;
            }
        }

        public virtual ReadOnlyCollection<string> CustomSettingKeys
        {
            get { return null; }
        }

        public virtual ReadOnlyCollection<Guid> SupportedMigrationProviderNames
        {
            get
            {
                // TODO: TERRY: Please verify this is correct
                List<Guid> customSettingKeys = new List<Guid>();

                // ClearQuestAdapter
                customSettingKeys.Add(new Guid("D9637401-7385-4643-9C64-31585D77ED16"));

                return new ReadOnlyCollection<Guid>(customSettingKeys);
            }
        }
        #endregion

        #region IServiceProvider Members

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IUserIdentityLookupServiceProvider))
            {
                return this;
            }

            return null;
        }

        #endregion

        #region IDisposable members
        public void Dispose()
        {
        }
        #endregion

        #region IUserIdentityLookupServiceProvider Members

        public bool TryLookup(RichIdentity richIdentity, IdentityLookupContext context)
        {
            if (TryLookupInLocalUsers(richIdentity, context))
            {
                return true;
            }

            return TryLookupInLDAPUsers(richIdentity, context);
        }

        public void Initialize(Configuration configuration)
        {
            CredentialManagementService credManagementService = new CredentialManagementService(configuration);

            foreach (MigrationSource ms in configuration.SessionGroup.MigrationSources.MigrationSource)
            {
                try
                {
                    string dbSet = ms.ServerUrl;
                    string userDb = ms.SourceIdentifier;

                    ICQLoginCredentialManager loginCredManager = 
                        CQLoginCredentialManagerFactory.CreateCredentialManager(credManagementService, ms);
                    Guid migrSrcId = new Guid(ms.InternalUniqueId);

                    var userSessionConnConfig = new ClearQuestConnectionConfig(loginCredManager.UserName,
                                                           loginCredManager.Password,
                                                           userDb,
                                                           dbSet);
                    ClearQuestOleServer.Session userSession = CQConnectionFactory.GetUserSession(userSessionConnConfig);

                    if (!m_cqSessions.ContainsKey(migrSrcId))
                    {
                        m_cqSessions.Add(migrSrcId, userSession);
                    }
                }
                catch (ClearQuestInvalidConfigurationException)
                {
                    continue;
                }
                catch (Exception e)
                {
                    TraceManager.TraceException(e);
                }
            }
        }

        #endregion

        ReadOnlyCollection<string> GetUserNames(Guid migrationSourceId)
        {
            lock (m_userNamesLock)
            {
                if (m_cqSessions.ContainsKey(migrationSourceId))
                {
                    return new List<string>().AsReadOnly();
                }

                if (null == m_userNames && null != m_cqSessions[migrationSourceId])
                {
                    m_userNames = new List<string>();

                    object[] userNameObjs = CQWrapper.GetAllUsers(m_cqSessions[migrationSourceId], (int)CQConstants.ExtendedNameOption._NAME_EXTEND_WHEN_NEEDED);

                    foreach (object userNameObj in userNameObjs)
                    {
                        string userName = (string)userNameObj;
                        m_userNames.Add(userName);
                    }
                }

                return m_userNames.AsReadOnly();
            }
        }

        private bool TryLookupInLocalUsers(RichIdentity richIdentity, IdentityLookupContext context)
        {
            if (string.IsNullOrEmpty(richIdentity.Alias) && string.IsNullOrEmpty(richIdentity.DisplayName))
            {
                return false;
            }

            ReadOnlyCollection<string> userNames = GetUserNames(context.SourceMigrationSourceId);
            if (userNames.Contains(richIdentity.Alias))
            {
                richIdentity.DisplayName = richIdentity.Alias;
            }
            else if (userNames.Contains(richIdentity.DisplayName))
            {
                richIdentity.Alias = richIdentity.DisplayName;
            }
            else
            {
                return false;
            }

            return true;
        }

        private bool TryLookupInLDAPUsers(RichIdentity richIdentity, IdentityLookupContext context)
        {
            // throw new NotImplementedException();
            return false;
        }
    }
}
