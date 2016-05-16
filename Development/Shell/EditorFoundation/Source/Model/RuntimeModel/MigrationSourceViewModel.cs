// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    public class MigrationSourceViewModel : ModelObject
    {
        RTMigrationSource m_migrationSource;
        ProviderViewModel m_providerViewModel;

        public MigrationSourceViewModel(RTMigrationSource migrationSource)
        {
            m_migrationSource = migrationSource;
        }

        #region Properties
        // RTMigrationSource
        public int Id
        {
            get { return m_migrationSource.Id; }
        }

        public Guid UniqueId
        {
            get { return m_migrationSource.UniqueId; }
        }

        public string FriendlyName
        {
            get { return m_migrationSource.FriendlyName; }
        }
                
        public string ServerIdentifier
        {
            get { return m_migrationSource.ServerIdentifier; }
        }
                
        public string ServerUrl
        {
            get { return m_migrationSource.ServerUrl; }
        }
                        
        public string SourceIdentifier
        {
            get { return m_migrationSource.SourceIdentifier; }
        }

        public ProviderViewModel Provider
        {
            get
            {
                if (m_providerViewModel == null)
                {
                    m_migrationSource.ProviderReference.Load();
                    m_providerViewModel = new ProviderViewModel(m_migrationSource.Provider);
                }
                return m_providerViewModel;
            }
        }

        #endregion

        //[XmlIgnore]
        //[EdmRelationshipNavigationProperty("TfsMigrationRuntimeEntityModel", "FK_RT_ArtifactLinks", "RUNTIME_ARTIFACT_LINKS")]
        //[SoapIgnore]
        //public EntityCollection<RTArtifactLink> ArtifactLinksAsSourceSide { get; set; }
        //[SoapIgnore]
        //[XmlIgnore]
        //[EdmRelationshipNavigationProperty("TfsMigrationRuntimeEntityModel", "FK_RT_ChangeGroups1", "RUNTIME_CHANGE_GROUPS")]
        //public EntityCollection<RTChangeGroup> ChangeGroupsAsSourceSide { get; set; }
        //[SoapIgnore]
        //[EdmRelationshipNavigationProperty("TfsMigrationRuntimeEntityModel", "FK_MigrationSourceConfigs", "MIGRATION_SOURCE_CONFIGS")]
        //[XmlIgnore]
        //public EntityCollection<RTMigrationSourceConfig> Configs { get; set; }
        //[SoapIgnore]
        //[EdmRelationshipNavigationProperty("TfsMigrationRuntimeEntityModel", "FK_Conflicts4", "CONFLICT_CONFLICTS")]
        //[XmlIgnore]
        //public EntityCollection<RTConflict> ConflictsAsSourceSide { get; set; }
        //[EdmRelationshipNavigationProperty("TfsMigrationRuntimeEntityModel", "FK_RT_MigrationItems", "RUNTIME_MIGRATION_ITEMS")]
        //[XmlIgnore]
        //[SoapIgnore]
        //public EntityCollection<RTMigrationItem> Items { get; set; }
        //[EdmRelationshipNavigationProperty("TfsMigrationRuntimeEntityModel", "FK_MigrationSources1", "PROVIDERS")]
        //[SoapIgnore]
        //[XmlIgnore]
        //public EntityReference<RTProvider> ProviderReference { get; set; }
        //[EdmRelationshipNavigationProperty("TfsMigrationRuntimeEntityModel", "FK_ConvHistory_to_MigrationSource", "RTConversionHistory")]
        //[SoapIgnore]
        //[XmlIgnore]
        //public EntityCollection<RTConversionHistory> RUNTIME_CONVERSION_HISTORY { get; set; }
        //[EdmRelationshipNavigationProperty("TfsMigrationRuntimeEntityModel", "FK_RT_Sessions1", "RUNTIME_SESSIONS")]
        //[XmlIgnore]
        //[SoapIgnore]
        //public EntityCollection<RTSession> SessionsAsLeftSource { get; set; }
        //[SoapIgnore]
        //[EdmRelationshipNavigationProperty("TfsMigrationRuntimeEntityModel", "FK_RT_Sessions2", "RUNTIME_SESSIONS")]
        //[XmlIgnore]
        //public EntityCollection<RTSession> SessionsAsRightSource { get; set; }
    }
}
