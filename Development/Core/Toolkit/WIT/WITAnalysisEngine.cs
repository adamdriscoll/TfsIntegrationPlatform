// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.TeamFoundation.Migration.EntityModel;
using System.Diagnostics;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.BusinessModel.WIT;

namespace Microsoft.TeamFoundation.Migration.Toolkit.WIT
{
    class WITAnalysisEngine : AnalysisEngine
    {
        internal WITAnalysisEngine(RuntimeSession session)
            : base(session)
        {
        }
    }
}
