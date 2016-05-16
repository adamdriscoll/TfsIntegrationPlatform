// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class MappingEntry
    {
        string m_path;
        bool m_cloak;
        string m_snapshotStartPoint;
        string m_peerSnapshotStartPoint;
        string m_mergeScope;

        /// <summary>
        /// Constructor to create a version control mapping entry. 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="cloak"></param>
        /// <param name="snapshotStartPoint"></param>
        /// <param name="peerSnapshotStartPoint"></param>
        /// <param name="mergeScope"></param>
        public MappingEntry(string path, bool cloak, string snapshotStartPoint, string peerSnapshotStartPoint, string mergeScope)
        {
            m_path = path;
            m_cloak = cloak;
            m_snapshotStartPoint = snapshotStartPoint;
            m_peerSnapshotStartPoint = peerSnapshotStartPoint;
            m_mergeScope = mergeScope;
        }

        /// <summary>
        /// The path of the mapping entry.
        /// </summary>
        public string Path
        {
            get
            {
                return m_path;
            }
        }

        /// <summary>
        /// Specify whether the path is cloaked in the mapping or not. 
        /// </summary>
        public bool Cloak
        {
            get
            {
                return m_cloak;
            }
        }

        /// <summary>
        /// The snapshot start point for the mapping path. 
        /// </summary>
        public string SnapshotStartPoint
        {
            get
            {
                return m_snapshotStartPoint;
            }
        }

        /// <summary>
        /// The peer snapshot start point for the mapping path.
        /// </summary>
        public string PeerSnapshotStartPoint
        {
            get
            {
                return m_peerSnapshotStartPoint;
            }
        }

        /// <summary>
        /// The scope for history integration commands s.a. branch and merge
        /// For branch|merge changes that are from paths outside the scope. 
        /// We will skip the history integration. E.g. change to add on branch.
        /// </summary>
        public string MergeScope
        {
            get
            {
                return m_mergeScope;
            }
        }

    }
}
