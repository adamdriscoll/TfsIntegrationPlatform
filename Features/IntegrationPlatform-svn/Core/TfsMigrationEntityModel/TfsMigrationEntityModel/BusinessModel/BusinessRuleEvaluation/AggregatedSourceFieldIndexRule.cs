// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel.WIT;

namespace Microsoft.TeamFoundation.Migration.BusinessModel.BusinessRuleEvaluation
{
    class AggregatedSourceFieldIndexResultItem : EvaluationResultItem
    {
        Dictionary<string, List<string>> m_perFldMapInvalidAggrTargetFld = new Dictionary<string, List<string>>();
        const string Header = "Business rule evaluation of WIT aggregated source field index: {0}\n";

        internal override void Print(StringBuilder sb)
        {
            if (Passed)
            {
                sb.AppendFormat(Header, "Passed");
            }
            else
            {
                sb.AppendFormat(Header, "Failed");
                foreach (var fieldMap in m_perFldMapInvalidAggrTargetFld)
                {
                    sb.AppendLine(Indent + string.Format("Field Map: {0}", fieldMap.Key));

                    sb.AppendLine(Indent + "Invalid Aggregation Group's target field: ");
                    foreach (string field in fieldMap.Value)
                    {
                        sb.AppendFormat("'{0}' ", field);
                    }
                    sb.AppendLine();
                    sb.AppendLine();
                }
                sb.AppendLine();
            }
        }

        internal void AddInvalidAggregationGroup(FieldMap fieldMap, FieldsAggregationGroup aggregationGroup)
        {
            if (!m_perFldMapInvalidAggrTargetFld.ContainsKey(fieldMap.name))
            {
                m_perFldMapInvalidAggrTargetFld.Add(fieldMap.name, new List<string>());
            }

            if (!m_perFldMapInvalidAggrTargetFld[fieldMap.name].Contains(aggregationGroup.TargetFieldName))
            {
                m_perFldMapInvalidAggrTargetFld[fieldMap.name].Add(aggregationGroup.TargetFieldName);
            }
        }
    }

    class AggregatedSourceFieldIndexRule : IEvaluationRule
    {
        #region IEvaluationRule Members

        public EvaluationResultItem Evaluate(Configuration configuration)
        {
            AggregatedSourceFieldIndexResultItem resultItem = new AggregatedSourceFieldIndexResultItem();
            resultItem.Passed = true;

            foreach (Session session in configuration.SessionGroup.Sessions.Session)
            {
                string settingXml = BusinessModelManager.GenericSettingXmlToString(session.CustomSettings.SettingXml);
                if (!string.IsNullOrEmpty(settingXml))
                {
                    if (session.SessionType != SessionTypeEnum.WorkItemTracking)
                    {
                        continue;
                    }

                    foreach (FieldMap fieldMap in session.WITCustomSetting.FieldMaps.FieldMap)
                    {
                        foreach (FieldsAggregationGroup aggregationGroup in fieldMap.AggregatedFields.FieldsAggregationGroup)
                        {
                            if (!IsSourceFieldIndexValid(aggregationGroup))
                            {
                                resultItem.AddInvalidAggregationGroup(fieldMap, aggregationGroup);
                                resultItem.Passed = false;
                            }
                        }
                    }
                }
            }

            return resultItem;
        }

        private bool IsSourceFieldIndexValid(FieldsAggregationGroup aggregationGroup)
        {
            List<int> usedIndex = new List<int>(aggregationGroup.SourceField.Count);

            foreach (var srcFld in aggregationGroup.SourceField)
            {
                if (usedIndex.Contains(srcFld.Index))
                {
                    return false;
                }

                usedIndex.Add(srcFld.Index);
            }
            
            return true;
        }

        #endregion
    }
}
