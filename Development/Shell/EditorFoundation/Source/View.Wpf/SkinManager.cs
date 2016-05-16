// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Manages runtime skin selection.
    /// </summary>
    public static class SkinManager
    {
        #region Fields
        private static readonly ObservableCollection<Skin> availableSkins;
        private static Skin activeSkin;
        #endregion

        #region Constructors
        static SkinManager ()
        {
            SkinManager.availableSkins = new ObservableCollection<Skin> ();
            SkinManager.availableSkins.CollectionChanged += SkinManager.OnAvailableSkinsChanged;
        }
        #endregion

        #region Events
        /// <summary>
        /// Occurs when the <see cref="SkinManager.ActiveSkin" /> changes.
        /// </summary>
        public static event EventHandler ActiveSkinChanged;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the active skin.
        /// </summary>
        /// <value>The active skin.</value>
        public static Skin ActiveSkin
        {
            get
            {
                return SkinManager.activeSkin;
            }
        }

        /// <summary>
        /// Gets the available skins.
        /// </summary>
        /// <value>The available skins.</value>
        public static IList<Skin> AvailableSkins
        {
            get
            {
                return SkinManager.availableSkins;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets the active skin by name.
        /// </summary>
        /// <param name="skinName">Name of the skin.</param>
        public static void SetActiveSkin (string skinName)
        {
            Skin skin = SkinManager.FindSkinByName (skinName);
            if (skin == null)
            {
                throw new Exception (string.Format ("{0} is not an available skin.", skinName));
            }

            SkinManager.SetActiveSkin (skin);
        }

        /// <summary>
        /// Sets the active skin.
        /// </summary>
        /// <param name="skin">The skin.</param>
        public static void SetActiveSkin (Skin skin)
        {
            if (skin != SkinManager.activeSkin)
            {
                // Unload the current skin if necessary
                if (SkinManager.activeSkin != null)
                {
                    Application.Current.Resources.MergedDictionaries.Remove (SkinManager.activeSkin.Resources);
                }

                // Set the active skin
                SkinManager.activeSkin = skin;

                // Load the skin if necessary
                if (SkinManager.activeSkin != null)
                {
                    // If the SkinManager doesn't already know about this skin, add it.
                    if (!SkinManager.availableSkins.Contains (SkinManager.activeSkin))
                    {
                        SkinManager.availableSkins.Add (SkinManager.activeSkin);
                    }

                    // Load the skin
                    Application.Current.Resources.MergedDictionaries.Add (SkinManager.activeSkin.Resources);

                    // Save the name of the skin so it can potentially
                    // be loaded automatically the next time the app starts
                    Properties.Settings.Default.LastSkin = skin.Name;
                    Properties.Settings.Default.Save ();
                }

                // Notify listeners that the active skin has changed
                SkinManager.RaiseActiveSkinChangedEvent ();
            }
        }

        /// <summary>
        /// Sets the active skin to the last skin used.
        /// </summary>
        /// <returns><c>true</c> if the active skin was successfully set; otherwise, <c>false</c>.</returns>
        public static bool SetActiveSkinToLast ()
        {
            return SkinManager.SetActiveSkinToLast ((Skin)null);
        }

        /// <summary>
        /// Sets the active skin to the last skin used if possible,
        /// otherwise sets the active skin to the specified skin.
        /// </summary>
        /// <param name="fallbackSkinName">Name of the fallback skin.</param>
        /// <returns><c>true</c> if the active skin was successfully set; otherwise, <c>false</c>.</returns>
        public static bool SetActiveSkinToLast (string fallbackSkinName)
        {
            Skin fallbackSkin = SkinManager.FindSkinByName (fallbackSkinName);
            if (fallbackSkin == null)
            {
                throw new Exception (string.Format ("{0} is not an available skin.", fallbackSkinName));
            }

            return SkinManager.SetActiveSkinToLast (fallbackSkin);
        }

        /// <summary>
        /// Sets the active skin to the last skin used if possible,
        /// otherwise sets the active skin to the specified skin.
        /// </summary>
        /// <param name="fallbackSkin">The fallback skin.</param>
        /// <returns><c>true</c> if the active skin was successfully set; otherwise, <c>false</c>.</returns>
        public static bool SetActiveSkinToLast (Skin fallbackSkin)
        {
            Skin lastSkin = SkinManager.FindSkinByName (Properties.Settings.Default.LastSkin);
            if (lastSkin != null)
            {
                SkinManager.SetActiveSkin (lastSkin);
            }
            else if (fallbackSkin != null)
            {
                SkinManager.SetActiveSkin (fallbackSkin);
            }
            else
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Loads skins from the directories specified in the application configuration file.
        /// </summary>
        public static void LoadSkins ()
        {
            SkinManager.LoadSkins (SkinManager.GetSkinDirectoriesFromConfig ());
        }

        /// <summary>
        /// Loads skins from the specified directories.
        /// </summary>
        /// <param name="probingDirectories">The directories from which to load skins.</param>
        public static void LoadSkins (params DirectoryInfo[] probingDirectories)
        {
            if (probingDirectories != null)
            {
                foreach (DirectoryInfo directory in probingDirectories)
                {
                    if (directory.Exists)
                    {
                        // Look for xaml files
                        SkinManager.LoadSkins (false, directory.GetFiles ("*.xaml"));

                        // TODO: Look for baml files?
                    }
                }
            }
        }

        /// <summary>
        /// Loads skins from the specified files.
        /// </summary>
        /// <param name="skinFiles">The skin files.</param>
        public static void LoadSkins (params FileInfo[] skinFiles)
        {
            SkinManager.LoadSkins (true, skinFiles);
        }
        #endregion

        #region Private Methods
        private static DirectoryInfo[] GetSkinDirectoriesFromConfig ()
        {
            List<DirectoryInfo> skinDirectories = new List<DirectoryInfo> (Properties.Settings.Default.SkinDirectories.Count);
            foreach (string skinDirectory in Properties.Settings.Default.SkinDirectories)
            {
                // Expand environment variables
                string resolvedSkinDirectory = Environment.ExpandEnvironmentVariables (skinDirectory);

                // If the directory is not rooted, make it relative to the application path
                if (!Path.IsPathRooted (resolvedSkinDirectory))
                {
                    Assembly entryAssembly = Assembly.GetEntryAssembly ();
                    if (entryAssembly != null)
                    {
                        resolvedSkinDirectory = Path.Combine (Path.GetDirectoryName (entryAssembly.Location), resolvedSkinDirectory);
                    }
                }

                // Add the directory to the running list
                skinDirectories.Add (new DirectoryInfo (resolvedSkinDirectory));
            }

            return skinDirectories.ToArray ();
        }

        /// <summary>
        /// Loads skins from the specified files.
        /// </summary>
        /// <param name="throwOnError">if set to <c>true</c> throw an exception if a skin fails to load.</param>
        /// <param name="skinFiles">The skin files.</param>
        private static void LoadSkins (bool throwOnError, params FileInfo[] skinFiles)
        {
            if (skinFiles != null)
            {
                foreach (FileInfo skinFile in skinFiles)
                {
                    try
                    {
                        SkinManager.availableSkins.Add (Skin.Load (skinFile.FullName));
                    }
                    catch (Exception exception)
                    {
                        if (throwOnError)
                        {
                            throw exception;
                        }
                        else
                        {
                            Utilities.DefaultTraceSource.TraceEvent (TraceEventType.Warning, 0, "Couldn't load {0} as skin.", skinFile.FullName);
                        }
                    }
                }
            }
        }

        private static Skin FindSkinByName (string skinName)
        {
            // Linear search - number of skins will almost certainly be small, so this is fine.
            // C# 3.0 - Use linq
            foreach (Skin skin in SkinManager.availableSkins)
            {
                if (skin.Name == skinName)
                {
                    return skin;
                }
            }

            return null;
        }

        private static void OnAvailableSkinsChanged (object sender, NotifyCollectionChangedEventArgs e)
        {
            // If the active skin was removed, then unload it.
            if (SkinManager.activeSkin != null)
            {
                foreach (Skin skin in e.OldItems)
                {
                    if (SkinManager.activeSkin == skin)
                    {
                        SkinManager.SetActiveSkin ((Skin)null);
                        break;
                    }
                }
            }
        }

        private static void RaiseActiveSkinChangedEvent ()
        {
            if (SkinManager.ActiveSkinChanged != null)
            {
                SkinManager.ActiveSkinChanged (null, EventArgs.Empty);
            }
        }
        #endregion
    }
}
