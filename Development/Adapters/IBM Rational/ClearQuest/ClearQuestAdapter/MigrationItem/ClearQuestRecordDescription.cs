// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.MigrationItem
{
    internal class ClearQuestRecordDescription
    {
        private XmlDocument m_doc;
        private XmlNode m_columnsNode;
        private XmlElement m_testNode;

        /*
\' - single quote, needed for character literals 
\" - double quote, needed for string literals 
\\ - backslash 
\0 - Unicode character 0 
\a - Alert (character 7) 
\b - Backspace (character 8) 
\f - Form feed (character 12) 
\n - New line (character 10) 
\r - Carriage return (character 13) 
\t - Horizontal tab (character 9) 
\v - Vertical quote (character 11) 
\uxxxx - Unicode escape sequence for character with hex value xxxx 
\xn[n][n][n] - Unicode escape sequence for character with hex value nnnn (variable length version of \uxxxx) 
\Uxxxxxxxx - Unicode escape sequence for character with hex value xxxxxxxx (for generating surrogates)  
         */
        private static char[] m_escapeChars = new char[] { '\0', '\a', '\f', '\v' }; // we are skipping these four

        public ClearQuestRecordDescription()
        {
            m_doc = new XmlDocument();
        }

        public ClearQuestRecordDescription(XmlDocument doc)
        {
            if (null == doc)
            {
                throw new ArgumentNullException("doc");
            }
            
            m_doc = doc;
        }

        public XmlDocument DescriptionDocument
        {
            get 
            { 
                return m_doc; 
            }
        }

        public void CreateHeader(
            string author,
            DateTime changedDate,
            string entityMigrationItemId,
            string entityDefName,
            string revision,
            bool isLastRevOfThisSyncCycle)
        {
            XmlElement root = m_doc.CreateElement("WorkItemChanges");
            root.SetAttribute("WorkItemID", entityMigrationItemId);
            root.SetAttribute("Revision", revision); // CQ record content has flat history
            root.SetAttribute("WorkItemType", entityDefName);
            root.SetAttribute("Author", author);
            root.SetAttribute(Constants.WITAuthorUserIdPropertyName, "Alias");
            root.SetAttribute("ChangeDate", XmlConvert.ToString(changedDate, XmlDateTimeSerializationMode.Unspecified));
            if (isLastRevOfThisSyncCycle)
            {
                root.SetAttribute(Constants.WitLastRevOfThisSyncCycleAttributeName, XmlConvert.ToString(true));
            }
            m_doc.AppendChild(root);

            XmlElement cs = m_doc.CreateElement("Columns");
            root.AppendChild(cs);
        }

        public void AddField(
            string fieldDispName,
            string fieldType,
            string fieldValue)
        {
            XmlElement c = m_doc.CreateElement("Column");
            c.SetAttribute("DisplayName", fieldDispName);
            c.SetAttribute("ReferenceName", fieldDispName);
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
