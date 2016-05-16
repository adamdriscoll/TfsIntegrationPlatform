// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.EntityModel
{
    /// <summary>
    /// Exception that is thrown when the data in the storage is inconsistent.
    /// </summary>
    public class InconsistentDataException : Exception
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="msg"></param>
        public InconsistentDataException(string msg)
            :base(msg)
        {}
    }
}
