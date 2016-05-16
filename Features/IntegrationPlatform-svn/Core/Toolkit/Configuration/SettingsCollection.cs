// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.Serialization;
using System.Xml.XPath;
using System.Globalization;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// The abstract class from which all types that implement a collection of Settings should derive.
    /// </summary>
    public abstract class SettingsSection : ISettingsSection
    {
        public virtual void RegisterParent(ISettingsSection parent)
        {
            m_parent = parent;
        }

        /// <summary>
        /// The current array of Setting objects.
        /// </summary>
        public Setting[] Settings
        {
            get
            {
                lock (locker)
                {
                    if (m_cacheIsInvalid)
                    {
                        rebuildCache();
                    }

                    return m_SettingsCache;
                }
            }

            // this is only public because the default XmlSerializer needs it that way
            // we could write our own serializer to avoid this
            set
            {
                lock (locker)
                {
                    m_SettingsCache = null;
                    m_cacheIsInvalid = true;

                    m_Settings = new Dictionary<string, string>(value.Length, StringComparer.OrdinalIgnoreCase);
                    foreach (Setting s in value)
                    {
                        AddSetting(s);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a Setting to the existing Settings collection.  If a Setting with the same Name already exists 
        /// in the collection and 'overwrite' is set to false, a MigrationException is thrown.
        /// </summary>
        /// <param name="setting">The Setting to add to the collection.</param>
        /// <param name="overwrite">Set this value to true to allow overwrite settings.</param>
        public void AddSetting(Setting setting, bool overwrite)
        {
            if (setting == null)
            {
                throw new ArgumentNullException("setting");
            }

            lock (locker)
            {
                if (m_Settings.ContainsKey(setting.Name))
                {
                    if (overwrite)
                    {
                        m_Settings.Remove(setting.Name);
                    }
                    else
                    {
                        throw new MigrationException(string.Format(MigrationToolkitResources.Culture,
                            MigrationToolkitResources.SettingDefinedTwice, setting.Name));
                    }
                }
                m_cacheIsInvalid = true;
                m_Settings.Add(setting.Name, setting.Value);

            }
        }

        /// <summary>
        /// Adds a Setting to the existing Settings collection.  If a Setting with the same Name already exists
        /// in the collection a MigrationException is thrown.
        /// </summary>
        /// <param name="setting">The Setting to add to the collection.</param>
        public void AddSetting(Setting setting)
        {
            AddSetting(setting, false);
        }

        /// <summary>
        /// Creates a new Setting object based on the provided name and valkue and adds the Setting to the 
        /// existing Settings collection.  If a Setting with the same Name already exists in the collection 
        /// and 'overwrite' is set to false, a MigrationException is thrown.
        /// </summary>
        /// <param name="name">The name of the Setting to add to the collection.</param>
        /// <param name="value">The value of the Setting to add to the collection.</param>
        /// <param name="overwrite">Set this value to true to allow overwrite settings.</param>
        public void AddSetting(string name, object value, bool overwrite)
        {
            Setting setting = new Setting(name, (value != null) ? value.ToString() : null);
            AddSetting(setting, overwrite);
        }

        /// <summary>
        /// Creates a new Setting object based on the provided name and valkue and adds the Setting to the 
        /// existing Settings collection.  If a Setting with the same Name already exists in the collection 
        /// a MigrationException is thrown.
        /// </summary>
        /// <param name="name">The name of the Setting to add to the collection.</param>
        /// <param name="value">The value of the Setting to add to the collection.</param>
        public void AddSetting(string name, object value)
        {
            AddSetting(name, value, false);
        }

        /// <summary>
        /// Rebuilds the Setting cache after an invalidation (a new Setting is added to the cache).
        /// </summary>
        private void rebuildCache()
        {
            lock (locker)
            {
                if (m_Settings.Count > 0)
                {
                    m_SettingsCache = new Setting[m_Settings.Count];

                    int index = 0;
                    foreach (string name in m_Settings.Keys)
                    {
                        m_SettingsCache[index++] = new Setting(name, m_Settings[name]);
                    }
                }

                m_cacheIsInvalid = false;
            }
        }

        /// <summary>
        /// Loads the collection from XML. This is an internal method used to populate the
        /// collection when it is created.
        /// </summary>
        /// <param name="nodes">Iterator for child nodes</param>
        internal void InitFromXml(
            XPathNodeIterator nodes)
        {
            // The method must be called only once during initialization.
            Debug.Assert(m_Settings.Count == 0, "Settings are already initialized!");

            foreach (XPathNavigator setting in nodes)
            {
                string name = setting.GetAttribute("name", string.Empty);
                string value = setting.GetAttribute("value", string.Empty);
                AddSetting(new Setting(name, value));
            }
        }

        bool m_cacheIsInvalid;
        Dictionary<string, string> m_Settings = new Dictionary<string, string>();
        Setting[] m_SettingsCache = new Setting[0];
        object locker = new object();

        ISettingsSection m_parent;

        #region ISettingsSection Members

        /// <summary>
        /// Looks up and returns a value based on the provided key.  If the named key does not exist
        /// a MigrationException is thrown.
        /// </summary>
        /// <param name="name">The named value to find</param>
        /// <returns>The value of the provided named key</returns>
        public string GetValue(string name)
        {
            string value;
            if (TryGetValue(name, out value))
            {
                return value;
            }

            throw new MigrationException(string.Format(MigrationToolkitResources.Culture, MigrationToolkitResources.SettingNotFound, name));
        }

        /// <summary>
        /// Looks up and returns a strongly typed value based on the provided key and generic type parameters.
        /// If the requested named key does not exist in the Settings collection the default value is returned.
        /// If the found value cannot be converted to the request type a MigrationException is thrown.
        /// </summary>
        /// <typeparam name="T">The generic type that the found value should be converted to and returned as.  The type must implement IConvertible.</typeparam>
        /// <param name="name">The named key of the Setting to find.</param>
        /// <param name="defaultValue">The default value to return if the named value does not exist.</param>
        /// <returns>An instance of the type requested in the T parameter.</returns>
        public T GetValue<T>(string name, T defaultValue)
            where T : IConvertible
        {
            T value;
            if (TryGetValue<T>(name, out value))
            {
                return value;
            }

            return defaultValue;
        }

        /// <summary>
        /// Looks up and returns a strongly typed value based on the provided key.  If the value is found the 
        /// out parameter "value" is set to the found value and true is returned.  Otherwise the value 
        /// of "value" is undefined and false is returned.
        /// </summary>
        /// <typeparam name="T">The generic type that the found value should be converted to and returned as.  The type must implement IConvertible.</typeparam>
        /// <param name="name">The named value to find.</param>
        /// <param name="value">The output value set to the found value.</param>
        /// <returns>True if the value is found, false otherwise.</returns>
        public bool TryGetValue<T>(string name, out T value)
            where T : IConvertible
        {
            value = default(T);
            bool success = false;

            string stringValue;
            if (TryGetValue(name, out stringValue))
            {
                try
                {
                    object oValue = Convert.ChangeType(stringValue, typeof(T), CultureInfo.InvariantCulture);

                    if (oValue != null)
                    {
                        value = (T)oValue;
                        success = true;
                    }
                }
                catch (FormatException)
                {
                    /* ChangeType will throw this when the format provider fails. 
                     * Eat this exception.
                     */
                }
                catch (ArithmeticException)
                {
                    /* ChangeType will throw types derived from this when the conversion
                     * causes overflow, etc.
                     */
                }

                // InvalidCastException is not caught as I can not create a test case
                // where this happens with a source type of string (which this always is)
            }

            return success;
        }

        /// <summary>
        /// Looks up and returns a value based on the provided key.  If the value is found the out parameter 
        /// "value" is set to the found value and true is returned.  Otherwise the value of "value" is 
        /// undefined and false is returned.
        /// </summary>
        /// <param name="name">The named value to find.</param>
        /// <param name="value">The output value set to the found value.</param>
        /// <returns>True if the value is found, false otherwise.</returns>
        public bool TryGetValue(string name, out string value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (m_cacheIsInvalid)
            {
                rebuildCache();
            }

            value = null;

            if (m_Settings.ContainsKey(name))
            {
                value = m_Settings[name];
                return true;
            }
            else
            {
                if (m_parent != null)
                {
                    return m_parent.TryGetValue(name, out value);
                }
            }

            return false;
        }

        #endregion
    }
}
