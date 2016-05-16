// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Shell.Extensibility
{
    /// <summary>
    /// Provides information about a Plugin.
    /// </summary>
    public class PluginDescriptor
    {
        #region Fields
        private readonly Guid id;
        private readonly string name;
        private readonly string description;
        #endregion

        #region Constructors
        internal PluginDescriptor (Guid id, string name, string description)
        {
            this.id = id;
            this.name = name;
            this.description = description;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the Id of the Plugin.
        /// </summary>
        public Guid Id
        {
            get
            {
                return this.id;
            }
        }

        /// <summary>
        /// Gets the Name of the Plugin.
        /// </summary>
        public string Name
        {
            get
            {
                return this.name;
            }
        }

        /// <summary>
        /// Gets the Description of the Plugin.
        /// </summary>
        public string Description
        {
            get
            {
                return this.description; 
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Determines whether two PluginDescriptor instances are equal. 
        /// </summary>
        /// <param name="obj">The PluginDescriptor to compare with the current PluginDescriptor.</param>
        /// <returns><c>true</c> if the specified Object is equal to the current PluginDescriptor, <c>false</c> otherwise.</returns>
        public override bool Equals (object obj)
        {
            if (obj is PluginDescriptor)
            {
                PluginDescriptor descriptor = (PluginDescriptor)obj;
                return this.Id == descriptor.Id && this.Name == descriptor.Name && this.Description == descriptor.Description;
            }
            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode ()
        {
            return this.Id.GetHashCode () ^ this.Name.GetHashCode () ^ this.Description.GetHashCode ();
        }
        #endregion
    }
}
