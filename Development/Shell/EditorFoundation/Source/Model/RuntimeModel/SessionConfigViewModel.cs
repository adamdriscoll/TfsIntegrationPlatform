// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    public class SessionConfigViewModel : ModelObject
    {

        RTSessionConfig m_sessionConfig;
        MigrationSourceViewModel m_leftMigrationSource;
        MigrationSourceViewModel m_rightMigrationSource;

        public SessionConfigViewModel(RTSessionConfig sessionConfig)
        {
            m_sessionConfig = sessionConfig;
        }

        #region Properties
        // Selected RTSessionConfig properties
        public int Id { get { return m_sessionConfig.Id; } }
        public string FriendlyName { get { return m_sessionConfig.FriendlyName; } }
        public string SettingXml { get { return m_sessionConfig.SettingXml; } }
        public string SettingXmlSchema { get { return m_sessionConfig.SettingXmlSchema; } }
        public int Type { get { return m_sessionConfig.Type; } }
        public Guid SessionUniqueId { get { return m_sessionConfig.SessionUniqueId; } }

        // Demand loaded references
        public MigrationSourceViewModel LeftMigrationSource 
        { 
            get 
            {
                if (! m_sessionConfig.LeftSourceConfigReference.IsLoaded)
                {
                    m_sessionConfig.LeftSourceConfigReference.Load();
                    m_sessionConfig.LeftSourceConfig.MigrationSourceReference.Load();
                    m_leftMigrationSource = new MigrationSourceViewModel(m_sessionConfig.LeftSourceConfig.MigrationSource);
                }

                return m_leftMigrationSource; 
            } 
        }
        
        public MigrationSourceViewModel RightMigrationSource 
        { 
            get 
            {
                if (! m_sessionConfig.RightSourceConfigReference.IsLoaded)
                {
                    m_sessionConfig.RightSourceConfigReference.Load();
                    m_sessionConfig.RightSourceConfig.MigrationSourceReference.Load();
                    m_rightMigrationSource = new MigrationSourceViewModel(m_sessionConfig.RightSourceConfig.MigrationSource);
                }

                return m_rightMigrationSource; 
            } 
        }
               
        #endregion
    }
}
