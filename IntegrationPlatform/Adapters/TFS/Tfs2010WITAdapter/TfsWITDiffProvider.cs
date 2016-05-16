// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.BusinessModel.WIT;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    public class TfsWITDiffProvider : IWITDiffProvider
    {
        private static string[] s_ignoredFieldNames = new string[] 
        { 
            "WorkItemID", 
            "Revision",
            "ChangeDate",
            "Author",
            "System.ChangedBy", 
            "System.ChangedDate", 
            "System.CreatedBy",
            "System.CreatedDate", 
            "System.History", 
            "System.IterationID", 
            "System.AreaID", 
            "Microsoft.VSTS.Common.StateChangeDate", 
            "Microsoft.VSTS.Common.ActivatedDate", 
            "TfsMigrationTool.ReflectedWorkItemId"
        };

        private WorkItemStore m_workItemStore;
        private string m_projectName;
        private string m_filterString;
        private bool m_provideForContentComparison;
        private HashSet<string> m_ignoredFieldNames;
        private ILinkProvider m_linkProvider;
        private ITranslationService m_translationService;
        private Session m_session;
        private Guid m_migrationSourceGuid;
        private SourceSideTypeEnum m_sourceSide;
        private HashSet<string> m_mappedWorkItemTypes;
        private TfsWorkItemPager m_workItemPager;

        /// <summary>
        /// Obtain references to services needed by this class
        /// </summary>
        public void InitializeServices(IServiceContainer witDiffServiceContainer)
        {
            m_linkProvider = witDiffServiceContainer.GetService(typeof(ILinkProvider)) as ILinkProvider;
            m_session = witDiffServiceContainer.GetService(typeof(Session)) as Session;
            m_translationService = witDiffServiceContainer.GetService(typeof(ITranslationService)) as ITranslationService;
        }

        /// <summary>
        /// Perform the adapter-specific initialization
        /// </summary>
        public virtual void InitializeClient(MigrationSource migrationSource)
        {
            TfsTeamProjectCollection tfs = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(migrationSource.ServerUrl));
            tfs.Authenticate();

            m_workItemStore = (WorkItemStore)tfs.GetService(typeof(WorkItemStore));

            m_projectName = migrationSource.SourceIdentifier;

            m_migrationSourceGuid = new Guid(migrationSource.InternalUniqueId);
            if (m_migrationSourceGuid.Equals(m_session.LeftMigrationSourceUniqueId))
            {
                m_sourceSide = SourceSideTypeEnum.Left;
            }
            else
            {
                m_sourceSide = SourceSideTypeEnum.Right;
            }
        }

        #region IWITDiffProvider implementation

        /// <summary>
        /// The implementation can perform any one-time initialization here.
        /// Some adapter implementations may not need to perform any such initialization.
        /// It takes as optional arguments a filterString and a version that would be applied during subsequent queries.
        /// </summary>
        /// <param name="filterString">A string that specifies some filtering condition; if null or empty no additional filtering is applied</param>
        /// <param name="version">The version of the item; if null or empty, the tip version is accessed</param>
        /// <param name="provideForContentComparison">If true, any IDiffItem returned by any method should include the contents of the work item for comparison purposed.
        /// If false, detailed content data can be left out.
        /// </param>
        public void InitializeForDiff(string filterString, bool provideForContentComparison)
        {
            m_filterString = filterString;
            m_provideForContentComparison = provideForContentComparison;
        }

        /// <summary>
        /// Enumerate the diff items found based on the query passed in as well as the filterString and version passed
        /// to InitializeForDiff.  The return type is IEnumerable<> so that adapter implementations do not need download and keep 
        /// all of the IWITDiffItems in memory at once.
        /// </summary>
        /// <param name="queryCondition">A string that specifies a query used to select a subset of the work items defined by 
        /// the set that the filter string identified.</param>
        /// <returns>An enumeration of IWITDiffItems each representing a work item to be compared by the WIT Diff operation</returns>
        public IEnumerable<IWITDiffItem> GetWITDiffItems(string queryCondition)
        {
            if (null == m_workItemPager)
            {
                StringBuilder conditionBuilder = new StringBuilder(m_filterString);

                if (!string.IsNullOrEmpty(queryCondition))
                {
                    if (conditionBuilder.Length > 0)
                    {
                        conditionBuilder.Append(" AND ");
                    }
                    conditionBuilder.Append(queryCondition);
                }

                m_workItemPager = new TfsWorkItemPager(m_projectName, m_workItemStore, conditionBuilder.ToString());
            }

            WorkItemCollection workItems;
            bool workItemFromProjectFound = false;
            do
            {
                workItems = m_workItemPager.GetNextPage();
                if (null == workItems)
                {
                    yield break;
                }
                else
                {
                    foreach (WorkItem workItem in workItems)
                    {
                        IWITDiffItem witDiffItem = null;
                        try
                        {
                            if (string.Equals(workItem.Project.Name, m_projectName, StringComparison.OrdinalIgnoreCase))
                            {
                                workItemFromProjectFound = true;
                                witDiffItem = (IWITDiffItem)new TfsWITDiffItem(this, workItem);
                            }
                        }
                        catch (DeniedOrNotExistException)
                        {
                            // Ignore this work item and continue
                        }
                        if (witDiffItem != null)
                        {
                            yield return witDiffItem;
                        }
                    }
                }
            }
            // Continue with the next page if none of the work item in the current page are in the configured project
            while (workItems != null && !workItemFromProjectFound);
        }

        /// <summary>
        /// Return a IWITDiffItem representing a single work item as identified by the adapter specific workItemId string
        /// </summary>
        /// <param name="workItemId"></param>
        /// <returns></returns>
        public IWITDiffItem GetWITDiffItem(string workItemIdStr)
        {
            IWITDiffItem tfsWitDiffItem = null;
            int workItemId;
            if (int.TryParse(workItemIdStr, out workItemId))
            {
                try
                {
                    WorkItem workItem = m_workItemStore.GetWorkItem(workItemId);
                    tfsWitDiffItem = new TfsWITDiffItem(this, workItem);
                }
                catch (DeniedOrNotExistException)
                {
                    // Treat this as not found rather than an Exception so that processing continues
                    return null;
                }
            }
            return tfsWitDiffItem;
        }

        /// <summary>
        /// IgnoreFieldInComparison is called to allow an adapter to specify that a named field should not be compared
        /// in the diff operation
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public bool IgnoreFieldInComparison(string fieldName)
        {
            return IgnoredFieldNames.Contains(fieldName);
        }

        /// <summary>
        /// Give the IWITDiffProvider a chance to cleanup any reources allocated during InitializeForDiff()
        /// </summary>
        public void Cleanup()
        {
            m_filterString = null;
            m_mappedWorkItemTypes = null;
            m_workItemPager = null;
        }

        #endregion



        #region IServiceProvider implementation
        /// <summary>
        /// Gets the service object of the specified type. 
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public Object GetService(Type serviceType)
        {
            if (serviceType == typeof(ILinkProvider))
            {
                return m_linkProvider;
            }
            return this as IServiceProvider;
        }
        #endregion

        #region IDisposable members
        public void Dispose()
        {
        }
        #endregion

        #region internal properties and methods
        internal Guid MigrationSourceGuid
        {
            get { return m_migrationSourceGuid; }
        }

        internal ILinkProvider LinkProvider
        {
            get { return m_linkProvider; }
        }

        internal ITranslationService TranslationService
        {
            get { return m_translationService; }
        }


        internal bool HasWorkItemEverBeenInScope(int workItemId)
        {
            WorkItem workItem = m_workItemStore.GetWorkItem(workItemId);

            // If the work item is not in the current project, it is not in scope
            if (!string.Equals(workItem.Project.Name, m_projectName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // If the work item type is currently mapped, assume it is in scope
            if (IsWorkItemTypeMapped(workItem.Type.Name))
            {
                return true;
            }

            // The work item may have been in scope in a past configuration in which case the ITranslationService.TryGetTargetItem will find
            // it and it should be counted
            if (!string.IsNullOrEmpty(m_translationService.TryGetTargetItemId(workItem.Id.ToString(), m_migrationSourceGuid)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region private methods
        private bool IsWorkItemTypeMapped(string workItemTypeName)
        {
            if (m_mappedWorkItemTypes == null)
            {
                m_mappedWorkItemTypes = m_session.WITCustomSetting.WorkItemTypes.GetMappedTypeNames(m_sourceSide);
            }

            return m_mappedWorkItemTypes.Contains(workItemTypeName) || m_mappedWorkItemTypes.Contains(WitMappingConfigVocab.Any);
        }
        #endregion

        #region private properties
        private HashSet<string> IgnoredFieldNames
        {
            get
            {
                if (m_ignoredFieldNames == null)
                {
                    m_ignoredFieldNames = new HashSet<string>(TFStringComparer.WorkItemFieldReferenceName);
                    foreach (string fieldName in s_ignoredFieldNames)
                    {
                        m_ignoredFieldNames.Add(fieldName);
                    }
                }
                return m_ignoredFieldNames;
            }
        }
        #endregion
    }

}
