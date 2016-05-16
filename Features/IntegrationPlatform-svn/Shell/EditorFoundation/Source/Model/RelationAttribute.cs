// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    /// <summary>
    /// Indicates the relation between two ModelObjects.
    /// </summary>
    public enum Relation
    {
        /// <summary>
        /// Indicates that one ModelObject owns another ModelObject.
        /// </summary>
        Owner,

        /// <summary>
        /// Indicates that one ModelObject references another ModelObject but is not the owner.
        /// </summary>
        Referencer
    }

    /// <summary>
    /// Specifies the relation between a ModelObject and one of its ModelObject-based properties.
    /// </summary>
    [AttributeUsage (AttributeTargets.Property)]
    public class RelationAttribute : Attribute
    {
        #region Fields
        private readonly Relation relation;

        /// <summary>
        /// Represents an owner relationship.
        /// </summary>
        public readonly static RelationAttribute Owner = new RelationAttribute (Relation.Owner);

        /// <summary>
        /// Represents a referencer relationship.
        /// </summary>
        public readonly static RelationAttribute Referencer = new RelationAttribute (Relation.Referencer);

        /// <summary>
        /// Represents the default relationship (owner).
        /// </summary>
        public readonly static RelationAttribute Default = RelationAttribute.Owner;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="RelationAttribute"/> class.
        /// </summary>
        /// <param name="relation">The relation.</param>
        public RelationAttribute (Relation relation)
        {
            this.relation = relation;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the relation.
        /// </summary>
        /// <value>The relation.</value>
        public Relation Relation
        {
            get
            {
                return this.relation;
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
            RelationAttribute relationAttribute = obj as RelationAttribute;
            if (relationAttribute != null)
            {
                return relationAttribute.Relation == this.Relation;
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
