// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.TeamFoundation.Migration.Shell.ConflictManagement
{
    public interface IConflictTypeUserControl
    {
        /// <summary>
        /// Get the UserControl.
        /// </summary>
        UserControl UserControl { get; }

        /// <summary>
        /// Return a lookup of details.
        /// </summary>
        Dictionary<string, FrameworkElement> Details { get; }

        /// <summary>
        /// Get the ConflictRuleViewModel ready to be saved.
        /// </summary>
        void Save();

        /// <summary>
        /// Set the ConflictRuleViewModel which the UserControl draws its details from.
        /// </summary>
        /// <param name="viewModel"></param>
        void SetConflictRuleViewModel(ConflictRuleViewModel viewModel);
    }
}
