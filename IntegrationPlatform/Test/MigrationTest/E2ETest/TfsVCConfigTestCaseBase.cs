// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MigrationTestLibrary;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace TfsVCTest
{
    /// <summary>
    /// The base class of all TFS VC configuration test. The tests of this category only does configuraiton validation.
    /// </summary>
    public class TfsVCConfigTestCaseBase : TfsVCTestCaseBase
    {
        protected override void RunAndValidate(bool useExistingConfiguration, bool AddOnBranchSourceNotFound)
        {
            if (!useExistingConfiguration || Configuration == null)
            {
                // Generate a new configuration file
                Configuration = ConfigurationCreator.CreateConfiguration(TestConstants.ConfigurationTemplate.SingleVCSession, TestEnvironment);

                // Try moving foreach here ...
                foreach (var session in Configuration.SessionGroup.Sessions.Session)
                {
                    VCSession = session;
                    break;
                }

                ConfigurationCreator.CreateConfigurationFile(Configuration, ConfigurationFileName);

                Configuration.LoadFromFile(ConfigurationFileName);
            }
        }
    }
}
