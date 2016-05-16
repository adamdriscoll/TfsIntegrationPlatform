// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace Microsoft.TeamFoundation.Migration.Shell.Globalization
{
    /// <summary>
    /// Provides a <see cref="System.Windows.Data.CollectionView" /> that is synchronized with the <see cref="LocalizationManager"/>.
    /// </summary>
    public class LocalizationCollectionView : ListCollectionView
    {
        #region Fields
        private static readonly LocalizationCollectionView defaultInstance = new LocalizationCollectionView ();
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizationCollectionView"/> class.
        /// </summary>
        public LocalizationCollectionView ()
            : base ((IList)LocalizationManager.AvailableCultures)
        {
            // Set the current culture
            this.MoveCurrentTo (LocalizationManager.ActiveCulture);

            // When the LocalizationManager's active culture changes, update the CollectionView's current item
            LocalizationManager.ActiveCultureChanged += delegate
            {
                if (this.CurrentItem != LocalizationManager.ActiveCulture)
                {
                    this.MoveCurrentTo (LocalizationManager.ActiveCulture);
                }

                this.Refresh ();
            };

            // When the CollectionView's current item changes, update the LocalizationManager's active culture
            this.CurrentChanged += delegate
            {
                if (LocalizationManager.ActiveCulture != this.CurrentItem)
                {
                    LocalizationManager.ActiveCulture = (CultureInfo)this.CurrentItem;
                }
            };

            // Sort the cultures alphabetically by display name
            this.SortDescriptions.Add (new SortDescription ("DisplayName", ListSortDirection.Ascending));
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the default <see cref="LocalizationCollectionView" /> instance.
        /// </summary>
        public static LocalizationCollectionView Default
        {
            get
            {
                return LocalizationCollectionView.defaultInstance;
            }
        }
        #endregion
    }
}
