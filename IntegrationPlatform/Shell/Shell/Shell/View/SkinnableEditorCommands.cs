// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Microsoft.TeamFoundation.Migration.Shell
{
    public static class SkinnableEditorCommands
    {
        private static readonly RoutedUICommand addProvider = new RoutedUICommand("Add Provider", "Add Provider", typeof(SkinnableEditorCommands));

        public static RoutedUICommand AddProvider
        {
            get
            {
                return SkinnableEditorCommands.addProvider;
            }
        }
    }
}
