// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement
{
    class DebugAssertChannel : IErrorRoutingChannel
    {
        #region IErrorRoutingChannel Members

        public void RouteError(Exception e)
        {
            Debug.Assert(e != null, "e is NULL");
            Debug.Assert(false, e.ToString());
        }

        #endregion
    }
}
