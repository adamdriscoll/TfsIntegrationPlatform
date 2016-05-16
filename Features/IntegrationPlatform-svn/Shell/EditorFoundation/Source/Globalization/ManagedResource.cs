// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Resources;

namespace Microsoft.TeamFoundation.Migration.Shell.Globalization
{
    /// <summary>
    /// Represents a single managed resource.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    public class ManagedResource<T> : Mutable<T>, IDisposable
    {
        #region Fields
        private bool disposed;
        private readonly ResourceManager resourceManager;
        private readonly string resourceKey;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedResource&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="resourceManager">The resource manager.</param>
        /// <param name="resourceKey">The resource key.</param>
        public ManagedResource (ResourceManager resourceManager, string resourceKey)
        {
            if (resourceManager == null)
            {
                throw new ArgumentNullException ("resourceManager");
            }

            if (string.IsNullOrEmpty (resourceKey))
            {
                throw new ArgumentNullException ("resourceName");
            }

            this.resourceManager = resourceManager;
            this.resourceKey = resourceKey;

            LocalizationManager.ActiveCultureChanged += this.OnActiveCultureChanged;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the resource value.
        /// </summary>
        /// <remarks>
        /// The value may vary based on the active culture, as specified by <see cref="LocalizationManager.ActiveCulture"/>.
        /// </remarks>
        public override T Value
        {
            get
            {
                object value = this.resourceManager.GetObject (this.resourceKey, LocalizationManager.ActiveCulture);

                if (value == null)
                {
                    throw new KeyNotFoundException (string.Format ("Resource key {0} not found.", this.resourceKey));
                }
                else
                {
                    return (T)value;
                }
            }
        }

        /// <summary>
        /// Gets the resource manager.
        /// </summary>
        protected ResourceManager ResourceManager
        {
            get
            {
                return this.resourceManager;
            }
        }

        /// <summary>
        /// Gets the resource key.
        /// </summary>
        protected string ResourceKey
        {
            get
            {
                return this.resourceKey;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="ManagedResource&lt;T&gt;"/>.
        /// </returns>
        public override int GetHashCode ()
        {
            unchecked
            {
                return this.resourceManager.GetHashCode () + this.resourceKey.GetHashCode ();
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>.</param>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
        /// </returns>
        /// <exception cref="T:System.NullReferenceException">The <paramref name="obj"/> parameter is null.</exception>
        public override bool Equals (object obj)
        {
            ManagedResource<T> resource = obj as ManagedResource<T>;
            if (resource != null)
            {
                return this.resourceManager == resource.resourceManager && this.resourceKey == resource.resourceKey;
            }
            return false;
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        public override string ToString ()
        {
            T value = this.Value;
            if (value != null)
            {
                return value.ToString ();
            }
            else
            {
                return "<Null Resource>";
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose ()
        {
            this.Dispose (true);
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose (bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    LocalizationManager.ActiveCultureChanged -= this.OnActiveCultureChanged;
                }

                this.disposed = true;
            }
        }
        #endregion

        #region Private Methods
        private void OnActiveCultureChanged (object sender, EventArgs e)
        {
            this.RaiseValueChangedEvent ();
        }
        #endregion
    }

    /// <summary>
    /// Represents a single managed resource string.
    /// </summary>
    /// <remarks>
    /// This class adds string formatting options to the <see cref="ManagedResource&lt;T&gt;"/> base class.
    /// </remarks>
    public class ManagedResourceString : ManagedResource<string>
    {
        #region Fields
        private readonly object[] formatArgs;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedResourceString"/> class.
        /// </summary>
        /// <param name="resourceManager">The resource manager.</param>
        /// <param name="resourceKey">The resource key.</param>
        /// <param name="formatArgs">The string format arguments.</param>
        public ManagedResourceString (ResourceManager resourceManager, string resourceKey, params object[] formatArgs)
            : base (resourceManager, resourceKey)
        {
            this.formatArgs = formatArgs;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the formated resource string.
        /// </summary>
        /// <value></value>
        /// <remarks>
        /// The value may vary based on the active culture, as specified by <see cref="LocalizationManager.ActiveCulture"/>.
        /// </remarks>
        public override string Value
        {
            get
            {
                string value = base.Value as string;
                if (value == null)
                {
                    throw new InvalidOperationException (string.Format ("Resource key {0} does not refer to a resource string.", this.ResourceKey));
                }

                return string.Format (LocalizationManager.ActiveCulture, value, this.formatArgs);
            }
        }
        #endregion
    }
}
