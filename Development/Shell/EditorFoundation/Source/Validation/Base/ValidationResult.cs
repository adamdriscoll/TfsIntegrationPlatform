// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.Shell.Validation
{
    /// <summary>
    /// The level of a validation result.
    /// </summary>
    public enum ValidationLevel
    {
        /// <summary>
        /// Informational message.
        /// </summary>
        Information = TraceEventType.Information,

        /// <summary>
        /// Noncritical problem.
        /// </summary>
        Warning = TraceEventType.Warning,

        /// <summary>
        /// Recoverable error.
        /// </summary>
        Error = TraceEventType.Error
    }

    /// <summary>
    /// Represents the result of a single validation.
    /// </summary>
    public class ValidationResult : IEquatable<ValidationResult>, INotifyPropertyChanged, IDisposable
    {
        #region Fields
        private readonly object source;
        private readonly ValidationLevel level;
        private readonly string message;
        private readonly Mutable<string> mutableMessage;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResult"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="level">The level.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">An <see cref="Object"/> array containing zero or more objects to format.</param>
        public ValidationResult (object source, ValidationLevel level, string message, params object[] args)
            : this (source, level)
        {
            if (string.IsNullOrEmpty (message))
            {
                throw new ArgumentNullException ("message");
            }

            this.message = string.Format (message, args);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResult"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="level">The level.</param>
        /// <param name="message">The message.</param>
        public ValidationResult (object source, ValidationLevel level, Mutable<string> message)
            : this (source, level)
        {
            if (message == null)
            {
                throw new ArgumentNullException ("message");
            }

            this.mutableMessage = message;
            this.mutableMessage.ValueChanged += this.OnMessageChanged;
        }
        #endregion

        #region Events
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
        /// <summary>
        /// Gets the source.
        /// </summary>
        /// <value>The source.</value>
        public object Source
        {
            get
            {
                return this.source;
            }
        }

        /// <summary>
        /// Gets the level.
        /// </summary>
        /// <value>The level.</value>
        public ValidationLevel Level
        {
            get
            {
                return this.level;
            }
        }

        /// <summary>
        /// Gets the message.
        /// </summary>
        /// <value>The message.</value>
        public string Message
        {
            get
            {
                if (this.mutableMessage != null)
                {
                    return this.mutableMessage;
                }
                else
                {
                    return this.message;
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override string ToString ()
        {
            return string.Format ("{0}: {1}", this.Level, this.Message);
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override int GetHashCode ()
        {
            unchecked
            {
                int hashCode = this.source.GetHashCode () + this.level.GetHashCode ();

                if (this.mutableMessage != null)
                {
                    hashCode += this.mutableMessage.GetHashCode ();
                }
                else
                {
                    hashCode += this.message.GetHashCode ();
                }

                return hashCode;
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object"></see> to compare with the current <see cref="T:System.Object"></see>.</param>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>; otherwise, false.
        /// </returns>
        public override bool Equals (object obj)
        {
            return this.Equals (obj as ValidationResult);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the other parameter; otherwise, false.
        /// </returns>
        public bool Equals (ValidationResult other)
        {
            if (other != null)
            {
                bool equal = this.source == other.source && this.level == other.level;

                if (equal)
                {
                    if (this.mutableMessage != null)
                    {
                        if (other.mutableMessage != null)
                        {
                            equal = this.mutableMessage.Equals (other.mutableMessage);
                        }
                        else
                        {
                            equal = false;
                        }
                    }
                    else
                    {
                        equal = this.message == other.message;
                    }
                }

                return equal;    
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose ()
        {
            if (this.mutableMessage != null)
            {
                this.mutableMessage.ValueChanged -= this.OnMessageChanged;
            }
        }
        #endregion

        #region Private Methods
        private void RaiseMessageChangedEvent ()
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged (this, new PropertyChangedEventArgs ("Message"));
            }
        }

        private void OnMessageChanged (object sender, EventArgs e)
        {
            this.RaiseMessageChangedEvent ();
        }

        private ValidationResult (object source, ValidationLevel level)
        {
            if (source == null)
            {
                throw new ArgumentNullException ("source");
            }

            this.source = source;
            this.level = level;
        }
        #endregion
    }
}
