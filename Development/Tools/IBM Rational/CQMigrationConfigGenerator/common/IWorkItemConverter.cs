// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// Description: Work Item Converter Generic Interface
//              This interface will be implemented for different sources
//              like Product Studio, ClearQuest etc.

#region Using directives

using System;
using System.Collections.Generic;
using System.Text;
#endregion

namespace Microsoft.TeamFoundation.Converters.WorkItemTracking.Common
{
    /// <remarks>
    /// Work Item Converter Interface
    /// </remarks>
    interface IWorkItemConverter
    {
        void Initialize(ConverterParameters convParams);
        void Convert();
        void CleanUp();
    }
}
