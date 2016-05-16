// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.ComponentModel;

namespace Microsoft.TeamFoundation.Migration.Shell.Controller
{
    /// <summary>
    /// Provides data for an event that results from an operation on a file
    /// </summary>
    public abstract class FileEventArgs : EventArgs
    {
        #region Fields
        private readonly string filePath;	
        #endregion

        #region Constructors
        public FileEventArgs (string filePath)
        {
            this.filePath = filePath;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the full path of the file associated with the operation.
        /// </summary>
        public string FilePath
        {
            get
            {
                return this.filePath;
            }
        }
        #endregion
    }

    /// <summary>
    /// Provides data for an event that may contain an error.
    /// </summary>
    public abstract class ErrorEventArgs : FileEventArgs
    {
        #region Fields
        private readonly Exception error;	
        #endregion

        #region Constructors
        public ErrorEventArgs(string filePath, Exception error) 
            : base (filePath)
        {
            this.error = error;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the error that occurrred during the asynchronous operation.
        /// </summary>
        public Exception Error
        {
            get
            {
                return this.error;
            }
        }
        #endregion
    }

    /// <summary>
    /// Provides data for the Opening event.
    /// </summary>
    public class OpeningEventArgs : FileEventArgs
    {
        #region Constructors
        public OpeningEventArgs(string filePath)
            : base (filePath)
        {
        }
        #endregion
    }

    /// <summary>
    /// Provides data for the Opened event.
    /// </summary>
    public class OpenedEventArgs : ErrorEventArgs
    {
        #region Constructors
        public OpenedEventArgs(string filePath, Exception error) 
            : base (filePath, error)
        {
        }
        #endregion
    }

    /// <summary>
    /// Provides data for the Closing event.
    /// </summary>
    public class ClosingEventArgs : CancelEventArgs
    {
        #region Fields
        private readonly string filePath;
        #endregion

        #region Constructors
        public ClosingEventArgs(string filePath)
        {
            this.filePath = filePath;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the full path of the file associated with the operation.
        /// </summary>
        public string FilePath
        {
            get
            {
                return this.filePath;
            }
        }
        #endregion
    }

    /// <summary>
    /// Provides data for the Closed event.
    /// </summary>
    public class ClosedEventArgs : FileEventArgs
    {
        #region Constructors
        public ClosedEventArgs(string filePath)
            : base (filePath)
        {
        }
        #endregion
    }

    /// <summary>
    /// Provides data for the Saving event.
    /// </summary>
    public class SavingEventArgs : FileEventArgs
    {
        #region Constructors
        public SavingEventArgs(string filePath)
            : base (filePath)
        {
        }
        #endregion
    }

    /// <summary>
    /// Provides data for the Saved event.
    /// </summary>
    public class SavedEventArgs : ErrorEventArgs
    {
        #region Constructors
        public SavedEventArgs(string filePath, Exception error)
            : base (filePath, error)
        {
        }
        #endregion
    }
}
