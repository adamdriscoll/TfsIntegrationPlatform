// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Shell.ViewModel
{
    public class SessionViewModel : ModelObject
    {
        public SessionViewModel(Guid sessionUniqueId, RTSessionConfig recentSessionConfig)
        {
            OneWaySessions = new Dictionary<Guid, OneWaySessionViewModel>();

            SessionUniqueId = sessionUniqueId;
            m_recentSessionConfig = recentSessionConfig;

            recentSessionConfig.SessionGroupConfigReference.Load();
            recentSessionConfig.LeftSourceConfigReference.Load();
            recentSessionConfig.LeftSourceConfig.MigrationSourceReference.Load();
            recentSessionConfig.RightSourceConfigReference.Load();
            recentSessionConfig.RightSourceConfig.MigrationSourceReference.Load();

            OneWaySessions[recentSessionConfig.LeftSourceConfig.MigrationSource.UniqueId] = new OneWaySessionViewModel(recentSessionConfig, recentSessionConfig.LeftSourceConfig.MigrationSource, recentSessionConfig.RightSourceConfig.MigrationSource);
            WorkFlowType workFlowType = new WorkFlowType(recentSessionConfig.SessionGroupConfig.WorkFlowType);
            if (workFlowType.DirectionOfFlow == DirectionOfFlow.Bidirectional)
            {
                OneWaySessions[recentSessionConfig.RightSourceConfig.MigrationSource.UniqueId] = new OneWaySessionViewModel(recentSessionConfig, recentSessionConfig.RightSourceConfig.MigrationSource, recentSessionConfig.LeftSourceConfig.MigrationSource);
            }
        }

        public string FriendlyName
        {
            get
            {
                return m_recentSessionConfig.FriendlyName;
            }
        }

        public Guid SessionUniqueId { get; private set; }

        public Dictionary<Guid, OneWaySessionViewModel> OneWaySessions { get; private set; }

        private RTSessionConfig m_recentSessionConfig;
    }
}
