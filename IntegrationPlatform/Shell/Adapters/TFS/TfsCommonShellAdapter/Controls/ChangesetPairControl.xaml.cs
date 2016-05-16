// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter
{
    /// <summary>
    /// Interaction logic for ChangesetPairControl.xaml
    /// </summary>
    public partial class ChangesetPairControl : UserControl
    {
        public ChangesetPairControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// An event handler for hyperlink navigate request.
        /// </summary>
        private void OnHyperlinkRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ApplicationCommands.Help.Execute(null, this);
        }
    }

    public class ChangesetPairControlViewModel
    {
        public string SourceID { get; set; }
        public string TargetID { get; set; }
    }
}
