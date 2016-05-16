// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// The interface that types which have settings collections implement.
    /// </summary>
    public interface ISettingsSection
    {
        /// <summary>
        ///  The current list of settings.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        Setting[] Settings
        {
            get;
        }

        /// <summary>
        /// Looks up and returns a value based on the provided key.  If the named key does not exist
        /// a MigrationException is thrown.
        /// </summary>
        /// <param name="name">The named value to find</param>
        /// <returns>The value of the provided named key</returns>
        string GetValue(string name);

        /// <summary>
        /// Looks up and returns a strongly typed value based on the provided key and generic type parameters.
        /// If the requested named key does not exist in the Settings collection the default value is returned.
        /// If the found value cannot be converted to the request type a MigrationException is thrown.
        /// </summary>
        /// <typeparam name="T">The generic type that the found value should be converted to and returned as.  The type must implement IConvertible.</typeparam>
        /// <param name="name">The named key of the Setting to find.</param>
        /// <param name="defaultValue">The default value to return if the named value does not exist.</param>
        /// <returns>An instance of the type requested in the T parameter.</returns>
        T GetValue<T>(string name, T defaultValue)
            where T : IConvertible;

        /// <summary>
        /// Looks up and returns a value based on the provided key.  If the value is found the out parameter 
        /// "value" is set to the found value and true is returned.  Otherwise the value of "value" is 
        /// undefined and false is returned.
        /// </summary>
        /// <param name="name">The named value to find.</param>
        /// <param name="value">The output value set to the found value.</param>
        /// <returns>True if the value is found, false otherwise.</returns>
        bool TryGetValue(string name, out string value);

        /// <summary>
        /// Looks up and returns a strongly typed value based on the provided key.  If the value is found the 
        /// out parameter "value" is set to the found value and true is returned.  Otherwise the value 
        /// of "value" is undefined and false is returned.
        /// </summary>
        /// <typeparam name="T">The generic type that the found value should be converted to and returned as.  The type must implement IConvertible.</typeparam>
        /// <param name="name">The named value to find.</param>
        /// <param name="value">The output value set to the found value.</param>
        /// <returns>True if the value is found, false otherwise.</returns>
        bool TryGetValue<T>(string name, out T value)
            where T : IConvertible;

        void RegisterParent(ISettingsSection parent);
    }
}
