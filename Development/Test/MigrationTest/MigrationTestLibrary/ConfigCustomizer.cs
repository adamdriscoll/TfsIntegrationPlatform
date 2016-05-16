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
        public static void CustomActions_EnableBypassRulesOnTarget(MigrationTestEnvironment env, Configuration config)
        {
            var enableBypassRule = new CustomSetting();
            enableBypassRule.SettingKey = "EnableBypassRuleDataSubmission";
            //enableBypassRule.SettingValue = string.Empty;

            MigrationSource target = env.GetTargetMigrationSource(config);
            target.CustomSettings.CustomSetting.Add(enableBypassRule);
        }

        public static void CustomActions_DisableBypassRulesOnTarget(MigrationTestEnvironment env, Configuration config)
        {
            var enableBypassRule = new CustomSetting();
            enableBypassRule.SettingKey = "EnableBypassRuleDataSubmission";
            enableBypassRule.SettingValue = "false";

            MigrationSource target = env.GetTargetMigrationSource(config);
            target.CustomSettings.CustomSetting.Add(enableBypassRule);
        }

        public static void CustomActions_DisableCSSNodeCreationOnTarget(MigrationTestEnvironment env, Configuration config)
        {
            var disableAreaPathCreation = new CustomSetting();
            disableAreaPathCreation.SettingKey = "DisableAreaPathAutoCreation";
            //disableAreaPathCreation.SettingValue = string.Empty;

            var disableIterationPathCreation = new CustomSetting();
            disableIterationPathCreation.SettingKey = "DisableIterationPathAutoCreation";
            //disableIterationPathCreation.SettingValue = string.Empty;

            MigrationSource target = env.GetTargetMigrationSource(config);
            target.CustomSettings.CustomSetting.Add(disableAreaPathCreation);
            target.CustomSettings.CustomSetting.Add(disableIterationPathCreation);
        }

        public static void CustomActions_DisableContextSync(MigrationTestEnvironment env, Configuration config)
        {
            config.SessionGroup.WorkFlowType.SyncContext = SyncContext.Disabled;
        }
    }
}
