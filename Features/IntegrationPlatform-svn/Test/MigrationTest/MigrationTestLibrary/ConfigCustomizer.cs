// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace MigrationTestLibrary
{
    public class ConfigCustomizer
    {
        public static void CustomActions_EnableBypassRulesOnTarget(Configuration config)
        {
            MigrationSource target = config.SessionGroup.MigrationSources.MigrationSource[1];
            var enableBypassRule = new CustomSetting();
            enableBypassRule.SettingKey = "EnableBypassRuleDataSubmission";
            //enableBypassRule.SettingValue = string.Empty;

            target.CustomSettings.CustomSetting.Add(enableBypassRule);
        }

        public static void CustomActions_DisableBypassRulesOnTarget(Configuration config)
        {
            MigrationSource target = config.SessionGroup.MigrationSources.MigrationSource[1];
            var enableBypassRule = new CustomSetting();
            enableBypassRule.SettingKey = "EnableBypassRuleDataSubmission";
            enableBypassRule.SettingValue = "false";

            target.CustomSettings.CustomSetting.Add(enableBypassRule);
        }

        public static void CustomActions_DisableCSSNodeCreationOnTarget(Configuration config)
        {
            MigrationSource target = config.SessionGroup.MigrationSources.MigrationSource[1];
            var disableAreaPathCreation = new CustomSetting();
            disableAreaPathCreation.SettingKey = "DisableAreaPathAutoCreation";
            //disableAreaPathCreation.SettingValue = string.Empty;

            var disableIterationPathCreation = new CustomSetting();
            disableIterationPathCreation.SettingKey = "DisableIterationPathAutoCreation";
            //disableIterationPathCreation.SettingValue = string.Empty;

            target.CustomSettings.CustomSetting.Add(disableAreaPathCreation);
            target.CustomSettings.CustomSetting.Add(disableIterationPathCreation);
        }

        public static void CustomActions_DisableContextSync(Configuration config)
        {
            config.SessionGroup.WorkFlowType.SyncContext = SyncContext.Disabled;
        }

        public static void CustomActions_SetBidirectionalNoContextSync(Configuration config)
        {
            config.SessionGroup.WorkFlowType.DirectionOfFlow = DirectionOfFlow.Bidirectional;
            config.SessionGroup.WorkFlowType.Frequency = Frequency.ContinuousManual;
            config.SessionGroup.WorkFlowType.SyncContext = SyncContext.Disabled;
        }

        public static void CustomActions_SetOneWayNoContextSync(Configuration config)
        {
            config.SessionGroup.WorkFlowType.DirectionOfFlow = DirectionOfFlow.Unidirectional;
            config.SessionGroup.WorkFlowType.Frequency = Frequency.ContinuousManual;
            config.SessionGroup.WorkFlowType.SyncContext = SyncContext.Disabled;
        }
    }
}
