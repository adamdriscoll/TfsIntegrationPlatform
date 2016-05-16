// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.BusinessModel
{
    /// <summary>
    /// Exception that is thrown when an identical configuration is already saved in the storage.
    /// </summary>
    public class DuplicateConfigurationException : Exception
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="msg"></param>
        public DuplicateConfigurationException(string msg)
            :base(msg)
        {}
    }

    /// <summary>
    /// Exception that is thrown when a configuration change is not valid.
    /// </summary>
    public class InvalidConfigurationChangeException : Exception
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="msg"></param>
        public InvalidConfigurationChangeException(string msg)
            :base(msg)
        {}
    }

    /// <summary>
    /// Exception that is thrown when an identical configuration is already saved in the storage.
    /// </summary>
    public class SavingUnsavableConfigurationException : Exception
    {
        /// <summary>
        /// Gets the configuration that the platform tries to save but fails.
        /// </summary>
        public Configuration ConfigurationAttemptedForSaving
        {
            get;
            private set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configurationAttemptedForSaving">The configuration, of which the saving fails.</param>
        public SavingUnsavableConfigurationException(Configuration configurationAttemptedForSaving)
            : base()
        {
            ConfigurationAttemptedForSaving = configurationAttemptedForSaving;
        }
    }
}
