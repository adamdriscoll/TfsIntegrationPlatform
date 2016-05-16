// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Shell.Validation
{
    /// <summary>
    /// Provides a notification when a change occurs on an object that potentially affects its validity.
    /// </summary>
    public interface ISupportValidationNotification : ISupportValidation
    {
        /// <summary>
        /// Indicates that a change has occurred that potentially affects the validity of the object.
        /// </summary>
        event EventHandler ValidityAffected;
    }
}
