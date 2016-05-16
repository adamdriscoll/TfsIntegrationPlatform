// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    class FormatStringDecompositionRule : IStringManipulationRule
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

            int indexOfFormatIndex = replacementValue.IndexOf(
                FormatStrIndexStr, 0, StringComparison.OrdinalIgnoreCase);

            string prefix = string.Empty;
            if (indexOfFormatIndex > 0)
            {
                prefix = replacementValue.Substring(0, indexOfFormatIndex);
            }

            string postfix = string.Empty;
            if (replacementValue.Length > prefix.Length + FormatStrIndexStr.Length)
            {
                postfix = replacementValue.Substring(prefix.Length + FormatStrIndexStr.Length);
            }

            string retVal = originalValue;
            if (prefix.Length > 0)
            {
                if (!originalValue.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    // cannot transform, format incompatible
                    return originalValue;
                }
                else
                {
                    retVal = retVal.Substring(0, prefix.Length);
                }
            }

            if (postfix.Length > 0)
            {
                if (!originalValue.EndsWith(postfix, StringComparison.OrdinalIgnoreCase))
                {
                    // cannot transform, format incompatible
                    return originalValue;
                }
                else
                {
                    retVal = retVal.Substring(
                        0, retVal.LastIndexOf(postfix, 0, StringComparison.OrdinalIgnoreCase));
                    return retVal;
                }
            }

            return originalValue;
        }
    }
}
