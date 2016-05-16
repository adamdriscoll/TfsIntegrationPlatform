// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration
{
    /// <summary>
    /// Represents the name of a property of a class.
    /// This helps avoid bugs that arise from typos in
    /// the property name when handling PropertyChangedEvents.
    /// If the property name specified in the comparison
    /// is not a valid property of the class, an assertion
    /// fails (debug only).
    /// </summary>
    public class Property
    {
        #region Fields
        private readonly ModelObject owner;
        private readonly string name;
        #endregion

        #region Constructors
        internal Property (ModelObject owner, string name)
        {
            this.owner = owner;
            this.name = name;
        }
        #endregion

        #region Operators
        /// <summary>
        /// Implicitly converts this Property instance to a string.
        /// </summary>
        /// <param name="property">The Property to convert.</param>
        /// <returns>The name of the property as a string.</returns>
        public static implicit operator string (Property property)
        {
            return property.name;
        }

        /// <summary>
        /// Determines whether a Property is equivalent to a property name.
        /// </summary>
        /// <param name="property">The Property instance.</param>
        /// <param name="name">The string that represents the name of the property.</param>
        /// <returns><c>true</c> if the Property is equivalent to the property name, <c>false</c> otherwise.</returns>
        public static bool operator == (Property property, string name)
        {
            return property.Equals (name);
        }

        /// <summary>
        /// Determines whether a Property is not equivalent to a property name.
        /// </summary>
        /// <param name="property">The Property instance.</param>
        /// <param name="name">The string that represents the name of the property.</param>
        /// <returns><c>false</c> if the Property is equivalent to the property name, <c>true</c> otherwise.</returns>
        public static bool operator != (Property property, string name)
        {
            return !property.Equals (name);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:EditorFoundation.Model.Property"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:EditorFoundation.Model.Property"></see>.
        /// </returns>
        public override string ToString ()
        {
            return this.name;
        }

        /// <summary>
        /// Determines whether this Property is equal to another object.
        /// </summary>
        /// <param name="obj">The object to compare with the current Property.</param>
        /// <returns><c>true</c> if the Property is equivalent to the property name, <c>false</c> otherwise.</returns>
        public override bool Equals (object obj)
        {
            this.ValidateProperty (obj);
            return (this.name.Equals (obj));
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode ()
        {
            return this.name.GetHashCode ();
        }
        #endregion

        #region Private Methods
        private void ValidateProperty (object name)
        {
            Debug.Assert (name is string, "Property names should only be compared to strings");
            this.owner.ValidateProperty ((string)name);      
        }
        #endregion
    }
}
