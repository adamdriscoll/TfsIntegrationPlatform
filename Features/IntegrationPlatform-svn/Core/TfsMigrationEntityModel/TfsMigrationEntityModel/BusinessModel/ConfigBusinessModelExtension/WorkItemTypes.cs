// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.BusinessModel.WIT
{
    /// <summary>
    /// Class WorkItemTypes
    /// </summary>
    public partial class WorkItemTypes
    {
        /// <summary>
        /// get the mapped work item type name
        /// </summary>
        /// <param name="sourceSide"></param>
        /// <param name="sourceWorkItemType"></param>
        /// <param name="mappingEntry"></param>
        /// <returns>the mapped work item type; the sourceType if wildcard character
        /// "*" is used; NULL if none is applicable</returns>
        /// <remarks>
        /// WorkItemTypes element must have at least one entry (WorkItemType).
        /// The following line shows where the wildcard characters can be used:
        /// &lt;WorkItemType LeftWorkItemTypeName="*" RightWorkItemTypeName="*" fieldMap="@@ALL@@" /&gt;
        /// - "*" means ANY in above context.
        /// - "@@ALL@@" means "map all fields" in above context.
        /// </remarks>
        public string GetMappedType(
            SourceSideTypeEnum sourceSide, 
            string sourceWorkItemType,
            WorkItemTypeMappingElement mappingEntry)
        {
            if (null == mappingEntry)
            {
                throw new ArgumentNullException();
            }

            string retVal = (sourceSide == SourceSideTypeEnum.Left)
                            ? mappingEntry.RightWorkItemTypeName 
                            : mappingEntry.LeftWorkItemTypeName;

            if (retVal.Equals(WitMappingConfigVocab.Any, System.StringComparison.OrdinalIgnoreCase))
            {
                retVal = sourceWorkItemType;
            }
            
            return retVal;
        }

        /// <summary>
        /// get the work item type mapping entry
        /// </summary>
        /// <param name="sourceSide"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public WorkItemTypeMappingElement GetMappingEntry(SourceSideTypeEnum sourceSide, string sourceType)
        {
            AddMissingDefaultMap();
                
            WorkItemTypeMappingElement retVal = null;

            foreach (WorkItemTypeMappingElement map in this.WorkItemType)
            {
                switch (sourceSide)
                {
                    case SourceSideTypeEnum.Left:
                        if (map.LeftWorkItemTypeName == sourceType)
                        {
                            return map;
                        }
                        else if (map.LeftWorkItemTypeName.Equals(WitMappingConfigVocab.Any, System.StringComparison.OrdinalIgnoreCase))
                        {
                            retVal = map;
                        }
                        break;
                    case SourceSideTypeEnum.Right:
                        if (map.RightWorkItemTypeName == sourceType)
                        {
                            return map;
                        }
                        else if (map.RightWorkItemTypeName.Equals(WitMappingConfigVocab.Any, System.StringComparison.OrdinalIgnoreCase))
                        {
                            retVal = map;
                        }
                        break;
                }
            }

            return retVal;
        }

        private void AddMissingDefaultMap()
        {
            // Note:
            // The old version of the WIT custom setting schema allows the <WorkItemTypes>
            // tag to have no real setting. And the semantics of it is equivalent to having
            // the following setting:
            //  <WorkItemType LeftWorkItemTypeName="*" RightWorkItemTypeName="*" fieldMap="@@ALL@@" />
            if (WorkItemType.Count == 0)
            {
                WorkItemTypeMappingElement witMappingElem = new WorkItemTypeMappingElement();
                witMappingElem.LeftWorkItemTypeName = WitMappingConfigVocab.Any;
                witMappingElem.RightWorkItemTypeName = WitMappingConfigVocab.Any;
                witMappingElem.fieldMap = WitMappingConfigVocab.All;
                WorkItemType.Add(witMappingElem);
            }
        }
    }
}
