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
using Microsoft.TeamFoundation.Migration.Toolkit;
using System.IO;

namespace Rangers.TFS.Migration.PocAdapter.VC
{
    [Serializable]
    public class PocVCMigrationItem : IMigrationItem
    {
        public string filePath;
        public string fileVersion;

        public PocVCMigrationItem()
        {
        }

        public PocVCMigrationItem(string filePath)
        {
            this.filePath = filePath;
        }

        #region IMigrationItem Members

        string IMigrationItem.DisplayName
        {
            get { return filePath; }
        }

        void IMigrationItem.Download(string localPath)
        {
            TraceManager.TraceInformation("POC:Item:Download - {0}", localPath);
            if (new FileInfo(filePath).Exists)
            {
                Directory.CreateDirectory(new FileInfo(localPath).DirectoryName);
                File.Copy(filePath, localPath);
            }
            else
            {
                Directory.CreateDirectory(localPath);
            }
        }

        #endregion
    }
}
