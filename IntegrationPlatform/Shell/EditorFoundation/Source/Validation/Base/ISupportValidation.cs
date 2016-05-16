// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Shell.Validation
{
    /// <summary>
    /// Indicates that a class supports validation.
    /// </summary>
    public interface ISupportValidation
    {
        /// <summary>
        /// Validates this instance.
        /// </summary>
        /// <returns>The set of validation results.</returns>
        IEnumerable<ValidationResult> Validate ();
    }
}
