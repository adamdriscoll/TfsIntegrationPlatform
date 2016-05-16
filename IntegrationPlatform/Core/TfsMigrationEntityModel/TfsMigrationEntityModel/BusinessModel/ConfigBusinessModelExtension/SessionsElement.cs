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
    public partial class SessionsElement
    {
        [XmlIgnore]
        public Session this[Guid sessionUniqueId]
        {
            get
            {
                foreach (var session in this.Session)
                {
                    if (session.SessionUniqueIdGuid.Equals(sessionUniqueId))
                    {
                        return session;
                    }
                }

                return null;
            }
        }
    }
}
