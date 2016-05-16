// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MigrationTestLibrary
{
    public class WITLink
    {
        public string Location { get; set; }
        public string Comment { get; set; }

        public WITLink(string location)
            : this(location, string.Empty)
        {
        }

        public WITLink(string location, string comment)
        {
            Location = location;
            Comment = comment;
        }
    }
}
