// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// This base class provides the basic implementation of IAnalysisProvider interface
    /// </summary>
    public abstract class AnalysisProviderBase : IAnalysisProvider
    {
        protected ICollection<Guid> m_supportedChangeActionsOther;
        protected Collection<ContentType> m_supportedContentTypesOther;

        public abstract Dictionary<Guid, Services.ChangeActionHandler> SupportedChangeActions
        {
            get;
        }

        public virtual ICollection<Guid> SupportedChangeActionsOther
        {
            protected get
            {
                return m_supportedChangeActionsOther;
            }
            set
            {
                m_supportedChangeActionsOther = value;
            }
        }

        public abstract Collection<ContentType> SupportedContentTypes
        {
            get;
        }

        public virtual Collection<ContentType> SupportedContentTypesOther
        {
            protected get
            {
                return m_supportedContentTypesOther;
            }
            set
            {
                m_supportedContentTypesOther = value;
            }
        }

        public abstract void InitializeServices(IServiceContainer analysisServiceContainer);
        
        public abstract void InitializeClient();
       
        public abstract void RegisterSupportedChangeActions(ChangeActionRegistrationService changeActionRegistrationService);
        
        public abstract void RegisterSupportedContentTypes(ContentTypeRegistrationService contentTypeRegistrationService);
        
        public abstract void RegisterConflictTypes(ConflictManager conflictManager);
        
        public virtual void GenerateContextInfoTable()
        {
            return;
        }

        public abstract void GenerateDeltaTable();
        
        public virtual void DetectConflicts(ChangeGroup changeGroup)
        {
            return;
        }

        public virtual object GetService(Type serviceType)
        {
            if (serviceType == typeof(IAnalysisProvider))
            {
                return this;
            }

            return null;
        }

        public virtual void Dispose()
        {
        }

        public virtual string GetNativeId(BusinessModel.MigrationSource migrationSourceConfig)
        {
            return migrationSourceConfig.ServerUrl;
        }
    }
}
