// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using WinCredentials;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Services
{
    public class CredentialManagementService : ICredentialManagementService
    {
        private Configuration m_config;
        private MigrationSource m_migrationSource;
        private object m_configLock = new object();

        public CredentialManagementService(Configuration config)
        {
            Debug.Assert(config != null, "config is null");
            m_config = config;
            m_migrationSource = null;
        }

        public CredentialManagementService(MigrationSource migrationSource)
        {
            Debug.Assert(migrationSource != null, "migrationSource is null");
            m_config = null;
            m_migrationSource = migrationSource;
        }

        public void AddCredential(Uri uriPrefix, CredentialType credType, NetworkCredential cred)
        {
            if (null == uriPrefix)
            {
                throw new ArgumentNullException("uriPrefix");
            }

            if (null == cred)
            {
                throw new ArgumentNullException("cred");
            }

            // todo: we are not saving any description/comments for the creds we save in the
            // credential store. we may want to add a comment
            WinCredentialsProxy.AddCredential(
                CreateWinCredentialsTargetName(uriPrefix), credType, cred.UserName, cred.Password,
                CredentialPersistence.Session, string.Empty);
        }

        public void DeleteCredential(Uri uriPrefix)
        {
            WinCredentialsProxy.DeleteCredential(CreateWinCredentialsTargetName(uriPrefix));
        }

        public void DeleteCredential(Uri uriPrefix, CredentialType credType)
        {
            WinCredentialsProxy.DeleteCredential(CreateWinCredentialsTargetName(uriPrefix), credType);
        }

        public ReadOnlyCollection<NetworkCredential> GetCredentials(Uri uriPrefix)
        {
            string credTargetName = CreateWinCredentialsTargetName(uriPrefix);
            return GetCredentials(credTargetName);            
        }

        public ReadOnlyCollection<NetworkCredential> GetCredentials(string targetUri)
        {
            var creds = WinCredentialsProxy.LoadCredentials(targetUri);

            List<NetworkCredential> retVal = new List<NetworkCredential>();
            if (creds.Count > 0)
            {
                foreach (var cred in creds)
                {
                    switch (cred.Type)
                    {
                        case CredentialType.DomainPassword:
                        case CredentialType.DomainVisiblePassword:
                            DomainUser dUser = new DomainUser(creds[0].UserName);
                            retVal.Add(new System.Net.NetworkCredential(
                                dUser.UserName, WinCredentialsProxy.GetPlaintextPassword(creds[0]), dUser.Domain));
                            break;
                        default:
                            retVal.Add(new System.Net.NetworkCredential(
                                creds[0].UserName, WinCredentialsProxy.GetPlaintextPassword(creds[0])));
                            break;
                    }
                }
            }

            return retVal.AsReadOnly();
        }

        public NetworkCredential GetCredential(Uri uriPrefix, CredentialType credType)
        {
            var creds = WinCredentialsProxy.LoadCredentials(CreateWinCredentialsTargetName(uriPrefix));
            
            NetworkCredential retVal = null;
            if (creds.Count > 0)
            {
                foreach (var cred in creds)
                {
                    if (cred.Type == credType)
                    {
                        retVal = new System.Net.NetworkCredential(
                            creds[0].UserName, WinCredentialsProxy.GetPlaintextPassword(creds[0]));
                        break;
                    }
                }
            }

            return retVal;            
        }

        public bool IsCredentialStored(Uri uriPrefix, CredentialType credType)
        {
            return WinCredentialsProxy.CredentialExists(
                CreateWinCredentialsTargetName(uriPrefix), credType);
        }
        
        public bool IsMigrationSourceConfiguredToUseStoredCredentials(Guid migrationSourceId)
        {
            lock (m_configLock)
            {
                MigrationSource migrationSource = m_migrationSource;
                if (migrationSource == null && m_config != null)
                {
                    migrationSource = m_config.SessionGroup.MigrationSources[migrationSourceId];
                }

                Debug.Assert(null != migrationSource, 
                    string.Format("Cannot find migration source '{0}'", migrationSourceId.ToString()));

                if (migrationSource == null)
                {
                    return false;
                }

                return !string.IsNullOrEmpty(migrationSource.StoredCredential.CredentialString);
            }
        }

        public ReadOnlyCollection<NetworkCredential> GetCredentialsForMigrationSource(Guid migrationSourceId)
        {
            lock (m_configLock)
            {
                var retVal = new List<NetworkCredential>().AsReadOnly();
                if (IsMigrationSourceConfiguredToUseStoredCredentials(migrationSourceId))
                {
                    MigrationSource migrationSource = m_migrationSource;
                    if (migrationSource == null && m_config != null)
                    {
                        migrationSource = m_config.SessionGroup.MigrationSources[migrationSourceId];
                    }
                    
                    Debug.Assert(null != migrationSource,
                        string.Format("Cannot find migration source '{0}'", migrationSourceId.ToString()));
                    if (migrationSource == null)
                    {
                        return retVal;
                    }

                    try
                    {
                        // 1. try searching by the raw credential string first
                        retVal = this.GetCredentials(NormalizeCredentialsTargetName(migrationSource.StoredCredential.CredentialString));

                        if (retVal.Count == 0)
                        {
                            // 2. try searching by the normalized uri string, if we fail in step 1
                            Uri migrationSourceUri = new Uri(migrationSource.StoredCredential.CredentialString);
                            retVal = this.GetCredentials(migrationSourceUri);
                        }
                    }
                    catch (UriFormatException e)
                    {
                        throw new InvalidMigrationSourceUriException(migrationSource.StoredCredential.CredentialString, e);
                    }
                }
                
                return retVal;
            }
        }

        public static string CreateWinCredentialsTargetName(Uri uriPrefix)
        {
            string absoluteUri = uriPrefix.AbsoluteUri;
            return NormalizeCredentialsTargetName(absoluteUri);
        }

        static string NormalizeCredentialsTargetName(string targetName)
        {
            if (targetName.EndsWith(@"/") || targetName.EndsWith(@"\"))
            {
                targetName = targetName.Substring(0, targetName.Length - 1);
                targetName += "*";
            }

            return targetName;
        }
    }
}
