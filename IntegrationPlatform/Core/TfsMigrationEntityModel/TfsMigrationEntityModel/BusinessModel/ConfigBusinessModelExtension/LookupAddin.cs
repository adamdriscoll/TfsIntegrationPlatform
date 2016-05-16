// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Microsoft.TeamFoundation.Migration.BusinessModel
{
    public partial class LookupAddin
    {
        [XmlIgnore]
        public Guid ReferenceNameGuid
        {
            get
            {
                return new Guid(ReferenceName);
            }
        }
    }
}
