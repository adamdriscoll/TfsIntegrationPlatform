// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Specifies the id and name of a Provider
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ProviderDescriptionAttribute : Attribute
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the PluginDescriptionAttribute class.
        /// </summary>
        /// <param name="id">A static identifier for the Plugin. This must be a valid Guid string.</param>
        /// <param name="name">A friendly name for the Plugin.</param>
        /// <param name="version">The version of the provider.</param>
        public ProviderDescriptionAttribute(string id, string name, string version)
        {
            Initialize(id, name, version, string.Empty);
        }

        /// <summary>
        /// Initializes a new instance of the PluginDescriptionAttribute class.
        /// </summary>
        /// <param name="id">A static identifier for the Plugin. This must be a valid Guid string.</param>
        /// <param name="name">A friendly name for the Plugin.</param>
        /// <param name="version">The version of the provider.</param>
        public ProviderDescriptionAttribute(string id, string name, string version, string shellAdapterIdentifier)
        {
            Initialize(id, name, version, shellAdapterIdentifier);
        }

        private void Initialize(string id, string name, string version, string shellAdapterIdentifier)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            if (string.IsNullOrEmpty(version))
            {
                throw new ArgumentNullException("version");
            }

            Id = new Guid(id);
            Name = name;
            Version = version;
            ShellAdapterIdentifier = (string.IsNullOrEmpty(shellAdapterIdentifier) ? Id : new Guid(shellAdapterIdentifier));
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the Id of the Plugin.
        /// </summary>
        public Guid Id
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the name of the Plugin.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the version of the Plugin.
        /// </summary>
        public string Version
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the unique identifier of the corresponding Shell Adapter.
        /// </summary>
        /// <remarks>When not explicitly specified, it defaults to be identical to Id.</remarks>
        public Guid ShellAdapterIdentifier
        {
            get;
            private set;
        }

        #endregion
    }
}
