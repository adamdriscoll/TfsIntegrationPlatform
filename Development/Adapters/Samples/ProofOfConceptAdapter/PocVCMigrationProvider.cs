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
using System.ComponentModel.Design;
using System.Diagnostics;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Rangers.TFS.Migration.PocAdapter.VC.PocUtilities;

namespace Rangers.TFS.Migration.PocAdapter.VC
{
    public class PocVCMigrationProvider : MigrationProviderBase
    {
        IServiceContainer m_migrationServiceContainer;
        ChangeGroupService m_changeGroupService;
        ConfigurationService m_configurationService;
        ConflictManager m_conflictManagementService;
        PocUtil pocUtil;

        #region IMigrationProvider Members
        
        public override void InitializeClient()
        {
            TraceManager.TraceInformation("POC:MP:InitializeClient");
        }

        public override void InitializeServices(IServiceContainer migrationServiceContainer)
        {
            TraceManager.TraceInformation("POC:MP:InitializevicesSer");
            m_migrationServiceContainer = migrationServiceContainer;
            m_changeGroupService = (ChangeGroupService)m_migrationServiceContainer.GetService(typeof(ChangeGroupService));
            Debug.Assert(m_changeGroupService != null, "Change group service is not initialized");
            m_configurationService = (ConfigurationService)m_migrationServiceContainer.GetService(typeof(ConfigurationService));
            Debug.Assert(m_configurationService != null, "Configuration service is not initialized");
            m_changeGroupService.RegisterDefaultSourceSerializer(new PocVCMigrationItemSerializer());
            pocUtil = new PocUtil();
        }

        public override ConversionResult ProcessChangeGroup(ChangeGroup changeGroup)
        {
            TraceManager.TraceInformation("POC:MP:ProcessChangeGroup - {0}", changeGroup.Name);

            ConversionResult convResult = null;
            Guid targetSideSourceId = m_configurationService.SourceId;
            Guid sourceSideSourceId = m_configurationService.MigrationPeer;
            convResult = new ConversionResult(sourceSideSourceId, targetSideSourceId);
            
            foreach (MigrationAction action in changeGroup.Actions)
            {
                TraceManager.TraceInformation("     > {0} - {1}", action.Path, action.SourceItem.GetType().ToString());
                if (action.Action == WellKnownChangeActionId.Add && action.ItemTypeReferenceName == WellKnownContentType.VersionControlledFile.ReferenceName)
                {
                   // action.SourceItem.Download(action.Path);
                   pocUtil.AddFile(action.Path);
                }
                else if (action.Action == WellKnownChangeActionId.Add && action.ItemTypeReferenceName == WellKnownContentType.VersionControlledFolder.ReferenceName)
                {
                  //  action.SourceItem.Download(action.Path);
                  pocUtil.CreateFolder(action.Path);
                }
            }
            convResult.ChangeId = changeGroup.ReflectedChangeGroupId.ToString(); // we do not have a unique id on this side yet ... hijacking the other side's ID
            convResult.ItemConversionHistory.Add(new ItemConversionHistory(changeGroup.Name, string.Empty, convResult.ChangeId, string.Empty));
            return convResult;
        }

        public override void RegisterConflictTypes(ConflictManager conflictManager)
        {
            TraceManager.TraceInformation("POC:MP:RegisterConflictTypes");
            m_conflictManagementService = conflictManager;
            //m_conflictManagementService = (ConflictManager)
            //   m_migrationServiceContainer.GetService(typeof(ConflictManager));           
            m_conflictManagementService.RegisterConflictType(new GenericConflictType());            
        }

        #endregion

    }
}
