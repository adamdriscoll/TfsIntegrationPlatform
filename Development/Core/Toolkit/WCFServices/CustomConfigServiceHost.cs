// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.ServiceModel.Configuration;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Toolkit.WCFServices
{
    /// <summary>
    /// A customized ServiceHost class that loads WCF config from a custom location instead of app.config
    /// </summary>
    /// <remarks>
    /// refer to http://blogs.msdn.com/dotnetinterop/archive/2008/09/22/custom-service-config-file-for-a-wcf-service-hosted-in-iis.aspx
    /// </remarks>
    public class CustomConfigServiceHost : ServiceHost
    {
        //string ConfigFilePath { get; set; }

        public CustomConfigServiceHost(object singletonInstance)
            : base(singletonInstance)
        {            
        }

        public CustomConfigServiceHost(Type serviceType)
            : base(serviceType)
        {            
        }

        protected override void ApplyConfiguration()
        {
            if (string.IsNullOrEmpty(GlobalConfiguration.GlobalConfigPath) 
                || !File.Exists(GlobalConfiguration.GlobalConfigPath))
            {
                base.ApplyConfiguration();
            }
            else
            {
                LoadConfigFromCustomLocation();
            }
        }

        private void LoadConfigFromCustomLocation()
        {
            Configuration config = GlobalConfiguration.Configuration;

            var serviceModel = ServiceModelSectionGroup.GetSectionGroup(config);

            bool loaded = false;
            foreach (ServiceElement serviceElem in serviceModel.Services.Services)
            {
                if (serviceElem.Name.Equals(Description.ConfigurationName, StringComparison.OrdinalIgnoreCase))
                {
                    LoadConfigurationSection(serviceElem);
                    loaded = true;
                    break;
                }
            }

            if (!loaded)
            {
                TraceManager.TraceError("Cannot find ServiceElement in the configuration file '{0}' - attempting to use default app.config.", GlobalConfiguration.GlobalConfigPath);
                base.ApplyConfiguration();
            }
        }
    }
}
