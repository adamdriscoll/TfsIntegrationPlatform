// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Shell.Extensibility
{
    /// <summary>
    /// Specifies the id, name, and description of a Plugin
    /// </summary>
    [AttributeUsage (AttributeTargets.Class)]
    public class PluginDescriptionAttribute : Attribute
    {
        #region Fields
        private readonly Guid id;
        private readonly string name;
        private string description;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the PluginDescriptionAttribute class.
        /// </summary>
        /// <param name="id">A static identifier for the Plugin. This must be a valid Guid.</param>
        /// <param name="name">A friendly name for the Plugin.</param>
        public PluginDescriptionAttribute (string id, string name)
        {
            this.id = new Guid (id);
            this.name = name;
            this.description = string.Empty;
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
        /// Gets the name of the Plugin.
        /// </summary>
        public string Name
        {
            get
            {
                return this.name;
            }
        }

        /// <summary>
        /// Gets or sets the description of the Plugin.
        /// </summary>
        public string Description
        {
            get
            {
                return this.description;
            }
            set
            {
                this.description = value;
            }
        }

        internal PluginDescriptor PluginDescriptor
        {
            get
            {
                return new PluginDescriptor (this.Id, this.Name, this.Description);
            }
        }
        #endregion
    }
}
