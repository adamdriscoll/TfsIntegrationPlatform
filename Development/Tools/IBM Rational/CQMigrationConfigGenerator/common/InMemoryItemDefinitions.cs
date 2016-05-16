// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// Description: Helper Classes for migrating work item into Currituck

#region Using directives

using System;
using System.Collections;
using System.Text;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

#endregion

namespace Microsoft.TeamFoundation.Converters.WorkItemTracking.Common
{
    // Internal
    /// <summary>
    /// In Memory Attachment representation class
    /// </summary>
    public class InMemoryAttachment
    {
        public InMemoryAttachment(string Name, bool isLinked)
        {
            m_fileName = Name;
            m_isLinkedFile = isLinked;
            m_comment = String.Empty;
        }
        public InMemoryAttachment(string Name, string comment, bool isLinked)
            :
            this(Name, isLinked)
        {
            if (comment == null)
            {
                m_comment = String.Empty;
            }
            else
            {
                m_comment = comment;
            }
        }
        public string FileName
        {
            get { return (m_fileName); }
            set { m_fileName = value; }
        }
        public bool IsLinkedFile
        {
            get { return (m_isLinkedFile); }
        }
        public string Comment
        {
            get { return (m_comment); }
        }

        private string m_fileName;
        private bool m_isLinkedFile;
        private string m_comment;
    }

    // Public
    /// <remarks>
    /// In memory history item representation class
    /// </remarks>
    public class InMemoryHistoryItem
    {
        public Hashtable UpdatedView = new Hashtable();
    }

    /// <remarks>
    /// In memory representation for link item
    /// This is the base class for all types of links that 
    /// will be migrated to VSTS system
    /// </remarks>
    public class InMemoryLinkItem
    {
        public InMemoryLinkItem(int id, string description)
        {
            m_currituckLinkedId = id;
            m_linkDescription = description;
        }
        public int CurrituckLinkedId
        {
            get { return (m_currituckLinkedId); }
        }
        public string LinkDescription
        {
            get { return (m_linkDescription); }
        }

        private int m_currituckLinkedId;
        private string m_linkDescription;
    }

    /// <remarks>
    /// This is the related link item instance in memory
    /// Inherited from base InMemoryLinkItem class
    /// </remarks>
    public class InMemoryRelatedLinkItem : InMemoryLinkItem
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id"></param>
        /// <param name="description"></param>
        public InMemoryRelatedLinkItem(int id, string description)
            : base(id, description)
        {
        }
    }

    /// <remarks>
    /// Dependent link item class inherited from base Link Item class
    /// </remarks>
    public class InMemoryDependentLinkItem : InMemoryLinkItem
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id"></param>
        /// <param name="description"></param>
        public InMemoryDependentLinkItem(int id, bool isParent, string description)
            : base(id, description)
        {
            m_isParent = isParent;
        }

		private bool m_isParent;
    }

    /// <remarks>
    /// Dependent link item class inherited from base Link Item class
    /// </remarks>
    public class InMemoryDuplicateLinkItem : InMemoryLinkItem
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id"></param>
        /// <param name="description"></param>
        public InMemoryDuplicateLinkItem(int id, bool isParent, string description)
            : base(id, description)
        {
            m_isParent = isParent;
        }

		private bool m_isParent;
    }

    /// <remarks>
    /// In memory work item representation
    /// </remarks>
    public class InMemoryWorkItem
    {
        public Hashtable InitialView = new Hashtable();
        public ArrayList Attachments = new ArrayList();
        public ArrayList HistoryItems = new ArrayList();
        public ArrayList Links = new ArrayList();
    }
}
