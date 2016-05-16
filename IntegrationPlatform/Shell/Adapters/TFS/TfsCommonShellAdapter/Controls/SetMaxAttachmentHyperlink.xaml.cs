// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter
{
    /// <summary>
    /// Interaction logic for SetMaxAttachmentHyperlink.xaml
    /// </summary>
    public partial class SetMaxAttachmentHyperlink : UserControl
    {
        public SetMaxAttachmentHyperlink()
        {
            InitializeComponent();
        }

        /// <summary>
        /// An event handler for hyperlink navigate request.
        /// </summary>
        private void OnHyperlinkRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            String url = e.Uri.AbsoluteUri;
            try
            {
                OpenHyperlink(url);
            }
            catch (Exception ex)
            {
                Utilities.HandleException(ex);
            }
        }

        private void OpenHyperlink(string url)
        {
            try
            {
                using (Process.Start(url))
                {
                }
            }
            catch (Win32Exception)
            {
                using (Process.Start("iexplore", url))
                {
                }
            }
        }
    }
}
