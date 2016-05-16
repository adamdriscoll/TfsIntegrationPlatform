// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace Microsoft.TeamFoundation.Migration.BusinessModel.WIT
{
    /// <summary>
    /// Class MappedFields
    /// </summary>
    public partial class MappedFields
    {
        /// <summary>
        /// Get the mapped field name from a particular side of a MappedField entry
        /// </summary>
        /// <param name="sourceSide"></param>
        /// <param name="mappedFieldEntry"></param>
        /// <returns>The name of the mapped field</returns>
        public string GetMappedFieldName(
            SourceSideTypeEnum sourceSide, 
            string sourceFieldName,
            MappedField mappedFieldEntry)
        {
            if (null == mappedFieldEntry)
            {
                throw new ArgumentNullException();
            }

            return (sourceSide == SourceSideTypeEnum.Left)
                ? (mappedFieldEntry.RightName == WitMappingConfigVocab.Any ? sourceFieldName : mappedFieldEntry.RightName)
                : (mappedFieldEntry.LeftName == WitMappingConfigVocab.Any ? sourceFieldName : mappedFieldEntry.LeftName);
        }

        /// <summary>
        /// Get the mapped field entry given a specific migration source Unique Id and source field name
        /// </summary>
        /// <param name="sourceSide"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public MappedField GetMappedFieldEntry(SourceSideTypeEnum sourceSide, string fieldName)
        {
            MappedField retVal = null;

            foreach (MappedField mappedField in this.MappedField)
            {
                if (mappedField.MapFromSide == sourceSide)
                {
                    if (fieldName == (sourceSide == SourceSideTypeEnum.Left ? mappedField.LeftName : mappedField.RightName))
                    {
                        retVal = mappedField;
                        break;
                    }
                    else
                    {
                        if (sourceSide == SourceSideTypeEnum.Left 
                            && mappedField.LeftName == WitMappingConfigVocab.Any 
                            && retVal == null)
                        {
                            retVal = mappedField;
                        }
                        else if (sourceSide == SourceSideTypeEnum.Right 
                            && mappedField.RightName == WitMappingConfigVocab.Any
                            && retVal == null)
                        {
                            retVal = mappedField;
                        }
                        continue;
                    }
                }
            }

            return retVal;
        }

        /// <summary>
        /// Get all the "missing" field entries given a specific migration source Unique Id
        /// </summary>
        /// <param name="sourceSide"></param>
        /// <returns></returns>
        public ReadOnlyCollection<MappedField> GetMissingFieldEntries(SourceSideTypeEnum sourceSide)
        {
            List<MappedField> fields = new List<MappedField>();

            foreach (MappedField mappedField in this.MappedField)
            {
                string mapFromFieldName = (sourceSide == SourceSideTypeEnum.Left ? mappedField.LeftName : mappedField.RightName);
                if (mappedField.MapFromSide == sourceSide
                    && WitMappingConfigVocab.MissingField.Equals(mapFromFieldName, System.StringComparison.OrdinalIgnoreCase))
                {
                    fields.Add(mappedField);
                }
            }

            return fields.AsReadOnly();
        }
    }
}
