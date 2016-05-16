// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    internal class TfsWITRecordDetails
    {
        private XmlDocument m_doc;
        private XmlNode m_columnsNode;
        private XmlElement m_testNode;

        public TfsWITRecordDetails(WorkItem tfsWorkItem)
        {
            m_doc = new XmlDocument();
            CreateLastRevHeader(tfsWorkItem);
            ExtractLastRevFieldDetails(tfsWorkItem);
        }

        public TfsWITRecordDetails(WorkItem tfsWorkItem, int revision)
        {
            m_doc = new XmlDocument();
            CreateHeader(tfsWorkItem, revision);
            ExtractFieldDetails(tfsWorkItem, revision, true);
        }

        public XmlDocument DetailsDocument
        {
            get 
            { 
                return m_doc; 
            }
        }

        private void CreateLastRevHeader(WorkItem tfsWorkItem)
        {
            CreateHeader(tfsWorkItem, tfsWorkItem.Revision);
        }

        private void CreateHeader(WorkItem tfsWorkItem, int revision)
        {
            IsRevisionValid(tfsWorkItem, revision);

            XmlElement root = m_doc.CreateElement("WorkItemChanges");
            root.SetAttribute("WorkItemID", tfsWorkItem.Id.ToString(CultureInfo.InvariantCulture));
            Revision rev = tfsWorkItem.Revisions[revision - 1];
            int revIndex = (int)rev.Fields[CoreField.Rev].Value;
            root.SetAttribute("Revision", XmlConvert.ToString(revIndex));
            root.SetAttribute("WorkItemType", tfsWorkItem.Type.Name);
            root.SetAttribute("Author", tfsWorkItem.CreatedBy);
            root.SetAttribute("ChangeDate", XmlConvert.ToString(tfsWorkItem.ChangedDate, XmlDateTimeSerializationMode.Unspecified));

            m_doc.AppendChild(root);

            XmlElement cs = m_doc.CreateElement("Columns");
            root.AppendChild(cs);
        }
        
        private void ExtractLastRevFieldDetails(WorkItem tfsWorkItem)
        {
            ExtractFieldDetails(tfsWorkItem, tfsWorkItem.Revision, false);
        }

        private void ExtractFieldDetails(WorkItem tfsWorkItem, int revision, bool useDeltaComputeFieldSkipLogic)
        {
            IsRevisionValid(tfsWorkItem, revision);

            Revision rev = tfsWorkItem.Revisions[revision - 1];
            foreach (Field field in rev.Fields)
            {
                if (useDeltaComputeFieldSkipLogic)
                {
                    if (TfsMigrationWorkItem.MustTakeTfsField(field.FieldDefinition))
                    {
                        AddField(field.ReferenceName, field.Name, field.FieldDefinition.FieldType.ToString(), (field.Value == null) ? string.Empty : field.Value.ToString());
                    }
                }
                else
                {
                    if (!field.IsComputed ||
                        string.Equals(field.ReferenceName, "System.AreaPath", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(field.ReferenceName, "System.IterationPath", StringComparison.OrdinalIgnoreCase))
                    {
                        AddField(field.ReferenceName, field.Name, field.FieldDefinition.FieldType.ToString(), (field.Value == null) ? string.Empty : field.Value.ToString());
                    }
                }
            }
        }

        private static void IsRevisionValid(WorkItem tfsWorkItem, int revision)
        {
            if (revision <= 0 || revision > tfsWorkItem.Revision)
            {
                throw new ArgumentOutOfRangeException("revision");
            }
        }

        private void AddField(
            string fieldReferenceName,
            string fieldDispName,
            string fieldType,
            string fieldValue)
        {
            XmlElement c = m_doc.CreateElement("Column");
            c.SetAttribute("ReferenceName", fieldReferenceName);
            c.SetAttribute("DisplayName", fieldDispName);
            c.SetAttribute("Type", fieldType);
            XmlElement v = m_doc.CreateElement("Value");
            v.InnerText = NormalizeString(fieldValue);
            c.AppendChild(v);
            ColumnsNode.AppendChild(c);
        }

        private string NormalizeString(string valueString)
        {
            if (string.IsNullOrEmpty(valueString))
            {
                return string.Empty;
            }

            TestNode.InnerText = valueString;

            return TestNode.InnerXml;
        }

        private XmlElement TestNode
        {
            get
            {
                if (null == m_testNode)
                {
                    XmlDocument doc = new XmlDocument();
                    m_testNode = doc.CreateElement("test");
                }

                return m_testNode;
            }
        }

        private XmlNode ColumnsNode
        {
            get
            {
                if (null == m_columnsNode)
                {
                    m_columnsNode = m_doc.SelectSingleNode("/WorkItemChanges/Columns");
                }
                return m_columnsNode;
            }
        }
    }
}
