// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// An interface that defines the contents of a VC label involved in a MigrationAction
    /// </summary>
    public class LabelFromMigrationAction : ILabel
    {
        private string m_name;
        private string m_comment;
        private string m_owner;
        private string m_scope;

        private List<ILabelItem> m_labelItems;

        public LabelFromMigrationAction(IMigrationAction labelMigrationAction)
        {
            LabelProperties labelProperties = new LabelProperties(FileMetadataProperties.CreateFromXmlDocument(labelMigrationAction.MigrationActionDescription));
            m_name = labelProperties[LabelProperties.LabelNameKey];
            m_comment = labelProperties[LabelProperties.LabelCommentKey];
            m_owner = labelProperties[LabelProperties.LabelOwnerKey];
            m_scope = labelProperties[LabelProperties.LabelScopeKey];
            m_labelItems = new List<ILabelItem>();
        }

        /// <summary>
        /// The name of the label (a null or empty value is invalid)
        /// </summary>
        public string Name { get { return m_name; } }

        /// <summary>
        /// The comment associated with the label
        /// It may be null or empty
        /// </summary>
        public string Comment { get { return m_comment; } }

        /// <summary>
        /// The name of the owner (it may be null or empty)
        /// </summary>
        public string OwnerName { get { return m_owner; } }

        /// <summary>
        /// The scope is a server path that defines the namespace for labels in some VC servers
        /// In this case, label names must be unique within the scope, but two or more labels with the
        /// same name may exist as long as their Scopes are distinct.
        /// It may be string.Empty is source from a VC server that does not have the notion of label scopes
        /// </summary>
        public string Scope
        {
            get { return m_scope; }
            set { m_scope = value; }
        }

        /// <summary>
        /// The set of items included in the label
        /// </summary>
        public List<ILabelItem> LabelItems
        { get { return m_labelItems; } }
    }


}
