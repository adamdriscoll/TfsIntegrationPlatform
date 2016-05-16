// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration
{
    /// <summary>
    /// Represents a membership restriction associated with a collection.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    public class CollectionRestriction<T> 
    {
        #region Fields
        private Predicate<T> restriction;
        private string reason;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the CollectionRestriction class.
        /// </summary>
        /// <param name="restriction">The restriction specified as a predicate.</param>
        /// <param name="reason">A reason string returned with an exception when the restriction verification fails.</param>
        public CollectionRestriction (Predicate<T> restriction, string reason)
        {
            this.restriction = restriction;
            this.reason = reason;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Verifies that the specified item can be added to the collection.
        /// </summary>
        /// <remarks>
        /// If the collection restriction disallows the specified item to be
        /// added to the collection, a CollectionRestrictionException is thrown.
        /// </remarks>
        /// <param name="item">The item to verify.</param>
        public void Verify (T item)
        {
            if (!this.restriction (item))
            {
                string message = string.Format ("Cannot add the specified item to the collection because {0}", this.reason);
                throw new CollectionRestrictionException (message);
            }
        }
        #endregion
    }

    /// <summary>
    /// The exception that is thrown when a collection restriction verification fails.
    /// </summary>
    public class CollectionRestrictionException : InvalidOperationException
    {
        #region Constructors
        internal CollectionRestrictionException (string message)
            : base (message)
        {
        }
        #endregion
    }
}
