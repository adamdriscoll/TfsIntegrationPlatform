// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;

namespace MigrationTestLibrary
{
    public class WITChangeAction
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string History { get; set; }
        public string Reason { get; set; }
        public string Priority { get; set; }
        public string AssignedTo { get; set; }
    }
}
