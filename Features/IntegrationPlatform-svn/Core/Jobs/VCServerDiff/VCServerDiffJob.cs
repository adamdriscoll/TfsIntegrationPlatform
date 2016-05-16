// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Globalization;

using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ServerDiff;

namespace Microsoft.TeamFoundation.Migration.TfsIntegrationJobService
{
    public class VCServerDiffJob : TfsIntegrationJobBase
    {
        EventLogSource m_eventLog = new EventLogSource(
            Toolkit.Constants.TfsIntegrationJobServiceName,
            Toolkit.Constants.TfsServiceEventLogName);

        private bool m_verbose;
        private bool m_noContentComparison;

        public override Guid ReferenceName
        {
            get { return new Guid("8CEE17A0-0414-49E6-B86D-F8ECDFA9A2FA"); }
        }

        public override string FriendlyName
        {
            get { return "VC Server Diff Job"; }
        }

        public override void Initialize(Job jobConfiguration)
        {
            foreach (Setting setting in jobConfiguration.Settings.NamedSettings.Setting)
            {
                // Note: The command line options /session, /leftVersion, and /rightVersion are not supported in the job

                if (string.Equals(setting.SettingKey, "noContentComparison", StringComparison.OrdinalIgnoreCase))
                {
                    m_noContentComparison = string.Equals(setting.SettingValue, "true", StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (string.Equals(setting.SettingKey, "verbose", StringComparison.OrdinalIgnoreCase))
                {
                    m_verbose = string.Equals(setting.SettingValue, "true", StringComparison.OrdinalIgnoreCase);
                    continue;
                }
            }
        }

        protected override void DoJob()
        {
            TraceManager.TraceInformation(String.Format(ServerDiffResources.ServerDiffJobRunning, FriendlyName, SessionTypeEnum.VersionControl.ToString()));
            try
            {
                List<Guid> activeVCSessions = ServerDiffEngine.FindAllActiveSessionsOfType(SessionTypeEnum.VersionControl);

                if (activeVCSessions.Count == 0)
                {
                    TraceManager.TraceInformation(String.Format(ServerDiffResources.NoActiveSessionsWithTypeFound, SessionTypeEnum.VersionControl.ToString()));
                }
                else
                {
                    foreach (Guid activeVCSessionId in activeVCSessions)
                    {

                        ServerDiffEngine diffEngine =
                            new ServerDiffEngine(activeVCSessionId, m_noContentComparison, m_verbose, SessionTypeEnum.VersionControl, true, true);
                        try
                        {
                            VCDiffComparer vcDiffComparer = new VCDiffComparer(diffEngine);
                            diffEngine.RegisterDiffComparer(vcDiffComparer);
                            if (diffEngine.VerifyContentsMatch(null, null))
                            {
                                diffEngine.LogResult(ServerDiffResources.AllContentsMatch);
                            }
                            else
                            {
                                diffEngine.LogResult(ServerDiffResources.ContentsDoNotMatch);
                            }
                        }
                        catch (Exception e)
                        {
                            diffEngine.LogError(String.Format(CultureInfo.InvariantCulture, ServerDiffResources.ExceptionRunningServerDiff,
                                m_verbose ? e.ToString() : e.Message));
                        }
                    }
                }
            }
            catch (MigrationSessionNotFoundException sessionNotFoundException)
            {
                TraceManager.TraceInformation(sessionNotFoundException.Message);
            }
            finally
            {
                TraceManager.TraceInformation(String.Format(ServerDiffResources.ServerDiffJobSleeping, FriendlyName));
            }
        }
    }
}
