// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Shell.Validation
{
    /// <summary>
    /// Indicates whether the attribute target affects the validity of the object.
    /// </summary>
    [AttributeUsage (AttributeTargets.Property)]
    public class AffectsValidityAttribute : Attribute
    {
        #region Fields
        private readonly bool affectsValidity;

        /// <summary>
        /// Indicates that the attribute target does affect the validity of the object.
        /// </summary>
        public static readonly AffectsValidityAttribute Yes = new AffectsValidityAttribute (true);

        /// <summary>
        /// Indicates that the attribute target does not affect the validity of the object.
        /// </summary>
        public static readonly AffectsValidityAttribute No = new AffectsValidityAttribute (false);

        /// <summary>
        /// Indicates that the attribute target does affect the validity of the object.
        /// </summary>
        public static readonly AffectsValidityAttribute Default = AffectsValidityAttribute.Yes;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AffectsValidityAttribute"/> class.
        /// </summary>
        /// <param name="affectsValidity">if set to <c>true</c> the attribute target affects the validity of the object.</param>
        public AffectsValidityAttribute (bool affectsValidity)
        {
            this.affectsValidity = affectsValidity;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets a value indicating whether the attribute target affects the validity of the object.
        /// </summary>
        /// <value><c>true</c> if the attribute target affects the validity of the object; otherwise, <c>false</c>.</value>
        public bool AffectsValidity
        {
            get
            {
                return this.affectsValidity;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">An <see cref="T:System.Object"></see> to compare with this instance or null.</param>
        /// <returns>
        /// true if obj equals the type and value of this instance; otherwise, false.
        /// </returns>
        public override bool Equals (object obj)
        {
            AffectsValidityAttribute affectsValidityAttribute = obj as AffectsValidityAttribute;
            if (affectsValidityAttribute != null)
            {
                return affectsValidityAttribute.AffectsValidity == this.AffectsValidity;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode ()
        {
            return base.GetHashCode ();
        }
        #endregion
    }
}
