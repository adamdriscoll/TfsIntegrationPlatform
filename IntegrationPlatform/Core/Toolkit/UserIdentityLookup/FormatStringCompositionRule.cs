// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    class FormatStringCompositionRule : IStringManipulationRule
    {
        const string FormatStrIndexStr = "{0}";

        public string Apply(
            string originalValue,
            string replacementValue)
        {
            if (string.IsNullOrEmpty(replacementValue))
            {
                throw new ArgumentNullException("replacementValue");
            }

            if (!replacementValue.Contains(FormatStrIndexStr))
            {
                throw new ArgumentException(
                    MigrationToolkitResources.ErrorUserIdFormatStringCompositionRule_InvalidFormat,
                    "replacementValue");
            }

            return string.Format(FormatStrIndexStr, originalValue);
        }
    }
}
