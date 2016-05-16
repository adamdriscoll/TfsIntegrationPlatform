// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//
// 20091101 TFS Integration Platform Custom Adapter Proof-of-Concept
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using System.ComponentModel.Design;
using System.Collections.ObjectModel;
using System.IO;

namespace Rangers.TFS.Migration.PocAdapter.VC
{
    public class PocVCAnalysisProvider : AnalysisProviderBase
    {
        Dictionary<Guid, ChangeActionHandler> m_supportedChangeActions;
        Collection<ContentType> m_supportedContentTypes;
        ChangeGroupService m_changeGroupService;
        ConflictManager m_conflictManagementService;
        IServiceContainer m_analysisServiceContainer;
        ChangeActionRegistrationService m_changeActionRegistrationService;
        HighWaterMark<DateTime> m_hwmDelta;
        HighWaterMark<int> m_hwmChangeset;
        DateTime deltaTableStartTime;
        ConfigurationService ConfigurationService;

        #region IAnalysisProvider Members

        public override void GenerateDeltaTable()
        {
            TraceManager.TraceInformation("POC:AP:GenerateDeltaTable");
            m_hwmDelta.Reload();
            TraceManager.TraceInformation("     POC:HighWaterMark {0} ", m_hwmDelta.Value);
            deltaTableStartTime = DateTime.Now;
            TraceManager.TraceInformation("     POC:CutOff {0} ", deltaTableStartTime);

            ReadOnlyCollection<MappingEntry> filters = ConfigurationService.Filters;

            GetPocUpdates(filters[0].Path);
            m_hwmDelta.Update(deltaTableStartTime);
            m_changeGroupService.PromoteDeltaToPending();
        }

        public override void InitializeClient()
        {
            TraceManager.TraceInformation("POC:AP:InitializeClient");
        }

        public override void InitializeServices(IServiceContainer analysisServiceContainer)
        {
            TraceManager.TraceInformation("POC:AP:Initialize");
            TraceManager.TraceInformation("Press enter..");
            Console.ReadLine();
            m_analysisServiceContainer = analysisServiceContainer;
            
            m_supportedContentTypes = new Collection<ContentType>();
            m_supportedContentTypes.Add(WellKnownContentType.VersionControlledFile);
            m_supportedContentTypes.Add(WellKnownContentType.VersionControlledFolder);

            var handler = new BasicChangeActionHandlers(this);
            m_supportedChangeActions = new Dictionary<Guid, ChangeActionHandler>();
            m_supportedChangeActions.Add(WellKnownChangeActionId.Add, handler.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Delete, handler.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Edit, handler.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Rename, handler.BasicActionHandler);

            ConfigurationService = (ConfigurationService)m_analysisServiceContainer.GetService(typeof(ConfigurationService));

            m_hwmDelta = new HighWaterMark<DateTime>(Constants.HwmDelta);
            m_hwmChangeset = new HighWaterMark<int>("LastChangeSet");
            ConfigurationService.RegisterHighWaterMarkWithSession(m_hwmDelta);
            ConfigurationService.RegisterHighWaterMarkWithSession(m_hwmChangeset);
            m_changeGroupService = (ChangeGroupService)m_analysisServiceContainer.GetService(typeof(ChangeGroupService));
            m_changeGroupService.RegisterDefaultSourceSerializer(new PocVCMigrationItemSerializer());
        }

        public override void RegisterConflictTypes(ConflictManager conflictManager)
        {
            TraceManager.TraceInformation("POC:AP:RegisterConflictTypes");
            m_conflictManagementService = conflictManager;
            //m_conflictManagementService = (ConflictManager)
            //    m_analysisServiceContainer.GetService(typeof(ConflictManager));
            m_conflictManagementService.RegisterConflictType(new GenericConflictType());
        }

