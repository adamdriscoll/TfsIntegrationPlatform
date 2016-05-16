// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// Description: Helper Class to support Field Map XML file Read/Write

using System;
using System.Xml;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.TeamFoundation.Converters.Utility;
using Microsoft.TeamFoundation.Converters.WorkItemTracking.Common;
using System.IO;
using System.Xml.Serialization;
using System.Collections;
using Microsoft.TeamFoundation.Converters.Reporting;

namespace Microsoft.TeamFoundation.Converters.WorkItemTracking.Common
{
    /// <summary>
    /// Helper class for WITFieldMappings
    /// </summary>
    public class WITFieldMappings
    {
        private FieldMaps fieldMappings;

        public static FieldMaps CreateFromFile(string fileName)
        {
            UtilityMethods.ValidateXmlFile(fileName, "WorkItemFieldMap.xsd");
            FieldMaps allFieldsMaps = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(FieldMaps));
                using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    allFieldsMaps = (FieldMaps)serializer.Deserialize(stream);
                }
            }
            catch (IOException ioEx)
            {
                throw new ConverterException(ioEx.Message);
            }
            // validate the field map that is does not contain any 1-many or many-1 map
            if (allFieldsMaps != null)
            {
                Dictionary<string, bool> srcFields = new Dictionary<string, bool>(TFStringComparer.WIConverterFieldRefName);
                Dictionary<string, bool> targetFields = new Dictionary<string, bool>(TFStringComparer.WIConverterFieldRefName);
                foreach (FieldMapsFieldMap fldMap in allFieldsMaps.FieldMap)
                {
                    if (String.IsNullOrEmpty(fldMap.from))
                    {
                        string errMsg = UtilityMethods.Format(CurConResource.NullFromField, fileName);
                        ConverterMain.MigrationReport.WriteIssue(((int)CommonErrorNumbers.NullFromField).ToString(),
                             ReportIssueType.Critical, errMsg, string.Empty, null, CurConResource.Config, null);

                        Logger.Write(LogSource.Common, TraceLevel.Error, errMsg);
                        throw new ConverterException(errMsg);
                    }
                    
                    // validate source field
                    if (!srcFields.ContainsKey(fldMap.from))
                    {
                        srcFields.Add(fldMap.from, false);
                    }
                    else
                    {
                        string errMsg = UtilityMethods.Format(CurConResource.InvalidSourceFieldMap, fldMap.from, fileName);
                        ConverterMain.MigrationReport.WriteIssue(((int)CommonErrorNumbers.InvalidSourceFieldMap).ToString(),
                             ReportIssueType.Critical, errMsg, string.Empty, null, CurConResource.Config, null);

                        Logger.Write(LogSource.Common, TraceLevel.Error, errMsg);
                        throw new ConverterException(errMsg);
                    }
                    //validate target field
                    if (String.IsNullOrEmpty(fldMap.to))
                    {
                        fldMap.to = fldMap.from;
                    }
                    
                    if (!targetFields.ContainsKey(fldMap.to))
                    {
                        targetFields.Add(fldMap.to, false);
                    }
                    else
                    {
                        string errMsg = UtilityMethods.Format(CurConResource.InvalidTargetFieldMap,
                                fldMap.to, fileName);
                        ConverterMain.MigrationReport.WriteIssue(((int)CommonErrorNumbers.InvalidTargetFieldMap).ToString(),
                             ReportIssueType.Critical, errMsg, string.Empty, null, CurConResource.Config, null);

                        throw new ConverterException(errMsg);
                    }
                }
            }

            return allFieldsMaps;
        }

        public WITFieldMappings()
        {
            fieldMappings = new FieldMaps();
        }

        public FieldMaps GetFieldMappings()
        {
            return fieldMappings;
        }

        /// <summary>
        ///  Creates the Schema Map XML file
        /// </summary>
        /// <param name="fieldMapFile">Schema file name</param>
        public void GenerateFieldMappings(string fieldMapFile)
        {
            // this method will move all the objects from collection to Array
            // and same will be done for all the subobjects
            fieldMappings.ReadyForDump();

            // write the xml file. The using statement will close the file.
            // REVIEW: What happens if fieldMapFile already exists?
            using (TextWriter tw = new StreamWriter(fieldMapFile))
            {
                XmlSerializer sr = new XmlSerializer(typeof(FieldMaps));

                // serialize the XML file
                Logger.WritePerf(LogSource.Common, "Begining serialization of {0}", fieldMapFile);
                sr.Serialize(tw, fieldMappings);
                Logger.WritePerf(LogSource.Common, "Serialization of {0} done", fieldMapFile);
            }
        }
    }

    public partial class FieldMaps
    {
        // for populating
        private List<FieldMapsFieldMap> fieldMappings;

        /// <summary>
        /// Constructor
        /// </summary>
        public FieldMaps()
        {
            fieldMappings = new List<FieldMapsFieldMap>();
        }

        public void AddFieldMap(string from,
                                 string to,
                                 bool exclude)
        {
            Logger.EnteredMethod(LogSource.Common, from, to, exclude);
            Debug.Assert(from != null);

            if (from != null)
            {
                FieldMapsFieldMap fldMap = new FieldMapsFieldMap();
                fldMap.from = from;
                if (String.IsNullOrEmpty(to))
                {
                    // set the default map
                    fldMap.to = from;
                }
                else
                {
                    fldMap.to = to;
                }

                fldMap.exclude = exclude.ToString();
                AddFieldMap(fldMap);
                Logger.ExitingMethod(LogSource.Common);
                return;
            }

            Logger.Write(LogSource.Common, TraceLevel.Error, "Attempt to provide NULL 'from' field while creation of a Field Map");
        }

        public FieldMapsFieldMap GetFieldMap(string key)
        {
            foreach (FieldMapsFieldMap fldMap in this.fieldMappings)
            {
                if (TFStringComparer.XmlAttributeValue.Equals(fldMap.from, key))
                {
                    return fldMap;
                }
            }

            return null;
        }

        //Return a fieldmap using To value
        public FieldMapsFieldMap GetFieldMapUsingTo(string key)
        {
            foreach (FieldMapsFieldMap fldMap in this.fieldMapField)
            {
                if (TFStringComparer.XmlAttributeValue.Equals(fldMap.to, key))
                {
                    return fldMap;
                }
            }

            return null;
        }


        public void AddFieldMap(FieldMapsFieldMap fldMap)
        {
            Logger.EnteredMethod(LogSource.Common, fldMap);
            Debug.Assert(fldMap != null);

            if (fldMap != null)
            {
                this.fieldMappings.Add(fldMap);
                Logger.ExitingMethod(LogSource.Common);
                return;
            }

            Logger.Write(LogSource.Common, TraceLevel.Error, "Attempt to provide NULL FieldMap while creation of a Field Map");
        }


        public void ReadyForDump()
        {
            this.fieldMapField = fieldMappings.ToArray();
        }
    } // end of class FieldMaps
}
