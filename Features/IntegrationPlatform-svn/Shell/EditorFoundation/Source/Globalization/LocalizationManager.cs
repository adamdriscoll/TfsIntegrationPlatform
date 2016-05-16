// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Threading;

namespace Microsoft.TeamFoundation.Migration.Shell.Globalization
{
    /// <summary>
    /// Manages runtime localization selection.
    /// </summary>
    public static class LocalizationManager
    {
        #region Fields
        private static readonly ObservableCollection<CultureInfo> availableCultures;
        private static CultureInfo activeCulture;
        #endregion

        #region Constructors
        static LocalizationManager ()
        {
            //LocalizationManager.availableCultures = new ObservableCollection<CultureInfo> (CultureInfo.GetCultures (CultureTypes.SpecificCultures).Select (cultureInfo => CultureInfo.ReadOnly (cultureInfo))); ** Uncomment when we have C# 3.0 support **
            LocalizationManager.availableCultures = new ObservableCollection<CultureInfo> (new List<CultureInfo> (Enumerable.Select<CultureInfo, CultureInfo> (CultureInfo.GetCultures (CultureTypes.SpecificCultures), delegate (CultureInfo cultureInfo) { return CultureInfo.ReadOnly (cultureInfo); } )));

            if (Properties.Settings.Default.LastCulture == CultureInfo.InvariantCulture)
            {
                LocalizationManager.activeCulture = CultureInfo.CurrentCulture;
            }
            else
            {
                LocalizationManager.activeCulture = Properties.Settings.Default.LastCulture;
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Occurs when the active culture changed.
        /// </summary>
        public static event EventHandler ActiveCultureChanged;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the active culture.
        /// </summary>
        public static CultureInfo ActiveCulture
        {
            get
            {
                return LocalizationManager.activeCulture;
            }
            set
            {
                if (value != LocalizationManager.activeCulture)
                {
                    LocalizationManager.activeCulture = value;

                    Thread thread = Thread.CurrentThread;
                    if (Dispatcher.CurrentDispatcher != null && Dispatcher.CurrentDispatcher.Thread != null)
                    {
                        thread = Dispatcher.CurrentDispatcher.Thread;
                    }

                    thread.CurrentCulture = value;
                    thread.CurrentUICulture = value;

                    Properties.Settings.Default.LastCulture = value;
                    Properties.Settings.Default.Save ();

                    LocalizationManager.RaiseActiveCultureChangedEvent ();
                }
            }
        }

        /// <summary>
        /// Gets the available cultures.
        /// </summary>
        public static IList<CultureInfo> AvailableCultures
        {
            get
            {
                return LocalizationManager.availableCultures;
            }
        }
        #endregion

        #region Private Methods
        private static void RaiseActiveCultureChangedEvent ()
        {
            if (LocalizationManager.ActiveCultureChanged != null)
            {
                LocalizationManager.ActiveCultureChanged (null, EventArgs.Empty);
            }
        }
        #endregion
    }
}
