// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//
// 20091101 TFS Integration Platform Custom Adapter Proof-of-Concept
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Rangers.TFS.Migration.PocAdapter.VC.PocUtilities
{
    public class PocUtil
    {
        public string PocSiteUrl { get; set; }

        public string DocumentLibraryName {get; set;}

        public string WorkspacePath { get; set; }

        public void CreateFolder(string folderPath)
        {
            TraceManager.TraceInformation("Adding folder <{0}> to Poc", folderPath);
        }

        public void AddFile(string localPath)
        {
            TraceManager.TraceInformation("Adding file <{0}> to Poc", Path.GetFileName(localPath));
        }

        public PocVCItem GetFile(string filePath, string version)
        {
            throw new System.NotImplementedException();
        }

        public List<PocVCItem> GetFiles(string folderPath)
        {
            throw new System.NotImplementedException();
        }

        public List<PocVCItem> GetFolders()
        {
            throw new System.NotImplementedException();
        }

        public List<PocVCItemVersionInfo> GetFileVersions()
        {
            throw new System.NotImplementedException();
        }

        public void RenameFile(string filePath, string newName)
        {
            throw new System.NotImplementedException();
        }

        public void DeleteFile(string filePath)
        {
            throw new System.NotImplementedException();
        }
    }
}
