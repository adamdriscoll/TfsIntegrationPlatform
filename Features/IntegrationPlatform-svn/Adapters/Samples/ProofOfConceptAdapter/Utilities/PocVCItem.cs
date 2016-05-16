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

namespace Rangers.TFS.Migration.PocAdapter.VC.PocUtilities
{
    public class PocVCItem
    {
        public string FileName  { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime LastUpdatedTime  { get; set; }
        public PocVCItemVersionInfo Version { get; set; }
        public bool IsFolder {get; set;}

        public byte[] GetContents() 
        {
            return null;
        }
    }
}
