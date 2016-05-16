// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter
{
    /// <summary>
    /// Interaction logic for RetryMultipleTimesControl.xaml
    /// </summary>
    public partial class RetryMultipleTimesControl : UserControl
    {
        public RetryMultipleTimesControl()
        {
            InitializeComponent();
        }
    }

    public class RetryMultipleTimesViewModel
    {
        public int SelectedOption { get; set; }
        private List<int> m_options;
        public List<int> Options
        {
            get
            {
                if (m_options == null)
                {
                    m_options = new List<int>();
                    for (int i = 1; i <= 10; i++)
                    {
                        m_options.Add(i);
                    }

                    SelectedOption = m_options.First();
                }
                return m_options;
            }
        }
    }
}
