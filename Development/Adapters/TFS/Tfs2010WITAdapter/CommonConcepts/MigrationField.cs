// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    /// <summary>
    /// Field class; a name + value pair. Used to represent field's value within a revision.
    /// </summary>
    public sealed class MigrationField
    {
        private string m_name;                              // Field name
        private object m_value;                             // Value

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Field name</param>
        /// <param name="value">Field value</param>
        public MigrationField(
            string name,
            object value)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }
            m_name = name;
            m_value = value;
        }

        /// <summary>
        /// Gets field name.
        /// </summary>
        public string Name { get { return m_name; } }

        /// <summary>
        /// Gets field value.
        /// </summary>
        public object Value { get { return m_value; } }
    }
}
