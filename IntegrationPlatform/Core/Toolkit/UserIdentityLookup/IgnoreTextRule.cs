// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    class IgnoreTextRule : IStringManipulationRule
    {
        public string Apply(
            string originalValue,
            string replacementValue)
        {
            return string.Empty;
        }
    }
}
