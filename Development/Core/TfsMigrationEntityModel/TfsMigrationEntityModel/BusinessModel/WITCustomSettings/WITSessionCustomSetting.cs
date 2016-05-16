// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Microsoft.TeamFoundation.Migration.BusinessModel.WIT
{
    public partial class WITSessionCustomSetting
    {
        private Dictionary<SourceSideTypeEnum, List<string>> m_perSideReferencedFieldNames 
            = new Dictionary<SourceSideTypeEnum, List<string>>();

        [XmlIgnore]
        public Session SessionConfig
        {
            get;
            set;
        }

        /// <summary>
        /// Update the parent session configuration with changes to this custom setting
        /// </summary>
        public void Update()
        {
            if (null != SessionConfig)
            {
                SessionConfig.UpdateCustomSetting(this);
            }
            else
            {
                throw new InvalidOperationException("WITSessionCustomSetting does not have an associated parent Session configuration. Update failed.");
            }
        }

        public List<string> GetReferencedFieldReferenceNames(SourceSideTypeEnum sourceSide)
        {
            if (!m_perSideReferencedFieldNames.ContainsKey(sourceSide))
            {
                List<string> referencedFields = new List<string>();
                foreach (var fieldMap in this.FieldMaps.FieldMap)
                {
                    foreach (var aggregationGroup in fieldMap.AggregatedFields.FieldsAggregationGroup)
                    {
                        if (aggregationGroup.MapFromSide == sourceSide)
                        {
                            foreach (var f in aggregationGroup.SourceField)
                            {
                                if (!referencedFields.Contains(f.SourceFieldName, StringComparer.OrdinalIgnoreCase))
                                {
                                    referencedFields.Add(f.SourceFieldName);
                                }
                            }
                        }
                    }
                }

                foreach (var valueMap in this.ValueMaps.ValueMap)
                {
                    foreach (var mappedValue in valueMap.Value)
                    {
                        if (mappedValue.When != null && mappedValue.When.ConditionalSrcFieldName != null
                            && !mappedValue.When.ConditionalSrcFieldName.Equals(WitMappingConfigVocab.Any)
                            && !referencedFields.Contains(mappedValue.When.ConditionalSrcFieldName, StringComparer.OrdinalIgnoreCase))
                        {
                            referencedFields.Add(mappedValue.When.ConditionalSrcFieldName);
                        }
                    }
                }

                m_perSideReferencedFieldNames.Add(sourceSide, referencedFields);
            }

            return m_perSideReferencedFieldNames[sourceSide];
        }
    }
}
