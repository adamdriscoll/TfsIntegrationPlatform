// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    internal static class TfsWITCustomSetting
    {
        public static bool GetBooleanSettingValueDefaultToTrue(CustomSetting setting)
        {
            bool val;
            if (!bool.TryParse(setting.SettingValue, out val))
            {
                val = true;
            }

            return val;
        }
    }
}
