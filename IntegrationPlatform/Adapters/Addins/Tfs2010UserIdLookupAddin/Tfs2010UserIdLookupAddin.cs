// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2010UserIdLookupAddin
{
    /// <summary>
    /// Provides the Active Directory user lookup service based on TFS Object Model
    /// </summary>
    public class Tfs2010UserIdLookupAddin : IAddin, IUserIdentityLookupServiceProvider
    {
        // teyang_todo cache search results

        /// <summary>
        /// The GUID string of the Reference Name of this Add-in
        /// </summary>
        public const string ReferenceNameString = "CDDE6B6B-72FC-43b6-BBD1-B8A89A788C6F";

        Dictionary<Guid, TfsCore> m_perMigrationSourceTfsCores = new Dictionary<Guid,TfsCore>();
        object m_perMigrationSourceTfsCoresLock = new object();

        #region IAddin Members

        /// <summary>
        /// The Reference Name of this Add-in
        /// </summary>
        public Guid ReferenceName
        {
            get { return new Guid(ReferenceNameString); }
        }

        public virtual string FriendlyName
        {
            get
            {
                return Tfs2010UserIdLookupAddinResources.AddinFriendlyName;
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
                // TODO: TERRY: Please verify these are correct?   2008 adapters not supported?   TfsVC2010Adapter supported?
                List<Guid> customSettingKeys = new List<Guid>();

                // Tfs2008WITAdapter
                customSettingKeys.Add(new Guid("663A8B36-7852-4750-87FC-D189B0640FC1"));

                // TfsVC2008Adapter
                customSettingKeys.Add(new Guid("2F82C6C4-BBEE-42fb-B3D0-4799CABCF00E"));

                // Tfs2010WITAdapter
                customSettingKeys.Add(new Guid("04201D39-6E47-416f-98B2-07F0013F8455"));

                // TfsVC2010Adapter
                customSettingKeys.Add(new Guid("FEBC091F-82A2-449e-AED8-133E5896C47A"));

                // Tfs11WITAdapter
                customSettingKeys.Add(new Guid("B84B30DD-1496-462A-BD9D-5A078A617779"));

                // Tfs11VCAdapter
                customSettingKeys.Add(new Guid("4CC33B2B-4B76-451F-8C2C-D86A3846D6D2"));

                return new ReadOnlyCollection<Guid>(customSettingKeys);
            }   
        }

        public void Initialize(Configuration configuration)
        {
            foreach (MigrationSource ms in configuration.SessionGroup.MigrationSources.MigrationSource)
            {
                try
                {
                    // teyang_todo : consider to use only one tfscore per tfs instance
                    TfsCore core = new TfsCore(ms.ServerUrl);
                    Guid migrSrcUniqueId = new Guid(ms.InternalUniqueId);
                    if (!m_perMigrationSourceTfsCores.ContainsKey(migrSrcUniqueId))
                    {
                        m_perMigrationSourceTfsCores.Add(migrSrcUniqueId, core);
                    }
                }
                catch (Exception)
                {
                    TraceManager.TraceInformation("Active Directory lookup will be used for this end point.");

                    // use the default constructor to create a core that only queries AD
                    TfsCore core = new TfsCore();
                    Guid migrSrcUniqueId = new Guid(ms.InternalUniqueId);
                    if (!m_perMigrationSourceTfsCores.ContainsKey(migrSrcUniqueId))
                    {
                        m_perMigrationSourceTfsCores.Add(migrSrcUniqueId, core);
                    }
                }
            }
        }

        public void Dispose()
        {
        }

        #endregion

        #region IServiceProvider Members

        /// <summary>
        /// Gets the service object of the specified type
        /// </summary>
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IUserIdentityLookupServiceProvider))
            {
                return this;
            }

            return null;
        }

        #endregion

        #region IUserIdentityLookupServiceProvider Members

        public bool TryLookup(RichIdentity richIdentity, IdentityLookupContext context)
        {
            TfsCore core = null;
            lock (m_perMigrationSourceTfsCoresLock)
            {
                if (!m_perMigrationSourceTfsCores.ContainsKey(context.SourceMigrationSourceId))
                {
                    return false;
                }

                core = m_perMigrationSourceTfsCores[context.SourceMigrationSourceId];
            }

            return core.TryLookup(richIdentity, context);
        }

        #endregion

        public bool TryLookup(RichIdentity richIdentity, string serverUrl)
        {
            TfsCore core = new TfsCore(serverUrl);
            return core.TryLookup(richIdentity, null);
        }
    }
}
