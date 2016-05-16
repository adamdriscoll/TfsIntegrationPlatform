// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Shell.Validation
{
    /// <summary>
    /// Defines the public interface of a Validation Manager.
    /// </summary>
    public interface IValidationManager
    {
        /// <summary>
        /// Raised when the status of the Validation Manager changes.
        /// </summary>
        event EventHandler StatusChanged;

        /// <summary>
        /// Raised when a <see cref="ValidationResult"/> is added to the Validation Manager.
        /// </summary>
        event EventHandler<ValidationResultsChangedEvent> ValidationResultAdded;

        /// <summary>
        /// Raised when a <see cref="ValidationResult"/> is removed from the Validation Manager.
        /// </summary>
        event EventHandler<ValidationResultsChangedEvent> ValidationResultRemoved;

        /// <summary>
        /// Gets the status of the Validation Manager.
        /// </summary>
        ValidationStatus Status { get; }

        /// <summary>
        /// Gets the current number of validation results.
        /// </summary>
        /// <value>The number of validation results.</value>
        int ValidationResultCount { get; }

        /// <summary>
        /// Starts a complete validation of all registered data.
        /// </summary>
        void Validate ();

        /// <summary>
        /// Enumerates the current validation results for the specified object.
        /// </summary>
        /// <param name="obj">The object for which validation results are needed.</param>
        /// <returns>The validation results.</returns>
        IEnumerable<ValidationResult> EnumerateValidationResults (ISupportValidation obj);

        /// <summary>
        /// Enumerates the current validation results for all registered data.
        /// </summary>
        /// <returns>The validation results.</returns>
        IEnumerable<ValidationResult> EnumerateValidationResults ();
    }
}
