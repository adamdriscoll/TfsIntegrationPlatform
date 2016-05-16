// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// Description: Helper Class to support Schema Map XML file Read/Write

using System;
using System.Xml;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.TeamFoundation.Converters.Utility;
using System.IO;
using System.Xml.Serialization;
using System.Security;

namespace Microsoft.TeamFoundation.Converters.WorkItemTracking.Common
{
    /// <summary>
    /// Helper class for WITSchemaMappings
    /// </summary>
    public class WITSchemaMappings
    {
        private Mappings mappings;

        public WITSchemaMappings()
        {
            mappings = new Mappings();
        }

        public Mappings Mappings
        {
            get { return mappings; }
        }

        public static Mappings CreateFromFile(string fileName)
        {
            UtilityMethods.ValidateXmlFile(fileName, "WorkItemSchemaMap.xsd");
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Mappings));
                using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    return (Mappings)serializer.Deserialize(stream);
                }
            }
            catch (InvalidOperationException ex)
            {
                throw new ConverterException(UtilityMethods.Format(CurConResource.InvalidSchemaMap, fileName),
                                             ex);
            }
            catch (IOException ioEx)
            {
                throw new ConverterException(ioEx.Message);
            }
        }

        /// <summary>
        ///  Creates the Schema Map XML file
        /// </summary>
        /// <param name="schemaMapFile">Schema file name</param>
        public void GenerateSchemaMappings(string schemaMapFile, string userMapFile)
        {
            // this method will move all the objects from collection to Array
            // and same will be done for all the subobjects
            mappings.ReadyForDump();

            // set the User Map file name..
            // default is UserMap.xml
            mappings.UserMap = new MappingsUserMap();
            mappings.UserMap.File = userMapFile;

            // write the xml file. The using statement will close the file.
            using (TextWriter tw = new StreamWriter(schemaMapFile))
            {
                XmlSerializer sr = new XmlSerializer(typeof(Mappings));

                // serialize the XML file
                Logger.WritePerf(LogSource.Common, "Begining serialization of {0}", schemaMapFile);
                sr.Serialize(tw, mappings);
                Logger.WritePerf(LogSource.Common, "Serialization of {0} done", schemaMapFile);
            }
        }
    }

    public partial class Mappings
    {
        private List<MappingsSchemaMap> schemaMappings;
        public Mappings()
        {
            schemaMappings = new List<MappingsSchemaMap>();
        }

        public void AddSchemaMap(string sourceEntity,
                                 string targetWit,
                                 string witdFile,
                                 string fieldMapFile)
        {
            Logger.EnteredMethod(LogSource.Common, sourceEntity, targetWit, witdFile, fieldMapFile);
            Debug.Assert((sourceEntity != null) && (targetWit != null) &&
                        (witdFile != null) && (fieldMapFile != null));
            if ((sourceEntity != null) && (targetWit != null) &&
                        (witdFile != null) && (fieldMapFile != null))
            {
                MappingsSchemaMap map = new MappingsSchemaMap();
                map.SourceEntity = sourceEntity;
                map.TargetWIT = targetWit;
                map.WITDFile = witdFile;
                map.FieldMapFile = fieldMapFile;
                this.schemaMappings.Add(map);
                Logger.ExitingMethod(LogSource.Common);
                return;
            }

            Logger.Write(LogSource.Common, TraceLevel.Error, "Attempt to provide NULL parameters while creation of a Schema Map");
        }

        public void ReadyForDump()
        {
            this.schemaMapsField = schemaMappings.ToArray();
        }
    }
}
