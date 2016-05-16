// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    class SimpleTextReplacementRule : IStringManipulationRule
    {
        public string Apply(
            string originalValue,
            string replacementValue)
        {
            if (!string.IsNullOrEmpty(replacementValue)
                && replacementValue.Equals(UserIdentityMappingConfigSymbols.ANY, StringComparison.OrdinalIgnoreCase))
            {
                return originalValue;
            }
            else
            {
                return replacementValue ?? string.Empty;
            }
        }
    }
}
