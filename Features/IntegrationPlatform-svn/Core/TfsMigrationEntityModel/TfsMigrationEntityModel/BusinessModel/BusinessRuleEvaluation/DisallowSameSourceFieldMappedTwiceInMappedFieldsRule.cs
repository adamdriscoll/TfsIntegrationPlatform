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
    class DisallowSameSourceFieldMappedTwiceInMappedFieldsResultItem : EvaluationResultItem
    {
        Dictionary<FieldMap, List<string>> m_perFieldMapCollidedFields = new Dictionary<FieldMap, List<string>>();
        const string Header = "Business rule evaluation of no multiple WIT source fields mapped in MappedField: {0}\n";

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

                    sb.AppendLine(Indent + "Conflicted source field(s): ");
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

    class DisallowSameSourceFieldMappedTwiceInMappedFieldsRule : IEvaluationRule
    {
        Dictionary<SourceSideTypeEnum, List<string>> m_perDirectionSourceMappedFields
            = new Dictionary<SourceSideTypeEnum,List<string>>();

        public EvaluationResultItem Evaluate(Configuration configuration)
        {
            DisallowSameSourceFieldMappedTwiceInMappedFieldsResultItem resultItem =
                new DisallowSameSourceFieldMappedTwiceInMappedFieldsResultItem();
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
                            if (IsSourceFieldMapped(mappedField, out collidedField))
                            {
                                resultItem.AddCollidedField(fieldMap, collidedField);
                            }

                            UpdateMappedFieldCache(mappedField.MapFromSide, mappedField.LeftName, mappedField.RightName);
                        }
                    }
                }
            }

            return resultItem;
        }

        private void UpdateMappedFieldCache(SourceSideTypeEnum sourceSideTypeEnum, string leftName, string rightName)
        {
            switch (sourceSideTypeEnum)
            {
                case SourceSideTypeEnum.Left:
                    if (!m_perDirectionSourceMappedFields[sourceSideTypeEnum].Contains(leftName))
                    {
                        m_perDirectionSourceMappedFields[sourceSideTypeEnum].Add(leftName);
                    }
                    break;
                case SourceSideTypeEnum.Right:
                    if (!m_perDirectionSourceMappedFields[sourceSideTypeEnum].Contains(rightName))
                    {
                        m_perDirectionSourceMappedFields[sourceSideTypeEnum].Add(rightName);
                    }
                    break;
                case SourceSideTypeEnum.Any:
                    if (!m_perDirectionSourceMappedFields[SourceSideTypeEnum.Left].Contains(rightName))
                    {
                        m_perDirectionSourceMappedFields[SourceSideTypeEnum.Left].Add(rightName);
                    }
                    if (!m_perDirectionSourceMappedFields[SourceSideTypeEnum.Right].Contains(leftName))
                    {
                        m_perDirectionSourceMappedFields[SourceSideTypeEnum.Right].Add(leftName);
                    }
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        private bool IsSourceFieldMapped(MappedField mappedField, out string collidedField)
        {
            collidedField = string.Empty;

            bool retVal = false;
            switch (mappedField.MapFromSide)
            {
                case SourceSideTypeEnum.Left:
                    retVal = IsSourceFieldMapped(mappedField.MapFromSide, mappedField.LeftName);
                    if (retVal)
                    {
                        collidedField = mappedField.LeftName;
                    }
                    break;
                case SourceSideTypeEnum.Right:
                    retVal = IsSourceFieldMapped(mappedField.MapFromSide, mappedField.RightName);
                    if (retVal)
                    {
                        collidedField = mappedField.RightName;
                    }
                    break;
                case SourceSideTypeEnum.Any:
                    bool rNameRslt = IsSourceFieldMapped(mappedField.MapFromSide, mappedField.RightName);
                    if (rNameRslt)
                    {
                        collidedField = mappedField.RightName;
                    }
                    bool lNameRslt = IsSourceFieldMapped(mappedField.MapFromSide, mappedField.LeftName);
                    if (lNameRslt)
                    {
                        collidedField = mappedField.LeftName;
                    }

                    retVal = (lNameRslt || rNameRslt);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            return retVal;
        }

        private bool IsSourceFieldMapped(SourceSideTypeEnum mapFromSide, string sourceField)
        {
            if (string.IsNullOrEmpty(sourceField))
            {
                // if source field is string.Empty, it is allowed but won't be applied to any field
                return false;
            }

            if (IsFieldInCachedSourceFields(mapFromSide, sourceField))
            {
                return true;
            }

            if (mapFromSide == SourceSideTypeEnum.Any)
            {
                if (IsFieldInCachedSourceFields(SourceSideTypeEnum.Left, sourceField)
                    || IsFieldInCachedSourceFields(SourceSideTypeEnum.Right, sourceField))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsFieldInCachedSourceFields(SourceSideTypeEnum mapFromSide, string targetField)
        {
            if (m_perDirectionSourceMappedFields.ContainsKey(mapFromSide)
                && m_perDirectionSourceMappedFields[mapFromSide].Contains(targetField))
            {
                return true;
            }

            return false;
        }

        private void Reset()
        {
            m_perDirectionSourceMappedFields.Clear();
            m_perDirectionSourceMappedFields.Add(SourceSideTypeEnum.Left, new List<string>());
            m_perDirectionSourceMappedFields.Add(SourceSideTypeEnum.Right, new List<string>());
        }
    }
}
