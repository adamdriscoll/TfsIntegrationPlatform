// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
using System;
using System.Windows;
using System.Windows.Interop;

namespace Microsoft.TeamFoundation.Migration.Shell
{
    //IWin32Window wrapper around a WPF window.   
    public class WindowWrapper : System.Windows.Forms.IWin32Window
    {
        public WindowWrapper(Window window)
        {
            _hWnd = new WindowInteropHelper(window).Handle;
        }

        public IntPtr Handle
        {
            get
            {
                return _hWnd;
            }
        }
        private readonly IntPtr _hWnd;
    }
}

