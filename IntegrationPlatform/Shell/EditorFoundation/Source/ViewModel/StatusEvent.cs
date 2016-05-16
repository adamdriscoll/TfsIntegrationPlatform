// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.ComponentModel;
using Microsoft.TeamFoundation.Migration.Shell.Globalization;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Provides information about a change of status.
    /// </summary>
    public class StatusEvent : INotifyPropertyChanged, IDisposable
    {
        #region Fields
        private bool disposed;
        private readonly DateTime timestamp;
        private readonly object eventData;
        private readonly IMutable mutableEventData;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="StatusEvent"/> class.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        public StatusEvent (object eventData)
            : this ()
        {
            this.eventData = eventData;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusEvent"/> class.
        /// </summary>
        /// <param name="eventData">The mutable event data.</param>
        public StatusEvent (IMutable eventData)
            : this ()
        {
            this.mutableEventData = eventData;
            this.mutableEventData.ValueChanged += this.OnEventDataChanged;
        }

        private StatusEvent ()
        {
            this.timestamp = DateTime.Now;
            LocalizationManager.ActiveCultureChanged += this.OnActiveCultureChanged;
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
        /// Gets the time at which the status even occurred.
        /// </summary>
        public string Timestamp
        {
            get
            {
                return this.timestamp.ToString (LocalizationManager.ActiveCulture);
            }
        }

        /// <summary>
        /// Gets the status event data.
        /// </summary>
        public object EventData
        {
            get
            {
                if (this.mutableEventData != null)
                {
                    return this.mutableEventData.Value;
                }
                else
                {
                    return this.eventData;
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose ()
        {
            this.Dispose (true);
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        public override string ToString ()
        {
            return string.Format (LocalizationManager.ActiveCulture, "{0}: {1}", this.Timestamp, this.EventData);
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

                    if (this.mutableEventData != null)
                    {
                        this.mutableEventData.ValueChanged -= this.OnEventDataChanged;
                    }
                }

                this.disposed = false;
            }
        }
        #endregion

        #region Private Methods
        private void RaisePropertyChangedEvent (string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged (this, new PropertyChangedEventArgs (propertyName));
            }
        }

        private void OnEventDataChanged (object sender, EventArgs e)
        {
            this.RaisePropertyChangedEvent ("EventData");
        }

        private void OnActiveCultureChanged (object sender, EventArgs e)
        {
            this.RaisePropertyChangedEvent ("Timestamp");
        }
        #endregion
    }
}
