// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.ComponentModel;
using System.Globalization;

namespace Microsoft.TeamFoundation.Migration.Shell.Globalization
{
    /// <summary>
    /// Represents a dynamic resource provider that provides notifications when resource values change.
    /// </summary>
    public abstract class ObservableResourceProvider : IResourceProvider, INotifyPropertyChanged
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableResourceProvider"/> class.
        /// </summary>
        public ObservableResourceProvider ()
        {
            LocalizationManager.ActiveCultureChanged += delegate
            {
                this.RaisePropertyChangedEvent ("ActiveCulture");
                this.RaisePropertyChangedEvent ("Item[]");
            };
        }
        #endregion

        #region Events
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the active culture.
        /// </summary>
        public CultureInfo ActiveCulture
        {
            get
            {
                return LocalizationManager.ActiveCulture;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets the resource with the specified key.
        /// </summary>
        /// <value></value>
        public abstract object this[string key]
        {
            get;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Raises the property changed event.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected void RaisePropertyChangedEvent (string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged (this, new PropertyChangedEventArgs (propertyName));
            }
        }
        #endregion
    }
}
