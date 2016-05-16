// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    /// <summary>
    /// Wrapper on a Migration Toolkit ResolutionAction.
    /// </summary>
    public class ResolutionActionViewModel : ModelObject
    {
        ResolutionAction m_resolutionAction;

        public ResolutionActionViewModel(ResolutionAction resolutionAction)
        {
            m_resolutionAction = resolutionAction;
        }

        #region Properties
        public string FriendlyName { get { return m_resolutionAction.FriendlyName; } }
        public Guid ReferenceName { get { return m_resolutionAction.ReferenceName; } }

        // TODO: This name/value pattern is awkward.  We need the UI to inform the interface here.
        // Read/write view on values
        /// <summary>
        /// A collection of pipeline stage name and count pairs.
        /// </summary>
        private ObservableCollection<KeyValuePair<string, string>> m_actionDataKeyValuePairs;

        public ObservableCollection<KeyValuePair<string, string>> ActionDataKeyValuePairs
        {
            get
            {
                if (m_actionDataKeyValuePairs == null)
                {
                    m_actionDataKeyValuePairs = new ObservableCollection<KeyValuePair<string, string>>();

                    foreach (string key in m_resolutionAction.ActionDataKeys)
                    {
                        m_actionDataKeyValuePairs.Add(new KeyValuePair<string,string>(key, ""));
                    }
                }
                return m_actionDataKeyValuePairs;
            }
            set
            {
                m_actionDataKeyValuePairs = value;
            }
        }
        #endregion
    }
}
