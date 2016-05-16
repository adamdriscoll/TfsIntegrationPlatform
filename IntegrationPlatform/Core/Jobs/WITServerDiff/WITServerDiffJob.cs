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
    public class WITServerDiffJob : TfsIntegrationJobBase
    {
        EventLogSource m_eventLog = new EventLogSource(
            Toolkit.Constants.TfsIntegrationJobServiceName,
            Toolkit.Constants.TfsServiceEventLogName);

        private bool m_verbose;
        private bool m_noContentComparison;
        private string m_leftQueryCondition;
        private string m_rightQueryCondition;
        private HashSet<string> m_leftFieldNamesToIgnore = new HashSet<string>();
        private HashSet<string> m_rightFieldNamesToIgnore = new HashSet<string>();

        public override Guid ReferenceName
        {
            get { return new Guid("2964F398-E06D-470A-8AA4-99FD5D42AD5F"); }
        }

        public override string FriendlyName
        {
            get { return "WIT Server Diff Job"; }
        }

        public override void Initialize(Job jobConfiguration)
        {
            foreach (Setting setting in jobConfiguration.Settings.NamedSettings.Setting)
            {
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

                if (string.Equals(setting.SettingKey, "LeftQueryCondition", StringComparison.OrdinalIgnoreCase))
                {
                    m_leftQueryCondition = setting.SettingValue;
                    continue;
                }

                if (string.Equals(setting.SettingKey, "RightQueryCondition", StringComparison.OrdinalIgnoreCase))
                {
                    m_rightQueryCondition = setting.SettingValue;
                    continue;
                }

                if (string.Equals(setting.SettingKey, "IgnoreLeftFields", StringComparison.OrdinalIgnoreCase))
                {
                    string[] leftFieldNames = setting.SettingValue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string fieldName in leftFieldNames)
                    {
                        m_leftFieldNamesToIgnore.Add(fieldName.Trim());
                    }
                    continue;
                }

                if (string.Equals(setting.SettingKey, "IgnoreRightFields", StringComparison.OrdinalIgnoreCase))
                {
                    string[] rightFieldNames = setting.SettingValue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string fieldName in rightFieldNames)
                    {
                        m_rightFieldNamesToIgnore.Add(fieldName.Trim());
                    }
                    continue;
                }
            }
        }

        protected override void DoJob()
        {
            try
            {
                List<Guid> activeWitSessions = ServerDiffEngine.FindAllActiveSessionsOfType(SessionTypeEnum.WorkItemTracking);
                foreach (Guid activeWitSessionId in activeWitSessions)
                {
                    ServerDiffEngine diffEngine =
                        new ServerDiffEngine(activeWitSessionId, m_noContentComparison, m_verbose, SessionTypeEnum.WorkItemTracking, true, true);
                    try
                    {
                        WITDiffComparer witDiffComparer = new WITDiffComparer(diffEngine);
                        witDiffComparer.LeftFieldNamesToIgnore = m_leftFieldNamesToIgnore;
                        witDiffComparer.RightFieldNamesToIgnore = m_rightFieldNamesToIgnore;
                        diffEngine.RegisterDiffComparer(witDiffComparer);
                        if (diffEngine.VerifyContentsMatch(m_leftQueryCondition, m_rightQueryCondition))
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
            catch (MigrationSessionNotFoundException sessionNotFoundException)
            {
                TraceManager.TraceInformation(sessionNotFoundException.Message);
            }
        }
    }
}
