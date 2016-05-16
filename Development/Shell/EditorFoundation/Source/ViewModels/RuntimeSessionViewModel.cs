// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.Shell.Model;

namespace Microsoft.TeamFoundation.Migration.Shell.ViewModel
{
    public abstract class RuntimeSessionViewModel : ModelObject
    {
        public RuntimeSessionViewModel()
        {
            m_host = RuntimeManager.GetInstance();
            IRefreshService refresh = (IRefreshService)m_host.GetService(typeof(IRefreshService));
            refresh.AutoRefresh += this.AutoRefresh;
        }

        public virtual void Detach()
        {
            IRefreshService refresh = (IRefreshService)m_host.GetService(typeof(IRefreshService));
            refresh.AutoRefresh -= this.AutoRefresh;
        }

        public void AutoRefresh(object sender, EventArgs e)
        {
            Refresh();
        }

        public virtual void Refresh()
        {
        }

        public abstract SessionViewModel Session { get; set; }

        public string FriendlyName
        {
            get
            {
                return Session.FriendlyName;
            }
        }

        protected RuntimeManager m_host;
    }
}
