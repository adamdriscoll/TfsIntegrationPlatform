// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Xml;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Proxy;


namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    /// <summary>
    /// Shared TFS work item store; used by all TFS stores connected to the same data source.
    /// </summary>
    public class TfsCore
    {
        /// <summary>
        /// Returns store's configuration.
        /// </summary>
        public TfsMigrationDataSource Config { get { return m_cfg; } }

        /// <summary>
        /// Returns TF server name.
        /// </summary>
        public string ServerName { get { return m_name; } }

        /// <summary>
        /// Returns the TfsTeamProjectCollection object
        /// </summary>
        public TfsTeamProjectCollection TfsTPC { get { return m_srv; } }

        /// <summary>
        /// Returns identity of the calling process.
        /// </summary>
        public string UserName
        {
            get
            {
                TeamFoundationIdentity authenticatedUser;
                m_srv.GetAuthenticatedIdentity(out authenticatedUser);
                if (null == authenticatedUser)
                {
                    return "Unknown";
                }

                return authenticatedUser.DisplayName;
            }
        }

        /// <summary>
        /// Get/Set DisableAreaPathAutoCreation
        /// </summary>
        public bool DisableAreaPathAutoCreation 
        {
            get
            {
                return m_disableAreaPathAutoCreation;
            }
            set
            {
                m_disableAreaPathAutoCreation = value;
            }
        }

        /// <summary>
        /// Get/Set DisableIterationPathAutoCreation
        /// </summary>
        public bool DisableIterationPathAutoCreation
        {
            get
            {
                return m_disableIterationPathAutoCreation;
            }
            set
            {
                m_disableIterationPathAutoCreation = value;
            }
        }

        /// <summary>
        /// Returns URL of the work item tracking service.
        /// </summary>
        public string WorkItemTrackingUrl { get { return m_witUrl; } }

        /// <summary>
        /// Gets the default area id.
        /// </summary>
        public int DefaultAreaId { get { return m_defaultAreaId; } }

        /// <summary>
        /// Gets the default iteration id.
        /// </summary>
        public int DefaultIterationId { get { return m_defaultIterationId; } }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cfg">Configuration</param>
        public TfsCore(TfsMigrationDataSource cfg)
        {
            m_rwLock = new ReaderWriterLock();
            m_cfg = cfg;
            //m_missingArea = missingArea;
            //m_missingIteration = missingIteration;

            m_srv = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(cfg.ServerName));
            m_srv.EnsureAuthenticated();
            TraceManager.TraceInformation("Authenticated User for Uri {0} is '{1}'", m_srv.Uri, m_srv.AuthorizedIdentity.DisplayName);

            //// Verify whether the user is in the service account group. Throw an exception if it is not.
            //// TODO: move this to proper location
            //IGroupSecurityService gss = (IGroupSecurityService)m_srv.GetService(typeof(IGroupSecurityService));
            //Identity serviceAccountIdentity = gss.ReadIdentity(SearchFactor.ServiceApplicationGroup, null, QueryMembership.None);
            //if (!gss.IsMember(serviceAccountIdentity.Sid, m_srv.AuthenticatedUserIdentity.Sid))
            //{
            //    throw new MigrationException(
            //        string.Format(TfsWITAdapterResources.UserNotInServiceAccountGroup, m_srv.AuthenticatedUserName, m_srv.Name));
            //}

            m_store = CreateWorkItemStore();
            m_name = string.Format(
                CultureInfo.InvariantCulture,
                "{0} ({1})",
                m_store.TeamProjectCollection.Name,
                m_cfg.Project);

            Project p = m_store.Projects[cfg.Project];
            m_projectUri = p.Uri.ToString();
            m_projectId = p.Id;

            //// Check existence of default area and iteration, if any
            //if (!string.IsNullOrEmpty(cfg.DefaultArea))
            //{
            //    m_defaultAreaId = GetNode(Node.TreeType.Area, cfg.DefaultArea, false);
            //}
            //else
            //{
            //    m_defaultAreaId = p.Id;
            //}
            //if (!string.IsNullOrEmpty(cfg.DefaultIteration))
            //{
            //    m_defaultIterationId = GetNode(Node.TreeType.Iteration, cfg.DefaultIteration, false);
            //}
            //else
            //{
            //    m_defaultIterationId = p.Id;
            //}
            /// TODO: replace the code below with configuration in consideration
            m_defaultAreaId = p.Id;
            m_defaultIterationId = p.Id; 

            // Obtain registration info
            IRegistration regSvc = (IRegistration)m_store.TeamProjectCollection.GetService(typeof(IRegistration));
            RegistrationEntry[] res = regSvc.GetRegistrationEntries(ToolNames.WorkItemTracking);

            if (res.Length != 1)
            {
                throw new MigrationException(TfsWITAdapterResources.ErrorMalformedRegistrationData, cfg.ServerName);
            }

            RegistrationEntry e = res[0];

            // Extract all data from the registration entry.
            for (int i = 0; i < e.ServiceInterfaces.Length; i++)
            {
                ServiceInterface si = e.ServiceInterfaces[i];

                if (TFStringComparer.ServiceInterface.Equals(si.Name, ServiceInterfaces.WorkItem))
                {
                    m_witUrl = si.Url;
                }
                else if (TFStringComparer.ServiceInterface.Equals(si.Name, "ConfigurationSettingsUrl"))
                {
                    m_configUrl = si.Url;
                }
            }

            for (int i = 0; i < e.RegistrationExtendedAttributes.Length; i++)
            {
                RegistrationExtendedAttribute a = e.RegistrationExtendedAttributes[i];

                if (RegistrationUtilities.Compare(a.Name, "AttachmentServerUrl") == 0)
                {
                    m_attachUrl = a.Value;
                    break;
                }
            }

            if (string.IsNullOrEmpty(m_witUrl) || string.IsNullOrEmpty(m_configUrl)
                || string.IsNullOrEmpty(m_attachUrl))
            {
                throw new MigrationException(TfsWITAdapterResources.ErrorMalformedRegistrationData, 
                                             m_cfg.ServerName);
            }

            m_attachUrl = CombineUrl(m_attachUrl, m_witUrl);
        }

        /// <summary>
        /// Translates string path into id.
        /// </summary>
        /// <param name="type">Path type (area/iteration)</param>
        /// <param name="path">Path to translate</param>
        /// <returns>Id of the node</returns>
        public int TranslatePath(
            Node.TreeType type,
            string path)
        {
            m_rwLock.AcquireReaderLock(-1);
            try
            {
                return GetNode(type, path, true);
            }
            finally
            {
                m_rwLock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Returns project URI.
        /// </summary>
        public string ProjectUri 
        { 
            get 
            { 
                return m_projectUri; 
            } 
        }

        /// <summary>
        /// Returns URI of the root area node.
        /// </summary>
        public string AreaNodeUri
        {
            get
            {
                GetRootNodes();
                return m_areaNodeUri;
            }
        }

        /// <summary>
        /// Returns URI of the root iteration node.
        /// </summary>
        public string IterationNodeUri
        {
            get
            {
                GetRootNodes();
                return m_iterationNodeUri;
            }
        }

        /// <summary>
        /// Creates work item store object.
        /// </summary>
        /// <returns>Work item store</returns>
        public WorkItemStore CreateWorkItemStore()
        {
            m_rwLock.AcquireReaderLock(-1);
            TraceManager.TraceInformation("Connecting to '{0}'", m_srv.Uri);
            try
            {
                return (WorkItemStore)m_srv.GetService(typeof(WorkItemStore));
            }
            finally
            {
                TraceManager.TraceInformation("Connected to '{0}'", m_srv.Uri);
                m_rwLock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Creates a client service.
        /// </summary>
        /// <returns>Client service object</returns>
        public ITfsWorkItemServer CreateWorkItemServer()
        {
            WorkItemStore store = this.CreateWorkItemStore();
            WorkItemServer svc = store.TeamProjectCollection.GetService(typeof(WorkItemServer)) as WorkItemServer;
            return new Tfs2010WorkItemServer(svc);
        }

        /// <summary>
        /// Saves revision's fields into the given XML element.
        /// </summary>
        /// <param name="e">Target XML element</param>
        /// <param name="rev">Source revision</param>
        /// <param name="typeName">Work item type name</param>
        /// <param name="setDefaultPaths">Tells to set default area/iteration paths in case they were not specified explicitly</param>
        /// <param name="extraFields">Extra fields to set</param>
        public void SaveRevision(
            XmlElement e,
            MigrationRevision rev,
            string typeName,
            bool setDefaultPaths,
            MigrationField[] extraFields)
        {
            m_rwLock.AcquireReaderLock(-1);
            try
            {
                WorkItemType wit = m_store.Projects[m_cfg.Project].WorkItemTypes[typeName];
                bool hasArea = false;
                bool hasIteration = false;

                for (int i = 0; i < rev.Fields.Count; i++)
                {
                    MigrationField f = rev.Fields[i];

                    // Note: this cannot throw - we checked presence of each field while populating the revision
                    FieldDefinition fd = wit.FieldDefinitions[f.Name];
                    object value;

                    if (fd.Id == (int)CoreField.AreaPath)
                    {
                        // Substitute AreaPath with AreaId
                        fd = wit.FieldDefinitions[CoreField.AreaId];
                        value = TranslatePath(Node.TreeType.Area, (string)f.Value);
                        hasArea = true;
                    }
                    else if (fd.Id == (int)CoreField.IterationPath)
                    {
                        // Substitute IterationPath with IterationId
                        fd = wit.FieldDefinitions[CoreField.IterationId];
                        value = TranslatePath(Node.TreeType.Iteration, (string)f.Value);
                        hasIteration = true;
                    }
                    else
                    {
                        value = f.Value;
                    }

                    AddColumn(e, fd, value);
                }

                if (setDefaultPaths)
                {
                    if (!hasArea)
                    {
                        AddColumn(e, wit.FieldDefinitions[CoreField.AreaId], DefaultAreaId);
                    }
                    if (!hasIteration)
                    {
                        AddColumn(e, wit.FieldDefinitions[CoreField.IterationId], DefaultIterationId);
                    }
                }

                // Process extra fields
                for (int i = 0; i < extraFields.Length; i++)
                {
                    MigrationField f = extraFields[i];
                    AddColumn(e, wit.FieldDefinitions[f.Name], f.Value);
                }

                // Changed By
                AddColumn(e, wit.FieldDefinitions[CoreField.ChangedBy], rev.Author);
            }
            finally
            {
                m_rwLock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Checks whether given user is valid for the work item store.
        /// </summary>
        /// <param name="name">User name</param>
        /// <returns>True if the user is valid</returns>
        public static bool IsValidUser(
            string name)
        {
            return !string.IsNullOrEmpty(name);
        }

        /// <summary>
        /// Checks whether a field is valid for a work item type.
        /// </summary>
        /// <param name="workItemType">Work item type</param>
        /// <param name="fieldName">Field name</param>
        /// <returns>True if the field is valid for the work item type</returns>
        public bool IsValidField(
            string workItemType,
            string fieldName)
        {
            m_rwLock.AcquireReaderLock(-1);
            try
            {
                WorkItemType t = m_store.Projects[m_cfg.Project].WorkItemTypes[workItemType];
                return t.FieldDefinitions.Contains(fieldName);
            }
            finally
            {
                m_rwLock.ReleaseReaderLock();
            }
        }

        internal string ReflectedWorkItemIdFieldReferenceName { get; set; }

        internal bool EnableInsertReflectedWorkItemId { get; set; }

        internal virtual void CheckBypassRulePermission()
        {
            VersionSpecificUtils.CheckBypassRulePermission(m_srv);
        }

        /// <summary>
        /// Initializes URI of root area and iteration nodes.
        /// </summary>
        private void GetRootNodes()
        {
            if (!m_hasRootNodes)
            {
                ICommonStructureService css = Css;
                NodeInfo[] nodes = css.ListStructures(m_store.Projects[m_cfg.Project].Uri.ToString());
                string areaUri = null;
                string iterationUri = null;

                for (int i = 0; i < nodes.Length; i++)
                {
                    NodeInfo n = nodes[i];

                    if (TFStringComparer.CssStructureType.Equals(n.StructureType, "ProjectLifecycle"))
                    {
                        iterationUri = n.Uri;
                    }
                    else if (TFStringComparer.CssStructureType.Equals(n.StructureType, "ProjectModelHierarchy"))
                    {
                        areaUri = n.Uri;
                    }
                }

                m_areaNodeUri = areaUri;
                m_iterationNodeUri = iterationUri;
                m_hasRootNodes = true;
            }
        }

        /// <summary>
        /// Gets the CSS interface
        /// </summary>
        internal ICommonStructureService Css
        {
            get
            {
                if (m_css == null)
                {
                    m_css = (ICommonStructureService)m_store.TeamProjectCollection.GetService(typeof(ICommonStructureService));
                }
                return m_css;
            }
        }

        /// <summary>
        /// Adds field column to the update package statement
        /// </summary>
        /// <param name="parent">Parent XML element</param>
        /// <param name="fd">Field definition</param>
        /// <param name="value">Field value</param>
        private static void AddColumn(
            XmlElement parent,
            FieldDefinition fd,
            object value)
        {
            string stringVal = TranslateValue(fd, value);

            if (fd.FieldType == FieldType.Html || fd.FieldType == FieldType.PlainText || fd.FieldType == FieldType.History)
            {
                // Large text are different
                parent = (XmlElement)parent.ParentNode;
                XmlElement e = parent.OwnerDocument.CreateElement("InsertText");

                e.SetAttribute("FieldName", fd.ReferenceName);
                e.SetAttribute("FieldDisplayName", fd.Name);
                e.InnerText = stringVal;
                parent.AppendChild(e);
            }
            else
            {
                string typeName;

                if (value is TfsServerDateTime)
                {
                    typeName = "ServerDateTime";
                    Debug.Assert(stringVal.Length == 0, "Server date/time value was not translated correctly!");
                }
                else
                {
                    switch (fd.FieldType)
                    {
                        case FieldType.Integer: typeName = "Number"; break;
                        case FieldType.Double: typeName = "Double"; break;
                        case FieldType.DateTime: typeName = "DateTime"; break;

                        default:
                            Debug.Assert(fd.FieldType == FieldType.String, "Unsupported field type!");
                            typeName = null;
                            break;
                    }
                }

                XmlElement c = parent.OwnerDocument.CreateElement("Column");
                c.SetAttribute("Column", fd.ReferenceName);
                if (!string.IsNullOrEmpty(typeName))
                {
                    c.SetAttribute("Type", typeName);
                }
                XmlElement v = parent.OwnerDocument.CreateElement("Value");
                v.InnerText = stringVal;
                c.AppendChild(v);
                parent.AppendChild(c);
            }
        }

        /// <summary>
        /// Translates field's value.
        /// </summary>
        /// <param name="fd">Field definition</param>
        /// <param name="value">Original value</param>
        /// <returns>Translated value</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        private static string TranslateValue(
            FieldDefinition fd,
            object value)
        {
            if (value != null && !(value is DBNull || value is string && ((string)value).Length == 0))
            {
                if (value is TfsServerDateTime)
                {
                    return string.Empty;
                }
                // Convert to the native type
                try
                {
                    value = Convert.ChangeType(value, fd.SystemType, CultureInfo.InvariantCulture);

                    switch (fd.FieldType)
                    {
                        case FieldType.Integer: return XmlConvert.ToString((int)value);
                        case FieldType.Double: return XmlConvert.ToString((double)value);
                        case FieldType.DateTime: return XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.Unspecified);
                        default: return value.ToString();
                    }
                }
                catch (OverflowException e)
                {
                    string msg = string.Format(
                        TfsWITAdapterResources.Culture, TfsWITAdapterResources.ErrorFieldConversion, fd.ReferenceName);
                    throw new WitMigrationException(msg, e);
                }
                catch (InvalidCastException e)
                {
                    string msg = string.Format(
                        TfsWITAdapterResources.Culture, TfsWITAdapterResources.ErrorFieldConversion, fd.ReferenceName);
                    throw new WitMigrationException(msg, e);
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Combines two URLs into one.
        /// </summary>
        /// <param name="url">Original URL (either full or part)</param>
        /// <param name="baseUrl">Base URL that should be used if original URL is only partial</param>
        /// <returns>Combined URL</returns>
        private static string CombineUrl(
            string url,
            string baseUrl)
        {
            Uri uri = new Uri(url, UriKind.RelativeOrAbsolute);

            if (uri.IsAbsoluteUri)
            {
                url = uri.AbsolutePath;
            }
            else
            {
                uri = new Uri(
                    new Uri(baseUrl, UriKind.Absolute),
                    uri);
            }
            return uri.AbsoluteUri;
        }

        /// <summary>
        /// Finds given node and returns its id.
        /// </summary>
        /// <param name="type">Node type (area/iteration)</param>
        /// <param name="path">Path to the node</param>
        /// <param name="respectPoliciesFlag">Tells whether policies should be respected in case the node is not found</param>
        /// <returns>Id of the node</returns>
        private int GetNode(
            Node.TreeType type,
            string path,
            bool respectPoliciesFlag)
        {
            string[] names = path.Split('\\');
            Project p = m_store.Projects[m_cfg.Project];
            NodeCollection nc = type == Node.TreeType.Area ? p.AreaRootNodes : p.IterationRootNodes;
            Node n = null;
            //WitMigrationConflictPolicy policy = type == Node.TreeType.Area ? m_missingArea : m_missingIteration;

            for (int i = 0; i < names.Length && !string.IsNullOrEmpty(names[i]); i++)
            {
                string name = names[i];

                try
                {
                    n = nc[name];
                    nc = n.ChildNodes;
                    continue;
                }
                catch (DeniedOrNotExistException e)
                {
                    string msg = string.Format(
                        TfsWITAdapterResources.Culture,
                        type == Node.TreeType.Area ? TfsWITAdapterResources.ErrorMissingArea : TfsWITAdapterResources.ErrorMissingIteration,
                        p.Name + "\\" + path,
                        m_name);

                    TraceManager.TraceInformation(msg);

                    if ((type == Node.TreeType.Area && DisableAreaPathAutoCreation)
                        || (type == Node.TreeType.Iteration && DisableIterationPathAutoCreation))
                    {
                        throw new MissingPathException(msg, e);
                    }
                }

                //Debug.Assert(policy.Reaction != WitConflictReaction.Throw, "Invalid reaction!");
                //if (policy.Reaction == WitConflictReaction.Default)
                //{
                //    return type == Node.TreeType.Area ? m_defaultAreaId : m_defaultIterationId;
                //}

                string parentUri;
                if (n == null)
                {
                    parentUri = type == Node.TreeType.Area ? AreaNodeUri : IterationNodeUri;
                }
                else
                {
                    parentUri = n.Uri.ToString();
                }

                LockCookie cookie = m_rwLock.UpgradeToWriterLock(-1);
                try
                {
                    int newId = CreatePath(type, parentUri, names, i);
                    TraceManager.TraceInformation(string.Format(
                        "Created path '{0}'(Id: {1}) in the TFS Work Item store '{2}'",
                        p.Name + "\\" + path,
                        newId,
                        m_name));
                    return newId;
                }
                finally
                {
                    m_rwLock.DowngradeFromWriterLock(ref cookie);
                }
            }

            return n == null ? m_projectId : n.Id;
        }


        /// <summary>
        /// Creates path.
        /// </summary>
        /// <param name="type">Type of the node to be created</param>
        /// <param name="parentUri">Parent node</param>
        /// <param name="nodes">Node names</param>
        /// <param name="first">Index of the first node to create</param>
        /// <returns>Id of the node</returns>
        private int CreatePath(
            Node.TreeType type,
            string parentUri,
            string[] nodes,
            int first)
        {
            Debug.Assert(first < nodes.Length, "Nothing to create!");

            // Step 1: create in CSS
            ICommonStructureService css = Css;
            for (int i = first; i < nodes.Length; i++)
            {
                string node = nodes[i];
                if (!string.IsNullOrEmpty(node))
                {
                    try
                    {
                        parentUri = css.CreateNode(node, parentUri);
                    }
                    catch (CommonStructureSubsystemException cssEx)
                    {
                        if (cssEx.Message.Contains("TF200020"))
                        {
                            // TF200020 may be thrown if the tree node metadata has been propagated
                            // from css to WIT cache. In this case, we will wait for the node id
                            //   Microsoft.TeamFoundation.Server.CommonStructureSubsystemException: 
                            //   TF200020: The parent node already has a child node with the following name: {0}. 
                            //   Child nodes must have unique names.
                            Node existingNode = WaitForTreeNodeId(type, new string[] { node });
                            if (existingNode == null)
                            {
                                throw;
                            }
                            else
                            {
                                parentUri = existingNode.Uri.AbsoluteUri;
                            }
                        }
                    }
                }
            }

            // Step 2: locate in the cache
            // Syncing nodes into WIT database is an asynchronous process, and there's no way to tell
            // the exact moment. 
            Node newNode = WaitForTreeNodeId(type, nodes);
            if (newNode == null)
            {
                return -1;
            }
            else
            {
                return newNode.Id;
            }
        }


        internal Node WaitForTreeNodeId(
            Node.TreeType type, 
            string[] nodes)
        {
            int[] TIMEOUTS = { 100, 500, 1000, 5000 };
            int[] RetryTimes = { 1, 2, 70, 36 };

            for (int i = 0; i < TIMEOUTS.Length; ++i)
            {
                for (int k = 0; k < RetryTimes[i]; ++k)
                {
                    Thread.Sleep(TIMEOUTS[i]);
                    TraceManager.TraceInformation("Wake up from {0} millisec sleep for polling CSS node Id", TIMEOUTS[i]);

                    m_store.RefreshCache();
                    Project p = m_store.Projects[m_cfg.Project];
                    NodeCollection nc = type == Node.TreeType.Area ? p.AreaRootNodes : p.IterationRootNodes;
                    Node n = null;

                    try
                    {
                        for (int j = 0; j < nodes.Length; j++)
                        {
                            string name = nodes[j];
                            if (!string.IsNullOrEmpty(name))
                            {
                                n = nc[name];
                                nc = n.ChildNodes;
                            }
                        }

                        return n;
                    }
                    catch (DeniedOrNotExistException)
                    {
                        // The node is not there yet. Try one more time...
                    }
                }
            }

            return null;
        }



        private ReaderWriterLock m_rwLock;                  // RW lock
        private TfsMigrationDataSource m_cfg;               // Configuration

        private TfsTeamProjectCollection m_srv;             // Team foundation server
        private WorkItemStore m_store;                      // Private store for accessing metadata
        private string m_name;                              // Store name

        // Client service data
        private string m_witUrl;                            // WIT server URL
        private string m_attachUrl;                         // Attachments server URL
        private string m_configUrl;                         // Config server URL

        // Hierarchy data
        private ICommonStructureService m_css;              // CSS interface
        private bool m_hasRootNodes;                        // Tells whether root area/iteration nodes are initialized
        private int m_projectId;                            // Project id
        private string m_projectUri;                        // Project URI
        private string m_areaNodeUri;                       // URI of the root area node
        private string m_iterationNodeUri;                  // URI of the root iteration node
        //private WitMigrationConflictPolicy m_missingArea;   // Missing area policy
        //private WitMigrationConflictPolicy m_missingIteration;  // Missing iteration policy
        private int m_defaultAreaId;                        // Default area id
        private int m_defaultIterationId;                   // Default iteration id

        private bool m_disableAreaPathAutoCreation = false;
        private bool m_disableIterationPathAutoCreation = false;
    }
}
