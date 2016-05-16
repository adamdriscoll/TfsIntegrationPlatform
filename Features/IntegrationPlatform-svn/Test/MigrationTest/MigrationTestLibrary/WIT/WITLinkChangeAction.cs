// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;

namespace MigrationTestLibrary
{
    public enum LinkChangeActionType
    {
        Add,
        Delete,
        Edit
    }

    public class WITLinkChangeAction
    {
        private List<WITLink> m_links = new List<WITLink>();

        public WITLinkChangeAction(LinkChangeActionType actionType)
        {
            ActionType = actionType;
        }

        public LinkChangeActionType ActionType { get; set; }

        public List<WITLink> Links
        {
            get
            {
                return m_links;
            }
        }

        public void AddLink(WITLink link)
        {
            m_links.Add(link);
        }
    }
}
