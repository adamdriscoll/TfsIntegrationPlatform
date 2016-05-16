// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.TeamFoundation.Migration.BusinessModel.WIT;
using Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts;
using Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement.WITBasicConflicts;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Proxy;
using ToolkitConstants = Microsoft.TeamFoundation.Migration.Toolkit.Constants;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    /// <summary>
    /// TFS migration store.
    /// </summary>
    public partial class TfsMigrationWorkItemStore : IComparer<Identity>
    {
        private ITfsWorkItemServer m_workItemServer;                // WIT proxy service 
        private readonly TfsCore m_core;                            // Shared TFS core
        private readonly StringComparer m_fieldNameComparer;        // Comparer for field names
        private readonly StringComparer m_idComparer;               // Comparer for Ids
        private readonly StringComparer m_stringValueComparer;      // Comparer for fields' values of string type
        private readonly Dictionary<string, int> m_mappedWorkItem;  // key on generic "work item" Id
        private bool m_byPassRules = true;                          // default to enable bypassrule
        private WorkItemStore m_store;                              // Work item store

        private const int c_maxItemsInQuery = 100;
        private const string c_emptyTfsWitQuery = "[System.Id] = 0";

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="core">Shared core</param>
        public TfsMigrationWorkItemStore(
            TfsCore core)
        {
            m_core = core;

            //$TODO_VNEXT: it would be nice to compare string values in server locale. However,
            // that property is unavailable in the current version.
            m_stringValueComparer = StringComparer.InvariantCultureIgnoreCase;
            m_fieldNameComparer = TFStringComparer.FieldName;
            m_idComparer = TFStringComparer.WorkItemId;

            m_mappedWorkItem = new Dictionary<string, int>();
        }

        public IServiceContainer ServiceContainer { get; set; }

        /// <summary>
        /// Gets/Sets if the store should submit WIT changes bypassing rule validation.
        /// </summary>
        internal bool ByPassrules
        {
            get
            {
                return m_byPassRules;
            }
            set
            {
                m_byPassRules = value;

                if (m_byPassRules)
                {
                    m_core.CheckBypassRulePermission();
                }
            }
        }

        internal string ReflectedWorkItemIdFieldReferenceName
        {
            get
            {
                return m_core.ReflectedWorkItemIdFieldReferenceName;
            }
            set
            {
                m_core.ReflectedWorkItemIdFieldReferenceName = value;
            }
        }

        internal bool EnableInsertReflectedWorkItemId
        {
            get
            {
                return m_core.EnableInsertReflectedWorkItemId;
            }
            set
            {
                m_core.EnableInsertReflectedWorkItemId = value;
            }
        }

        internal long MaxAttachmentSize
        {
            get
            {
                return WorkItemServer.MaxAttachmentSize;
            }
        }

        /// <summary>
        /// Returns flags describing the work item store.
        /// </summary>
        public MigrationWorkItemData Flags
        {
            get { return MigrationWorkItemData.All; }
        }

        /// <summary>
        /// Gets comparer used with string values.
        /// </summary>
        public StringComparer StringValueComparer
        {
            get { return m_stringValueComparer; }
        }

        /// <summary>
        /// Gets comparer used with field names.
        /// </summary>
        public StringComparer FieldNameComparer
        {
            get { return m_fieldNameComparer; }
        }

        /// <summary>
        /// Gets comparer for work items' ids.
        /// </summary>
        public StringComparer IdComparer
        {
            get { return m_idComparer; }
        }

        /// <summary>
        /// Gets datastore name.
        /// </summary>
        public string StoreName
        {
            get { return m_core.ServerName; }
        }

        /// <summary>
        /// Returns shared TFS core object.
        /// </summary>
        public TfsCore Core
        {
            get { return m_core; }
        }

        /// <summary>
        /// Gets the underlying work item store object.
        /// </summary>
        public WorkItemStore WorkItemStore
        {
            get
            {
                if (m_store == null)
                {
                    m_store = m_core.CreateWorkItemStore();
                }
                return m_store;
            }
        }

        /// <summary>
        /// Gets the TFS group security service
        /// </summary>
        public IGroupSecurityService GroupSecurityService
        {
            get
            {
                return GetGroupSecurityService(WorkItemStore);
            }
        }

        protected virtual IGroupSecurityService GetGroupSecurityService(
            WorkItemTracking.Client.WorkItemStore workItemStore)
        {
            return workItemStore.TeamProjectCollection.GetService(typeof(IGroupSecurityService)) as IGroupSecurityService;
        }

        /// <summary>
        /// Returns work item proxy service.
        /// </summary>
        public ITfsWorkItemServer WorkItemServer
        {
            get
            {
                if (m_workItemServer == null)
                {
                    m_workItemServer = m_core.CreateWorkItemServer();
                }

                if (m_workItemServer == null)
                {
                    throw new MigrationException(TfsWITAdapterResources.WorkItemServerIsNotAvailable);
                }

                return m_workItemServer;
            }
        }

        internal string LocalWorkDir { get; set; }

        protected Guid SourceSideSourceId { get; set; }

        #region IComparer<Identity> Members

        /// <summary>
        /// Comparer for identity type.
        /// </summary>
        /// <param name="x">Identity 1</param>
        /// <param name="y">Identity 2</param>
        /// <returns>Results of comparison</returns>
        int IComparer<Identity>.Compare(
            Identity x,
            Identity y)
        {
            Debug.Assert(x != null && y != null, "Null identity!");

            if (x.Deleted)
            {
                if (!y.Deleted) return -1;
            }
            else if (y.Deleted)
            {
                return 1;
            }

            return TFStringComparer.UserName.Compare(x.DisplayName, y.DisplayName);
        }

        #endregion

        /// <summary>
        /// Obtains collection of items scheduled for synchronization.
        /// </summary>
        /// <param name="highWatermark">Highwater mark object</param>
        /// <returns>Collection of items</returns>
        public IEnumerable<TfsMigrationWorkItem> GetItems(
            ref string highWatermark)
        {
            // Create condition
            var c = new StringBuilder("[System.TeamProject]=@project");
            if (!string.IsNullOrEmpty(m_core.Config.Filter))
            {
                c.AppendFormat(CultureInfo.InvariantCulture, " AND ({0})", m_core.Config.Filter);
            }

            DateTime dt = Convert.ToDateTime(highWatermark, CultureInfo.InvariantCulture);
            if (!dt.Equals(default(DateTime)))
            {
                c.AppendFormat(CultureInfo.InvariantCulture, " AND [System.ChangedDate] > '{0:u}'", dt);
            }

            var items = new TfsMigrationWorkItems(m_core, WorkItemStore, c.ToString());
            highWatermark = items.AsOf.ToString(CultureInfo.InvariantCulture);
            return items;
        }

        /// <summary>
        /// Obtains collection of work items that have had link changes made to them since the last HighWaterMark
        /// </summary>
        /// <param name="timeHighWaterMark">Time-based Highwater mark object</param>
        /// <param name="linkChangeIdHighWaterMark">Long Highwater mark value to be passed in as the rowNumber argument to WorkItemServer.GetWorkItemLinkChanges()</param>
        /// <returns>Collection of items</returns>
        public IEnumerable<TfsMigrationWorkItem> GetItemsWithLinkChanges(
            ref long linkChangeIdHighWaterMark,
            DateTime excludeItemsChangedBeforeTime)
        {
            List<TfsMigrationWorkItem> itemsToReturn = new List<TfsMigrationWorkItem>();

            // If the filter string is the empty filter string that returns nothing, skip all of this processing and return an empty collection
            if (string.Equals(m_core.Config.Filter, c_emptyTfsWitQuery, StringComparison.OrdinalIgnoreCase))
            {
                return itemsToReturn;
            }

            Dictionary<int, List<WorkItemLinkChange>> perWorkItemLinkChanges = GetLinkChanges(ref linkChangeIdHighWaterMark, excludeItemsChangedBeforeTime);

            // Create a Queue with all of the work item Ids
            Queue<int> workItemIdQueue = new Queue<int>();
            foreach (int workItemId in perWorkItemLinkChanges.Keys)
            {
                workItemIdQueue.Enqueue(workItemId);
            }

            while(workItemIdQueue.Count > 0)
            {
                // Create condition
                StringBuilder queryBuilder = new StringBuilder("[System.TeamProject]=@project");
                if (!string.IsNullOrEmpty(m_core.Config.Filter))
                {
                    queryBuilder.AppendFormat(CultureInfo.InvariantCulture, " AND ({0})", m_core.Config.Filter);
                }

                queryBuilder.AppendFormat(" AND [System.ID] in (");
                bool itemAdded = false;
                for (int itemsInQuery = 0; itemsInQuery < c_maxItemsInQuery && workItemIdQueue.Count > 0; itemsInQuery++)
                {
                    if (itemAdded)
                    {
                        queryBuilder.Append(',');
                    }
                    itemAdded = true;
                    queryBuilder.Append(workItemIdQueue.Dequeue());
                }
                queryBuilder.Append(')');
                if (!itemAdded)
                {
                    break;
                }

                string wiqlCondition = queryBuilder.ToString();
                Stopwatch queryStopwatch = Stopwatch.StartNew();
                var items = new TfsMigrationWorkItems(m_core, WorkItemStore, wiqlCondition);
                queryStopwatch.Stop();
                TraceManager.TraceVerbose("GetItemsWithLinkChanges: Time to query work items: " + queryStopwatch.Elapsed.TotalSeconds + " seconds");

                // Add WorkItemLinkChange as found to each item
                foreach (TfsMigrationWorkItem migrationWorkItem in items)
                {
                    List<WorkItemLinkChange> linkChanges;
                    if (!perWorkItemLinkChanges.TryGetValue(migrationWorkItem.WorkItem.Id, out linkChanges))
                    {
                        linkChanges = new List<WorkItemLinkChange>();
                        TraceManager.TraceWarning(String.Format(CultureInfo.InvariantCulture,
                            "GetItemsWithLinkChanges() did not find expected LinkChanges for work item {0} in perWorkItemLinkChanges", migrationWorkItem.WorkItem.Id));
                    }
                    if (migrationWorkItem.LinkChanges == null)
                    {
                        migrationWorkItem.LinkChanges = linkChanges;
                    }
                    else
                    {
                        TraceManager.TraceVerbose(String.Format(CultureInfo.InvariantCulture,
                            "Adding {0} link change(s) to the existing list containing {1} link change(s) for work item {2}", 
                            linkChanges.Count, migrationWorkItem.LinkChanges.Count, migrationWorkItem.WorkItem.Id));
                        foreach (WorkItemLinkChange linkChange in linkChanges)
                        {
                            migrationWorkItem.LinkChanges.Add(linkChange);
                        }
                    }
                    if (linkChanges.Count > 0)
                    {
                        itemsToReturn.Add(migrationWorkItem);
                    }
                    else
                    {
                        TraceManager.TraceWarning(String.Format(CultureInfo.InvariantCulture,
                            "GetItemsWithLinkChanges() did not find expected LinkChanges for work item {0}", migrationWorkItem.WorkItem.Id));
                    }
                }
            }

            return itemsToReturn;
        }

        private Dictionary<int, List<WorkItemLinkChange>> GetLinkChanges(
            ref long linkChangeIdHighWaterMark,
            DateTime excludeItemsChangedBeforeTime)
        {
            // Get all of the link changes since the last high water mark and create a Dictionary with a SortedList of WorkItemLinkChange objects for each
            // source work item involved in a link change
            Stopwatch stopwatch = Stopwatch.StartNew();
            Dictionary<int, SortedList<DateTime, List<WorkItemLinkChange>>> perWorkItemLinkChangesSorted = new Dictionary<int, SortedList<DateTime, List<WorkItemLinkChange>>>();
            foreach (WorkItemLinkChange linkChange in WorkItemServer.GetWorkItemLinkChanges(Guid.NewGuid().ToString(), linkChangeIdHighWaterMark))
            {
                /* Uncomment for very verbose tracking of this method:
                TraceManager.TraceVerbose(String.Format(CultureInfo.InvariantCulture,
                    "Link change detected by GetWorkItemLinkChanges: Source work item: {0}, Target work item: {1}, Link type: {2}, Change date: {3}, Action: {4}",
                    linkChange.SourceID, linkChange.TargetID, linkChange.LinkType, linkChange.ChangedDate.ToString("yyyy-MM-dd HH:mm:ss.fff"), linkChange.IsActive ? "Add" : "Delete"));
                 */

                if (linkChange.ChangedDate < excludeItemsChangedBeforeTime)
                {
                    // TraceManager.TraceVerbose("Skipping detected link change because it's ChangeDate was before the time-based HighWaterMark value");
                    continue;
                }

                SortedList<DateTime, List<WorkItemLinkChange>> linkChanges;
                if (!perWorkItemLinkChangesSorted.TryGetValue(linkChange.SourceID, out linkChanges))
                {
                    linkChanges = new SortedList<DateTime, List<WorkItemLinkChange>>();
                    perWorkItemLinkChangesSorted.Add(linkChange.SourceID, linkChanges);
                }

                List<WorkItemLinkChange> perTimeLinkChanges = new List<WorkItemLinkChange>();
                if (!linkChanges.TryGetValue(linkChange.ChangedDate, out perTimeLinkChanges))
                {
                    perTimeLinkChanges = new List<WorkItemLinkChange>();
                    linkChanges.Add(linkChange.ChangedDate, perTimeLinkChanges);
                }
                perTimeLinkChanges.Add(linkChange);

                linkChangeIdHighWaterMark = linkChange.RowVersion;
            }

            Dictionary<int, List<WorkItemLinkChange>> perWorkItemLinkChanges = new Dictionary<int, List<WorkItemLinkChange>>();
            foreach (KeyValuePair<int, SortedList<DateTime, List<WorkItemLinkChange>>> perTimelinkChangesSortedEntry in perWorkItemLinkChangesSorted)
            {
                if (!perWorkItemLinkChanges.ContainsKey(perTimelinkChangesSortedEntry.Key))
                {
                    perWorkItemLinkChanges.Add(perTimelinkChangesSortedEntry.Key, new List<WorkItemLinkChange>());
                }
                foreach (List<WorkItemLinkChange> perTimeLinkChanges in perTimelinkChangesSortedEntry.Value.Values)
                {
                    perWorkItemLinkChanges[perTimelinkChangesSortedEntry.Key].AddRange(perTimeLinkChanges);
                }
            }

            stopwatch.Stop();
            TraceManager.TraceVerbose("GetLinkChanges: Time to call GetWorkItemLinkChanges() and build list sorted by ChangeDate: " +
                stopwatch.Elapsed.TotalSeconds + " seconds");

            return perWorkItemLinkChanges;
        }

        /// <summary>
        /// Reopens store from a different thread.
        /// </summary>
        /// <returns>Store safe to work with from the calling thread</returns>
        public TfsMigrationWorkItemStore Reopen()
        {
            return new TfsMigrationWorkItemStore(m_core);
        }

        /// <summary>
        /// Checks whether specified user exists in the store.
        /// </summary>
        /// <param name="name">User name</param>
        /// <returns>True if the user exists</returns>
        public bool IsValidUser(
            string name)
        {
            return TfsCore.IsValidUser(name);
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
            return m_core.IsValidField(workItemType, fieldName);
        }

        /// <summary>
        /// Refreshes store's data.
        /// </summary>
        public void Refresh()
        {
            if (m_store != null)
            {
                m_store.SyncToCache();
            }
        }

        /// <summary>
        /// Closes the work item store.
        /// </summary>
        public void Close()
        {
            m_store = null;
            if (m_workItemServer != null)
            {
                m_workItemServer.Dispose();
                m_workItemServer = null;
            }
        }

        public string TeamProject
        {
            get
            {
                if (null == m_core || null == m_core.Config)
                {
                    return string.Empty;
                }
                return m_core.Config.Project;
            }
        }

        public void SyncAccounts(
            List<Identity> globGroups,
            List<Identity> projGroups)
        {
            string dstProjUri = WorkItemStore.Projects[m_core.Config.Project].Uri.ToString();

            if (globGroups.Count > 0)
            {
                CreateGroups(GroupSecurityService, null, globGroups);
            }

            if (projGroups.Count > 0)
            {
                CreateGroups(GroupSecurityService, dstProjUri, projGroups);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        internal XmlDocument GetAreaPaths(
            Project p)
        {
            return GetPathDoc(p, @"\{0}\Area");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        internal XmlDocument GetIterationPaths(
            Project p)
        {
            return GetPathDoc(p, @"\{0}\Iteration");
        }

        protected XmlDocument GetPathDoc(Project p, string filter)
        {
            string rootPath = string.Format(filter, p.Name);
            NodeInfo rootNode = Core.Css.GetNodeFromPath(rootPath);
            XmlElement nodes = Core.Css.GetNodesXml(new string[] { rootNode.Uri }, true);

            XmlDocument areaPathsDoc = new XmlDocument();
            areaPathsDoc.LoadXml(nodes.OuterXml);
            return areaPathsDoc;
        }

        /// <summary>
        /// Synchronizes own global lists with those from the project.
        /// </summary>
        /// <param name="p">Source project</param>
        /// <param name="ignoredLists">name of lists to be ignored from syncing</param>
        internal XmlDocument GetGlobalList(
            Project p,
            ReadOnlyCollection<string> ignoredLists)
        {
            if (null == ignoredLists)
            {
                throw new ArgumentNullException("ignoredLists");
            }

            XmlDocument doc = p.Store.ExportGlobalLists();
            if (null == doc || null == doc.DocumentElement)
            {
                return null;
            }

            XmlNodeList lists = doc.DocumentElement.SelectNodes("GLOBALLIST");
            if (null == lists)
            {
                return null;
            }

            foreach (XmlElement list in lists)
            {
                string name = list.GetAttribute("name");
                for (int i = 0; i < ignoredLists.Count; i++)
                {
                    if (StringValueComparer.Equals(name, ignoredLists[i]))
                    {
                        list.ParentNode.RemoveChild(list);
                        break;
                    }
                }
            }

            return doc;
        }

        internal void UploadGlobalList(
            XmlDocument globalListDoc)
        {
            WorkItemStore.ImportGlobalLists(globalListDoc.DocumentElement);
        }

        /// <summary>
        /// Synchronizes own accounts with those from the project.
        /// </summary>
        /// <param name="p">Source project</param>
        internal Identity[] GetGlobalGroups(
            Project p)
        {
            // Obtain global- and project-scoped URI's from the source project
            var srcGss = GetGroupSecurityService(p.Store);
            return LoadGroups(srcGss, null);
        }

        /// <summary>
        /// Synchronizes own accounts with those from the project.
        /// </summary>
        /// <param name="p">Source project</param>
        internal Identity[] GetProjectGroups(
            Project p)
        {
            // Obtain global- and project-scoped URI's from the source project
            var srcGss = GetGroupSecurityService(p.Store);
            return LoadGroups(srcGss, p.Uri.ToString());
        }

        /// <summary>
        /// Get work item type definition
        /// </summary>
        /// <param name="p">Source project</param>
        internal XmlDocument GetWorkItemTypes(
            Project p)
        {
            var wiTypes = new XmlDocument();
            XmlElement root = wiTypes.CreateElement("WorkItemTypes");
            wiTypes.AppendChild(root);

            foreach (WorkItemType t in p.WorkItemTypes)
            {
                XmlDocument doc = t.Export(false);
                Debug.Assert(null != doc.DocumentElement);

                root.AppendChild(wiTypes.ImportNode(doc.DocumentElement, true));
            }

            return wiTypes;
        }

        /// <summary>
        /// Synchronize the Work Item Type definition.
        /// </summary>
        /// <param name="witd"></param>
        internal void SyncWorkItemTypes(
            XmlDocument witd)
        {
            WorkItemStore.Projects[m_core.Config.Project].WorkItemTypes.Import(witd.DocumentElement);
        }

        /// <summary>
        /// Loads all groups from the given project.
        /// </summary>
        /// <param name="gss">GSS service</param>
        /// <param name="uri">Project's URI. Passing null will return array of global groups</param>
        /// <returns>Array of groups</returns>
        protected Identity[] LoadGroups(
            IGroupSecurityService gss,
            string uri)
        {
            Identity[] groups = gss.ListApplicationGroups(uri);
            Array.Sort(groups, this);
            return groups;
        }

        /// <summary>
        /// Creates given groups.
        /// </summary>
        /// <param name="gss">GSS service</param>
        /// <param name="projUri">Uri of the project under which groups must be created. Specifying NULL here will
        /// result in creation of global groups</param>
        /// <param name="groups">Groups that must be created</param>
        protected void CreateGroups(
            IGroupSecurityService gss,
            string projUri,
            IList<Identity> groups)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                Identity g = groups[i];

                if (!g.Deleted)
                {
                    try
                    {
                        gss.CreateApplicationGroup(projUri, g.DisplayName, g.Description);
                    }
                    catch (Exception e)
                    {
                        throw new WitMigrationException("Failed to create user groups", e);
                    }
                }
            }
        }

        protected bool TryApplyAddWorkItemChangesByOM(
            IMigrationAction action,
            Guid sourceSideSourceId,
            ConflictManager conflictMgrService,
            out WorkItem workItem)
        {
            workItem = CreateNewWorkItem(action, conflictMgrService);
            bool result = TryApplyWitDataChanges(action, workItem, true, sourceSideSourceId, conflictMgrService);

            if (result == false)
            {
                return result;
            }

            if (!EnableInsertReflectedWorkItemId)
            {
                return result;
            }

            if (!workItem.Fields.Contains(ReflectedWorkItemIdFieldReferenceName))
            {
                TraceManager.TraceInformation(
                    "WorkItem type '{0}' does not contain field '{1}'. Writing source item Id will be skipped.",
                    workItem.Type.Name,
                    ReflectedWorkItemIdFieldReferenceName);
                return result;
            }

            FieldType typeInWITD = workItem.Fields[ReflectedWorkItemIdFieldReferenceName].FieldDefinition.FieldType;
            if (!typeInWITD.Equals(TfsConstants.MigrationTracingFieldType))
            {
                TraceManager.TraceInformation(
                    "The field '{0}' is not defined with type '{1}'. Writing source item Id will be skipped.",
                    ReflectedWorkItemIdFieldReferenceName,
                    TfsConstants.MigrationTracingFieldType.ToString());
                return result;
            }

            workItem.Fields[ReflectedWorkItemIdFieldReferenceName].Value = GetSourceWorkItemId(action);
            return result;
        }

        internal WorkItem CreateNewWorkItem(IMigrationAction action, ConflictManager conflictMgrService)
        {
            var workItemType = ValidateAndGetWorkItemType(action, conflictMgrService);
            return new WorkItem(workItemType);
        }

        private WorkItemType ValidateAndGetWorkItemType(
            IMigrationAction action, 
            ConflictManager conflictMgrService)
        {
            Debug.Assert(null != action.MigrationActionDescription.DocumentElement);
            var workItemType = action.MigrationActionDescription.DocumentElement.Attributes["WorkItemType"].Value;

            if (!WorkItemStore.Projects.Contains(Core.Config.Project))
            {
                throw new MigrationException(TfsWITAdapterResources.TeamProjectNotFound, Core.Config.Project);
            }
            var p = WorkItemStore.Projects[Core.Config.Project];

            if (!p.WorkItemTypes.Contains(workItemType))
            {
                var conflict = WITUnmappedWITConflictType.CreateConflict(workItemType, action);

                List<MigrationAction> actions;
                ConflictResolutionResult rslt = conflictMgrService.TryResolveNewConflict(conflictMgrService.SourceId,
                                                                                         conflict,
                                                                                         out actions);
                if (!rslt.Resolved)
                {
                    throw new MigrationUnresolvedConflictException();
                }
            }

            return p.WorkItemTypes[workItemType];
        }

        protected bool TryApplyEditWorkItemChangesByOM(
            IMigrationAction action,
            Guid sourceSideSourceId,
            ConflictManager conflictMgrService,
            out WorkItem workItem)
        {
            workItem = null;

            if (IsSourceWorkItemInBacklog(conflictMgrService, action))
            {
                return false;
            }

            workItem = GetTargetTfsWorkItem(action);

            return TryApplyWitDataChanges(action, workItem, false, sourceSideSourceId, conflictMgrService);
        }

        protected bool TryApplyAttachmentChangesByOM(
            IMigrationAction action,
            Guid sourceSideSourceId,
            ConflictManager conflictMgrService,
            ConversionResult changeResult,
            out WorkItem workItem,
            out string tmpDataFolder)
        {
            Debug.Assert(null != action.MigrationActionDescription.DocumentElement);

            workItem = null;
            tmpDataFolder = string.Empty;

            if (IsSourceWorkItemInBacklog(conflictMgrService, action))
            {
                return false;
            }

            workItem = GetTargetTfsWorkItem(action);

            XmlNode attachmentNode = action.MigrationActionDescription.DocumentElement.FirstChild;
            string originalName = attachmentNode.Attributes["Name"].Value;
            string utcCreationDate = attachmentNode.Attributes["UtcCreationDate"].Value;
            string utcLastWriteDate = attachmentNode.Attributes["UtcLastWriteDate"].Value;
            string length = attachmentNode.Attributes["Length"].Value;
            string comment = attachmentNode.FirstChild.InnerText;

            int targetWorkItemId = FindTargetWorkItemId(action, conflictMgrService);
            Debug.Assert(targetWorkItemId != int.MinValue, "Target Work Item does not exist");
            int[] fileId = FindAttachmentFileId(targetWorkItemId, originalName, utcCreationDate, utcLastWriteDate, length, comment);

            if (action.Action == WellKnownChangeActionId.DelAttachment)
            {
                if (fileId.Length == 0)
                {
                    action.State = ActionState.Complete;
                }
                else
                {
                    workItem.Attachments.RemoveAt(fileId[0]);
                }
            }
            else
            {
                try
                {
                    string sourceStoreCountString = attachmentNode.Attributes["CountInSourceSideStore"].Value;
                    int sourceStoreCount;
                    if (int.TryParse(sourceStoreCountString, out sourceStoreCount))
                    {
                        if (sourceStoreCount <= fileId.Length)
                        {
                            action.State = ActionState.Complete;
                        }
                    }
                }
                catch (Exception e)
                {
                    TraceManager.TraceVerbose(e.ToString());
                    // for backward compatibility, just proceed
                }

                if (AttachmentIsOversized(length))
                {
                    MigrationConflict conflict = new FileAttachmentOversizedConflictType().CreateConflict(
                        originalName, length, MaxAttachmentSize, targetWorkItemId.ToString(), Core.ServerName, Core.Config.Project, action);

                    List<MigrationAction> actions;
                    ConflictResolutionResult resolveRslt = conflictMgrService.TryResolveNewConflict(conflictMgrService.SourceId, conflict, out actions);

                    if (!resolveRslt.Resolved)
                    {
                        return false;
                    }

                    if (resolveRslt.ResolutionType == ConflictResolutionType.SuppressedConflictedChangeAction)
                    {
                        if (action.State == ActionState.Pending)
                        {
                            action.State = ActionState.Complete;
                        }
                        return true;
                    }

                    if (resolveRslt.ResolutionType == ConflictResolutionType.Other)
                    {
                        // conflict resolved, just proceed
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }

                //Now download the file
                Guid fileGuid = Guid.NewGuid();
                string filePath = string.Empty;

                Debug.Assert(!string.IsNullOrEmpty(LocalWorkDir));
                filePath = Path.Combine(LocalWorkDir, fileGuid.ToString());
                Directory.CreateDirectory(filePath);
                tmpDataFolder = filePath;

                filePath = Path.Combine(filePath, originalName);
                action.SourceItem.Download(filePath);

                if (!File.Exists(filePath))
                {
                    throw new MigrationException(TfsWITAdapterResources.ErrorAttachmentDownloadFailure, originalName);
                }

                workItem.Attachments.Add(new Attachment(filePath, comment));
            }

            return true;
        }

        protected bool AttachmentIsOversized(string length)
        {
            long attachmentSize = long.Parse(length);
            return attachmentSize > MaxAttachmentSize;
        }

        protected void SubmitChangesWithWitOM(
            ChangeGroup changeGroup,
            ConversionResult changeResult,
            Guid sourceSideSourceId)
        {
            try
            {
                SourceSideSourceId = sourceSideSourceId;

                var conflictMgrService = ServiceContainer.GetService(typeof(ConflictManager)) as ConflictManager;
                Debug.Assert(null != conflictMgrService, "cannot get conflict management service.");

                // NOTE / TODO:
                //   Currently, work item revisions are submitted separately. To minimize server round-trips for
                //   performance improvement, we may want to submit changes in batch.
                foreach (IMigrationAction action in changeGroup.Actions)
                {
                    if (action.State == ActionState.Skipped)
                    {
                        action.State = ActionState.Complete;
                        continue;
                    }

                    // Try apply changes
                    if (action.MigrationActionDescription == null
                        || action.MigrationActionDescription.DocumentElement == null)
                    {
                        throw new MigrationException(TfsWITAdapterResources.ErrorInvalidActionDescription, action.ActionId);
                    }

                    string tmpDataFolder = string.Empty;
                    WorkItem workItem;
                    if (action.Action == WellKnownChangeActionId.Add)
                    {
                        if (!TryApplyAddWorkItemChangesByOM(action, sourceSideSourceId, conflictMgrService, out workItem))
                        {
                            continue;
                        }
                    }
                    else if (action.Action == WellKnownChangeActionId.Edit)
                    {
                        if (!TryApplyEditWorkItemChangesByOM(action, sourceSideSourceId, conflictMgrService, out workItem))
                        {
                            continue;
                        }
                    }
                    else if (action.Action == WellKnownChangeActionId.AddAttachment
                             || action.Action == WellKnownChangeActionId.DelAttachment)
                    {
                        if (!TryApplyAttachmentChangesByOM(action, sourceSideSourceId, conflictMgrService,
                                                           changeResult, out workItem, out tmpDataFolder)
                            || action.State == ActionState.Skipped
                            || action.State == ActionState.Complete)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }

                    // Try submit changes
                    UpdateResult result;
                    try
                    {
                        string convHistComment = GenerateMigrationHistoryComment(action);
                        Field historyField = workItem.Fields[CoreField.History];
                        historyField.Value = ((string)historyField.Value ?? string.Empty) + " " + convHistComment;

                        workItem.Save();
                        result = new UpdateResult(new Watermark(workItem.Id.ToString(), workItem.Revision));
                    }
                    catch (FileAttachmentException)
                    {
                        Debug.Assert(action.Action == WellKnownChangeActionId.AddAttachment
                                     || action.Action == WellKnownChangeActionId.DelAttachment);

                        XmlDocument desc = action.MigrationActionDescription;
                        XmlElement rootNode = desc.DocumentElement;
                        Debug.Assert(null != rootNode);
                        XmlNode attachmentNode = rootNode.FirstChild;
                        string originalName = attachmentNode.Attributes["Name"].Value;
                        string utcCreationDate = attachmentNode.Attributes["UtcCreationDate"].Value;
                        string utcLastWriteDate = attachmentNode.Attributes["UtcLastWriteDate"].Value;
                        string length = attachmentNode.Attributes["Length"].Value;
                        string comment = attachmentNode.FirstChild.InnerText;

                        int targetWorkItemId = FindTargetWorkItemId(action, conflictMgrService);
                        string targetRevision = rootNode.Attributes["TargetRevision"].Value;

                        MigrationConflict conflict = new FileAttachmentOversizedConflictType().CreateConflict(
                            originalName, length, MaxAttachmentSize, targetWorkItemId.ToString(), Core.ServerName, Core.Config.Project, action);

                        List<MigrationAction> actions;
                        ConflictResolutionResult resolveRslt =
                            conflictMgrService.TryResolveNewConflict(conflictMgrService.SourceId, conflict, out actions);

                        if (!resolveRslt.Resolved)
                        {
                            continue;
                        }
                        else
                        {
                            if (resolveRslt.ResolutionType == ConflictResolutionType.SuppressedConflictedChangeAction)
                            {
                                if (action.State == ActionState.Pending)
                                {
                                    action.State = ActionState.Complete;
                                }
                                // NOTE: conflict manager is responsible for marking the parent change group
                                // to be completed in case there is single action in the group
                                continue;
                            }
                            else if (resolveRslt.ResolutionType == ConflictResolutionType.Other)
                            {
                                workItem.Save();
                                result = new UpdateResult(new Watermark(workItem.Id.ToString(), workItem.Revision));
                            }
                            else
                            {
                                throw new NotImplementedException();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result = new UpdateResult(ex);
                    }
                    finally
                    {
                        if (!string.IsNullOrEmpty(tmpDataFolder))
                        {
                            Directory.Delete(tmpDataFolder, true);
                        }
                    }

                    if (result.Exception != null)
                    {
                        TryResolveWitSubmissionException(conflictMgrService, result.Exception, action);
                    }
                    else
                    {
                        if (action.State == ActionState.Pending)
                        {
                            action.State = ActionState.Complete;
                        }

                        // NOTE: if we allow multiple change actions per group for WIT
                        // this has to be moved outside the for loop
                        UpdateConversionHistory(action, result.Watermark, changeResult);
                    }
                }
            }
            finally
            {
                SourceSideSourceId = Guid.Empty;
            }
        }

        protected ConflictResolutionResult TryResolveWitSubmissionException(
            ConflictManager conflictManager,
            Exception ex,
            IMigrationAction action)
        {
            TraceManager.TraceError(ex.ToString());

            string sourceItemId = GetSourceWorkItemId(action);
            string sourceItemRevision = GetSourceWorkItemRevision(action);

            MigrationConflict conflict = InvalidSubmissionConflictType.CreateConflict(
                action, ex, sourceItemId, sourceItemRevision);

            List<MigrationAction> actions;
            ConflictResolutionResult resolutionRslt =
                conflictManager.TryResolveNewConflict(conflictManager.SourceId, conflict, out actions);
            return resolutionRslt;
        }

        internal WorkItem GetTargetTfsWorkItem(
            IMigrationAction action)
        {
            ConflictManager conflictManager = ServiceContainer.GetService(typeof(ConflictManager)) as ConflictManager;

            int targetWorkItemId = FindTargetWorkItemId(action, conflictManager);

            try
            {
                return WorkItemStore.GetWorkItem(targetWorkItemId);
            }
            catch (DeniedOrNotExistException ex)
            {
                throw new MigrationException(
                    string.Format(TfsWITAdapterResources.ErrorOrphanedWorkItem, targetWorkItemId),
                    ex);
            }
        }

        /// <summary>
        /// Determine if an action's source item is in backlog, if so, backlog the action
        /// </summary>
        /// <param name="conflictManager"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        internal bool IsSourceWorkItemInBacklog(
            ConflictManager conflictManager,
            IMigrationAction action)
        {
            string sourceSideItemId = action.FromPath;
            Debug.Assert(!string.IsNullOrEmpty(sourceSideItemId), "Work Item Id is not available in conflict details");

            // look up in backlog db table
            bool workItemInBacklog = conflictManager.IsItemInBacklog(sourceSideItemId);

            if (workItemInBacklog)
            {
                MigrationConflict chainedConflict = new ChainOnBackloggedItemConflictType().CreateConflict(
                    ChainOnBackloggedItemConflictType.CreateConflictDetails(sourceSideItemId, action.Version),
                    ChainOnBackloggedItemConflictType.CreateScopeHint(sourceSideItemId),
                    action);

                // previous revision of the work item has conflict, push this revision to backlog as well
                conflictManager.BacklogUnresolvedConflict(conflictManager.SourceId, chainedConflict, false);
            }

            return workItemInBacklog;
        }

        internal bool TryApplyWitDataChanges(
            IMigrationAction action,
            WorkItem workItem,
            bool isNewItem,
            Guid sourceSideSourceId,
            ConflictManager conflictManager)
        {
            bool allFieldDataAreValid = true;
            bool hasArea = false;
            bool hasIteration = false;

            XmlNodeList columns = action.MigrationActionDescription.SelectNodes("/WorkItemChanges/Columns/Column");
            if (null == columns)
            {
                throw new MigrationException(TfsWITAdapterResources.ErrorInvalidActionDescription, action.ActionId);
            }

            foreach (XmlNode columnData in columns)
            {
                string stringVal = columnData.FirstChild.InnerText;
                string fieldRefName = columnData.Attributes["ReferenceName"].Value;

                Debug.Assert(!string.IsNullOrEmpty(fieldRefName),
                             "Field ReferenceName is absent in the Migration Description");

                try
                {
                    if (fieldRefName.Equals(CoreFieldReferenceNames.AreaPath))
                    {
                        // Substitute AreaPath with AreaId
                        fieldRefName = CoreFieldReferenceNames.AreaId;
                        stringVal = Core.TranslatePath(Node.TreeType.Area, stringVal).ToString();
                        hasArea = true;
                    }
                    else if (fieldRefName.Equals(CoreFieldReferenceNames.IterationPath))
                    {
                        // Substitute IterationPath with IterationId
                        fieldRefName = CoreFieldReferenceNames.IterationId;
                        stringVal = Core.TranslatePath(Node.TreeType.Iteration, stringVal).ToString();
                        hasIteration = true;
                    }

                    object value = string.IsNullOrEmpty(stringVal) ?
                        null : ParseFieldValue(workItem.Fields[fieldRefName].FieldDefinition.FieldType, stringVal);

                    workItem.Fields[fieldRefName].Value = value;
                }
                catch (ValidationException vEx)
                {
                    if (!string.IsNullOrEmpty(vEx.Message)
                        && vEx.Message.Contains("TF26194"))
                    {
                        workItem.Fields[fieldRefName].Value = null;
                        continue;
                    }
                }
                catch (FieldDefinitionNotExistException)
                {
                    string sourceItemId = GetSourceWorkItemId(action);
                    string sourceItemRevision = GetSourceWorkItemRevision(action);

                    MigrationConflict conflict = new InvalidFieldConflictType().CreateConflict(
                        InvalidFieldConflictType.CreateConflictDetails(sourceItemId, sourceItemRevision, fieldRefName,
                                                                       workItem.Type),
                        InvalidFieldConflictType.CreateScopeHint(workItem),
                        action);

                    List<MigrationAction> actions;
                    ConflictResolutionResult resolutionRslt =
                        conflictManager.TryResolveNewConflict(conflictManager.SourceId, conflict, out actions);
                    if (!resolutionRslt.Resolved)
                    {
                        return false;
                    }

                    if (resolutionRslt.ResolutionType == ConflictResolutionType.UpdatedConflictedChangeAction)
                    {
                        // recursively apply changes until there is no conflict or we fail
                        return TryApplyWitDataChanges(action, workItem, isNewItem, sourceSideSourceId,
                                                      conflictManager);
                    }
                }
            }

            if (isNewItem)
            {
                if (!hasArea)
                {
                    // TODO: may consult path missing setting, using default id for now
                    workItem.Fields[CoreField.AreaId].Value = Core.DefaultAreaId;
                }
                if (!hasIteration)
                {
                    // TODO: may consult path missing setting, using default id for now
                    workItem.Fields[CoreField.IterationId].Value = Core.DefaultIterationId;
                }
            }

            ConflictManager conflictMgrService = null;
            foreach (Field field in workItem.Fields)
            {
                if (!allFieldDataAreValid)
                {
                    break;
                }

                if (!field.IsValid)
                {
                    if (null == conflictMgrService)
                    {
                        conflictMgrService = ServiceContainer.GetService(typeof(ConflictManager)) as ConflictManager;
                    }

                    Debug.Assert(
                        m_fieldNameComparer.Compare(field.ReferenceName, CoreFieldReferenceNames.IterationId) != 0
                        && m_fieldNameComparer.Compare(field.ReferenceName, CoreFieldReferenceNames.AreaId) != 0,
                        "There is invalid AreaId or IterationId revision on Work Item.");

                    MigrationConflict conflict = new InvalidFieldValueConflictType().CreateConflict(
                        InvalidFieldValueConflictType.CreateConflictDetails(action.FromPath, action.Version, field),
                        InvalidFieldValueConflictType.CreateScopeHint(field),
                        action);
                    List<MigrationAction> rsltActions;
                    ConflictResolutionResult result = conflictMgrService.TryResolveNewConflict(
                        conflictMgrService.SourceId, conflict, out rsltActions);

                    if (!result.Resolved)
                    {
                        // we reach here because sync orchestrator wants us to proceed
                        // even after we detect above unresolvable conflict
                        // here we push the source item in the backlogged item list
                        allFieldDataAreValid = false;
                        break;
                    }
                    if (result.ResolutionType == ConflictResolutionType.UpdatedConflictedChangeAction)
                    {
                        // extract the mapped value in the action description doc
                        // and apply to the field
                        Debug.Assert(null != conflict.ConflictedChangeAction,
                                     "Invalid field value conflict does not contain a conflicted change action.");

                        XmlDocument mappedChangeData = conflict.ConflictedChangeAction.MigrationActionDescription;
                        XmlNode fieldCol = mappedChangeData.SelectSingleNode(
                            string.Format("/WorkItemChanges/Columns/Column[@ReferenceName='{0}']",
                                          field.ReferenceName));

                        if (null != fieldCol)
                        {
                            string mappedFieldValueStr = fieldCol.FirstChild.InnerText;
                            object value = string.IsNullOrEmpty(mappedFieldValueStr) ?
                                null : ParseFieldValue(field.FieldDefinition.FieldType, mappedFieldValueStr);
                            field.Value = value;

                            if (!field.IsValid)
                            {
                                TraceManager.TraceVerbose("{0} is not valid after a conflict resolution rule drops it from sync", field.ReferenceName);
                                conflict = new InvalidFieldValueConflictType().CreateConflict(
                                    InvalidFieldValueConflictType.CreateConflictDetails(action.FromPath, action.Version, field),
                                    InvalidFieldValueConflictType.CreateScopeHint(field),
                                    action);
                                conflictMgrService.BacklogUnresolvedConflict(conflictMgrService.SourceId, conflict, false);
                                allFieldDataAreValid = false;
                                break;
                            }
                        }
                        else
                        {
                            field.Value = field.OriginalValue;
                            if (!field.IsValid)
                            {
                                field.Value = null;
                                if (!field.IsValid)
                                {
                                    TraceManager.TraceVerbose("{0} is not valid after a conflict resolution rule drops it from sync", field.ReferenceName);
                                    conflictMgrService.BacklogUnresolvedConflict(conflictMgrService.SourceId, conflict, false);
                                    allFieldDataAreValid = false;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return allFieldDataAreValid;
        }

        protected static object ParseFieldValue(
            FieldType fieldType,
            string fieldValue)
        {
            object newValue;
            switch (fieldType)
            {
                case FieldType.Integer:
                    int intOutput;
                    newValue = Int32.TryParse(fieldValue, out intOutput) ? intOutput as object : null;
                    break;
                case FieldType.Double:
                    Double dblOutput;
                    newValue = Double.TryParse(fieldValue, out dblOutput) ? dblOutput as object : null;
                    break;
                case FieldType.String:
                case FieldType.PlainText:
                case FieldType.Html:
                case FieldType.TreePath:
                case FieldType.History:
                    newValue = fieldValue;
                    break;
                case FieldType.DateTime:
                    DateTime dtOutput;
                    newValue = DateTime.TryParse(fieldValue, out dtOutput) ? dtOutput as object : null;
                    break;
                default:
                    Debug.Assert(false, "Invalid field type - cannot convert to system type");
                    throw new InvalidOperationException(string.Format("Cannot convert {0} to System Type", fieldValue));
            }
            return newValue;
        }

        internal void SubmitLinkChanges(
            LinkChangeGroup linkChanges,
            ServiceContainer serviceContainer,
            TfsLinkingProviderBase.LinkSubmissionPhase submissionPhase)
        {
            if (ByPassrules)
            {
                SubmitLinkChangesWithUpdateDoc(linkChanges, serviceContainer, submissionPhase);
            }
            else
            {
                SubmitLinkChangesWithUpdateDoc(linkChanges, serviceContainer, submissionPhase);
                //SubmitLinkChangesWithWitOM(linkChanges);
            }
        }

        protected virtual TfsUpdateDocument InitializeUpdateDocument()
        {
            return new TfsUpdateDocument(this);
        }

        protected virtual void SubmitLinkChangesWithUpdateDoc(
            LinkChangeGroup linkChanges,
            ServiceContainer serviceContainer,
            TfsLinkingProviderBase.LinkSubmissionPhase submissionPhase)
        {
            ConfigurationService configService = serviceContainer.GetService(typeof(ConfigurationService)) as ConfigurationService;
            ITranslationService translationService = serviceContainer.GetService(typeof(ITranslationService)) as ITranslationService;

            if (linkChanges.Actions.Count == 0)
            {
                linkChanges.Status = LinkChangeGroup.LinkChangeGroupStatus.Completed;
                return;
            }

            // group changes by work item Id
            Dictionary<int, List<LinkChangeAction>> perWorkItemLinkChanges = RegroupLinkChangeActions(linkChanges);
            var orderedWorkitemId = new Dictionary<int, int>();
            int index = 0;
            foreach (int workItemId in perWorkItemLinkChanges.Keys)
            {
                orderedWorkitemId.Add(index++, workItemId);
            }

            // batch-submit links of each work item
            var updateDocs = new List<XmlDocument>(perWorkItemLinkChanges.Count);
            int unsuppportedLinkChangeActions = 0;
            List<int> noUpdatesWorkItemIds = new List<int>();
            foreach (var perWorkItemLinkChange in perWorkItemLinkChanges)
            {
                WorkItem workItem = WorkItemStore.GetWorkItem(perWorkItemLinkChange.Key);

                TfsUpdateDocument tfsUpdateDocument = InitializeUpdateDocument();
                tfsUpdateDocument.CreateWorkItemUpdateDoc(workItem);

                int readyForMigrationLinkActionCount = 0;
                foreach (LinkChangeAction linkChangeAction in perWorkItemLinkChange.Value)
                {
                    if (linkChangeAction.Status != LinkChangeAction.LinkChangeActionStatus.ReadyForMigration
                        || linkChangeAction.IsConflicted)
                    {
                        continue;
                    }

                    if (!ProcessActionInCurrentSubmissionPhase(linkChangeAction, submissionPhase))
                    {
                        continue;
                    }

                    var handler = linkChangeAction.Link.LinkType as ILinkHandler;
                    Debug.Assert(null != handler, "linktype is not an ILinkHandler");
                    if (handler.UpdateTfs(tfsUpdateDocument, linkChangeAction))
                    {
                        ++readyForMigrationLinkActionCount;
                    }
                    else
                    {
                        linkChangeAction.Status = LinkChangeAction.LinkChangeActionStatus.Completed;
                        linkChangeAction.IsConflicted = true;
                        unsuppportedLinkChangeActions++;
                    }
                }

                if (readyForMigrationLinkActionCount > 0)
                {
                    updateDocs.Add(tfsUpdateDocument.UpdateDocument);
                }
                else
                {
                    noUpdatesWorkItemIds.Add(perWorkItemLinkChange.Key);
                }
            }

            if (updateDocs.Count == 0)
            {
                linkChanges.Status = LinkChangeGroup.LinkChangeGroupStatus.Completed;
                return;
            }

            foreach (var noUpdateWorkItemId in noUpdatesWorkItemIds)
            {
                perWorkItemLinkChanges.Remove(noUpdateWorkItemId);
            }
            Debug.Assert(updateDocs.Count == perWorkItemLinkChanges.Count, "mismatch number of update documents");

            UpdateResult[] results = TfsBatchUpdateHelper.Submit(Core, WorkItemServer, updateDocs.ToArray());
            if (results.Length != updateDocs.Count)
            {
                string msg = string.Format(
                    TfsWITAdapterResources.Culture,
                    TfsWITAdapterResources.ErrorWrongNumberOfUpdateResults,
                    Core.ServerName,
                    StoreName,
                    updateDocs.Count,
                    results.Length);
                throw new SynchronizationEngineException(msg);
            }

            bool succeeded = true;
            for (int i = 0; i < results.Length; ++i)
            {
                UpdateResult rslt = results[i];

                if (rslt.Exception != null
                    && !rslt.Exception.Message.Contains("The specified link already exists"))
                {
                    TraceManager.TraceError(rslt.Exception.ToString());

                    succeeded = false;
                    // TODO
                    // Try resolve conflict and push to backlog if resolution fails
                    foreach (LinkChangeAction action in perWorkItemLinkChanges[orderedWorkitemId[i]])
                    {
                        action.IsConflicted = true;
                    }
                }
                else
                {
                    foreach (LinkChangeAction action in perWorkItemLinkChanges[orderedWorkitemId[i]])
                    {
                        if (ProcessActionInCurrentSubmissionPhase(action, submissionPhase))
                        {
                            MarkLinkChangeActionCompleted(action);
                        }
                    }

                    if (rslt.Exception == null)
                    {
                        UpdateLinkConversionHistory(configService, translationService, rslt, perWorkItemLinkChanges[orderedWorkitemId[i]]);
                    }
                }
            }

            linkChanges.Status = succeeded && AllActionSubmitted(linkChanges)
                                     ? LinkChangeGroup.LinkChangeGroupStatus.Completed
                                     : LinkChangeGroup.LinkChangeGroupStatus.ReadyForMigration;
        }

        protected bool AllActionSubmitted(LinkChangeGroup linkChanges)
        {
            bool retVal = true;
            foreach (var action in linkChanges.Actions)
            {
                if (action.Status != LinkChangeAction.LinkChangeActionStatus.Completed)
                {
                    retVal = false;
                    break;
                }
            }
            return retVal;
        }

        protected bool ProcessActionInCurrentSubmissionPhase(
            LinkChangeAction linkChangeAction, 
            TfsLinkingProviderBase.LinkSubmissionPhase submissionPhase)
        {
            switch (submissionPhase)
            {
                case TfsLinkingProviderBase.LinkSubmissionPhase.Add:
                    return linkChangeAction.ChangeActionId.Equals(WellKnownChangeActionId.Add);
                case TfsLinkingProviderBase.LinkSubmissionPhase.Edit:
                    return linkChangeAction.ChangeActionId.Equals(WellKnownChangeActionId.Edit);
                case TfsLinkingProviderBase.LinkSubmissionPhase.Deletion:
                    return linkChangeAction.ChangeActionId.Equals(WellKnownChangeActionId.Delete);
                default:
                    throw new InvalidOperationException();
            }
        }

        protected void UpdateLinkConversionHistory(
            ConfigurationService configService, 
            ITranslationService translationService, 
            UpdateResult rslt,
            List<LinkChangeAction> syncedLinkActions)
        {
            if (null == configService)
            {
                TraceManager.TraceWarning("Cannot update link conversion history: configuration service is not initialized");
                return;
            }

            if (null == translationService)
            {
                TraceManager.TraceWarning("Cannot update link conversion history: translation service is not initialized");
                return;
            }

            if (null == rslt)
            {
                TraceManager.TraceWarning("Cannot update link conversion history: update result is not initialized");
                return;
            }

            if (null == rslt.Watermark)
            {
                TraceManager.TraceWarning("Cannot update link conversion history: update watermark is not available");
                return;
            }

            if (string.IsNullOrEmpty(rslt.Watermark.Id))
            {
                TraceManager.TraceWarning("Cannot update link conversion history: watermark.Id is empty");
                return;
            }

            string sourceItemId = translationService.TryGetTargetItemId(rslt.Watermark.Id, configService.SourceId);
            if (!string.IsNullOrEmpty(sourceItemId))
            {
                ConversionResult convRslt = new ConversionResult(configService.MigrationPeer, configService.SourceId);
                convRslt.ItemConversionHistory.Add(
                    new ItemConversionHistory(sourceItemId, "Link", rslt.Watermark.Id, rslt.Watermark.Revision.ToString()));

                convRslt.ChangeId = rslt.Watermark.Id + ":" + rslt.Watermark.Revision + " (Links)";
                convRslt.Save(configService.SourceId);
            }
            else
            {
                TraceManager.TraceError(
                    "Cannot find mirrored item for Work Item '{0}'. Link converion update will be skipped.",
                    rslt.Watermark.Id);
            }

            List<LinkChangeAction> artifactLinkActions = new List<LinkChangeAction>();
            foreach (LinkChangeAction action in syncedLinkActions)
            {
                if (!action.Link.LinkType.GetsActionsFromLinkChangeHistory)
                {
                    artifactLinkActions.Add(action);
                }
            }

            if (artifactLinkActions.Count > 0)
            {
                WorkItemLinkStore relatedArtifactsStore = new WorkItemLinkStore(configService.SourceId);
                relatedArtifactsStore.UpdateSyncedLinks(artifactLinkActions);
            }
        }

        protected void MarkLinkChangeActionCompleted(LinkChangeAction action)
        {
            if (action.Status == LinkChangeAction.LinkChangeActionStatus.ReadyForMigration)
            {
                action.Status = LinkChangeAction.LinkChangeActionStatus.Completed;
            }
        }

        protected virtual Dictionary<int, List<LinkChangeAction>> RegroupLinkChangeActions(
            LinkChangeGroup linkChangeGroup)
        {
            var perWorkItemLinkChanges = new Dictionary<int, List<LinkChangeAction>>();

            foreach (LinkChangeAction linkChangeAction in linkChangeGroup.Actions)
            {
                Debug.Assert(!string.IsNullOrEmpty(linkChangeAction.Link.SourceArtifactId));

                int sourceArtifactWorkItemId;
                bool idConversionResult = int.TryParse(linkChangeAction.Link.SourceArtifactId,
                                                       out sourceArtifactWorkItemId);
                Debug.Assert(idConversionResult);

                if (!perWorkItemLinkChanges.ContainsKey(sourceArtifactWorkItemId))
                {
                    perWorkItemLinkChanges.Add(sourceArtifactWorkItemId, new List<LinkChangeAction>());
                }

                if (!perWorkItemLinkChanges[sourceArtifactWorkItemId].Contains(linkChangeAction))
                {
                    perWorkItemLinkChanges[sourceArtifactWorkItemId].Add(linkChangeAction);
                }
            }

            return perWorkItemLinkChanges;
        }

        protected void SubmitLinkChangesWithWitOM(
            LinkChangeGroup linkChanges)
        {
            linkChanges.Status = LinkChangeGroup.LinkChangeGroupStatus.Completed;
            throw new NotImplementedException();
        }

        internal void SubmitChanges(ChangeGroup changeGroup, ConversionResult changeResult, Guid sourceSideSourceId)
        {
            if (ByPassrules)
            {
                SubmitChangesWithUpdateDoc(changeGroup, changeResult, sourceSideSourceId);
            }
            else
            {
                SubmitChangesWithWitOM(changeGroup, changeResult, sourceSideSourceId);
            }
        }

        /// <summary>
        /// Submit Work Item changes with WIT update document.
        /// </summary>
        /// <param name="changeGroup"></param>
        /// <param name="changeResult"></param>
        /// <param name="sourceSideSourceId"></param>
        /// <returns></returns>
        protected void SubmitChangesWithUpdateDoc(
            ChangeGroup changeGroup,
            ConversionResult changeResult,
            Guid sourceSideSourceId)
        {
            try
            {
                SourceSideSourceId = sourceSideSourceId;

                var conflictMgrService = ServiceContainer.GetService(typeof(ConflictManager)) as ConflictManager;
                Debug.Assert(null != conflictMgrService, "cannot get conflict management service.");

                int skippedActionCount = 0;
                // NOTE / TODO:
                //   Currently, work item revisions are submitted separately. To minimize server round-trips for
                //   performance improvement, we may want to submit changes in batch.
                foreach (IMigrationAction action in changeGroup.Actions)
                {
                    if (action.State == ActionState.Skipped)
                    {
                        action.State = ActionState.Complete;
                        ++skippedActionCount;
                        continue;
                    }

                    try
                    {
                        if (action.Action.Equals(WellKnownChangeActionId.Edit)
                            || action.Action.Equals(WellKnownChangeActionId.AddAttachment)
                            || action.Action.Equals(WellKnownChangeActionId.DelAttachment))
                        {
                            if (IsSourceWorkItemInBacklog(conflictMgrService, action)) continue;
                        }
                        else if (action.Action.Equals(WellKnownChangeActionId.Add))
                        {
                            // check if the Work Item Type exists on the target team project
                            // if the WITD doesn't exist, the following method raises a conflict and throws
                            ValidateAndGetWorkItemType(action, conflictMgrService);
                        }

                        TfsUpdateDocument updateDocument = CreateUpdateOperationDoc(action);

                        if (updateDocument == null)
                        {
                            if (action.Action.Equals(WellKnownChangeActionId.AddAttachment))
                            {
                                if (action.State != ActionState.Skipped)
                                {
                                    // action is not skipped but update document is not generated - we have a conflict
                                    return;
                                }
                                else
                                {
                                    // this happens when an attachment file exists on the target side
                                    // or conflict resolution choose to drop the file
                                    ++skippedActionCount;
                                    continue;
                                }
                            }
                            else
                            {
                                return;
                            }
                        }

                        var updates = new XmlDocument[1] { updateDocument.UpdateDocument };
                        UpdateResult[] results = TfsBatchUpdateHelper.Submit(Core, WorkItemServer, updates);

                        if (results.Length != updates.Length)
                        {
                            string msg = string.Format(
                                TfsWITAdapterResources.Culture,
                                TfsWITAdapterResources.ErrorWrongNumberOfUpdateResults,
                                Core.ServerName,
                                StoreName,
                                updates.Length,
                                results.Length);
                            throw new SynchronizationEngineException(msg);
                        }

                        for (int i = 0; i < results.Length; ++i)
                        {
                            UpdateResult rslt = results[i];

                            if (rslt.Exception != null)
                            {
                                if (rslt.Exception.Message.Contains("The Work Item is either missing or has already been updated")
                                    && action.Action.Equals(WellKnownChangeActionId.Edit))
                                {
                                    updateDocument.IncrementRevision();
                                    UpdateResult[] resubmitResult = TfsBatchUpdateHelper.Submit(Core, WorkItemServer, new XmlDocument[1] { updateDocument.UpdateDocument });
                                    Debug.Assert(resubmitResult.Length == 1);
                                    rslt = resubmitResult[0];
                                }
                            }

                            if (rslt.Exception != null)
                            {
                                TryResolveWitSubmissionException(conflictMgrService, rslt.Exception, action);
                            }
                            else
                            {
                                if (action.State == ActionState.Pending)
                                {
                                    action.State = ActionState.Complete;
                                }

                                //TODO: Update watermark on pending update statements
                                UpdateConversionHistory(action, rslt.Watermark, changeResult);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex is MigrationUnresolvedConflictException)
                        {
                            throw;
                        }
                        else
                        {
                            TryResolveWitSubmissionException(conflictMgrService, ex, action);
                        }
                    }
                }

                if (skippedActionCount == changeGroup.Actions.Count)
                {
                    changeResult.ChangeId = Toolkit.Constants.MigrationResultSkipChangeGroup;
                }
            }
            finally
            {
                SourceSideSourceId = sourceSideSourceId;
            }
        }

        protected void UpdateConversionHistory(
            IMigrationAction action,
            Watermark targetItemWatermark,
            ConversionResult changeResult)
        {
            int targetWorkItemId = int.Parse(targetItemWatermark.Id);
            string sourceWorkItemId = GetSourceWorkItemId(action);
            string sourceWorkItemRevision = GetSourceWorkItemRevision(action);

            // update work item mapping cache
            if (!m_mappedWorkItem.ContainsKey(sourceWorkItemId))
            {
                m_mappedWorkItem.Add(sourceWorkItemId, targetWorkItemId);
            }

            // update conversion history cache
            if (action.Action.Equals(WellKnownChangeActionId.Add)
                || action.Action.Equals(WellKnownChangeActionId.Edit))
            {
                // insert conversion history for pushing to db
                changeResult.ItemConversionHistory.Add(
                    new ItemConversionHistory(sourceWorkItemId, sourceWorkItemRevision, targetItemWatermark.Id,
                                              targetItemWatermark.Revision.ToString()));
                changeResult.ChangeId = targetItemWatermark.Id + ":" + targetItemWatermark.Revision;
            }
            else if (action.Action.Equals(WellKnownChangeActionId.AddAttachment)
                     || action.Action.Equals(WellKnownChangeActionId.DelAttachment))
            {
                // insert conversion history for pushing to db
                changeResult.ItemConversionHistory.Add(
                    new ItemConversionHistory(sourceWorkItemId, "Attachment", targetItemWatermark.Id,
                                              targetItemWatermark.Revision.ToString()));
                changeResult.ChangeId = targetItemWatermark.Id + ":" + targetItemWatermark.Revision + " (Attachments)";

                WorkItemAttachmentStore store = new WorkItemAttachmentStore(action.ChangeGroup.SourceId);
                store.Update(targetWorkItemId.ToString(), action);
            }
        }

        protected TfsUpdateDocument CreateUpdateOperationDoc(
            IMigrationAction action)
        {
            var conflictMgrService = ServiceContainer.GetService(typeof(ConflictManager)) as ConflictManager;
            Debug.Assert(null != conflictMgrService, "cannot get conflict management service.");

            TfsUpdateDocument updateDocument = null;
            if (action.Action == WellKnownChangeActionId.Add)
            {
                updateDocument = CreateNewWorkItemOperation(action, conflictMgrService);
            }
            else if (action.Action == WellKnownChangeActionId.Edit)
            {
                FindTargetWorkItemLatestRevision(action);
                updateDocument = CreateWorkItemUpdateOperation(action, conflictMgrService);
            }
            else if (action.Action == WellKnownChangeActionId.AddAttachment
                     || action.Action == WellKnownChangeActionId.DelAttachment)
            {
                FindTargetWorkItemLatestRevision(action);
                updateDocument = SubmitAttachmentChanges(action, conflictMgrService);
            }

            return updateDocument;
        }

        internal WorkItemType GetWorkItemType(string workItemTypeName)
        {
            return WorkItemStore.Projects[TeamProject].WorkItemTypes[workItemTypeName];
        }

        protected TfsUpdateDocument CreateNewWorkItemOperation(
            IMigrationAction action,
            ConflictManager conflictMgrService)
        {
            try
            {
                XmlDocument desc = action.MigrationActionDescription;
                XmlElement rootNode = desc.DocumentElement;
                Debug.Assert(null != rootNode,
                            "Wit IMigrationAction.MigrationActionDescription is invalid.");
                Debug.Assert(null != rootNode.Attributes["WorkItemType"],
                            "WorkItemType is missing in MigrationActionDescription.");
                Debug.Assert(null != rootNode.Attributes["Author"],
                            "Author is missing in MigrationActionDescription.");
                Debug.Assert(null != rootNode.Attributes["ChangeDate"],
                            "ChangeDate is missing in MigrationActionDescription.");

                string workItemType = rootNode.Attributes["WorkItemType"].Value;
                string author = NormalizeAuthorName(rootNode.Attributes["Author"].Value);
                string changedDate = rootNode.Attributes["ChangeDate"].Value;

                if (IsLastRevisionOfThisSyncCycle(action))
                {
                    MakeTargetWorkItemValid(action, CreateNewWorkItem(action, conflictMgrService));
                }

                TfsUpdateDocument tfsUpdateDocument = InitializeUpdateDocument();

                tfsUpdateDocument.CreateWorkItemInsertDoc();
                tfsUpdateDocument.AddFields(action, workItemType, author, changedDate, true);

                // append a tracing comment to System.History
                tfsUpdateDocument.InsertConversionHistoryCommentToHistory(
                    workItemType,
                    GenerateMigrationHistoryComment(action));

                // insert source item Id to field TfsMigrationTool.ReflectedWorkItemId if it is in WITD
                tfsUpdateDocument.InsertConversionHistoryField(
                    workItemType,
                    GetSourceWorkItemId(action));

                return tfsUpdateDocument;
            }
            catch (MigrationUnresolvedConflictException)
            {
                return null;
            }
        }

        protected void MakeTargetWorkItemValid(
            IMigrationAction action,
            WorkItem workItem)
        {
            TraceManager.TraceInformation("Validating last revision of the incoming work item ...");
            XmlNodeList columns = action.MigrationActionDescription.SelectNodes("/WorkItemChanges/Columns/Column");
            foreach (XmlNode columnData in columns)
            {
                string stringVal = columnData.FirstChild.InnerText;
                string fieldRefName = columnData.Attributes["ReferenceName"].Value;

                Debug.Assert(!string.IsNullOrEmpty(fieldRefName),
                             "Field ReferenceName is absent in the Migration Description");

                try
                {
                    if (TFStringComparer.WorkItemFieldReferenceName.Equals(fieldRefName, CoreFieldReferenceNames.AreaPath)
                        || TFStringComparer.WorkItemFieldReferenceName.Equals(fieldRefName, CoreFieldReferenceNames.AreaId)
                        || TFStringComparer.WorkItemFieldReferenceName.Equals(fieldRefName, CoreFieldReferenceNames.IterationPath)
                        || TFStringComparer.WorkItemFieldReferenceName.Equals(fieldRefName, CoreFieldReferenceNames.IterationId))
                    {
                        // CSS node validation and auto-creation (if configured) will be applied 
                        // when creating the WIT update document later in the process
                        continue;
                    }

                    object value = string.IsNullOrEmpty(stringVal) ?
                        null : ParseFieldValue(workItem.Fields[fieldRefName].FieldDefinition.FieldType, stringVal);

                    workItem.Fields[fieldRefName].Value = value;
                }
                catch (FieldDefinitionNotExistException)
                {
                    // the actual field definition missing conflict will be detected later
                    return;
                }
            }

            FieldValueCorrectionAlgorithm fldValueCorrectionAlg = new FieldValueCorrectionAlgorithm();
            foreach (Field f in workItem.Fields)
            {
                fldValueCorrectionAlg.TryCorrectFieldValue(f, action);
            }
        }

        protected string GenerateMigrationHistoryComment(IMigrationAction action)
        {
            ICommentDecorationService commentDecorationService = ServiceContainer.GetService(typeof(ICommentDecorationService)) as ICommentDecorationService;
            Debug.Assert(null != commentDecorationService);

            string sourceWorkItemId = GetSourceWorkItemId(action);
            Debug.Assert(!string.IsNullOrEmpty(sourceWorkItemId));
            string sourceWorkItemRevision = GetSourceWorkItemRevision(action);

            return commentDecorationService.GetChangeGroupCommentSuffix(
                string.Format("{0} (rev {1})", sourceWorkItemId, sourceWorkItemRevision));
        }

        protected TfsUpdateDocument CreateWorkItemUpdateOperation(
            IMigrationAction action,
            ConflictManager conflictMgrService)
        {
            try
            {
                XmlDocument desc = action.MigrationActionDescription;
                XmlElement rootNode = desc.DocumentElement;
                Debug.Assert(null != rootNode,
                            "Wit IMigrationAction.MigrationActionDescription is invalid.");
                Debug.Assert(null != rootNode.Attributes["WorkItemType"],
                            "WorkItemType is missing in MigrationActionDescription.");
                Debug.Assert(null != rootNode.Attributes["Author"],
                            "Author is missing in MigrationActionDescription.");
                Debug.Assert(null != rootNode.Attributes["ChangeDate"],
                            "ChangeDate is missing in MigrationActionDescription.");
                string workItemType = rootNode.Attributes["WorkItemType"].Value;
                string author = NormalizeAuthorName(rootNode.Attributes["Author"].Value);
                string changeDate = rootNode.Attributes["ChangeDate"].Value;

                if (IsLastRevisionOfThisSyncCycle(action))
                {
                    MakeTargetWorkItemValid(action, GetTargetTfsWorkItem(action));
                }

                int targetWorkItemId = FindTargetWorkItemId(action, conflictMgrService);
                string targetRevision = rootNode.Attributes["TargetRevision"].Value;

                TfsUpdateDocument tfsUpdateDocument = InitializeUpdateDocument();
                tfsUpdateDocument.CreateWorkItemUpdateDoc(targetWorkItemId.ToString(), targetRevision);
                tfsUpdateDocument.AddFields(action, workItemType, author, changeDate, false);

                tfsUpdateDocument.InsertConversionHistoryCommentToHistory(
                    workItemType,
                    GenerateMigrationHistoryComment(action));

                // insert source item Id to field TfsMigrationTool.ReflectedWorkItemId if it is in WITD
                tfsUpdateDocument.InsertConversionHistoryField(
                    workItemType,
                    GetSourceWorkItemId(action));

                return tfsUpdateDocument;
            }
            catch (MigrationUnresolvedConflictException)
            {
                return null;
            }
        }

        protected virtual TfsUpdateDocument SubmitAttachmentChanges(
            IMigrationAction action,
            ConflictManager conflictMgrService)
        {
            /*
             * retrieve change details
             */
            XmlDocument desc = action.MigrationActionDescription;
            XmlElement rootNode = desc.DocumentElement;
            Debug.Assert(null != rootNode);
            XmlNode attachmentNode = rootNode.FirstChild;
            string originalName = attachmentNode.Attributes["Name"].Value;
            string utcCreationDate = attachmentNode.Attributes["UtcCreationDate"].Value;
            string utcLastWriteDate = attachmentNode.Attributes["UtcLastWriteDate"].Value;
            string length = attachmentNode.Attributes["Length"].Value;
            string comment = attachmentNode.FirstChild.InnerText;

            int targetWorkItemId = FindTargetWorkItemId(action, conflictMgrService);
            string targetRevision = rootNode.Attributes["TargetRevision"].Value;

            /*
             * create operation document
             */
            TfsUpdateDocument tfsUpdateDocument = InitializeUpdateDocument();
            tfsUpdateDocument.CreateWorkItemUpdateDoc(targetWorkItemId.ToString(), targetRevision);

            /*
             * insert Connector specific comment
             */
            WorkItem item = WorkItemStore.GetWorkItem(targetWorkItemId);
            Debug.Assert(null != item, "target work item does not exist");
            tfsUpdateDocument.InsertConversionHistoryCommentToHistory(item.Type.Name, GenerateMigrationHistoryComment(action));

            int[] fileId = FindAttachmentFileId(targetWorkItemId, originalName,
                                                   utcCreationDate, utcLastWriteDate, length, comment);
            /*
             * delete attachment
             */
            if (action.Action == WellKnownChangeActionId.DelAttachment)
            {
                if (fileId.Length == 0)
                {
                    action.State = ActionState.Skipped;
                    return null;
                }
                else
                {
                    tfsUpdateDocument.RemoveAttachment(fileId[0]);
                    return tfsUpdateDocument;
                }
            }

            /*
             * add attachment
             */
            try
            {
                string sourceStoreCountString = attachmentNode.Attributes["CountInSourceSideStore"].Value;
                int sourceStoreCount;
                if (int.TryParse(sourceStoreCountString, out sourceStoreCount))
                {
                    if (sourceStoreCount <= fileId.Length)
                    {
                        action.State = ActionState.Skipped;
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                TraceManager.TraceVerbose(e.ToString());
                // for backward compatibility, just proceed
            }

            if (AttachmentIsOversized(length))
            {
                MigrationConflict conflict = new FileAttachmentOversizedConflictType().CreateConflict(
                    originalName, length, MaxAttachmentSize, targetWorkItemId.ToString(), Core.ServerName, Core.Config.Project, action);

                List<MigrationAction> actions;
                ConflictResolutionResult resolveRslt = conflictMgrService.TryResolveNewConflict(conflictMgrService.SourceId, conflict, out actions);

                if (!resolveRslt.Resolved)
                {
                    return null;
                }

                if (resolveRslt.ResolutionType == ConflictResolutionType.SuppressedConflictedChangeAction)
                {
                    action.State = ActionState.Skipped;
                    return null;
                }

                if (resolveRslt.ResolutionType == ConflictResolutionType.Other)
                {
                    // conflict resolved, just proceed
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            Guid fileGuid = Guid.NewGuid();
            tfsUpdateDocument.AddAttachment(originalName, XmlConvert.ToString(fileGuid),
                                            utcCreationDate, utcLastWriteDate, length, comment);

            //Now upload the file since that has to be done before the Xml batch is executed.
            Debug.Assert(!string.IsNullOrEmpty(LocalWorkDir));
            string filePath = Path.Combine(LocalWorkDir, fileGuid.ToString());
            action.SourceItem.Download(filePath);
            using (var strm = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var f = new FileAttachment();
                f.AreaNodeUri = Core.AreaNodeUri;
                f.ProjectUri = Core.ProjectUri;
                f.FileNameGUID = fileGuid;
                f.LocalFile = strm; // attachment.GetFileContents();

                WorkItemServer.UploadFile(f);
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return tfsUpdateDocument;
        }

        protected int[] FindAttachmentFileId(
            int workItemId,
            string originalName,
            string utcCreationDate,
            string utcLastWriteDate,
            string length,
            string comment)
        {
            long fileSize;
            if (!long.TryParse(length, out fileSize))
            {
                return new int[0];
            }

            WorkItem item = WorkItemStore.GetWorkItem(workItemId);
            if (null == item)
            {
                return new int[0];
            }

            List<int> ids = new List<int>(item.Attachments.Count);
            foreach (Attachment a in item.Attachments)
            {
                if (fileSize == a.Length
                    && string.Equals(originalName, a.Name, StringComparison.InvariantCulture))
                {
                    bool commentIsEqual = false;
                    // compare "comment"
                    if (string.IsNullOrEmpty(comment))
                    {
                        if (string.IsNullOrEmpty(a.Comment))
                        {
                            commentIsEqual = true;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(a.Comment))
                        {
                            commentIsEqual = string.Equals(comment, a.Comment, StringComparison.InvariantCulture);
                        }
                    }

                    var fileAttachment = new TfsMigrationFileAttachment(a);
                    if (!ids.Contains(fileAttachment.FileID))
                    {
                        ids.Add(fileAttachment.FileID);

                    }
                }
            }

            return ids.ToArray();
        }

        /// <summary>
        /// Compares two times with 1 second precision.
        /// </summary>
        /// <param name="t1">Time 1</param>
        /// <param name="t2">Time 2</param>
        /// <returns></returns>
        protected static bool CompareTimes(
            DateTime t1,
            DateTime t2)
        {
            TimeSpan ts = t1 - t2;
            return Math.Abs(ts.TotalSeconds) < 1;
        }

        protected void FindTargetWorkItemLatestRevision(
            IMigrationAction action)
        {
            WorkItem workItem = GetTargetTfsWorkItem(action);

            XmlDocument desc = action.MigrationActionDescription;
            XmlElement rootNode = desc.DocumentElement;
            Debug.Assert(null != rootNode);
            rootNode.SetAttribute("TargetRevision", XmlConvert.ToString(workItem.Rev));
        }

        internal bool IsLastRevisionOfThisSyncCycle(
            IMigrationAction action)
        {
            Debug.Assert(null != action.MigrationActionDescription.DocumentElement, "MigrationAction description is null");
            XmlAttribute workItemIdAttr = action.MigrationActionDescription.DocumentElement.Attributes[Constants.WitLastRevOfThisSyncCycleAttributeName];

            if (workItemIdAttr == null)
            {
                return false;
            }
            else
            {
                bool retVal;
                if (bool.TryParse(workItemIdAttr.Value, out retVal))
                {
                    return retVal;
                }
                else
                {
                    return false;
                }
            }
        }

        internal static string GetSourceWorkItemId(
            IMigrationAction action)
        {
            Debug.Assert(null != action.MigrationActionDescription.DocumentElement, "MigrationAction description is null");
            XmlAttribute workItemIdAttr = action.MigrationActionDescription.DocumentElement.Attributes["WorkItemID"];
            return workItemIdAttr == null ? string.Empty : workItemIdAttr.Value;
        }

        internal static string GetSourceWorkItemRevision(
            IMigrationAction action)
        {
            Debug.Assert(null != action.MigrationActionDescription.DocumentElement, "MigrationAction description is null");
            XmlAttribute workItemRevAttr = action.MigrationActionDescription.DocumentElement.Attributes["Revision"];
            return workItemRevAttr == null ? string.Empty : workItemRevAttr.Value;
        }

        protected int FindTargetWorkItemId(
            IMigrationAction action,
            ConflictManager conflictMgrService)
        {
            int targetWorkItemId = int.MinValue;
            try
            {
                targetWorkItemId = FindTargetWorkItemId(action, true);
            }
            catch (HistoryNotFoundException ex)
            {
                ConfigurationService configService = ServiceContainer.GetService(typeof(ConfigurationService)) as ConfigurationService;
                string srcItemRev = GetSourceWorkItemRevision(action);
                MigrationConflict conflict = WorkItemHistoryNotFoundConflictType.CreateConflict(
                    ex.SourceItemId, srcItemRev, configService.MigrationPeer, configService.SourceId, action);

                List<MigrationAction> actions;
                var rslt = conflictMgrService.TryResolveNewConflict(conflictMgrService.SourceId, conflict, out actions);

                if (rslt.Resolved)
                {
                    targetWorkItemId = FindTargetWorkItemId(action, conflictMgrService);
                }
                else
                {
                    throw new MigrationUnresolvedConflictException();
                }
            }

            return targetWorkItemId;
        }

        protected int FindTargetWorkItemId(
            IMigrationAction action,
            bool inMigrationPhase)
        {
            Debug.Assert(null != action.MigrationActionDescription.DocumentElement, "MigrationAction description is null");

            int targetWorkItemId = int.MinValue;
            string sourceWorkItemId = GetSourceWorkItemId(action);

            if (string.IsNullOrEmpty(sourceWorkItemId))
            {
                throw new MigrationException(TfsWITAdapterResources.ErrorMissingInformationInActionDescription,
                                             "WorkItemID", action.ActionId);
            }

            if (m_mappedWorkItem.ContainsKey(sourceWorkItemId))
            {
                targetWorkItemId = m_mappedWorkItem[sourceWorkItemId];
            }
            else if (null != action.MigrationActionDescription.DocumentElement.Attributes["TargetWorkItemID"])
            {
                string targetWorkItemIdStr =
                    action.MigrationActionDescription.DocumentElement.Attributes["TargetWorkItemID"].Value;
                bool parseSucceeded = int.TryParse(targetWorkItemIdStr, out targetWorkItemId);
                if (!parseSucceeded)
                {
                    targetWorkItemId = int.MinValue;
                }
            }

            if (targetWorkItemId == int.MinValue)
            {
                ITranslationService transService = ServiceContainer.GetService(typeof(ITranslationService)) as ITranslationService;

                if (null != transService && !SourceSideSourceId.Equals(Guid.Empty))
                {
                    string targetWorkItemIdStr = transService.TryGetTargetItemId(sourceWorkItemId, SourceSideSourceId);

                    if (string.IsNullOrEmpty(targetWorkItemIdStr))
                    {
                        throw new HistoryNotFoundException(sourceWorkItemId);
                    }

                    bool parseSucceeded = int.TryParse(targetWorkItemIdStr, out targetWorkItemId);
                    if (!parseSucceeded)
                    {
                        throw new MigrationException(TfsWITAdapterResources.InvalidTFSWorkItemId, targetWorkItemIdStr);
                    }
                }
            }

            return targetWorkItemId;
        }

        internal void SyncIterationPaths(XmlDocument sourceIterationPathDoc, bool otherSideIsMaster)
        {
            SyncCSSNodes(sourceIterationPathDoc, otherSideIsMaster, Node.TreeType.Iteration);
        }

        internal void SyncAreaPaths(XmlDocument sourceAreaPathDoc, bool otherSideIsMaster)
        {
            SyncCSSNodes(sourceAreaPathDoc, otherSideIsMaster, Node.TreeType.Area);
        }

        protected void SyncCSSNodes(
            XmlDocument sourcePathDoc,
            bool otherSideIsMaster,
            Node.TreeType nodeType)
        {
            Project p = WorkItemStore.Projects[TeamProject];
            XmlDocument targetPathDoc = (nodeType == Node.TreeType.Iteration) ? GetIterationPaths(p) : GetAreaPaths(p);

            var targetPaths = ExtractPathFromTfsPathDocument(targetPathDoc);
            var sourcePaths = ExtractPathFromTfsPathDocument(sourcePathDoc, p.Name);

            SymDiff<string> pathsDiff = new SymDiff<string>(sourcePaths, targetPaths, TFStringComparer.CssTreePathName);
            string lastCreatedNode = string.Empty;
            foreach (string missingNodeOnTarget in pathsDiff.LeftOnly)
            {
                try
                {
                    NodeInfo newNode = Core.Css.GetNodeFromPath(missingNodeOnTarget);
                }
                catch (Exception e)
                {
                    // Node does not exist
                    if (e.Message.StartsWith("TF200014:", StringComparison.InvariantCultureIgnoreCase))
                    {
                        TraceManager.TraceInformation("Creating node: {0}", missingNodeOnTarget);

                        // Strip the last \Name off the path to get the parent path
                        string newPathParent = missingNodeOnTarget.Substring(0, missingNodeOnTarget.LastIndexOf('\\'));

                        // Grab the last \Name off the path to get the node name
                        string newPathName = missingNodeOnTarget.Substring(missingNodeOnTarget.LastIndexOf('\\') + 1);

                        // Lookup the parent node on the destination server so that we can get the parentUri
                        NodeInfo parentNode = Core.Css.GetNodeFromPath(newPathParent);

                        // Create the node
                        Core.Css.CreateNode(newPathName, parentNode.Uri);
                        lastCreatedNode = missingNodeOnTarget;
                    }
                }
            }

            if (!string.IsNullOrEmpty(lastCreatedNode))
            {
                // wait for CSS changes to be propagated to WIT
                switch (nodeType)
                {
                    case Node.TreeType.Area:
                        lastCreatedNode = GetAreaPathFromNodeInfoPath(lastCreatedNode);
                        break;
                    case Node.TreeType.Iteration:
                        lastCreatedNode = GetIterationPathFromNodeInfoPath(lastCreatedNode);
                        break;
                    default:
                        throw new InvalidOperationException(nodeType.ToString() + "is unknown");
                }

                string[] names = lastCreatedNode.Split('\\');
                Debug.Assert(names.Length > 1, "invalid length of the created path");
                string[] namesWithoutTPName = new string[names.Length - 1];
                for (int i = 0; i < namesWithoutTPName.Length; ++i)
                {
                    namesWithoutTPName[i] = names[i + 1];
                }
                Node node = Core.WaitForTreeNodeId(nodeType, namesWithoutTPName);
                int maxProbeRetries = 3;
                while (null == node && maxProbeRetries > 0)
                {
                    node = Core.WaitForTreeNodeId(nodeType, names);
                    --maxProbeRetries;
                }
            }

            if (otherSideIsMaster)
            {
                if (m_excessivePathReevaluationCount.ContainsKey(nodeType)
                    && null != m_excessivePathReevaluationCount[nodeType])
                {
                    foreach (string nodePath in m_excessivePathReevaluationCount[nodeType].Keys)
                    {
                        if (!pathsDiff.RightOnly.Contains(nodePath))
                        {
                            EvaluateSingleExcessiveCSSNode(nodeType, nodePath);
                        }
                    }
                }

                foreach (string extraNodePathOnTarget in pathsDiff.RightOnly)
                {
                    EvaluateSingleExcessiveCSSNode(nodeType, extraNodePathOnTarget);
                }
            }
        }

        // todo: move this to TfsCSS utility class
        Dictionary<Node.TreeType, Dictionary<string, int>> m_excessivePathReevaluationCount = new Dictionary<Node.TreeType, Dictionary<string, int>>();
        int MaxExcessivePathReevaluationAttempts = 5;
        private void EvaluateSingleExcessiveCSSNode(Node.TreeType nodeType, string extraNodePathOnTarget)
        {
            try
            {
                NodeInfo nodeToDelete = Core.Css.GetNodeFromPath(extraNodePathOnTarget);
                var c = new StringBuilder("[System.TeamProject]=@project");

                string displayableNodePath;
                switch (nodeType)
                {
                    case Node.TreeType.Area:
                        displayableNodePath = GetAreaPathFromNodeInfoPath(nodeToDelete.Path);
                        c.AppendFormat(CultureInfo.InvariantCulture, " AND [System.AreaPath] UNDER '{0}'", displayableNodePath);
                        break;
                    case Node.TreeType.Iteration:
                        displayableNodePath = GetIterationPathFromNodeInfoPath(nodeToDelete.Path);
                        c.AppendFormat(CultureInfo.InvariantCulture, " AND [System.IterationPath] UNDER '{0}'", displayableNodePath);
                        break;
                    default:
                        throw new InvalidOperationException(nodeType.ToString() + "is unknown");
                }

                // check if there is any work item under the path
                var items = new TfsMigrationWorkItems(m_core, WorkItemStore, c.ToString());
                if (items.Count == 0)
                {
                    TraceManager.TraceInformation("Deleting node: {0}", extraNodePathOnTarget);

                    // Strip the last \Name off the path to get the parent path
                    string nodeToDeleteParent = extraNodePathOnTarget.Substring(0, extraNodePathOnTarget.LastIndexOf('\\'));

                    // Lookup the parent node on the destination server so that we can get the parentUri
                    NodeInfo parentNode = Core.Css.GetNodeFromPath(nodeToDeleteParent);
                    Core.Css.DeleteBranches(new string[] { nodeToDelete.Uri }, parentNode.Uri);
                }
                else
                {
                    if (!m_excessivePathReevaluationCount.ContainsKey(nodeType)
                        || !m_excessivePathReevaluationCount[nodeType].ContainsKey(extraNodePathOnTarget)
                        || m_excessivePathReevaluationCount[nodeType][displayableNodePath] <= MaxExcessivePathReevaluationAttempts)
                    {
                        if (!m_excessivePathReevaluationCount.ContainsKey(nodeType))
                        {
                            m_excessivePathReevaluationCount.Add(nodeType, new Dictionary<string, int>());
                        }
                        if (!m_excessivePathReevaluationCount[nodeType].ContainsKey(extraNodePathOnTarget))
                        {
                            m_excessivePathReevaluationCount[nodeType].Add(extraNodePathOnTarget, 0);
                        }
                        m_excessivePathReevaluationCount[nodeType][extraNodePathOnTarget]
                            = m_excessivePathReevaluationCount[nodeType][extraNodePathOnTarget] + 1;
                    }
                    else
                    {
                        m_excessivePathReevaluationCount[nodeType].Remove(extraNodePathOnTarget);

                        // there are still work items under the path
                        // we raise a conflict and let admin to decide to which new path they will be moved
                        MigrationConflict excessivePathConflict = ExcessivePathConflictType.CreateConflict(displayableNodePath, nodeType);
                        var conflictMgrService = ServiceContainer.GetService(typeof(ConflictManager)) as ConflictManager;
                        conflictMgrService.BacklogUnresolvedConflict(conflictMgrService.SourceId, excessivePathConflict, false);
                    }
                }
            }
            catch (Exception e)
            {
                // Node does not exist any more
                if (!e.Message.StartsWith("TF200014:", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw;
                }
            }
        }

        internal string NormalizeAuthorName(string origAuthor)
        {
            if (string.IsNullOrEmpty(origAuthor))
            {
                // todo: instead of using current user, use the credential for migration
                origAuthor = Core.UserName;
            }

            return origAuthor;
        }

        protected string[] ExtractPathFromTfsPathDocument(XmlDocument tfsPathDoc, string newTeamProjectName)
        {
            bool replaceTPName = !string.IsNullOrEmpty(newTeamProjectName);
            string path = string.Empty;

            List<string> paths = new List<string>();
            foreach (XmlNode node in tfsPathDoc.SelectNodes("//@Path"))
            {
                path = node.InnerXml;
                if (replaceTPName)
                {
                    path = "\\" + newTeamProjectName + path.Substring(path.IndexOf('\\', 1));
                }
                paths.Add(path);
            }

            paths.Sort(TFStringComparer.CssTreePathName);
            return paths.ToArray();
        }

        protected string[] ExtractPathFromTfsPathDocument(XmlDocument tfsPathDoc)
        {
            return ExtractPathFromTfsPathDocument(tfsPathDoc, string.Empty);
        }

        protected string GetAreaPathFromNodeInfoPath(string nodeInfoPath)
        {
            return GetDisplayPathFromNodeInfoPath(nodeInfoPath, "\\Area");
        }

        protected string GetIterationPathFromNodeInfoPath(string nodeInfoPath)
        {
            return GetDisplayPathFromNodeInfoPath(nodeInfoPath, "\\Iteration");
        }

        protected string GetDisplayPathFromNodeInfoPath(string nodeInfoPath, string filter)
        {
            // NOTE
            // NodeInfoPath is in the form of: 
            // \<Team Project Name>\Area|Iteration\<Displayable Node Names under Team Project Name>
            // \<Team Project Name>\Area|Iteration (if no displayable node is present)
            // DisplayPath is in the form of:
            // \<Team Project Name>\<Displayable Node Names under Team Project Name>
            int index = nodeInfoPath.IndexOf(filter);

            string areaPath = nodeInfoPath.Substring(0, index);
            if (nodeInfoPath.Length > index + filter.Length)
            {
                areaPath += nodeInfoPath.Substring(index + filter.Length);
            }
            areaPath = areaPath.Substring(1);

            return areaPath;
        }
    }
}
