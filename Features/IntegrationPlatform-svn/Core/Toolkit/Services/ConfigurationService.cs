// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.BusinessModel.VC;
using Microsoft.TeamFoundation.Migration.BusinessModel.WIT;
using Microsoft.TeamFoundation.Migration.EntityModel;
using BM = Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// The ConfigurationService manages the configuration information for one of the MigrationSource objects in 
    /// the migration Session.
    /// </summary>
    /// <seealso cref="Microsoft.TeamFoundation.Migration.BusinessModel.Session"/>
    /// <seealso cref="Microsoft.TeamFoundation.Migration.BusinessModel.MigrationSource"/>
    public class ConfigurationService : IServiceProvider
    {
        private const int MaxWorkspaceNameLength = 64;

        Guid m_sourceId;
        Guid m_migrationPeer;
        bool m_isLeft;
        BM.Configuration m_configuration;
        Session m_session;
        string m_workspaceRoot;

        /// <summary>
        /// Instantiates a new ConfigurationService object.  
        /// </summary>
        /// <param name="session">The session object for this service</param>
        /// <param name="sourceId">The unique Id for the MigrationSource</param>
        /// <seealso cref="Microsoft.TeamFoundation.Migration.BusinessModel.Session"/>
        /// <seealso cref="Microsoft.TeamFoundation.Migration.BusinessModel.MigrationSource"/>
        public ConfigurationService(BM.Configuration configuration, Session session, Guid sourceId)
        {
            m_configuration = configuration;
            m_session = session;
            m_sourceId = sourceId;
            m_isLeft = (sourceId == new Guid(session.LeftMigrationSourceUniqueId));

            Debug.Assert(m_isLeft || new Guid(session.RightMigrationSourceUniqueId) == sourceId);
            
            m_migrationPeer = m_isLeft ? 
                  new Guid(session.RightMigrationSourceUniqueId)
                : new Guid(session.LeftMigrationSourceUniqueId);
        }

        /// <summary>
        /// Provides a method to get the service of current object.
        /// </summary>
        /// <param name="serviceType">Type of the service being requested</param>
        /// <returns>Returns this service object if the requested type is ConfigurationService; otherwise, null is returned.</returns>
        public object GetService(Type serviceType)
        {
            if (serviceType.Equals(typeof(ConfigurationService)))
            {
                return this;
            }
            return null;
        }

        /// <summary>
        /// Flag indicating whether this ConfigurationService is for the left side of the configuration.  
        /// This is true with the related Session object's LeftMigrationSourceUniqueId is the same as this
        /// ConfigurationService's Id.
        /// </summary>
        public bool IsLeftSideInConfiguration
        {
            get
            {
                return m_isLeft;
            }
        }

        /// <summary>
        /// Returns the WitCustomSettings associated with the Session object for this ConfiguationService.
        /// </summary>
        public WITSessionCustomSetting WitCustomSetting
        {
            get
            {
                return m_session.WITCustomSetting;
            }
        }

        /// <summary>
        /// Returns the VCSessionCustomSetting associated with the Session object for this ConfiguationService.
        /// </summary>
        public VCSessionCustomSetting VcCustomSetting
        {
            get
            {
                return m_session.VCCustomSetting;
            }
        }

        /// <summary>
        /// Returns the ServiceUrl for the MigrationSource associated with this ConfigurationService.
        /// </summary>
        public string ServerUrl
        {
            get
            {
                return m_session.MigrationSources[SourceId].ServerUrl;
            }
        }

        /// <summary>
        /// Returns the peer ServiceUrl for the MigrationSource associated with this ConfigurationService.
        /// </summary>
        public string PeerServerUrl
        {
            get
            {
                return m_session.MigrationSources[MigrationPeer].ServerUrl;
            }
        }

        /// <summary>
        /// GetValue(string) is not implemented for ConfigurationService. This method will raise a NotImplementedException.
        /// </summary>
        /// <param name="name">The name of the value to retrieve.</param>
        /// <returns>Not Applicable</returns>
        public string GetValue(string name)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// TODO (Currently returns the defaultValue passed in as a parameter.)
        /// </summary>
        /// <typeparam name="T">The type of the value to return</typeparam>
        /// <param name="name">Name of the value to return</param>
        /// <param name="defaultValue">Default value to return if the named value is not found.</param>
        /// <returns>TODO (Currently the defaultValue)</returns>
        public T GetValue<T>(string name, T defaultValue)
            where T : IConvertible
        {
            // ToDo 
            return defaultValue;
        }

        /// <summary>
        /// Registers this ConfigurationService's Session and SourceUniqueId with the specified IHighWaterMark.
        /// </summary>
        /// <param name="highWaterMark">The HighWaterMark object with which to register this ConfigurationService</param>
        /// <seealso cref="Microsoft.TeamFoundation.Migration.Toolkit.HighWaterMark&lt;T&gt;"/>
        /// <seealso cref="Microsoft.TeamFoundation.Migration.Toolkit.IHighWaterMark"/>
        public void RegisterHighWaterMarkWithSession(IHighWaterMark highWaterMark)
        {
            highWaterMark.SessionUniqueId = new Guid(m_session.SessionUniqueId);
            highWaterMark.SourceUniqueId = m_sourceId;
        }

        /// <summary>
        /// Registers this ConfigurationService's Session and SourceUniqueId with the specified IHighWaterMark.
        /// </summary>
        /// <param name="highWaterMark">The HighWaterMark object with which to register this ConfigurationService</param>
        /// <seealso cref="Microsoft.TeamFoundation.Migration.Toolkit.HighWaterMark&lt;T&gt;"/>
        /// <seealso cref="Microsoft.TeamFoundation.Migration.Toolkit.IHighWaterMark"/>
        public void RegisterHighWaterMarkWithSession(IHighWaterMark highWaterMark, Guid migrationPeer)
        {
            highWaterMark.SessionUniqueId = new Guid(m_session.SessionUniqueId);
            highWaterMark.SourceUniqueId = migrationPeer;
        }

        /// <summary>
        /// Returns the unique name for the workspace associated with this ConfigurationService
        /// Will truncate unnecessary information to fit within MaxWorkspaceNameLength using this order:
        /// 1. SourceId
        /// 2. ComputerName
        /// 3. SourceId.FriendlyName
        /// Format: [ComputerName][SourceId][SourceId.FriendlyName]
        /// </summary>
        public string Workspace
        {
            get
            {
                string workspaceName;

                string sourceId = m_sourceId.ToString("N"); // "N" uses less characters than default ToString()
                string computerName = SystemInformation.ComputerName;
                
                if (computerName.Length + sourceId.Length > MaxWorkspaceNameLength)
                {
                    workspaceName = computerName.Remove(MaxWorkspaceNameLength - sourceId.Length) + sourceId;
                }
                else
                {
                    workspaceName = computerName + sourceId;
                    string sourceFriendlyName = m_session.MigrationSources[m_sourceId].FriendlyName;

                    if (workspaceName.Length + sourceFriendlyName.Length > MaxWorkspaceNameLength)
                    {
                        workspaceName = workspaceName + sourceFriendlyName.Remove(MaxWorkspaceNameLength - workspaceName.Length);
                    }
                    else
                    {
                        workspaceName = workspaceName + sourceFriendlyName;
                    }
                }
                return workspaceName.Trim();
            }
        }

        /// <summary>
        /// Returns the filesystem path to the root of the associated workspace.  This path is located under the 
        /// root directory specified by the GlobalConfiguration's WorkSpaceRoot.
        /// </summary>
        /// <seealso cref="Microsoft.TeamFoundation.Migration.GlobalConfiguration"/>
        public string WorkspaceRoot
        {
            get
            {
                if (string.IsNullOrEmpty(m_workspaceRoot))
                {
                    string rootFolderName = FindUniqueWorkSpaceRootName();
                    m_workspaceRoot = Path.Combine(GlobalConfiguration.WorkSpaceRoot, rootFolderName);

                    if (!Directory.Exists(m_workspaceRoot))
                    {
                        Directory.CreateDirectory(m_workspaceRoot);
                    }
                }

                Debug.Assert(Directory.Exists(m_workspaceRoot),
                    string.Format("WorkSpaceRoot '{0}' does not exist on disk", m_workspaceRoot));
                return m_workspaceRoot;
            }
        }

        private string FindUniqueWorkSpaceRootName()
        {
            // use the unique MigrationSource internal Id as the work space root name
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var msQuery = context.RTMigrationSourceSet.Where(ms => ms.UniqueId.Equals(m_sourceId));
                Debug.Assert(msQuery.Count() > 0, "Migration source information has not been persisted to storage yet.");

                return msQuery.First().Id.ToString();
            }
        }

        public SessionGroupElement SessionGroup
        {
            get { return m_configuration == null ? null : m_configuration.SessionGroup; }
        }

        /// <summary>
        /// Returns the SourceId for the MigrationSource associated with this ConfigurationService's Session object.
        /// </summary>
        public Guid SourceId
        {
            get
            {
                return m_sourceId;
            }
        }

        /// <summary>
        /// Returns the MigrationSource object associated with this ConfigurationService SourceId.  If the 
        /// SourceId does not match a registered MigrationSource, this property returns null.
        /// </summary>
        public Microsoft.TeamFoundation.Migration.BusinessModel.MigrationSource MigrationSource
        {
            get
            {
                if (!m_session.MigrationSources.ContainsKey(this.m_sourceId))
                {
                    return null;
                }
                return m_session.MigrationSources[this.m_sourceId];
            }
        }

        /// <summary>
        /// Peer migration source 
        /// </summary>
        public Microsoft.TeamFoundation.Migration.BusinessModel.MigrationSource PeerMigrationSource
        {
            get
            {
                if (!m_session.MigrationSources.ContainsKey(MigrationPeer))
                {
                    return null;
                }
                return m_session.MigrationSources[MigrationPeer];
            }
        }

        /// <summary>
        /// Peer side's source Guid 
        /// </summary>
        public Guid MigrationPeer
        {
            get
            {
                return m_migrationPeer;
            }
        }

        /// <summary>
        /// Returns a list of the MappingEntry filters including the associated version control paths and 
        /// whether or not those paths are cloaked.
        /// </summary>
        public ReadOnlyCollection<MappingEntry> Filters
        {
            get
            {
                return GetFilters();
            }
        }

        private ReadOnlyCollection<MappingEntry> GetFilters()
        {
            List<MappingEntry> filters = new List<MappingEntry>();

            foreach (FilterPair filterPair in this.m_session.Filters.FilterPair)
            {
                foreach (FilterItem filterItem in filterPair.FilterItem)
                {
                    if (this.m_sourceId.Equals(new Guid(filterItem.MigrationSourceUniqueId)))
                    {
                        MappingEntry e = new MappingEntry(filterItem.FilterString, filterPair.Neglect, filterItem.SnapshotStartPoint, filterItem.PeerSnapshotStartPoint, filterItem.MergeScope);
                        filters.Add(e);
                    }
                }
            }
            
            return filters.AsReadOnly();
        }
    }
}
