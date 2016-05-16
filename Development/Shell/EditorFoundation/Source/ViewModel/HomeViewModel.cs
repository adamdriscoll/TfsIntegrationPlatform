// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
using Microsoft.TeamFoundation.Migration.Shell.ViewModel;
namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    public class HomeViewModel : ViewModelBase
    {
        public HomeViewModel(ShellViewModel shellVM)
        {
            ShellViewModel = shellVM;
        }

        public ShellViewModel ShellViewModel { get; set; }
    }

}
