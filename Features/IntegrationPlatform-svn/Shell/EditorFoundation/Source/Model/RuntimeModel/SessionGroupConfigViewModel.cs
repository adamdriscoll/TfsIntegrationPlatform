// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    public class SessionGroupConfigViewModel : ModelObject
    {
        RTSessionGroupConfig m_sessionGroupConfig;

        public SessionGroupConfigViewModel(RTSessionGroupConfig sessionGroupConfig)
        {
            m_sessionGroupConfig = sessionGroupConfig;
        }

        #region Properties
        // Selected RTSessionGroupConfig properties
        public int Id { get { return m_sessionGroupConfig.Id; } }
        public string FriendlyName { get { return m_sessionGroupConfig.FriendlyName; } }
        public int Status { get { return m_sessionGroupConfig.Status; } }
        public DateTime CreationTime { get { return m_sessionGroupConfig.CreationTime; } }
        public string Creator { get { return m_sessionGroupConfig.Creator; } }
        public DateTime? DeprecationTime { get { return m_sessionGroupConfig.DeprecationTime; } }
        public Guid UniqueId { get { return m_sessionGroupConfig.UniqueId; } }
        public int WorkFlowType { get { return m_sessionGroupConfig.WorkFlowType; } }

        // View properties
        private bool m_isSelected;

        public bool IsSelected
        {
            get 
            { 
                return m_isSelected; 
            }
            set 
            {
                if (m_isSelected != value)
                {
                    m_isSelected = value;
                    this.RaisePropertyChangedEvent("IsSelected", !m_isSelected, m_isSelected);
                }
                
            }
        }
        #endregion
    }
}
