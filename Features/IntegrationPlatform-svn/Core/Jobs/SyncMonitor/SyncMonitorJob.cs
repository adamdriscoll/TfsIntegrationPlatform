// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.SyncMonitor;

namespace Microsoft.TeamFoundation.Migration.TfsIntegrationJobService
{
    public class SyncMonitorJob : TfsIntegrationJobBase
    {
        EventLogSource m_eventLog = new EventLogSource(
            Toolkit.Constants.TfsIntegrationJobServiceName,
            Toolkit.Constants.TfsServiceEventLogName);

        private MonitorWatcher m_monitorWatcher;

        public override Guid ReferenceName
        {
            get { return new Guid("{776F372A-8B86-4516-A2CA-A7BE0ABC1AB6}"); }
        }

        public override string FriendlyName
        {
            get { return "Sync Monitor Job"; }
        }

        public override void Initialize(Job jobConfiguration)
        {
            bool verbose = false;
            foreach (Setting setting in jobConfiguration.Settings.NamedSettings.Setting)
            {
                if (string.Equals(setting.SettingKey, "verbose", StringComparison.OrdinalIgnoreCase))
                {
                    verbose = string.Equals(setting.SettingValue, "true", StringComparison.OrdinalIgnoreCase);
                    break;
                }
            }

            m_monitorWatcher = new MonitorWatcher(true, verbose);
        }

        protected override void DoJob()
        {
            m_monitorWatcher.Start();            
        }

        public override void Stop()
        {
            m_monitorWatcher.Stop();
        }
    }
}
