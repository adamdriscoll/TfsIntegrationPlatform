// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class ConflictResolutionRuleState
    {
        private int m_storageValue;
        private static Dictionary<int, ConflictResolutionRuleState> s_states;

        public static readonly ConflictResolutionRuleState Unknown = new ConflictResolutionRuleState(-1);
        public static readonly ConflictResolutionRuleState Valid = new ConflictResolutionRuleState(0);
        public static readonly ConflictResolutionRuleState Proposed = new ConflictResolutionRuleState(1);
        public static readonly ConflictResolutionRuleState Deprecated = new ConflictResolutionRuleState(2);

        public static ConflictResolutionRuleState GetStateFromStorageValue(int storageValue)
        {
            if (s_states.ContainsKey(storageValue))
            {
                return s_states[storageValue];
            }

            return Unknown;
        }

        public int StorageValue
        {
            get
            {
                return m_storageValue;
            }
        }

        static ConflictResolutionRuleState()
        {
            s_states = new Dictionary<int, ConflictResolutionRuleState>();
            s_states.Add(Unknown.StorageValue, Unknown);
            s_states.Add(Valid.StorageValue, Valid);
            s_states.Add(Proposed.StorageValue, Proposed);
            s_states.Add(Deprecated.StorageValue, Deprecated);
        }

        private ConflictResolutionRuleState()
        {
            Initialize(-1);
        }

        private ConflictResolutionRuleState(int storageValue)
        {
            Initialize(storageValue);
        }

        private void Initialize(int storageValue)
        {
            m_storageValue = storageValue;
        }
    }

    public partial class ConflictResolutionRule
    {
        [XmlIgnore]
        public Guid RuleRefNameGuid
        {
            get
            {
                return new Guid(RuleReferenceName);
            }
        }

        [XmlIgnore]
        public Guid ActionRefNameGuid
        {
            get 
            {
                return new Guid(ActionReferenceName);
            }
        }

        [XmlIgnore]
        public Dictionary<string, string> DataFieldDictionary
        {
            get
            {
                if (null == m_dataFieldDictionary)
                {
                    m_dataFieldDictionary = new Dictionary<string, string>(this.DataField.Length);
                    foreach (DataField df in this.DataField)
                    {
                        m_dataFieldDictionary.Add(df.FieldName, df.FieldValue);
                    }
                }
                return m_dataFieldDictionary;
            }
        }

        [XmlIgnore]
        public int InternalId
        {
            get;
            set;
        }

        private Dictionary<string, string> m_dataFieldDictionary;
    }

    public class ConfliceResolutionRuleSerializer : GenericSerializer<ConflictResolutionRule>
    {
        //public static string Serialize(ConflictResolutionRule rule)
        //{
        //    XmlSerializer serializer = new XmlSerializer(typeof(ConflictResolutionRule));

        //    using (MemoryStream memStrm = new MemoryStream())
        //    {
        //        serializer.Serialize(memStrm, rule);
        //        memStrm.Seek(0, SeekOrigin.Begin);
        //        using (StreamReader sw = new StreamReader(memStrm))
        //        {
        //            return sw.ReadToEnd();
        //        }
        //    }
        //}

        //public static ConflictResolutionRule Deserialize(string ruleBlob)
        //{
        //    if (string.IsNullOrEmpty(ruleBlob))
        //    {
        //        throw new ArgumentNullException("ruleBlob");
        //    }

        //    XmlSerializer serializer = new XmlSerializer(typeof(ConflictResolutionRule));

        //    using (StringReader strReader = new StringReader(ruleBlob))
        //    using (XmlReader xmlReader = XmlReader.Create(strReader))
        //    {
        //        ConflictResolutionRule rule = (ConflictResolutionRule)serializer.Deserialize(xmlReader);

        //        return rule as ConflictResolutionRule;
        //    }
        //}
    }

}
