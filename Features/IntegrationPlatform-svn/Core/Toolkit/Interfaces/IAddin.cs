// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Defines a mechanism for retrieving an Add-in service provider
    /// </summary>
    public interface IAddin : IServiceProvider, IDisposable
    {
        /// <summary>
        /// Gets The reference name of the Add-in
        /// </summary>
        Guid ReferenceName { get; }

        /// <summary>
        /// Gets The Friendly name of the Add-in
        /// </summary>
        string FriendlyName { get; }

        /// <summary>
        /// An implementation of IAddin may support some CustomSettings in the configuration file
        /// Returning the names of the CustomSettingKeys supported by the Addin via this property allows
        /// the user interface to prompt the user for values for each of these CustomSettings.
        /// If the Addin implementation does not support any CustomSettings, it may return null or an empty list.
        /// </summary>
        ReadOnlyCollection<string> CustomSettingKeys { get; }

        /// <summary>
        /// Some IAddin implementations may be written in a generic way such that they will work if configured
        /// with any MigrationSource.   However, other Addin implementations may be migration source or adapter
        /// specific, such as an Addin that communicates with ClearCase to gather information about ClearCase
        /// symbolic links.  This Addin would not work correctly if configured with a MigrationSource that does
        /// not use the ClearCaseDetailedHistory adapter.  The SupportedMigrationProviderNames allows an Addin
        /// to declare the MigrationProviders that it is intended to work with by specifying a list of their
        /// reference names.
        /// If the Addin can work with any migration provider, it may return null or an empty list.
        /// </summary>
        ReadOnlyCollection<Guid> SupportedMigrationProviderNames { get; }

        /// <summary>
        /// Called by the platform to initialize this addin with the configuration of the running session group.
        /// </summary>
        /// <param name="configuration"></param>
        void Initialize(Configuration configuration);

 
    }
}
