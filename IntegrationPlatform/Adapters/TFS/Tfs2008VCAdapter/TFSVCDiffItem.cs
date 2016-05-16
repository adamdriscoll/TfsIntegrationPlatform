// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2008VCAdapter
{

    /// <summary>
    /// Encapsulates just the information need to perform a diff on a VC item
    /// </summary>
    [Serializable]
    public sealed class TfsVCDiffItem : IVCDiffItem
    {
        private int m_treeLevel;
        private List<IVCDiffItem> m_subItems;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="item">The TFS version control Item object that this DiffItem represents</param>
        /// <param name="treeLevel">The level in the tree heirarchy (defined by a filter pair string) at which this item resides</param>
        public TfsVCDiffItem(Item item, int treeLevel)
        {
            ServerPath = item.ServerItem;
            m_treeLevel = treeLevel;

            HashValue = item.HashValue;
            switch (item.ItemType)
            {
                case ItemType.Folder:
                    this.VCItemType = VCItemType.Folder;
                    break;

                case ItemType.File:
                    this.VCItemType = VCItemType.File;
                    break;

                default:
                    throw new ArgumentException("Unexpected value for item.ItemType");
            }
        }

        internal List<IVCDiffItem> SubItems
        {
            get
            {
                if (m_subItems == null)
                {
                    m_subItems = new List<IVCDiffItem>();
                }
                return m_subItems;
            }
            set
            {
                m_subItems = value;
            }
        }

        internal int TreeLevel
        {
            get { return m_treeLevel; }
        }

        /// <summary>
        /// The path by which the version control item is known on it server
        /// </summary>
        public string ServerPath
        {
            get;
            set;
        }

        /// <summary>
        /// An MD5 hash value for the file
        /// </summary>
        public byte[] HashValue
        {
            get;
            set;
        }

        /// <summary>
        /// Identifies the type of version control item (file or folder)
        /// </summary>
        public VCItemType VCItemType
        {
            get;
            set;
        }

        public bool IsSubItemOf(string serverFolderPath)
        {
            return VersionControlPath.IsSubItem(ServerPath, serverFolderPath);
        }
    }
}
