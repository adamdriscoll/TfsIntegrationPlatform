// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.BusinessModel.VC;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;


namespace Microsoft.TeamFoundation.Migration.TfsFileSystemAdapter
{
    /// <summary>
    /// This class wraps custom settings specific to TfsFileSystem adapter.
    /// </summary>
    class TfsFileSystemConfiguration
    {
        /// <summary>
        /// Returns the list of branches in forward integration direction. 
        /// E.g. <main <sp1, sp2>>
        /// </summary>
        public Dictionary<string, List<string>> FIList { get; private set; }

        /// <summary>
        /// Returns the list of branches in reverse integration direction. 
        /// E.g. <sp1, <main>>
        /// </summary>
        public Dictionary<string, List<string>> RIList { get; private set; }
        
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="configurationService"></param>
        public TfsFileSystemConfiguration(ConfigurationService configurationService)
        {
            FIList = new Dictionary<string, List<string>>();
            RIList = new Dictionary<string, List<string>>();
            foreach ( BranchSetting branchSetting in configurationService.VcCustomSetting.BranchSettings.BranchSetting)
            {
                if (configurationService.SourceId != new Guid(branchSetting.SourceId))
                {
                    continue;
                }

                if (!FIList.ContainsKey(branchSetting.SourceBranch))
                {
                    FIList.Add(branchSetting.SourceBranch, new List<string>());
                }
                FIList[branchSetting.SourceBranch].Add(branchSetting.TargetBranch);

                if (!RIList.ContainsKey(branchSetting.TargetBranch))
                {
                    RIList.Add(branchSetting.TargetBranch, new List<string>());
                }
                RIList[branchSetting.TargetBranch].Add(branchSetting.SourceBranch);
            }
        }

        /// <summary>
        /// Given a source branch path, returns all possible forward integration branch paths. 
        /// Returns null if no FI branchs exist.
        /// </summary>
        public List<string> GetFIList(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            if (FIList.ContainsKey(path))
            {
                return FIList[path];
            }
            return null;
        }

        /// <summary>
        /// Given a target branch path, returns all possible reverse integration branch paths. 
        /// Returns null if no RI branchs exist.
        /// </summary>
        public List<string> GetRIList(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            if (RIList.ContainsKey(path))
            {
                return RIList[path];
            }
            return null;
        }
    }
}
