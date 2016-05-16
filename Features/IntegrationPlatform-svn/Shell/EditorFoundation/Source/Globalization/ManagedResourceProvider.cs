// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Resources;

namespace Microsoft.TeamFoundation.Migration.Shell.Globalization
{
    /// <summary>
    /// Represents a resource provider for managed resources that are accessible via a <see cref="ResourceManager"/>.
    /// </summary>
    public class ManagedResourceProvider : ObservableResourceProvider
    {
        #region Fields
        private ResourceManager resourceManager;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedResourceProvider"/> class.
        /// </summary>
        public ManagedResourceProvider ()
            : this (null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedResourceProvider"/> class.
        /// </summary>
        /// <param name="resourceManager">The resource manager.</param>
        public ManagedResourceProvider (ResourceManager resourceManager)
        {
            this.ResourceManager = resourceManager;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the resource manager.
        /// </summary>
        /// <value>The resource manager.</value>
        public ResourceManager ResourceManager
        {
            get
            {
                return this.resourceManager;
            }
            set
            {
                if (value != this.resourceManager)
                {
                    this.resourceManager = value;
                    this.RaisePropertyChangedEvent ("ResourceManager");
                    this.RaisePropertyChangedEvent ("Item[]");
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets the resource with the specified key.
        /// </summary>
        /// <value></value>
        public override object this[string key]
        {
            get
            {
                if (this.ResourceManager == null)
                {
                    throw new InvalidOperationException ("ResourceManager is not set.");
                }

                return this.resourceManager.GetObject (key, this.ActiveCulture);
            }
        }
        #endregion
    }
}
