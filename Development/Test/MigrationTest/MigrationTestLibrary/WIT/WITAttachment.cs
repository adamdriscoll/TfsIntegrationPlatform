// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MigrationTestLibrary
{
    public class WITAttachment
    {
        public string FileName { get; set; }
        public string Comment { get; set; }
        public AttachmentChangeActionType ActionType { get; set; }

        public WITAttachment(string filename, string comment)
        {
            FileName = filename;
            Comment = comment;
        }
    }
}
