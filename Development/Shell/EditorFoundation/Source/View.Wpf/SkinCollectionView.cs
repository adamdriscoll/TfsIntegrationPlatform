// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections;
using System.ComponentModel;
using System.Windows.Data;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Provides a <see cref="System.Windows.Data.CollectionView" /> that is synchronized with the <see cref="SkinManager"/>.
    /// </summary>
    public class SkinCollectionView : ListCollectionView
    {
        private readonly static SkinCollectionView defaultInstance = new SkinCollectionView ();

        /// <summary>
        /// Initializes a new instance of the <see cref="SkinCollectionView"/> class.
        /// </summary>
        public SkinCollectionView ()
            : base ((IList)SkinManager.AvailableSkins)
        {
            // Set the current skin
            this.MoveCurrentTo (SkinManager.ActiveSkin);

            // When the SkinManager's active skin changes, update the CollectionView's current item
            SkinManager.ActiveSkinChanged += delegate
            {
                if (this.CurrentItem != SkinManager.ActiveSkin)
                {
                    this.MoveCurrentTo (SkinManager.ActiveSkin);
                }
            };

            // When the CollectionView's current item changes, update the SkinManager's active skin
            this.CurrentChanged += delegate
            {
                if (SkinManager.ActiveSkin != this.CurrentItem)
                {
                    SkinManager.SetActiveSkin ((Skin)this.CurrentItem);
                }
            };

            // Sort alphabetically by skin name
            this.SortDescriptions.Add (new SortDescription ("Name", ListSortDirection.Ascending));
        }

        /// <summary>
        /// Gets the default <see cref="SkinCollectionView" /> instance.
        /// </summary>
        public static SkinCollectionView Default
        {
            get
            {
                return SkinCollectionView.defaultInstance;
            }
        }
    }
}
