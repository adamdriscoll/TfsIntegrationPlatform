// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.TeamFoundation.Migration.BusinessModel.WIT
{
    /// <summary>
    /// The SourceField class represents the fields that appear on the source
    /// side of the Aggregation Field Mapping in the WIT session confguration.
    /// </summary>
    public partial class SourceField
    {
        /// <summary>
        /// Gets and sets the XMLNode corresponding to this field in the WIT description document.
        /// </summary>
        [XmlIgnore]
        public XmlNode FieldColumnNode
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the value of this field after the value map is applied to the original field value.
        /// </summary>
        [XmlIgnore]
        public string MappedValue
        {
            get;
            set;
        }
    }
}
