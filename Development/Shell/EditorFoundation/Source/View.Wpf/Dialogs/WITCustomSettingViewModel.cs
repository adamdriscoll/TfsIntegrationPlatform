// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel.WIT;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    public class WITCustomSettingViewModel
    {
        private WITSessionCustomSetting m_setting;

        public WITCustomSettingViewModel(WITSessionCustomSetting setting)
        {
            m_setting = setting;

            WorkItemTypes = new List<WorkItemTypeViewModel>();

            foreach (WorkItemTypeMappingElement type in m_setting.WorkItemTypes.WorkItemType)
            {
                WorkItemTypes.Add(new WorkItemTypeViewModel(type, setting));
            }
        }

        public List<WorkItemTypeViewModel> WorkItemTypes { get; private set; }
    }

    public class WorkItemTypeViewModel
    {
        private WorkItemTypeMappingElement m_workItemType;
        private List<FieldMapViewModelBase> m_fieldMaps = new List<FieldMapViewModelBase>();

        public WorkItemTypeViewModel(WorkItemTypeMappingElement workItemType, WITSessionCustomSetting customSettings)
        {
            m_workItemType = workItemType;
            FieldMap fieldMap = customSettings.FieldMaps.FieldMap.FirstOrDefault(x => string.Equals(x.name, m_workItemType.fieldMap));

            if (fieldMap != null)
            {
                foreach (FieldsAggregationGroup aggregatedField in fieldMap.AggregatedFields.FieldsAggregationGroup)
                {
                    FieldMapViewModelBase fieldMapViewModel = FieldMapViewModelFactory.CreateInstance(aggregatedField, customSettings);
                    if (fieldMapViewModel != null)
                    {
                        m_fieldMaps.Add(fieldMapViewModel);
                    }
                }
                foreach (MappedField mappedField in fieldMap.MappedFields.MappedField)
                {
                    FieldMapViewModelBase fieldMapViewModel = FieldMapViewModelFactory.CreateInstance(mappedField, customSettings);
                    if (fieldMapViewModel != null)
                    {
                        m_fieldMaps.Add(fieldMapViewModel);
                    }
                }
            }
        }

        public string LeftWorkItemTypeName
        {
            get
            {
                return m_workItemType.LeftWorkItemTypeName;
            }
        }

        public string RightWorkItemTypeName
        {
            get
            {
                return m_workItemType.RightWorkItemTypeName;
            }
        }

        public List<FieldMapViewModelBase> FieldMaps
        {
            get
            {
                return m_fieldMaps;
            }
        }
    }

    public static class FieldMapViewModelFactory
    {
        public static FieldMapViewModelBase CreateInstance(object obj, WITSessionCustomSetting customSettings)
        {
            if (obj is FieldsAggregationGroup)
            {
                return new AggregatedFieldMapViewModel(obj as FieldsAggregationGroup, customSettings);
            }
            else if (obj is MappedField)
            {
                return new BasicFieldMapViewModel(obj as MappedField, customSettings);
            }
            else
            {
                return null;
            }
        }
    }

    public abstract class FieldMapViewModelBase
    {
        public abstract string LeftName { get; }
        public abstract string RightName { get; }
        public abstract SourceSideTypeEnum MapFromSide { get; }

        public string MapFromSideGlyph
        {
            get
            {
                switch (MapFromSide)
                {
                    case SourceSideTypeEnum.Left:
                        return "->";
                    case SourceSideTypeEnum.Right:
                        return "<-";
                    default:
                        return "<->";
                }
            }
        }
    }

    public class BasicFieldMapViewModel : FieldMapViewModelBase
    {
        private MappedField m_field;

        public BasicFieldMapViewModel(MappedField field, WITSessionCustomSetting customSettings)
        {
            m_field = field;
            if (!string.IsNullOrEmpty(m_field.valueMap))
            {
                Values = customSettings.ValueMaps.ValueMap.FirstOrDefault(x => string.Equals(x.name, m_field.valueMap));
            }
            else
            {
                Values = null;
            }
        }

        public ValueMap Values { get; private set; }

        public override string LeftName
        {
            get
            {
                return m_field.LeftName;
            }
        }

        public override string RightName
        {
            get
            {
                return m_field.RightName;
            }
        }

        public override SourceSideTypeEnum MapFromSide
        {
            get
            {
                return m_field.MapFromSide;
            }
        }
    }

    public class AggregatedFieldMapViewModel : FieldMapViewModelBase
    {
        private FieldsAggregationGroup m_field;

        public AggregatedFieldMapViewModel(FieldsAggregationGroup field, WITSessionCustomSetting customSettings)
        {
            m_field = field;
        }

        public string Values
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine(m_field.Format);
                foreach (SourceField sourceField in m_field.SourceField)
                {
                    builder.AppendLine(sourceField.Index + " = " + sourceField.SourceFieldName);
                }
                return builder.ToString().Trim();
            }
        }

        public override string LeftName
        {
            get
            {
                if (MapFromSide == SourceSideTypeEnum.Left)
                {
                    return "Multiple";
                }
                else
                {
                    return m_field.TargetFieldName;
                }
            }
        }

        public override string RightName
        {
            get
            {
                if (MapFromSide == SourceSideTypeEnum.Right)
                {
                    return "Multiple";
                }
                else
                {
                    return m_field.TargetFieldName;
                }
            }
        }

        public override SourceSideTypeEnum MapFromSide
        {
            get
            {
                return m_field.MapFromSide;
            }
        }
    }
}
