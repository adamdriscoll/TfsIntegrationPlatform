// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.TeamFoundation.Migration.Toolkit.ServerDiff;
using MigrationTestLibrary;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace MigrationTestLibrary
{
    public abstract class WITMigrationTestCaseBase : MigrationTestBase
    {
        private IWITTestCaseAdapter m_sourceAdapter;
        private IWITTestCaseAdapter m_targetAdapter;
        private List<int> m_sourceWorkItemIds;
        private List<int> m_targetWorkItemIds;

        protected IWITTestCaseAdapter SourceAdapter
        {
            get
            {
                return m_sourceAdapter;
            }
        }

        protected IWITTestCaseAdapter TargetAdapter
        {
            get
            {
                return m_targetAdapter;
            }
        }

        protected List<int> SourceWorkItemIdList
        {
            get
            {
                return m_sourceWorkItemIds;
            }
        }

        internal abstract WitDiffResult VerifySyncResult();

        [TestInitialize]
        public override void Initialize()
        {
 	        base.Initialize();

            m_sourceWorkItemIds = new List<int>();
            m_targetWorkItemIds = new List<int>();
            
            TestEnvironment.Mappings.Clear();

            TCAdapterEnvironment sourceTCAdapterEnv = TestEnvironment.SourceTCAdapterEnvironment;
            TCAdapterEnvironment targetTCAdapterEnv = TestEnvironment.TargetTCAdapterEnvironment;

            m_sourceAdapter = (IWITTestCaseAdapter)TestAdapterManager.LoadAdapter(new Guid(TestEnvironment.SourceTCAdapterEnvironment.AdapterRefName));
            m_targetAdapter = (IWITTestCaseAdapter)TestAdapterManager.LoadAdapter(new Guid(TestEnvironment.TargetTCAdapterEnvironment.AdapterRefName));

            m_sourceAdapter.Initialize(sourceTCAdapterEnv);
            m_targetAdapter.Initialize(targetTCAdapterEnv);

            m_sourceAdapter.WorkItemAdded += OnSourceWorkItemAdded;
            m_targetAdapter.WorkItemAdded += OnTargetWorkItemAdded;

            Trace.TraceInformation("Loaded WIT test environment successfully");
        }

        protected void RunAndNoValidate()
        {
            RunAndNoValidate(false);
        }

        protected virtual void RunAndNoValidate(bool useExistingConfiguration)
        {
            if (!useExistingConfiguration || Configuration == null)
            {
                BuildFilterStringPair();

                // Generate a new configuration file
                Configuration = ConfigurationCreator.CreateConfiguration(TestConstants.ConfigurationTemplate.SingleWITSession, TestEnvironment);
                ConfigurationCreator.CreateConfigurationFile(Configuration, ConfigurationFileName);
            }

            MigrationApp.Start(ConfigurationFileName);
        }

        protected void RunAndValidate()
        {
            RunAndNoValidate(false);

            VerifySyncResult();
        }

        private void OnSourceWorkItemAdded(object sender, WorkItemAddedEventArgs e)
        {
            m_sourceWorkItemIds.Add(e.WorkItemId);
        }

        private void OnTargetWorkItemAdded(object sender, WorkItemAddedEventArgs e)
        {
            m_targetWorkItemIds.Add(e.WorkItemId);
        }

        // Build filter string based on work item ids
        protected void BuildFilterStringPair()
        {
            StringBuilder filter1 = new StringBuilder();

            if (m_sourceWorkItemIds.Count > 5)
            {
                int firstId = m_sourceWorkItemIds[0];
                int lastId = m_sourceWorkItemIds[m_sourceWorkItemIds.Count - 1];
                filter1.AppendFormat("{0} >= {1} AND {2} <= {3}",
                    SourceAdapter.WorkItemIDColumnName, firstId, SourceAdapter.WorkItemIDColumnName, lastId);
            }
            else
            {
                for (int i = 0; i < m_sourceWorkItemIds.Count; i++)
                {
                    filter1.AppendFormat("{0} = {1}", SourceAdapter.WorkItemIDColumnName, m_sourceWorkItemIds[i]);

                    if (i < m_sourceWorkItemIds.Count - 1)
                    {
                        filter1.Append(" OR ");
                    }
                }
            }

            StringBuilder filter2 = new StringBuilder();
            if (m_targetWorkItemIds.Count > 5)
            {
                int firstId = m_targetWorkItemIds[0];
                int lastId = m_targetWorkItemIds[m_targetWorkItemIds.Count - 1];
                filter2.AppendFormat("{0} >= {1} AND {2} <= {3}",
                  TargetAdapter.WorkItemIDColumnName, firstId, TargetAdapter.WorkItemIDColumnName, lastId);
            }
            else
            {
                for (int i = 0; i < m_targetWorkItemIds.Count; i++)
                {
                    filter2.AppendFormat("{0} = {1}", TargetAdapter.WorkItemIDColumnName, m_targetWorkItemIds[i]);
                    
                    if (i < m_targetWorkItemIds.Count - 1)
                    {
                        filter2.Append(" OR ");
                    }
                }
            }

            string filterString1 = filter1.ToString();
            string filterString2 = filter2.ToString();

            if (string.IsNullOrEmpty(filterString1))
            {
                filterString1 = string.Format("{0} = 0", SourceAdapter.WorkItemIDColumnName);
            }

            if (string.IsNullOrEmpty(filterString2))
            {
                filterString2 = string.Format("{0} = 0", TargetAdapter.WorkItemIDColumnName);
            }

            if (!string.IsNullOrEmpty(SourceAdapter.FilterString))
            {
                filterString1 = SourceAdapter.FilterString + filterString1;
            }

            if (!string.IsNullOrEmpty(TargetAdapter.FilterString))
            {
                filterString1 = TargetAdapter.FilterString + filterString1;
            }

            MappingPair mapping = new MappingPair(filterString1, filterString2);
            TestEnvironment.AddMapping(mapping);
        }

        // from toolkit/WITTranslationService
        public string TryGetTargetItemId(string sourceWorkItemId, Guid sourceId)
        {
            Session session = Configuration.SessionGroup.Sessions.Session[0];
            Debug.Assert(session != null);

            string targetWorkItemId = string.Empty;
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var migrationItemResult =
                    from mi in context.RTMigrationItemSet
                    where mi.ItemId.Equals(sourceWorkItemId)
                        && !mi.ItemVersion.Equals(Constants.ChangeGroupGenericVersionNumber) // exclude non-versioned migration items (e.g. VC change group)
                    select mi;
                if (migrationItemResult.Count() == 0)
                {
                    return targetWorkItemId;
                }

                RTMigrationItem sourceItem = null;
                foreach (RTMigrationItem rtMigrationItem in migrationItemResult)
                {
                    rtMigrationItem.MigrationSourceReference.Load();
                    if (rtMigrationItem.MigrationSource.UniqueId.Equals(sourceId))
                    {
                        sourceItem = rtMigrationItem;
                    }
                }
                if (null == sourceItem)
                {
                    return targetWorkItemId;
                }

                var sessionUniqueId = new Guid(session.SessionUniqueId);
                var itemConvPairResult =
                    from p in context.RTItemRevisionPairSet
                    where (p.LeftMigrationItem.Id == sourceItem.Id || p.RightMigrationItem.Id == sourceItem.Id)
                        && (p.ConversionHistory.SessionRun.Config.SessionUniqueId.Equals(sessionUniqueId))
                    select p;

                if (itemConvPairResult.Count() == 0)
                {
                    return targetWorkItemId;
                }

                RTItemRevisionPair itemRevisionPair = itemConvPairResult.First();
                if (itemRevisionPair.LeftMigrationItem == sourceItem)
                {
                    itemRevisionPair.RightMigrationItemReference.Load();
                    targetWorkItemId = itemRevisionPair.RightMigrationItem.ItemId;
                }
                else
                {
                    itemRevisionPair.LeftMigrationItemReference.Load();
                    targetWorkItemId = itemRevisionPair.LeftMigrationItem.ItemId;
                }
            }

            return targetWorkItemId;
        }
    }
}
