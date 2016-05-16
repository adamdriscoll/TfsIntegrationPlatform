// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Xml.Serialization;

namespace Microsoft.TeamFoundation.Migration.BusinessModel
{
    /// <summary>
    /// Link element class
    /// </summary>
    public partial class LinkingElement
    {
        /// <summary>
        /// Get and set the creation time of this setting
        /// </summary>
        [XmlIgnore]
        public DateTime CreationTime
        {
            get;
            set;
        }
    }
}
