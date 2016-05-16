// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
using System.Windows.Controls;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Interaction logic for HeadlineControl.xaml
    /// </summary>
    public partial class HeadlineControl : UserControl
    {
        public HeadlineControl()
        {
            InitializeComponent();
            refreshLink.Command = ShellCommands.Refresh;
        }
    }
}
