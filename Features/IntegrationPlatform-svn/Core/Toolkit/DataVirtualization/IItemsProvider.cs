// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal interface IItemsProvider<T>
    {
        /// <summary>
        /// Returns the total number of items available.
        /// </summary>
        /// <returns></returns>
        int Count();

        /// <summary>
        /// Loads a range of items.
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        IList<T> LoadPage(int startIndex, int count);

        /// <summary>
        /// Store a NEW page to the backing store (if necessary)
        /// </summary>
        /// <param name="page"></param>
        void StorePage(int startIndex, IList<T> page);
    }
}
