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
    class NoMultipleMapsOnSameTargetFieldResultItem : EvaluationResultItem
    {
        Dictionary<FieldMap, List<string>> m_perFieldMapCollidedFields = new Dictionary<FieldMap, List<string>>();
        const string Header = "Business rule evaluation of no multiple WIT field maps on the same target field: {0}\n";

        internal override void Print(StringBuilder sb)
        {
            if (Passed)
            {
                sb.AppendFormat(Header, "Passed");
            }
            else
            {
                sb.AppendFormat(Header, "Failed");
                foreach (var fieldMap in m_perFieldMapCollidedFields)
                {
                    sb.AppendLine(Indent + string.Format("Field Map: {0}", fieldMap.Key.name));
                    
                    sb.AppendLine(Indent + "Conflicted target field(s): ");
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

        internal void AddCollidedField(FieldMap fieldMap, string collidedField)
        {
            if (!m_perFieldMapCollidedFields.ContainsKey(fieldMap))
            {
                m_perFieldMapCollidedFields.Add(fieldMap, new List<string>());
            }

            if (!m_perFieldMapCollidedFields[fieldMap].Contains(collidedField))
            {
                m_perFieldMapCollidedFields[fieldMap].Add(collidedField);
            }

            Passed = false;
        }
    }

    class NoMultipleMapsOnSameTargetFieldRule : IEvaluationRule
    {
        Dictionary<SourceSideTypeEnum, List<string>> m_perDirectionTargetField;
        
        public NoMultipleMapsOnSameTargetFieldRule()
        {
            m_perDirectionTargetField = new Dictionary<SourceSideTypeEnum, List<string>>();
        }

        #region IEvaluationRule Members

        public EvaluationResultItem Evaluate(Configuration configuration)
        {
            NoMultipleMapsOnSameTargetFieldResultItem resultItem = new NoMultipleMapsOnSameTargetFieldResultItem();
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
                        Reset();

                        foreach (MappedField mappedField in fieldMap.MappedFields.MappedField)
                        {
                            string collidedField;
                            if (IsTargetFieldMapped(mappedField, out collidedField))
                            {
                                resultItem.AddCollidedField(fieldMap, collidedField);
                            }

                            UpdateTargetFieldCache(mappedField.MapFromSide, mappedField.LeftName, mappedField.RightName);
                        }

                        foreach (FieldsAggregationGroup aggregationGroup in fieldMap.AggregatedFields.FieldsAggregationGroup)
                        {
                            if (IsTargetFieldMapped(aggregationGroup))
                            {
                                resultItem.AddCollidedField(fieldMap, aggregationGroup.TargetFieldName);
                            }

                            UpdateTargetFieldCache(aggregationGroup.MapFromSide, aggregationGroup.TargetFieldName, aggregationGroup.TargetFieldName);
                        }
                    }
                }
            }

            return resultItem;
        }

        #endregion

        private void Reset()
        {
            m_perDirectionTargetField.Clear();
            m_perDirectionTargetField.Add(SourceSideTypeEnum.Left, new List<string>());
            m_perDirectionTargetField.Add(SourceSideTypeEnum.Right, new List<string>());
        }

        private bool IsTargetFieldMapped(FieldsAggregationGroup aggregationGroup)
        {
            switch (aggregationGroup.MapFromSide)
            {
                case SourceSideTypeEnum.Left:
                case SourceSideTypeEnum.Right:
                    return IsTargetFieldMapped(aggregationGroup.MapFromSide, aggregationGroup.TargetFieldName);
                case SourceSideTypeEnum.Any:
                    return IsTargetFieldMapped(SourceSideTypeEnum.Left, aggregationGroup.TargetFieldName)
                        || IsTargetFieldMapped(SourceSideTypeEnum.Right, aggregationGroup.TargetFieldName);
                default:
                    throw new InvalidOperationException();
            }
        }

        private void UpdateTargetFieldCache(SourceSideTypeEnum sourceSideTypeEnum, string leftName, string rightName)
        {
            switch (sourceSideTypeEnum)
            {
                case SourceSideTypeEnum.Left:
                    if (!m_perDirectionTargetField[sourceSideTypeEnum].Contains(rightName))
                    {
                        m_perDirectionTargetField[sourceSideTypeEnum].Add(rightName);
                    }
                    break;
                case SourceSideTypeEnum.Right:
                    if (!m_perDirectionTargetField[sourceSideTypeEnum].Contains(leftName))
                    {
                        m_perDirectionTargetField[sourceSideTypeEnum].Add(leftName);
                    }
                    break;
                case SourceSideTypeEnum.Any:
                    if (!m_perDirectionTargetField[SourceSideTypeEnum.Left].Contains(rightName))
                    {
                        m_perDirectionTargetField[SourceSideTypeEnum.Left].Add(rightName);
                    }
                    if (!m_perDirectionTargetField[SourceSideTypeEnum.Right].Contains(leftName))
                    {
                        m_perDirectionTargetField[SourceSideTypeEnum.Right].Add(leftName);
                    }
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        private bool IsTargetFieldMapped(MappedField mappedField, out string collidedTargetField)
        {
            collidedTargetField = string.Empty;

            bool retVal = false;
            switch (mappedField.MapFromSide)
            {
                case SourceSideTypeEnum.Left:
                    retVal = IsTargetFieldMapped(mappedField.MapFromSide, mappedField.RightName);
                    if (retVal)
                    {
                        collidedTargetField = mappedField.RightName;
                    }
                    break;
                case SourceSideTypeEnum.Right:
                    retVal = IsTargetFieldMapped(mappedField.MapFromSide, mappedField.LeftName);
                    if (retVal)
                    {
                        collidedTargetField = mappedField.LeftName;
                    }
                    break;
                case SourceSideTypeEnum.Any:
                    bool rNameRslt = IsTargetFieldMapped(mappedField.MapFromSide, mappedField.RightName);
                    if (rNameRslt)
                    {
                        collidedTargetField = mappedField.RightName;
                    }
                    bool lNameRslt = IsTargetFieldMapped(mappedField.MapFromSide, mappedField.LeftName);
                    if (lNameRslt)
                    {
                        collidedTargetField = mappedField.LeftName;
                    }

                    retVal = (lNameRslt || rNameRslt);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            return retVal;
        }

        private bool IsTargetFieldMapped(SourceSideTypeEnum mapFromSide, string targetField)
        {
            if (string.IsNullOrEmpty(targetField))
            {
                // if target field is string.Empty, we will drop the source field after translation
                // thus, it is legal that there are multiple source field to string.Empty in the config
                return false;
            }

            if (IsFieldInCachedTargetField(mapFromSide, targetField))
            {
                return true;
            }

            if (mapFromSide == SourceSideTypeEnum.Any)
            {
                if (IsFieldInCachedTargetField(SourceSideTypeEnum.Left, targetField)
                    || IsFieldInCachedTargetField(SourceSideTypeEnum.Right, targetField))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsFieldInCachedTargetField(SourceSideTypeEnum mapFromSide, string targetField)
        {
            if (m_perDirectionTargetField.ContainsKey(mapFromSide) 
                && m_perDirectionTargetField[mapFromSide].Contains(targetField))
            {
                return true;
            }

            return false;
        }
    }
}
