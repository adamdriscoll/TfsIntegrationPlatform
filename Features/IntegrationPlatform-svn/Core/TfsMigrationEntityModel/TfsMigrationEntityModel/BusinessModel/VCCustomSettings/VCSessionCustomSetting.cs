// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Microsoft.TeamFoundation.Migration.BusinessModel.VC
{
    public partial class VCSessionCustomSetting
    {
        [XmlIgnore]
        public Session SessionConfig
        {
            get;
            set;
        }

        /// <summary>
        /// Update the parent session configuration with changes to this custom setting
        /// </summary>
        public void Update()
        {
            if (null != SessionConfig)
            {
                SessionConfig.UpdateCustomSetting(this);
            }
            else
            {
                throw new InvalidOperationException("VCSessionCustomSetting does not have an associated parent Session configuration. Update failed.");
            }
        }
    }
}
