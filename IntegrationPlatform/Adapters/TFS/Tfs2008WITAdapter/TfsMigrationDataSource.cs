// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

/// TODO: figure out what to do with TfsAttachmentConfig (not present in schema)
/// TODO: figure out what to do with TfsMetadaPolicy (not present in schema)
/// TODO: TFS specific: area and iteration path (not present in schema)

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter
{
    /// <summary>
    /// The enumeration defines what name type will be used with TFS fields.
    /// </summary>
    public enum TfsFieldForm
    {
        Friendly,
        Reference,
    };

    public class TfsMigrationDataSource
    {
        
        /// <summary>
        /// Server id.
        /// </summary>
        public string ServerId
        {
            get { return m_serverId; }
            set { m_serverId = value; }
        }

        /// <summary>
        /// Server Name
        /// </summary>
        public string ServerName
        {
            get
            {
                return m_serverName;
            }
            set
            {
                m_serverName = value;
            }
        }

        /// <summary>
        /// Name of the TFS project.
        /// </summary>
        public string Project
        {
            get { return m_project; }
            set { m_project = value; }
        }

        /// <summary>
        /// Optional filter which will be used to obtain work items. If no filter is specified, all
        /// work items from under the project will be returned.
        /// </summary>
        public string Filter
        {
            get { return m_filter; }
            set { m_filter = value; }
        }

        /// <summary>
        /// Returns the default area, if any.
        /// </summary>
        public string DefaultArea
        {
            get { return m_defaultArea; }
            internal set { m_defaultArea = value; }
        }

        /// <summary>
        /// Returns the default iteration, if any.
        /// </summary>
        public string DefaultIteration
        {
            get { return m_defaultIteration; }
            internal set { m_defaultIteration = value; }
        }

        ///// <summary>
        ///// File attachment configuration parameters.
        ///// </summary>
        //public TfsAttachmentConfiguration FileAttachmentConfig { get { return m_fileAttachConfig; } }

        /// <summary>
        /// Returns field type that will be used with the TFS source.
        /// </summary>
        public TfsFieldForm FieldForm { get { return TfsFieldForm.Reference; } }

        ///// <summary>
        ///// Returns metadata synchronization policy.
        ///// </summary>
        //public TfsMetadataPolicy MetadataSynchronizationPolicy { get { return m_metadataSync; } }

        ///// <summary>
        ///// Gets the list of flat fields.
        ///// </summary>
        //internal List<string> FlatFields { get { return m_flatFields; } }

        ///// <summary>
        ///// Initializes object from the XML.
        ///// </summary>
        ///// <param name="nav">XML navigator positioned to the Tfs section</param>
        ///// <param name="session">Current session information</param>
        //internal TfsMigrationDataSource(
        //    XPathNavigator nav,
        //    WorkItemTrackingSession session)
        //{
        //    m_session = session;
        //    m_flatFields = new List<string>();

        //    Debug.Assert(nav.Name == "Tfs", "Invalid XML!");
        //    m_server = nav.GetAttribute("server", string.Empty);
        //    m_project = nav.SelectSingleNode("Project").Value;

        //    // Filter is optional
        //    XPathNavigator filter = nav.SelectSingleNode("Filter");
        //    if (filter != null)
        //    {
        //        m_filter = filter.Value;
        //    }
        //    else
        //    {
        //        m_filter = string.Empty;
        //    }

        //    // Default area is optional
        //    XPathNavigator def = nav.SelectSingleNode("DefaultArea");
        //    m_defaultArea = def == null ? string.Empty : def.Value;

        //    // Default iteration is optional
        //    def = nav.SelectSingleNode("DefaultIteration");
        //    m_defaultIteration = def == null ? string.Empty : def.Value;

        //    m_writeQueueConfig = QueueConfiguration.Create(
        //        QueueType.Write, 
        //        nav.SelectSingleNode("WriteQueue"));

        //    m_fileAttachConfig = TfsAttachmentConfiguration.Create(
        //        nav.SelectSingleNode("FileAttachments"));

        //    m_linkConfig = TfsLinkConfiguration.Create(
        //        nav.SelectSingleNode("Links"));

        //    m_metadataSync = new TfsMetadataPolicy(nav.SelectSingleNode("MetadataSynchronization"));

        //    string fieldForm = nav.GetAttribute("fieldForm", string.Empty);

        //    if (fieldForm == "Friendly")
        //    {
        //        m_fieldForm = TfsFieldForm.Friendly;
        //    }
        //    else
        //    {
        //        Debug.Assert(fieldForm == "Reference", "Unsupported field type!");
        //        m_fieldForm = TfsFieldForm.Reference;
        //    }
        //}

        /// <summary>
        /// Creates a TFS work item store.
        /// </summary>
        /// <returns>TFS work item store</returns>
        public virtual TfsMigrationWorkItemStore CreateWorkItemStore()
        {
            TfsCore core = new TfsCore(this);
            //this,
            //m_session.Policies.MissingArea,
            //m_session.Policies.MissingIteration);

            return new TfsMigrationWorkItemStore(core);
        }

        private string m_serverId;                          // TF server instance id
        private string m_serverName;                        // TF server name
        private string m_project;                           // Project
        private string m_filter;                            // Optional filter
        //private TfsAttachmentConfiguration m_fileAttachConfig; //File attachment configuration
        private string m_defaultArea;                       // Default area
        private string m_defaultIteration;                  // Default iteration
        //private TfsMetadataPolicy m_metadataSync;           // Metadata synchronization policy
        //private List<string> m_flatFields;                  // List of flat fields (for testing purposes only!)
    }
}
