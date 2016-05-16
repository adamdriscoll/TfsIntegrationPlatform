// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Xml;
using System.Collections.Generic;
using System.Diagnostics;
namespace Microsoft.TeamFoundation.Migration.BusinessModel.WIT
{
    /// <summary>
    /// ValueMap class
    /// </summary>
    public partial class ValueMap
    {
        ///// <summary>
        ///// Get a mapped value given a migration source unique Id and the value to be mapped.
        ///// </summary>
        ///// <param name="sourceSide"></param>
        ///// <param name="sourceValue"></param>
        ///// <returns></returns>
        ///// <remarks>
        ///// The following evaluation precedence is honored:
        /////     map v1 => v2, when F.value = f
        /////     map *  => v2, when F.value = f
        /////     map v1 => v2, when F.value = *
        /////     map *  => v2, when F.value = *
        /////     map v1 => v2
        /////     map *  => v2
        ///// </remarks>


        /// <summary>
        /// Try getting a mapped value given a migration source unique Id and the value to be mapped.
        /// </summary>
        /// <param name="sourceSide">Left or Right of the migration source in the session configuration</param>
        /// <param name="sourceValue">Value before the mapping rule is applied</param>
        /// <param name="descriptionDocRootNode">The document that contains all the field information in a particular revision</param>
        /// <param name="mappedValue">Value after the mapping rule is applied</param>
        /// <returns>True if a mapping rule is applied; FALSE otherwise</returns>
        /// <remarks>
        /// The following evaluation precedence is honored:
        ///     map v1 => v2, when F.value = f
        ///     map *  => v2, when F.value = f
        ///     map v1 => v2, when F.value = *
        ///     map *  => v2, when F.value = *
        ///     map v1 => v2
        ///     map *  => v2
        /// </remarks>
        public bool TryGetMappedValue(
            SourceSideTypeEnum sourceSide, 
            string sourceValue, 
            XmlElement descriptionDocRootNode,
            out string mappedValue)
        {
            mappedValue = sourceValue;

            #region find applicable rules
            List<Value> conditionalMaps = new List<Value>();
            Value explicitMap = null;
            Value wildCardMap = null;
            foreach (Value valuePair in this.Value)
            {
                if (sourceValue == (sourceSide == SourceSideTypeEnum.Left ? valuePair.LeftValue : valuePair.RightValue))
                {
                    if (null == valuePair.When
                        || (string.IsNullOrEmpty(valuePair.When.ConditionalSrcFieldName) && string.IsNullOrEmpty(valuePair.When.ConditionalSrcFieldValue)))
                    {
                        if (null == explicitMap) explicitMap = valuePair;
                        else Trace.TraceWarning("There are multiple value maps mapping from the same source value '{0}'.", sourceValue ?? string.Empty);
                    }
                    else
                    {
                        conditionalMaps.Add(valuePair);
                    }
                }
                else if (WitMappingConfigVocab.Any == (sourceSide == SourceSideTypeEnum.Left ? valuePair.LeftValue : valuePair.RightValue))
                {
                    if (null == valuePair.When
                        || (string.IsNullOrEmpty(valuePair.When.ConditionalSrcFieldName) && string.IsNullOrEmpty(valuePair.When.ConditionalSrcFieldValue)))
                    {
                        if (null == wildCardMap) wildCardMap = valuePair;
                        else Trace.TraceWarning("There are multiple wildcard-character entries in the value map '{0}'.", this.name ?? string.Empty);
                    }
                    else
                    {
                        conditionalMaps.Add(valuePair);
                    }
                }
            } 
            #endregion

            #region try applying rules
            bool mappingRuleApplied = TryConditionalMap(sourceSide, conditionalMaps, descriptionDocRootNode, ref mappedValue);
            if (!mappingRuleApplied)
            {                
                if (null != explicitMap)
                {
                    mappedValue = (sourceSide == SourceSideTypeEnum.Left ? explicitMap.RightValue : explicitMap.LeftValue);
                    mappingRuleApplied = true;
                }
                else if (null != wildCardMap)
                {
                    mappedValue = (sourceSide == SourceSideTypeEnum.Left ? wildCardMap.RightValue : wildCardMap.LeftValue);
                    mappingRuleApplied = true;
                }
            }
            #endregion

            if (mappingRuleApplied && mappedValue.Equals(WitMappingConfigVocab.Any))
            {
                // mapping * to * semantically means copy source value to target
                mappedValue = sourceValue;
            }
            return mappingRuleApplied;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceSide"></param>
        /// <param name="conditionalMaps"></param>
        /// <param name="descriptionDocRootNode"></param>
        /// <param name="mappedValue"></param>
        /// <returns>True if a mapping rule is applied; FALSE otherwise</returns>
        /// <remarks>
        /// The following evaluation precedence is honored:
        ///     map v1 => v2, when F.value = f
        ///     map *  => v2, when F.value = f
        ///     map v1 => v2, when F.value = *
        ///     map *  => v2, when F.value = *
        /// </remarks>
        private bool TryConditionalMap(
            SourceSideTypeEnum sourceSide, 
            List<Value> conditionalMaps, 
            XmlElement descriptionDocRootNode, 
            ref string mappedValue)
        {
            Value explicitCondExplicitValue = null;
            Value explicteCondWildcardValue = null;
            Value wildcardCondExplicitValue = null;
            Value wildcardCondWildcardValue = null;

            foreach (Value vMap in conditionalMaps)
            {
                string srcFldRefNameInCond = vMap.When.ConditionalSrcFieldName;
                string srcFldValueInCond = vMap.When.ConditionalSrcFieldValue;

                XmlNode srcFldColNode = descriptionDocRootNode.SelectSingleNode(
                    string.Format("/WorkItemChanges/Columns/Column[@ReferenceName='{0}']", srcFldRefNameInCond));

                if (null != srcFldColNode)
                {
                    if (srcFldValueInCond.Equals(WitMappingConfigVocab.Any))
                    {
                        if (WitMappingConfigVocab.Any == (sourceSide == SourceSideTypeEnum.Left ? vMap.LeftValue : vMap.RightValue))
                        {
                            // map *  => v2, when F.value = *
                            wildcardCondWildcardValue = vMap;
                        }
                        else
                        {
                            // map v1 => v2, when F.value = *
                            wildcardCondExplicitValue = vMap;
                        }
                    }
                    else if (srcFldValueInCond.Equals(srcFldColNode.FirstChild.InnerText))
                    {
                        if (WitMappingConfigVocab.Any == (sourceSide == SourceSideTypeEnum.Left ? vMap.LeftValue : vMap.RightValue))
                        {
                            // map *  => v2, when F.value = f
                            explicteCondWildcardValue = vMap;
                        }
                        else
                        {
                            // map v1 => v2, when F.value = f
                            explicitCondExplicitValue = vMap;
                            break;
                        }
                    }
                }
            }

            bool retVal = false;
            if (explicitCondExplicitValue != null )
            {
                mappedValue = ApplyValueMap(explicitCondExplicitValue, sourceSide);
                retVal = true;
            }
            else if (explicteCondWildcardValue != null)
            {
                mappedValue = ApplyValueMap(explicteCondWildcardValue, sourceSide);
                retVal = true;
            }
            else if (wildcardCondExplicitValue != null)
            {
                mappedValue = ApplyValueMap(wildcardCondExplicitValue, sourceSide);
                retVal = true;
            }
            else if (wildcardCondWildcardValue != null)
            {
                mappedValue = ApplyValueMap(wildcardCondWildcardValue, sourceSide);
                retVal = true;
            }

            return retVal;
        }

        private string ApplyValueMap(Value valueMap, SourceSideTypeEnum sourceSide)
        {
            return (sourceSide == SourceSideTypeEnum.Left ? valueMap.RightValue : valueMap.LeftValue) ?? string.Empty;
        }
    }
}
