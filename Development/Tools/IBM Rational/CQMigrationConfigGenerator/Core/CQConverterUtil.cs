// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// Description: Helper Class to porvide generic method implementations
// in form of all static methods

#region Using directives

using System;
using System.Collections.Generic;
using System.Text;
using ClearQuestOleServer;
using System.Diagnostics;
using Microsoft.TeamFoundation.Converters.Utility;
using Microsoft.TeamFoundation.Converters.Reporting;
using System.IO;
using System.Xml;
using Microsoft.TeamFoundation.Converters.WorkItemTracking.Common;

#endregion

namespace Microsoft.TeamFoundation.Converters.WorkItemTracking.CQ
{
    static class CQConverterUtil
    {
        /// <summary>
        /// This mehtod returns a list of entity definition names that are referenced (to any level)
        /// with the baseEntityDef.
        /// </summary>
        /// <param name="cqSession">A valid Clear Quest Session object</param>
        /// <param name="baseEntityDefName">
        /// Entity Definition name to start with. 
        /// This entity has to be a submit type entity
        /// </param>
        /// <returns>
        /// List of (non system) entity definition names that are connected (referenced) with the
        /// entity name passed as parameter. Also includes the starting entity definition name.
        /// </returns>
        public static string[] GetReferencedEntityDefNames(Session cqSession, string baseEntityDefName, string configFile)
        {
            Logger.EnteredMethod(LogSource.CQ, cqSession, baseEntityDefName);

            // fetch the submit type entities from CQ DB
            object[] allSubmitEntities = (object[])CQWrapper.GetSubmitEntityDefNames(cqSession);

            List<string> allSubmitEntitiesList = new List<string>();
            foreach (object obEntity in allSubmitEntities)
            {
                allSubmitEntitiesList.Add((string)obEntity);
            }

            // ensure that the passed entity is part of Submit Entity Types
            if (!allSubmitEntitiesList.Contains(baseEntityDefName))
            {
                // log the problem
                string errMsg = UtilityMethods.Format(CQResource.CQ_NOT_SUBMIT_ENTITY, 
                                                      CurConResource.Analysis);
                Logger.Write(LogSource.CQ, TraceLevel.Error, errMsg);
                Microsoft.TeamFoundation.Converters.WorkItemTracking.Common.ConverterMain.MigrationReport.WriteIssue(String.Empty,
                         errMsg, string.Empty /* no item */, null, IssueGroup.Witd.ToString(), ReportIssueType.Critical);

                throw new ConverterException(errMsg);
            }

            // create a list for storing all entities name
            List<string> refEntities = new List<string>();

            // add the base entity name to start with
            refEntities.Add(baseEntityDefName);

            int noOfEntitesDone = 0;

            while (noOfEntitesDone < refEntities.Count)
            {
                OAdEntityDef cqEntityDef = CQWrapper.GetEntityDef(cqSession, refEntities[noOfEntitesDone]);

                // we processed one entity
                noOfEntitesDone++;

                // process all the fields and find out the names for other entities
                object[] cqFields = (object[])cqEntityDef.GetFieldDefNames();
                if (cqFields.Length > 0)
                {
                    foreach (object ob in cqFields)
                    {
                        string cqFldName = ob as String;
                        Debug.Assert(cqFldName != null);
                        // get field type
                        int fieldType = CQWrapper.GetFieldDefType(cqEntityDef, cqFldName);
                        bool isSystem = cqEntityDef.IsSystemOwnedFieldDefName(cqFldName);

                        // count the field only if it is not internal to CQ and is a reference field
                        if ((fieldType == CQConstants.FIELD_REFERENCE || fieldType == CQConstants.FIELD_REFERENCE_LIST)
                            && isSystem == false)
                        {
                            // add the referenced entity in the list (if it is not already there)
                            OAdEntityDef refEntity = CQWrapper.GetFieldReferenceEntityDef(cqEntityDef, cqFldName);

                            // the scanned entities should also be ther submit type entities only
                            if ((allSubmitEntitiesList.Contains(refEntity.GetName())) == true
                                    && refEntities.Contains(refEntity.GetName()) == false)
                            {
                                refEntities.Add(refEntity.GetName());
                            }
                        }
                    }   //foreach (object ob in cqFields)
                } //if (cqFields.Length > 0)
            } //while (noOfEntitesDone != refEntities.Count)

            Logger.ExitingMethod(LogSource.CQ);
            return refEntities.ToArray();
        }

        // setting output directory for generating files in specified dir
        private static string OutputDirectory;
        public static void SetOutputDirectory(string dirName, string configFile)
        {
            try
            {
                UtilityMethods.CreateDirectory(dirName);
            }
            catch (ConverterException exception)
            {
                // unable to create directory
                string errMsg = UtilityMethods.Format(CQResource.CQ_OUTDIR_CREATION_FAILED, dirName, configFile, exception.Message);
                throw new ConverterException(errMsg);
            }
            OutputDirectory = dirName;
        }

        public static string GetFileNameWithPath(string fileName)
        {
            if (OutputDirectory != null)
            {
                return Path.Combine(OutputDirectory, fileName);
            }
            else
            {
                return fileName;
            }
        }

        /// <summary>
        /// Validates if the input string is a valid string if put in xml file
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static string ValidateXmlString(string value)
        {
            string modified = value.Replace("--", "- -");
            return modified;
        }

     

        /// <summary>
        /// Converts given time to UTC time
        /// </summary>
        /// <param name="localTime">Local time</param>
        /// <returns>Converter time in UTC</returns>
        internal static DateTime ConvertLocalToUTC(DateTime localTime)
        {
            return TimeZone.CurrentTimeZone.ToUniversalTime(localTime);
        }

    } // end of class CQConverterUtil

}
