// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MigrationTestLibrary;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace TfsWitTest
{
    public class WitConfigurationTestCaseBase : TfsWITTestCaseBase
    {
        protected override void RunAndNoValidate(bool useExistingConfiguration)
        {
            if (!useExistingConfiguration || Configuration == null)
            {
                BuildFilterStringPair();

                // Generate a new configuration file
                Configuration = ConfigurationCreator.CreateConfiguration(TestConstants.ConfigurationTemplate.SingleWITSession, TestEnvironment);
                ConfigurationCreator.CreateConfigurationFile(Configuration, ConfigurationFileName);
            }

            // add validation step
            Configuration.LoadFromFile(ConfigurationFileName, true);
        }
    }
}
