// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal struct WorkItemField
    {
        private string m_fieldName;
        private string m_fieldValue;

        public WorkItemField(string name, string value)
        {
            m_fieldName = name;
            m_fieldValue = value;
        }

        public string FieldName
        {
            get { return m_fieldName; }
        }

        public string FieldValue
        {
            get { return m_fieldValue; }
        }

        public static ReadOnlyCollection<WorkItemField> ExtractFieldChangeDetails(
            XmlDocument changeDocument)
        {
            XmlElement rootNode = changeDocument.DocumentElement;
            if (null == rootNode)
            {
                throw new InvalidOperationException();
            }

            XmlNodeList columns = rootNode.SelectNodes("/WorkItemChanges/Columns/Column");
            if (null == columns)
            {
                throw new InvalidOperationException();
            }

            List<WorkItemField> revisedFields = new List<WorkItemField>();
            foreach (XmlNode col in columns)
            {
                string fieldRefName = col.Attributes["ReferenceName"].Value;
                Debug.Assert(!string.IsNullOrEmpty(fieldRefName), "Column ReferenceName is empty in the update document");
                string fieldValue = col.FirstChild.InnerText ?? string.Empty;
                revisedFields.Add(new WorkItemField(fieldRefName, fieldValue));
            }

            return revisedFields.AsReadOnly();
        }
    }
}
