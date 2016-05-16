// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Shell.Properties;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    /// <summary>
    /// ConfigurationModel is a wrapper that shows a file oriented face to the infrastructure 
    /// built on top of ModelRoot.
    /// </summary>
    public class ConfigurationModel : ModelRoot
    {
        #region Fields
        private Configuration m_configuration;
        #endregion

        #region Constructors
        static ConfigurationModel()
        {
            // TODO: Use form of constructor that takes schema: TfsMigrationConfigurationXMLSchema.xsd
            Initialize();
        }
        private static bool s_isInitialized = false;
        public static void Initialize()
        {
            if (!s_isInitialized)
            {
                ModelRoot.RegisterSerializer<ConfigurationModel>(new ConfigurationModelSerializer());
                s_isInitialized = true;
            }
        }

        public ConfigurationModel()
        {
            InitializeConfiguration();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Configuration is the real Model within the Model.  
        /// </summary>
        public Configuration Configuration
        {
            get
            {
                return m_configuration;
            }
            set
            {
                if (m_configuration != value)
                {
                    Configuration oldValue = m_configuration;
                    m_configuration = value;

                    m_configuration.PropertyChanged += new UndoablePropertyChangedEventHandler(OnConfigurationPropertyChanged);

                    RaisePropertyChangedEvent("Configuration", oldValue, value);
                }
            }
        }

        public Type ConfigurationType
        {
            get
            {
                return typeof(Configuration);
            }
        }
        #endregion

        #region Public Methods
        public static ConfigurationModel Load(Guid sessionGroupUniqueId)
        {
            // TODO: Statics on the ConfigurationManager should take the sessionGroupUniqueId
            BusinessModelManager businessModelManager = new BusinessModelManager();
            ConfigurationModel model = new ConfigurationModel();
            model.Configuration = businessModelManager.LoadConfiguration(sessionGroupUniqueId);
            return model;
        }

        public void Save(bool saveAsNew)
        {
            BusinessModelManager businessModelManager = new BusinessModelManager();

            if (businessModelManager.IsConfigurationPersisted(this.Configuration))
            {
                Configuration.UniqueId = Guid.NewGuid().ToString();
                if (saveAsNew)
                {
                    Configuration.SessionGroup.SessionGroupGUID = Guid.NewGuid().ToString();
                    foreach (Session session in Configuration.SessionGroup.Sessions.Session)
                    {
                        session.SessionUniqueId = Guid.NewGuid().ToString();
                    }
                }
            }

            SessionGroupConfigurationManager configSaver = new SessionGroupConfigurationManager(this.Configuration);
            configSaver.TrySave(!saveAsNew);
        }

        #endregion

        #region Protected Methods
        protected override void OnAfterLoad(string path)
        {
            // DEBUG - Pushing any loaded configs to TfsMigrationDB
            //m_configuration.TrySave();
        }
        #endregion

        #region Private Methods
        private void OnConfigurationPropertyChanged(ModelObject sender, UndoablePropertyChangedEventArgs eventArgs)
        {
        }
        
        /// <summary>
        /// Create and set up the basic top level structure of our Configuration object.
        /// </summary>
        private void InitializeConfiguration()
        {
            this.Configuration = new Configuration();

            this.Configuration.UniqueId = Guid.NewGuid().ToString();
            this.Configuration.FriendlyName = ModelResources.ConfigurationFriendlyNameString;

            this.Configuration.SessionGroup.SessionGroupGUID = Guid.NewGuid().ToString();
            this.Configuration.SessionGroup.FriendlyName = ModelResources.ConfigurationSessionGroupNameString;
        }
        #endregion
    }
}
