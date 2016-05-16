// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.ComponentModel;
using System.Windows.Data;

namespace Microsoft.TeamFoundation.Migration.Shell
{
    /// <summary>
    /// Represents a value that may change at run time.
    /// </summary>
    public interface IMutable
    {
        /// <summary>
        /// Gets the value.
        /// </summary>
        object Value
        {
            get;
        }

        /// <summary>
        /// Occurs when the value changes.
        /// </summary>
        event EventHandler ValueChanged;
    }

    /// <summary>
    /// Provides a default base class for mutable values.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    public abstract class Mutable<T> : IMutable, INotifyPropertyChanged
    {
        #region Events
        /// <summary>
        /// Occurs when the value changes.
        /// </summary>
        public event EventHandler ValueChanged;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        private event PropertyChangedEventHandler PropertyChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                this.PropertyChanged += value;
            }
            remove
            {
                this.PropertyChanged -= value;
            }
        }
        #endregion

        #region Properties
        object IMutable.Value
        {
            get
            {
                return this.Value;
            }
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public abstract T Value
        {
            get;
        }
        #endregion

        #region Operators
        /// <summary>
        /// Performs an implicit conversion from the <see cref="EditorFoundation.Mutable&lt;T&gt;"/> to its underlying value.
        /// </summary>
        /// <param name="mutable">The mutable.</param>
        /// <returns>The value.</returns>
        public static implicit operator T (Mutable<T> mutable)
        {
            return mutable.Value;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Raises the value changed event.
        /// </summary>
        protected void RaiseValueChangedEvent ()
        {
            if (this.ValueChanged != null)
            {
                this.ValueChanged (this, EventArgs.Empty);
            }

            if (this.PropertyChanged != null)
            {
                this.PropertyChanged (this, new PropertyChangedEventArgs ("Value"));
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a <see cref="DataSourceProvider"/> that wraps a mutable.
    /// </summary>
    /// <remarks>
    /// This is particularly useful in Wpf data binding.
    /// </remarks>
    public class Mutable : DataSourceProvider, IDisposable
    {
        #region Fields
        private IMutable source;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        /// <value>The source mutable.</value>
        protected IMutable Source
        {
            get
            {
                return this.source;
            }
            set
            {
                if (value != this.source)
                {
                    if (this.source != null)
                    {
                        this.source.ValueChanged -= this.OnValueChanged;
                    }

                    this.source = value;

                    if (this.source != null)
                    {
                        this.source.ValueChanged += this.OnValueChanged;
                    }

                    this.Refresh ();
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        public override string ToString ()
        {
            if (this.Data != null)
            {
                return this.Data.ToString ();
            }
            else
            {
                return "<Null Mutable>";
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose ()
        {
            this.Source = null;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// When overridden in a derived class, this base class calls this method when <see cref="M:System.Windows.Data.DataSourceProvider.InitialLoad"/> or <see cref="M:System.Windows.Data.DataSourceProvider.Refresh"/> has been called. The base class delays the call if refresh is deferred or initial load is disabled.
        /// </summary>
        protected override void BeginQuery ()
        {
            try
            {
                if (this.Source != null)
                {
                    this.OnQueryFinished (this.Source.Value);
                }
                else
                {
                    this.OnQueryFinished (null);
                }
            }
            catch (Exception exception)
            {
                this.OnQueryFinished (null, exception, null, null);
            }
        }
        #endregion

        #region Private Methods
        private void OnValueChanged (object sender, EventArgs e)
        {
            this.Refresh ();
        }
        #endregion
    }
}
