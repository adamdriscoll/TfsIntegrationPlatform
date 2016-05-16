// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Shell.Validation
{
    /// <summary>
    /// Provides data for the ValidationResultAdded and ValidationResultRemoved events.
    /// </summary>
    public class ValidationResultsChangedEvent : EventArgs
    {
        #region Fields
        private readonly ValidationResult validationResult;
        #endregion

        #region Constructors
        internal ValidationResultsChangedEvent (ValidationResult validationResult)
        {
            this.validationResult = validationResult;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the validation result.
        /// </summary>
        /// <value>The validation result.</value>
        public ValidationResult ValidationResult
        {
            get
            {
                return this.validationResult;
            }
        }
        #endregion
    }
}