        public override void RegisterSupportedChangeActions(ChangeActionRegistrationService contentActionRegistrationService)
        {
            TraceManager.TraceInformation("POC:AP:RegisterSupportedChangeActions");
            m_changeActionRegistrationService = contentActionRegistrationService;
            //m_changeActionRegistrationService = (ChangeActionRegistrationService)
            //   m_analysisServiceContainer.GetService(typeof(ChangeActionRegistrationService));
            foreach (KeyValuePair<Guid, ChangeActionHandler> supportedChangeAction in m_supportedChangeActions)
            {
                foreach (ContentType contentType in ((IAnalysisProvider)this).SupportedContentTypes)
                {
                    m_changeActionRegistrationService.RegisterChangeAction(
                        supportedChangeAction.Key,
                        contentType.ReferenceName,
                        supportedChangeAction.Value);
                }
            }
        }

        public override void RegisterSupportedContentTypes(ContentTypeRegistrationService contentTypeRegistrationService)
        {
            TraceManager.TraceInformation("POC:AP:RegisterSupportedContentTypes");
        }

        public override Dictionary<Guid, Microsoft.TeamFoundation.Migration.Toolkit.Services.ChangeActionHandler> SupportedChangeActions
        {
            get { return m_supportedChangeActions; }
        }

        public override System.Collections.ObjectModel.Collection<Microsoft.TeamFoundation.Migration.Toolkit.Services.ContentType> SupportedContentTypes
        {
            get { return m_supportedContentTypes; }
        }

        #endregion

        # region Helper Methods

        private void GetPocUpdates(string folderName)
        {
            DirectoryInfo info = new DirectoryInfo(folderName);
            FileInfo[] files = info.GetFiles();
            TraceManager.TraceInformation("     POC:GetPocUpdates - {0}", folderName);
            foreach (FileInfo file in files)
            {
                if (file.LastWriteTime.CompareTo(m_hwmDelta.Value) > 0 && file.LastWriteTime.CompareTo(deltaTableStartTime) < 0)
                {
                    Guid actionGuid;
                    if(file.CreationTime.CompareTo(m_hwmDelta.Value) < 0)
                    {
                        actionGuid = WellKnownChangeActionId.Edit;
                    }
                    else 
                    {
                        actionGuid = WellKnownChangeActionId.Add;
                    }
                    TraceManager.TraceInformation("         POC:ChangeSet:{0} - {1}", m_hwmChangeset.Value, file.FullName);
                    ChangeGroup cg = CreateChangeGroup(m_hwmChangeset.Value, 0);
                    cg.CreateAction(actionGuid, new PocVCMigrationItem(file.FullName),
                        null,
                        Translate(file.FullName),
                        null,
                        null,
                        WellKnownContentType.VersionControlledFile.ReferenceName,
                        null);
                    cg.Save();
                    m_hwmChangeset.Update(m_hwmChangeset.Value + 1);
                }
            }
            DirectoryInfo[] dirs = info.GetDirectories();
            foreach (DirectoryInfo dir in dirs)
            {
                Guid actionGuid;
                if (dir.CreationTime.CompareTo(m_hwmDelta.Value) > 0)
                {
                    actionGuid = WellKnownChangeActionId.Add;
                    ChangeGroup cg = CreateChangeGroup(m_hwmChangeset.Value, 0);
                    TraceManager.TraceInformation("         POC:ChangeSet:{0} - {1}", m_hwmChangeset.Value, dir.FullName);
                    cg.CreateAction(actionGuid, new PocVCMigrationItem(dir.FullName),
                        null,
                        Translate(dir.FullName),
                        null,
                        null,
                        WellKnownContentType.VersionControlledFolder.ReferenceName,
                        null);
                    cg.Save();
                    m_hwmChangeset.Update(m_hwmChangeset.Value + 1);
                }
                GetPocUpdates(dir.FullName);
            }
        }

        private string Translate(string p)
        {
            return p.Replace('\\', '/');
        }

        private ChangeGroup CreateChangeGroup(int changeset, long executionOrder)
        {
            ChangeGroup group = m_changeGroupService.CreateChangeGroupForDeltaTable(changeset.ToString());
            group.Owner = null;
            group.Comment = string.Format("Changeset {0}", changeset);
            group.ChangeTimeUtc = DateTime.UtcNow;
            group.Status = ChangeStatus.Delta;
            group.ExecutionOrder = executionOrder;
            return group;
        }

        # endregion 
    }
}
